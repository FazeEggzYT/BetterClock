// Turns the chain text or the Pomodoro numbers into a list of countdown steps.
using System;
using System.Collections.Generic;

namespace BetterClock
{
    public class PlanStep
    {
        public int Seconds;
        public string Name;
    }

    public static class CountdownPlan
    {
        // null = a single plain countdown.
        public static List<PlanStep> Build(AppSettings s)
        {
            List<PlanStep> list = null;
            if (s.CountdownPlan == CountdownPlanKind.Pomodoro) list = Pomodoro(s);
            else if (s.CountdownPlan == CountdownPlanKind.Chain) list = ParseChain(s.CountdownChain);
            if (list == null || list.Count == 0) return null;
            return list;
        }

        public static bool Loops(AppSettings s)
        {
            if (s.CountdownPlan == CountdownPlanKind.Pomodoro) return true;
            return s.CountdownChainLoop;
        }

        // A key that changes whenever the plan changes.
        public static string Key(AppSettings s)
        {
            return s.CountdownPlan + "|" + s.CountdownChain + "|" + s.PomoWorkMin + "|" + s.PomoShortMin + "|" + s.PomoLongMin + "|" + s.PomoRounds;
        }

        public static List<PlanStep> Pomodoro(AppSettings s)
        {
            var list = new List<PlanStep>();
            int rounds = ColorUtil.Clamp(s.PomoRounds, 1, 12);
            for (int i = 1; i <= rounds; i++)
            {
                list.Add(Step(ColorUtil.Clamp(s.PomoWorkMin, 1, 180) * 60, "Work " + i + "/" + rounds));
                if (i < rounds) list.Add(Step(ColorUtil.Clamp(s.PomoShortMin, 1, 60) * 60, "Short break"));
                else list.Add(Step(ColorUtil.Clamp(s.PomoLongMin, 1, 120) * 60, "Long break"));
            }
            return list;
        }

        // "25:00 Work;5:00 Break;90 Stretch"
        public static List<PlanStep> ParseChain(string text)
        {
            var list = new List<PlanStep>();
            if (string.IsNullOrEmpty(text)) return list;
            foreach (string raw in text.Split(';'))
            {
                string part = raw.Trim();
                if (part.Length == 0) continue;
                int sp = part.IndexOf(' ');
                string time = sp < 0 ? part : part.Substring(0, sp);
                string name = sp < 0 ? "" : part.Substring(sp + 1).Trim();
                int secs = ParseTime(time);
                if (secs < 1) continue;
                if (name.Length == 0) name = "Step " + (list.Count + 1);
                list.Add(Step(secs, name));
            }
            return list;
        }

        // "1:30:00" = 1h30m, "25:00" = 25 min, "25" = 25 min
        public static int ParseTime(string t)
        {
            if (string.IsNullOrEmpty(t)) return 0;
            string[] p = t.Trim().Split(':');
            int[] v = new int[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                if (!int.TryParse(p[i], out v[i]) || v[i] < 0) return 0;
            }
            int total;
            if (p.Length == 1) total = v[0] * 60;
            else if (p.Length == 2) total = v[0] * 60 + v[1];
            else if (p.Length == 3) total = v[0] * 3600 + v[1] * 60 + v[2];
            else return 0;
            return total > 359999 ? 359999 : total;
        }

        private static PlanStep Step(int seconds, string name)
        {
            var st = new PlanStep();
            st.Seconds = seconds;
            st.Name = name;
            return st;
        }
    }
}
