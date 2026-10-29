using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Shared data input for the pie chart and bar list: named numbers.
    /// Accepts a DataDictionary {"red": 4, "blue": 7}, a DataList of DataDictionaries (labelKey / valueKey),
    /// a DataList of [label, value] lists, a DataList of plain numbers, or the same as JSON.
    /// Feed it with SetJson / SetData / SetEntry, or point it at a variable (DataList, DataDictionary or JSON string).
    /// </summary>
    public abstract class UIHudSeries : UdonSharpBehaviour
    {
        [Header("Data")]
        [Tooltip("Starting data as JSON, e.g. {\"Red\":4,\"Blue\":7} or [{\"name\":\"Red\",\"value\":4}].")]
        [TextArea(2, 6)] public string json = "";
        [Tooltip("Script with a DataList, DataDictionary or JSON string variable to watch. Empty = only SetJson/SetData/SetEntry.")]
        public UdonSharpBehaviour source;
        public string variable = "";
        [Tooltip("Seconds between reads of the source.")]
        public float interval = 0.5f;
        [Tooltip("Key of the name in each row (lists of dictionaries).")]
        public string labelKey = "name";
        [Tooltip("Key of the number in each row (lists of dictionaries).")]
        public string valueKey = "value";
        [Tooltip("Biggest first. Off = keep the data order.")]
        public bool sortByValue = true;
        [Tooltip("{0} = the value.")]
        public string valueFormat = "{0:0}";
        public Color[] colors =
        {
            new Color(0.35f, 0.75f, 1f), new Color(1f, 0.55f, 0.3f), new Color(0.45f, 0.85f, 0.45f), new Color(0.95f, 0.4f, 0.55f),
            new Color(0.75f, 0.55f, 1f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.9f, 0.85f), new Color(0.7f, 0.7f, 0.7f),
        };

        protected string[] _labels = new string[0];
        protected float[] _values = new float[0];
        protected int _count;
        private string _lastJson;
        private float _next;

        private void Start()
        {
            if (!string.IsNullOrEmpty(json)) SetJson(json);
            else Render();
        }

        private void Update()
        {
            if (source == null || string.IsNullOrEmpty(variable) || Time.time < _next) return;
            _next = Time.time + interval;
            object v = source.GetProgramVariable(variable);
            if (v == null) return;
            if (v.GetType() == typeof(string)) { if ((string)v != _lastJson) SetJson((string)v); }
            else if (v.GetType() == typeof(DataList)) SetData(new DataToken((DataList)v)); // contents can change in place
            else if (v.GetType() == typeof(DataDictionary)) SetData(new DataToken((DataDictionary)v));
        }

        // ---------- Public API ----------

        public void SetJson(string text)
        {
            _lastJson = text;
            DataToken parsed;
            if (VRCJson.TryDeserializeFromJson(text, out parsed)) SetData(parsed);
            else { _count = 0; Render(); }
        }

        public void SetList(DataList rows) { SetData(new DataToken(rows)); }
        public void SetDictionary(DataDictionary rows) { SetData(new DataToken(rows)); }

        public void SetData(DataToken data)
        {
            _count = 0;
            if (data.TokenType == TokenType.DataDictionary)
            {
                DataList keys = data.DataDictionary.GetKeys();
                Grow(keys.Count);
                for (int i = 0; i < keys.Count; i++) Put(keys[i].ToString(), data.DataDictionary[keys[i]]);
            }
            else if (data.TokenType == TokenType.DataList)
            {
                DataList rows = data.DataList;
                Grow(rows.Count);
                for (int i = 0; i < rows.Count; i++)
                {
                    DataToken row = rows[i];
                    DataToken l, v;
                    if (row.TokenType == TokenType.DataDictionary)
                    {
                        row.DataDictionary.TryGetValue(labelKey, out l);
                        row.DataDictionary.TryGetValue(valueKey, out v);
                        Put(l.ToString(), v);
                    }
                    else if (row.TokenType == TokenType.DataList)
                    {
                        row.DataList.TryGetValue(0, out l);
                        row.DataList.TryGetValue(1, out v);
                        Put(l.ToString(), v);
                    }
                    else Put((i + 1).ToString(), row);
                }
            }
            Sort();
            Render();
        }

        /// <summary>Sets one entry by name (adds it when new). Handy for votes: SetEntry("Red", votes).</summary>
        public void SetEntry(string label, float value)
        {
            int i = 0;
            while (i < _count && _labels[i] != label) i++;
            if (i == _count) { Grow(_count + 1); _labels[i] = label; _count++; }
            _values[i] = value;
            Sort();
            Render();
        }

        public void Clear() { _count = 0; Render(); }

        /// <summary>Redraws. The chart widgets override this.</summary>
        public virtual void Render() { }

        public float Total()
        {
            float t = 0f;
            for (int i = 0; i < _count; i++) t += _values[i];
            return t;
        }

        // ---------- Helpers ----------

        protected Color ColorAt(int i) { return colors != null && colors.Length > 0 ? colors[i % colors.Length] : Color.white; }

        /// <summary>RRGGBB for TMP color tags (ColorUtility is not exposed to Udon).</summary>
        protected string Hex(Color c)
        {
            Color32 c32 = c;
            return c32.r.ToString("X2") + c32.g.ToString("X2") + c32.b.ToString("X2");
        }

        private void Grow(int n)
        {
            if (_labels.Length >= n) return;
            string[] l = new string[n];
            float[] v = new float[n];
            for (int i = 0; i < _count; i++) { l[i] = _labels[i]; v[i] = _values[i]; }
            _labels = l;
            _values = v;
        }

        private void Put(string label, DataToken value)
        {
            float f = 0f;
            if (value.IsNumber) f = (float)value.Number;
            else float.TryParse(value.ToString(), out f);
            _labels[_count] = label;
            _values[_count] = f;
            _count++;
        }

        // ponytail: insertion sort, fine for chart-sized data (a few dozen entries).
        private void Sort()
        {
            if (!sortByValue) return;
            for (int i = 1; i < _count; i++)
            {
                float v = _values[i];
                string l = _labels[i];
                int j = i - 1;
                while (j >= 0 && _values[j] < v) { _values[j + 1] = _values[j]; _labels[j + 1] = _labels[j]; j--; }
                _values[j + 1] = v;
                _labels[j + 1] = l;
            }
        }
    }
}
