using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace LogicCuteGuy.UIHUD
{
    /// <summary>
    /// Live text: write a template with tokens and it updates by itself.
    /// Tokens: time, time12, date, players, fps, name, uptime, height (in curly braces).
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UIHudInfo : UdonSharpBehaviour
    {
        public TextMeshProUGUI label;
        [TextArea] public string template = "{time}  ·  {players} players  ·  {fps} fps";
        [Tooltip("Seconds between updates.")]
        public float interval = 0.5f;

        private float _next;
        private int _frames;
        private float _fpsTime;
        private int _fps;

        private void Update()
        {
            _frames++;
            _fpsTime += Time.unscaledDeltaTime;
            if (_fpsTime >= 1f) { _fps = Mathf.RoundToInt(_frames / _fpsTime); _frames = 0; _fpsTime = 0f; }
            if (Time.time < _next) return;
            _next = Time.time + interval;
            Refresh();
        }

        public void Refresh()
        {
            if (label == null) return;
            string s = template;
            DateTime now = DateTime.Now;
            if (s.Contains("{time}")) s = s.Replace("{time}", now.ToString("HH:mm"));
            if (s.Contains("{time12}")) s = s.Replace("{time12}", now.ToString("h:mm tt"));
            if (s.Contains("{date}")) s = s.Replace("{date}", now.ToString("d MMM yyyy"));
            if (s.Contains("{players}")) s = s.Replace("{players}", VRCPlayerApi.GetPlayerCount().ToString());
            if (s.Contains("{fps}")) s = s.Replace("{fps}", _fps.ToString());
            if (s.Contains("{uptime}"))
            {
                int t = (int)Time.timeSinceLevelLoad;
                s = s.Replace("{uptime}", (t / 3600).ToString() + ":" + ((t / 60) % 60).ToString("00") + ":" + (t % 60).ToString("00"));
            }
            VRCPlayerApi local = Networking.LocalPlayer;
            if (s.Contains("{name}")) s = s.Replace("{name}", local != null ? local.displayName : "");
            if (s.Contains("{height}")) s = s.Replace("{height}", local != null ? local.GetAvatarEyeHeightAsMeters().ToString("0.00") + " m" : "");
            label.text = s;
        }
    }
}
