// Works out the look of an overlay for its current state (running, paused, low time...) (v1.4.7).
using System;

namespace BetterClock
{
    public static class StateLooks
    {
        public static readonly string[] Props = { "Background", "BorderColor", "BorderWidth", "GlowColor", "GlowSize", "Scale" };

        // Which state settings belong to a role (null = none).
        public static string KeyFor(OverlayRole role)
        {
            switch (role)
            {
                case OverlayRole.Running: return "Running";
                case OverlayRole.Paused: return "Paused";
                case OverlayRole.Warning: return "Low";
                case OverlayRole.Danger: return "Danger";
                case OverlayRole.Finished: return "Finished";
                case OverlayRole.Overtime: return "Overtime";
                case OverlayRole.Target: return "Target";
                default: return null;
            }
        }

        public static bool NightActive(ModeStyle s, DateTime now)
        {
            if (!s.NightOn) return false;
            int h = now.Hour;
            int from = ColorUtil.Clamp(s.NightFrom, 0, 23);
            int to = ColorUtil.Clamp(s.NightTo, 0, 23);
            if (from == to) return false;
            if (from < to) return h >= from && h < to;
            return h >= from || h < to;     // for example 22 to 7 crosses midnight
        }

        // The style to draw. This is the same object when no state changes anything,
        // otherwise a copy with the state's changes put on top.
        public static ModeStyle Resolve(ModeStyle s, OverlayRole role, DateTime now, bool forceNight)
        {
            string state = KeyFor(role);
            bool night = forceNight || NightActive(s, now);
            bool useState = state != null && Changes(s, state);
            bool useNight = night && Changes(s, "Night");
            if (!useState && !useNight) return s;

            ModeStyle c = new ModeStyle();
            c.CopyFrom(s, false);
            if (useNight) Put(c, "Night");
            if (useState) Put(c, state);
            return c;
        }

        private static bool Changes(ModeStyle s, string key)
        {
            if (!string.IsNullOrEmpty((string)Reflect.Get(s, key + "Background"))) return true;
            if (!string.IsNullOrEmpty((string)Reflect.Get(s, key + "BorderColor"))) return true;
            if ((int)Reflect.Get(s, key + "BorderWidth") >= 0) return true;
            if (!string.IsNullOrEmpty((string)Reflect.Get(s, key + "GlowColor"))) return true;
            if ((int)Reflect.Get(s, key + "GlowSize") >= 0) return true;
            if ((int)Reflect.Get(s, key + "Scale") > 0) return true;
            return false;
        }

        private static void Put(ModeStyle c, string key)
        {
            string bg = (string)Reflect.Get(c, key + "Background");
            if (!string.IsNullOrEmpty(bg))
            {
                c.BackgroundColor = bg;
                if (c.BackgroundStyle == BackgroundStyle.None) c.BackgroundStyle = BackgroundStyle.Solid;
            }
            string bc = (string)Reflect.Get(c, key + "BorderColor");
            if (!string.IsNullOrEmpty(bc)) c.BorderColor = bc;
            int bw = (int)Reflect.Get(c, key + "BorderWidth");
            if (bw >= 0) c.BorderThickness = bw;

            string gc = (string)Reflect.Get(c, key + "GlowColor");
            int gs = (int)Reflect.Get(c, key + "GlowSize");
            if (!string.IsNullOrEmpty(gc))
            {
                c.ShadowColor = gc;
                c.ShadowEnabled = true;
            }
            if (gs >= 0)
            {
                c.ShadowBlur = gs;
                c.ShadowEnabled = gs > 0;
            }
            int sc = (int)Reflect.Get(c, key + "Scale");
            if (sc > 0) c.Scale = sc;
        }
    }
}
