using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LoadingScreenMod
{
    internal sealed class Matcher
    {
        internal const int NUM = 3;

        internal const int BUILDINGS = 0;

        internal const int VEHICLES = 1;

        internal const int PROPS = 2;

        private const int LEVELS = 3;

        private readonly ByNames[] byNames = new ByNames[3]
        {
            new ByNames(),
            new ByNames(),
            new ByNames()
        };

        private readonly Dictionary<int, ByPatterns> byPatterns = new Dictionary<int, ByPatterns>(4);

        private readonly HashSet<int> byDLCs = new HashSet<int>();

        internal bool[] Has { get; } = new bool[3];


        private void AddName(string name, int index)
        {
            byNames[index].AddName(name);
            Has[index] = true;
        }

        private void AddPattern(string pattern, bool ic, int index, int svc)
        {
            int key = (index << 7) + svc;
            if (!this.byPatterns.TryGetValue(key, out var value))
            {
                ByPatterns byPatterns2 = (this.byPatterns[key] = new ByPatterns());
                value = byPatterns2;
            }
            try
            {
                value.AddPattern(pattern, ic);
                Has[index] = true;
            }
            catch (Exception exception)
            {
                Util.DebugPrint("Error in user regex:");
                Debug.LogException(exception);
            }
        }

        private void AddDLC(int dlc)
        {
            byDLCs.Add(dlc);
        }

        internal bool Matches(int dlc)
        {
            return byDLCs.Contains(dlc);
        }

        internal bool Matches(PrefabInfo info, int index)
        {
            string name = info.name.ToUpperInvariant();
            if (byNames[index].Matches(name))
            {
                return true;
            }
            int num = index << 7;
            if (byPatterns.TryGetValue(num - 1, out var value) && value.Matches(name))
            {
                return true;
            }
            if (byPatterns.TryGetValue((int)(info.GetService() + num), out value) && value.Matches(name))
            {
                return true;
            }
            int subService = (int)info.GetSubService();
            if (subService != 0 && byPatterns.TryGetValue(subService + 40 + num, out value))
            {
                return value.Matches(name);
            }
            return false;
        }

        internal static Matcher[] Load(string filePath)
        {
            Dictionary<string, int> enumMap = Util.GetEnumMap(typeof(ItemClass.Service));
            Dictionary<string, int> enumMap2 = Util.GetEnumMap(typeof(ItemClass.SubService));
            Dictionary<string, int> enumMap3 = Util.GetEnumMap(typeof(SteamHelper.DLC));
            Matcher matcher = new Matcher();
            Matcher matcher2 = new Matcher();
            string[] array = File.ReadAllLines(filePath);
            Regex regex = new Regex("^(?:([Ee]xcept|[Ss]kip)\\s*:)?(?:([a-zA-Z \\t]+):)?\\s*([^@:#\\t]+|@.+)$");
            int num = 0;
            string[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                string text = array2[i].Trim();
                if (string.IsNullOrEmpty(text) || text.StartsWith("#"))
                {
                    continue;
                }
                int num2 = text.IndexOf(':');
                int num3 = text.IndexOf('@');
                if (num2 > 0 && num3 < 0)
                {
                    string text2 = text.ToUpperInvariant();
                    if (text2.StartsWith("BUILDINGS"))
                    {
                        num = 0;
                        continue;
                    }
                    if (text2.StartsWith("VEHICLES"))
                    {
                        num = 1;
                        continue;
                    }
                    if (text2.StartsWith("PROPS"))
                    {
                        num = 2;
                        continue;
                    }
                    if (text2.StartsWith("LEVELS"))
                    {
                        num = 3;
                        continue;
                    }
                }
                if (num == 3)
                {
                    if (enumMap3.TryGetValue(text.ToUpperInvariant(), out var value))
                    {
                        matcher.AddDLC(value);
                    }
                    else
                    {
                        Msg(text, "unknown level");
                    }
                    continue;
                }
                Matcher matcher3;
                string text3;
                string text4;
                if (num2 >= 0 && (num2 < num3 || num3 < 0))
                {
                    Match match = regex.Match(text);
                    GroupCollection groups;
                    if (!match.Success || (groups = match.Groups).Count != 4)
                    {
                        Msg(text, "syntax error");
                        continue;
                    }
                    string value2 = groups[1].Value;
                    matcher3 = ((string.IsNullOrEmpty(value2) || value2.ToUpperInvariant() == "SKIP") ? matcher : matcher2);
                    value2 = groups[2].Value;
                    text3 = (string.IsNullOrEmpty(value2) ? string.Empty : value2.Replace(" ", string.Empty).Replace("\t", string.Empty).ToUpperInvariant());
                    text4 = groups[3].Value;
                }
                else
                {
                    matcher3 = matcher;
                    text3 = string.Empty;
                    text4 = text;
                }
                bool ic = false;
                int value3;
                if (text3 == string.Empty)
                {
                    value3 = -1;
                }
                else if (!enumMap.TryGetValue(text3, out value3))
                {
                    if (!enumMap2.TryGetValue(text3, out value3))
                    {
                        Msg(text, "unknown prefix");
                        continue;
                    }
                    value3 += 40;
                }
                string text5;
                if (!text4.StartsWith("@"))
                {
                    text5 = ((text4.IndexOf('*') < 0 && text4.IndexOf('?') < 0) ? null : ("^" + text4.ToUpperInvariant().Replace('?', '.').Replace("*", ".*") + "$"));
                }
                else
                {
                    text5 = text4.Substring(1);
                    ic = true;
                }
                if (text5 != null)
                {
                    matcher3.AddPattern(text5, ic, num, value3);
                    if (value3 < 0 && num == 0)
                    {
                        string text6 = text4.Replace("*", "");
                        string text7 = text6.Replace("?", "");
                        if (text4.Length != text6.Length && text7.Length == 0)
                        {
                            matcher2.AddName("STATUE OF SHOPPING", 0);
                            matcher2.AddName("ELECTRICITY POLE", 0);
                            matcher2.AddName("WIND TURBINE", 0);
                            matcher2.AddName("DAM POWER HOUSE", 0);
                            matcher2.AddName("DAM NODE BUILDING", 0);
                            matcher2.AddName("WATER PIPE JUNCTION", 0);
                            matcher2.AddName("HEATING PIPE JUNCTION", 0);
                        }
                    }
                }
                else
                {
                    matcher3.AddName(text4.ToUpperInvariant(), num);
                }
            }
            return new Matcher[2] { matcher, matcher2 };
        }

        private static void Msg(string line, string msg)
        {
            Util.DebugPrint(line + " -> " + msg);
        }
    }
}
