using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Rows and columns from VRChat data: a DataList of DataDictionaries (columns = keys), a DataList of DataLists
    /// (columns = positions), a DataDictionary (key | value), or the same as JSON text.
    /// Feed it with SetData / SetJson, or point it at a variable (DataList, DataDictionary or JSON string) on any script.
    /// Optional sort (leaderboards) and rank column.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudTable : UdonSharpBehaviour
    {
        public TextMeshProUGUI header;
        public TextMeshProUGUI body;

        [Header("Columns")]
        [Tooltip("Keys to show (DataList of DataDictionaries). Empty = the keys of the first row. For lists of lists: column count.")]
        public string[] columns = { "name", "score" };
        [Tooltip("Header text per column. Empty = the column key.")]
        public string[] titles = { "Player", "Score" };
        [Tooltip("Width of each column in percent. Empty = equal widths.")]
        public float[] widths = new float[0];
        [Tooltip("Adds a # column in front.")]
        public bool showRank = true;

        [Header("Rows")]
        [Tooltip("Sort by this column key (or index for lists of lists). Empty = keep the data order.")]
        public string sortBy = "score";
        public bool descending = true;
        [Tooltip("0 = all rows.")]
        public int maxRows = 10;
        public string emptyText = "No data";
        [Tooltip("Highlights rows whose first shown cell equals this (e.g. the local player's name). {me} = your name.")]
        public string highlight = "{me}";
        public Color highlightColor = new Color(0.5f, 0.82f, 1f, 1f);

        [Header("Data")]
        [Tooltip("Starting data as JSON, e.g. [{\"name\":\"Ann\",\"score\":12}].")]
        [TextArea(2, 6)] public string json = "[{\"name\":\"Ann\",\"score\":12},{\"name\":\"Bo\",\"score\":30},{\"name\":\"Cy\",\"score\":21}]";
        [Tooltip("Script with a DataList, DataDictionary or JSON string variable to watch. Empty = only SetData/SetJson.")]
        public UdonSharpBehaviour source;
        public string variable = "";
        [Tooltip("Seconds between reads of the source.")]
        public float interval = 0.5f;

        private DataToken _data;
        private object _lastSource;
        private string _lastJson;
        private float _next;

        private void Start()
        {
            if (!string.IsNullOrEmpty(json)) SetJson(json);
            else Render();
        }

        private void Update()
        {
            if (source == null || string.IsNullOrEmpty(variable) || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + interval;
            object v = source.GetProgramVariable(variable);
            if (v == null) return;
            if (v.GetType() == typeof(string))
            {
                if ((string)v != _lastJson) SetJson((string)v);
            }
            else if (v.GetType() == typeof(DataList)) SetData(new DataToken((DataList)v)); // contents can change in place: redraw
            else if (v.GetType() == typeof(DataDictionary)) SetData(new DataToken((DataDictionary)v));
        }

        // ---------- Public API ----------

        public void SetList(DataList rows) { SetData(new DataToken(rows)); }
        public void SetDictionary(DataDictionary rows) { SetData(new DataToken(rows)); }

        public void SetData(DataToken data) { _data = data; Render(); }

        public void SetJson(string text)
        {
            _lastJson = text;
            DataToken parsed;
            if (VRCJson.TryDeserializeFromJson(text, out parsed)) SetData(parsed);
            else { _data = new DataToken(); Render(); }
        }

        /// <summary>Redraw (after changing the DataList you passed in).</summary>
        public void Render()
        {
            DataList rows = Rows();
            int cols = ColumnCount(rows);
            string[] keys = Keys(rows, cols);

            if (header != null)
            {
                string h = showRank ? "<pos=0%>#" : "";
                for (int c = 0; c < cols; c++)
                {
                    string t = c < titles.Length && !string.IsNullOrEmpty(titles[c]) ? titles[c] : keys[c];
                    h += "<pos=" + Pos(c, cols) + "%>" + t;
                }
                header.text = h;
            }
            if (body == null) return;
            if (rows == null || rows.Count == 0) { body.text = emptyText; return; }

            int[] order = Order(rows, keys);
            int n = maxRows > 0 ? Mathf.Min(maxRows, order.Length) : order.Length;
            string me = MyName();
            string mark = highlight.Replace("{me}", me);
            Color32 hc = highlightColor; // ColorUtility is not exposed to Udon
            string hex = hc.r.ToString("X2") + hc.g.ToString("X2") + hc.b.ToString("X2");
            string s = "";
            for (int r = 0; r < n; r++)
            {
                DataToken row = rows[order[r]];
                string line = showRank ? "<pos=0%>" + (r + 1) : "";
                string first = null;
                for (int c = 0; c < cols; c++)
                {
                    string cell = Cell(row, keys[c], c).ToString();
                    if (first == null) first = cell;
                    line += "<pos=" + Pos(c, cols) + "%>" + cell;
                }
                if (!string.IsNullOrEmpty(mark) && first == mark) line = "<color=#" + hex + ">" + line + "</color>";
                s += line + "\n";
            }
            body.text = s;
        }

        // ---------- Helpers ----------

        /// <summary>The data as a list of rows. A dictionary becomes [key, value] rows.</summary>
        private DataList Rows()
        {
            if (_data.TokenType == TokenType.DataList) return _data.DataList;
            if (_data.TokenType != TokenType.DataDictionary) return null;
            DataList keys = _data.DataDictionary.GetKeys();
            DataList rows = new DataList();
            for (int i = 0; i < keys.Count; i++)
            {
                DataList pair = new DataList();
                pair.Add(keys[i]);
                pair.Add(_data.DataDictionary[keys[i]]);
                rows.Add(pair);
            }
            return rows;
        }

        private int ColumnCount(DataList rows)
        {
            if (columns != null && columns.Length > 0) return columns.Length;
            if (rows == null || rows.Count == 0) return 0;
            DataToken first = rows[0];
            if (first.TokenType == TokenType.DataDictionary) return first.DataDictionary.Count;
            if (first.TokenType == TokenType.DataList) return first.DataList.Count;
            return 1;
        }

        private string[] Keys(DataList rows, int cols)
        {
            string[] keys = new string[cols];
            DataList firstKeys = null;
            if ((columns == null || columns.Length == 0) && rows != null && rows.Count > 0 && rows[0].TokenType == TokenType.DataDictionary)
                firstKeys = rows[0].DataDictionary.GetKeys();
            for (int c = 0; c < cols; c++)
            {
                if (columns != null && c < columns.Length) keys[c] = columns[c];
                else if (firstKeys != null) keys[c] = firstKeys[c].ToString();
                else keys[c] = c.ToString();
            }
            return keys;
        }

        /// <summary>Cell by key (dictionary row), by index (list row), or the row itself (plain values).</summary>
        private DataToken Cell(DataToken row, string key, int index)
        {
            DataToken v;
            if (row.TokenType == TokenType.DataDictionary)
                return row.DataDictionary.TryGetValue(key, out v) ? v : new DataToken("");
            if (row.TokenType == TokenType.DataList)
            {
                int i;
                if (!int.TryParse(key, out i)) i = index;
                return row.DataList.TryGetValue(i, out v) ? v : new DataToken("");
            }
            return index == 0 ? row : new DataToken("");
        }

        // ponytail: insertion sort, fine for leaderboard-sized data (hundreds of rows); O(n²) beyond that.
        private int[] Order(DataList rows, string[] keys)
        {
            int n = rows.Count;
            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            if (string.IsNullOrEmpty(sortBy)) return order;
            int col = 0;
            for (int c = 0; c < keys.Length; c++) if (keys[c] == sortBy) col = c;
            for (int i = 1; i < n; i++)
            {
                int cur = order[i];
                DataToken a = Cell(rows[cur], sortBy, col);
                int j = i - 1;
                while (j >= 0 && Before(a, Cell(rows[order[j]], sortBy, col)))
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = cur;
            }
            return order;
        }

        /// <summary>True when a goes above b. Numbers compare as numbers, everything else as text.</summary>
        private bool Before(DataToken a, DataToken b)
        {
            int cmp;
            if (a.IsNumber && b.IsNumber) cmp = a.Number.CompareTo(b.Number);
            else cmp = a.ToString().CompareTo(b.ToString());
            return descending ? cmp > 0 : cmp < 0;
        }

        private string Pos(int c, int cols)
        {
            float start = showRank ? 8f : 0f;
            float x = start;
            if (widths != null && widths.Length > 0)
            {
                for (int i = 0; i < c && i < widths.Length; i++) x += widths[i];
            }
            else if (cols > 0) x += (100f - start) * c / cols;
            return x.ToString("0.#");
        }

        private string MyName()
        {
            VRC.SDKBase.VRCPlayerApi p = VRC.SDKBase.Networking.LocalPlayer;
            return p != null ? p.displayName : "";
        }
    }
}
