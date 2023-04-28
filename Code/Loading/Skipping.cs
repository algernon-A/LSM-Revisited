// <copyright file="Skipping.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using AlgernonCommons;
    using static PrefabLoader;

    /// <summary>
    /// Prefab name matcher for asset skipping.
    /// </summary>
    internal sealed class Skipping
    {
        // Matching types (additional to PrefabLoader defined type indexes).
        private const int Levels = 5;
        private const int NameTypes = 5;
        private const int NumTypes = 6;

        // Service index.
        private const int NoService = -1;
        private const int SubServiceIndexBase = 40;

        // Name Matching array.
        private readonly NameMatcher[] _nameMatchers = new NameMatcher[NameTypes]
        {
            new NameMatcher(),
            new NameMatcher(),
            new NameMatcher(),
            new NameMatcher(),
            new NameMatcher(),
        };

        // Regex pattern matching dictionary.
        private readonly Dictionary<int, PatternMatcher> _patternMatchers = new Dictionary<int, PatternMatcher>(NumTypes);

        // DLC matching hashset.
        private readonly HashSet<int> _dlcMatchers = new HashSet<int>();

        /// <summary>
        /// Gets an array of boolean values indicating whether or not there are any skip entries for the matching index.
        /// </summary>
        internal bool[] Has { get; } = new bool[NameTypes];

        /// <summary>
        /// Loads the specified skip patch.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>New Matcher data array.</returns>
        internal static Skipping[] Load(string filePath)
        {
            Dictionary<string, int> serviceMap = MapEnum(typeof(ItemClass.Service));
            Dictionary<string, int> subServiceMap = MapEnum(typeof(ItemClass.SubService));
            Dictionary<string, int> dlcMap = MapEnum(typeof(SteamHelper.DLC));

            Skipping skipMatcher = new Skipping();
            Skipping exceptMatcher = new Skipping();

            // Read file.
            Logging.KeyMessage("loading skip file ", filePath);
            string[] lines = File.ReadAllLines(filePath);

            Regex regex = new Regex("^(?:([Ee]xcept|[Ss]kip)\\s*:)?(?:([a-zA-Z \\t]+):)?\\s*([^@:#\\t]+|@.+)$");
            int categoryIndex = 0;

            // Iterate through each line in order.
            foreach (string rawLine in lines)
            {
                // Skip empty lines and comments.
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                // Check for colons(:) and atmarks(@).
                int colonIndex = line.IndexOf(':');
                int atmarkIndex = line.IndexOf('@');
                if (colonIndex > 0 && atmarkIndex < 0)
                {
                    // Found colon but no atmark - category title.
                    string categoryTitle = line.ToUpperInvariant();
                    if (categoryTitle.StartsWith("BUILDINGS"))
                    {
                        categoryIndex = Buildings;
                        continue;
                    }

                    if (categoryTitle.StartsWith("VEHICLES"))
                    {
                        categoryIndex = Vehicles;
                        continue;
                    }

                    if (categoryTitle.StartsWith("PROPS"))
                    {
                        categoryIndex = Props;
                        continue;
                    }

                    if (categoryTitle.StartsWith("TREES"))
                    {
                        categoryIndex = Trees;
                        continue;
                    }

                    if (categoryTitle.StartsWith("NETWORKS"))
                    {
                        categoryIndex = Nets;

                        // Add enforced net exceptions.
                        exceptMatcher.AddName("PEDESTRIAN CONNECTION", Nets);
                        exceptMatcher.AddName("PEDESTRIAN CONNECTION INSIDE", Nets);
                        exceptMatcher.AddName("PEDESTRIAN CONNECTION SURFACE", Nets);
                        exceptMatcher.AddName("PEDESTRIAN CONNECTION UNDERGROUND", Nets);
                        exceptMatcher.AddName("PEDESTRIAN CONNECTION TRANSITION", Nets);
                        exceptMatcher.AddName("PEDESTRIAN CONNECTION TRANSITION ONEWAY", Nets);
                        exceptMatcher.AddName("VEHICLE CONNECTION", Nets);
                        exceptMatcher.AddName("CARGO CONNECTION", Nets);
                        exceptMatcher.AddName("CROSSINGS ROAD", Nets);
                        exceptMatcher.AddName("LARGE CARGO AIRPORT ROAD", Nets);
                        exceptMatcher.AddName("AIRPORT CARGO CONNECTION", Nets);
                        exceptMatcher.AddName("AIRPLANE STOP", Nets);
                        exceptMatcher.AddName("AIRPLANE CARGO STOP", Nets);
                        exceptMatcher.AddName("DLC SMALL AIRPLANE STOP", Nets);
                        exceptMatcher.AddName("DLC MEDIUM LARGE AIRPLANE STOP", Nets);
                        exceptMatcher.AddName("DLC LARGE AIRPLANE STOP", Nets);
                        exceptMatcher.AddName("DLC AIRPLANE CARGO STOP", Nets);
                        exceptMatcher.AddName("AIRPLANE TAXIWAY", Nets);
                        exceptMatcher.AddName("AIRPLANE RUNWAY", Nets);
                        exceptMatcher.AddName("AVIATION CLUB RUNWAY", Nets);
                        exceptMatcher.AddName("BUS STATION STOP", Nets);
                        exceptMatcher.AddName("BUS STATION WAY", Nets);
                        exceptMatcher.AddName("TRAM DEPOT ROAD", Nets);
                        exceptMatcher.AddName("TROLLEYBUS DEPOT ROAD", Nets);
                        exceptMatcher.AddName("HELICOPTER STOP", Nets);
                        exceptMatcher.AddName("HELICOPTER PATH", Nets);
                        exceptMatcher.AddName("HELICOPTER DEPOT PATH", Nets);
                        exceptMatcher.AddName("BLIMP STOP", Nets);
                        exceptMatcher.AddName("BLIMP DEPOT PATH", Nets);
                        exceptMatcher.AddName("CABLECAR STOP", Nets);
                        exceptMatcher.AddName("HARBOR ROAD", Nets);
                        exceptMatcher.AddName("SHIP DOCK", Nets);
                        exceptMatcher.AddName("SHIP DOCKWAY", Nets);
                        exceptMatcher.AddName("FERRY DOCK", Nets);
                        exceptMatcher.AddName("FERRY DOCKWAY", Nets);
                        exceptMatcher.AddName("FISHING DOCKWAY", Nets);
                        exceptMatcher.AddName("HEATING PIPE", Nets);
                        exceptMatcher.AddName("PARKING LOT 01", Nets);

                        continue;
                    }

                    if (categoryTitle.StartsWith("LEVELS"))
                    {
                        categoryIndex = Levels;
                        continue;
                    }
                }

                // Handle any level entries.
                if (categoryIndex == Levels)
                {
                    // Try to match dlc name with number.
                    if (dlcMap.TryGetValue(line.ToUpperInvariant(), out int dlc))
                    {
                        skipMatcher.AddDLC(dlc);
                    }
                    else
                    {
                        Logging.Error("unknown level in skipfile: ", line);
                    }

                    continue;
                }

                Skipping currentMatcher;
                string prefix;
                string patternOrName;
                if (colonIndex >= 0 && (colonIndex < atmarkIndex || atmarkIndex < 0))
                {
                    // Colon before atmark (or colon with no atmark) - normal line entry.
                    Match match = regex.Match(line);
                    GroupCollection groups;
                    if (!match.Success || (groups = match.Groups).Count != 4)
                    {
                        Logging.Error("syntax error in skipfile: ", line);
                        continue;
                    }

                    // Set current matcher.
                    string s = groups[1].Value;
                    currentMatcher = (string.IsNullOrEmpty(s) || s.ToUpperInvariant() == "SKIP") ? skipMatcher : exceptMatcher;

                    // Get prefix (service) and pattern or name, removing any spaces or tabs.
                    s = groups[2].Value;
                    prefix = string.IsNullOrEmpty(s) ? string.Empty : s.Replace(" ", string.Empty).Replace("\t", string.Empty).ToUpperInvariant();
                    patternOrName = groups[3].Value;
                }
                else
                {
                    // If no colon, or colon after atmark - this is a skip entry with no prefix.
                    currentMatcher = skipMatcher;
                    prefix = string.Empty;
                    patternOrName = line;
                }

                bool ignoreCase = false;

                // Get service index.
                int service;
                if (prefix == string.Empty)
                {
                    // No service provided.
                    service = NoService;
                }
                else if (!serviceMap.TryGetValue(prefix, out service))
                {
                    // No valid service - check sub-service.
                    if (!subServiceMap.TryGetValue(prefix, out service))
                    {
                        Logging.Error("unknown service in skipfile: ", line);
                        continue;
                    }

                    // Add sub-service index base.
                    service += SubServiceIndexBase;
                }

                // Check for pattern.
                string pattern;
                if (!patternOrName.StartsWith("@"))
                {
                    // Line doesn't start with an atmark - check for a pattern (contains * or ?).
                    pattern = (patternOrName.IndexOf('*') < 0 && patternOrName.IndexOf('?') < 0) ? null : ("^" + patternOrName.ToUpperInvariant().Replace('?', '.').Replace("*", ".*") + "$");
                }
                else
                {
                    // Line starts with atmark - this is a direct pattern.
                    pattern = patternOrName.Substring(1);
                    ignoreCase = true;
                }

                // Did we get a valid pattern?
                if (pattern != null)
                {
                    // Yes - add to current matcher.
                    currentMatcher.AddPattern(pattern, ignoreCase, categoryIndex, service);

                    // If no service was provided and we're processing buildings, check if we need to apply 'wildcard protection'.
                    if (service < 0 && categoryIndex == Buildings)
                    {
                        // Check for wildcards (entirely question marks, or containing asterisks).
                        string noAsterisks = patternOrName.Replace("*", string.Empty);
                        string noQuestionMarks = noAsterisks.Replace("?", string.Empty);
                        if (patternOrName.Length != noAsterisks.Length && noQuestionMarks.Length == 0)
                        {
                            // No service provided, we're parsing buildings, and this entry has wildcards.
                            // Appy wildcard protection by automatically excepting key items.
                            exceptMatcher.AddName("STATUE OF SHOPPING", Buildings);
                            exceptMatcher.AddName("ELECTRICITY POLE", Buildings);
                            exceptMatcher.AddName("WIND TURBINE", Buildings);
                            exceptMatcher.AddName("DAM POWER HOUSE", Buildings);
                            exceptMatcher.AddName("DAM NODE BUILDING", Buildings);
                            exceptMatcher.AddName("WATER PIPE JUNCTION", Buildings);
                            exceptMatcher.AddName("HEATING PIPE JUNCTION", Buildings);
                        }
                    }
                }
                else
                {
                    // No valid pattern - treat this as a name entry.
                    currentMatcher.AddName(patternOrName.ToUpperInvariant(), categoryIndex);
                }
            }

            // Return skip and except matchers.
            return new Skipping[2] { skipMatcher, exceptMatcher };
        }

        /// <summary>
        /// Checks to see if the given prefab is matched by this data.
        /// </summary>
        /// <param name="prefab">Prefab to check.</param>
        /// <param name="index">Category index.</param>
        /// <returns>True if the prefab is matched by this data, false otherwise.</returns>
        internal bool Matches(PrefabInfo prefab, int index)
        {
            // Check direct name match.
            string prefabName = prefab.name.ToUpperInvariant();
            if (_nameMatchers[index].Matches(prefabName))
            {
                return true;
            }

            // Check name pattern match - -1 means no service match.
            int patternKey = index << 7;
            if (_patternMatchers.TryGetValue(patternKey + NoService, out PatternMatcher patternMatcher) && patternMatcher.Matches(prefabName))
            {
                return true;
            }

            // Check service pattern match.
            if (_patternMatchers.TryGetValue((int)(prefab.GetService() + patternKey), out patternMatcher) && patternMatcher.Matches(prefabName))
            {
                return true;
            }

            // Check sub-service pattern match.
            int subService = (int)prefab.GetSubService();
            if (subService != 0 && _patternMatchers.TryGetValue(subService + SubServiceIndexBase + patternKey, out patternMatcher))
            {
                return patternMatcher.Matches(prefabName);
            }

            // If we got here, no match was found.
            return false;
        }

        /// <summary>
        /// Returns a value indicating whether the given DLC has an entry in this data.
        /// </summary>
        /// <param name="dlc">DLC number to check.</param>
        /// <returns>True if the given DLC is included in this data, false otherwise.</returns>
        internal bool Matches(int dlc) => _dlcMatchers.Contains(dlc);

        /// <summary>
        /// Creates a dictionary enum names to values.
        /// </summary>
        /// <param name="enumType">Enum type to map.</param>
        /// <returns>Dictionary mapping enum names (to uppercase) to values.</returns>
        private static Dictionary<string, int> MapEnum(Type enumType)
        {
            Array values = Enum.GetValues(enumType);
            Dictionary<string, int> dictionary = new Dictionary<string, int>(values.Length);
            foreach (object item in values)
            {
                dictionary[item.ToString().ToUpperInvariant()] = (int)item;
            }

            return dictionary;
        }

        /// <summary>
        /// Adds a name to the given category.
        /// </summary>
        /// <param name="name">Name to add.</param>
        /// <param name="index">Category index.</param>
        private void AddName(string name, int index)
        {
            _nameMatchers[index].AddName(name);
            Has[index] = true;
        }

        /// <summary>
        /// Adds a regex pattern to the given index.
        /// </summary>
        /// <param name="pattern">Pattern to add.</param>
        /// <param name="ignoreCase">True to ignore case, false otherwise.</param>
        /// <param name="index">Category index.</param>
        /// <param name="service">Service index.</param>
        private void AddPattern(string pattern, bool ignoreCase, int index, int service)
        {
            // Calculate key from category and service index.
            int key = (index << 7) + service;

            // Check for existing entry.
            if (!_patternMatchers.TryGetValue(key, out PatternMatcher patternMatcher))
            {
                // No existing pattern - add a new one.
                patternMatcher = _patternMatchers[key] = new PatternMatcher();
            }

            // Add pattern to entry.
            try
            {
                patternMatcher.AddPattern(pattern, ignoreCase);
                Has[index] = true;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "error in user regex");
            }
        }

        /// <summary>
        /// Adds a DLC entry.
        /// </summary>
        /// <param name="dlc">DLC number.</param>
        private void AddDLC(int dlc) => _dlcMatchers.Add(dlc);

        /// <summary>
        /// Name matching.
        /// </summary>
        private sealed class NameMatcher
        {
            // List of names.
            private readonly HashSet<string> _names = new HashSet<string>();

            public bool Matches(string name)
            {
                return _names.Contains(name);
            }

            public void AddName(string name)
            {
                _names.Add(name);
            }
        }

        /// <summary>
        /// Regex pattern matching.
        /// </summary>
        private sealed class PatternMatcher
        {
            // List of pattens.
            private readonly List<Regex> _patterns = new List<Regex>(1);

            /// <summary>
            /// Checks the given name for a match.
            /// </summary>
            /// <param name="name">Name to check.</param>
            /// <returns>True if a match was made, false otherwise.</returns>
            public bool Matches(string name)
            {
                // Check name against each stored regex.
                foreach (Regex regex in _patterns)
                {
                    if (regex == null || regex.IsMatch(name))
                    {
                        return true;
                    }
                }

                // If we got here, no match was found.
                return false;
            }

            /// <summary>
            /// Adds a pattern.
            /// </summary>
            /// <param name="pattern">Regex pattern to add.</param>
            /// <param name="ignoreCase">True to ignore case, false otherwise.</param>
            public void AddPattern(string pattern, bool ignoreCase)
            {
                if (pattern == "^.*$")
                {
                    _patterns.Insert(0, null);
                }
                else
                {
                    _patterns.Add(ignoreCase ? new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) : new Regex(pattern));
                }
            }
        }
    }
}
