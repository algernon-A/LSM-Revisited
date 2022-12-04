// <copyright file="PrefabLoader.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using ColossalFramework;
    using HarmonyLib;
    using LoadingScreenMod;
    using UnityEngine;

    /// <summary>
    /// Prefab loader that incorporates prefab skipping.
    /// </summary>
    public sealed class PrefabLoader
    {
        /// <summary>
        /// The number of supported skipping types.
        /// </summary>
        internal const int SkipTypes = 5;

        /// <summary>
        /// The prefix applied to skipped prefab names.
        /// </summary>
        internal const string SkipPrefix = "lsm___";

        // Type indexes.
        private const int Buildings = 0;
        private const int Vehicles = 1;
        private const int Props = 2;
        private const int Trees = 3;
        private const int Nets = 4;

        // Interator routine string.
        private const string IteratorRoutine = "<InitializePrefabs>c__Iterator0";

        // Instance reference.
        private static PrefabLoader s_instance;

        // LoadingManager.m_hasQueuedActions private field by reflection.
        private readonly FieldInfo _hasQueuedActionsField = AccessTools.Field(typeof(LoadingManager), "m_hasQueuedActions");

        // NetCollection prefab iterator private field by reflection.
        private readonly FieldInfo _netPrefabsField = typeof(NetCollection).GetNestedType(IteratorRoutine, BindingFlags.NonPublic).GetField("prefabs", BindingFlags.Instance | BindingFlags.NonPublic);

        // Skipping arrays.
        private readonly FieldInfo[] _nameField = new FieldInfo[SkipTypes];
        private readonly FieldInfo[] _prefabsField = new FieldInfo[SkipTypes];
        private readonly FieldInfo[] _replacesField = new FieldInfo[SkipTypes];

        // Skipped prefab lists.
        private readonly HashSet<string>[] _skippedPrefabs = new HashSet<string>[SkipTypes];

        // Simulation prefab lists.
        private HashSet<string> _simulationPrefabs;

        // Kept prefabs lists.
        private HashSet<string> _keptProps = new HashSet<string>();
        private HashSet<string> _keptTrees = new HashSet<string>();
        private HashSet<string> _keptNets = new HashSet<string>();

        // Status flag.
        private bool _saveDeserialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabLoader"/> class.
        /// </summary>
        private PrefabLoader()
        {
            try
            {
                int skipIndex = 0;
                Type[] types = new Type[SkipTypes]
                {
                    typeof(BuildingCollection),
                    typeof(VehicleCollection),
                    typeof(PropCollection),
                    typeof(TreeCollection),
                    typeof(NetCollection),
                };

                // Initialize arrays.
                for (int i = 0; i < types.Length; ++i)
                {
                    Type nestedType = types[i].GetNestedType(IteratorRoutine, BindingFlags.NonPublic);
                    _nameField[skipIndex] = AccessTools.Field(nestedType, "name");
                    _prefabsField[skipIndex] = AccessTools.Field(nestedType, "prefabs");
                    _replacesField[skipIndex] = AccessTools.Field(nestedType, "replaces");
                    _skippedPrefabs[skipIndex++] = new HashSet<string>();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // Delegate to prefab array update.
        private delegate void UpdatePrefabsDelegate(Array prefabs);

        // Delegate to prefab collection update.
        private delegate void UpdateCollectionDelegate(string name, Array keptPrefabs, string[] replacedNames);

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static PrefabLoader Instance => s_instance;

        /// <summary>
        /// Gets the list of skipped props.
        /// </summary>
        internal HashSet<string> SkippedProps => _skippedPrefabs[Props];

        /// <summary>
        /// Gets the list of skipped trees.
        /// </summary>
        internal HashSet<string> SkippedTrees => _skippedPrefabs[Trees];

        /// <summary>
        /// Sets the list of skipped prefabs.
        /// </summary>
        internal HashSet<string>[] SkippedPrefabs { set => value.CopyTo(_skippedPrefabs, 0); }

        /// <summary>
        /// Gets a value indicating whether all simulation prefabs are available.
        /// </summary>
        internal bool AreSimulationPrefabsAvailable => CustomDeserializer.AllAvailable<BuildingInfo>(_simulationPrefabs, new HashSet<string>());

        /// <summary>
        /// Ensures an active instance.
        /// </summary>
        /// <returns>Instance reference.</returns>
        internal static PrefabLoader Create()
        {
            // Create a new active instance if none is already active.
            if (s_instance == null)
            {
                s_instance = new PrefabLoader();
            }

            return s_instance;
        }

        /// <summary>
        /// Loading action - remove skipped prefabs from simulation.
        /// </summary>
        /// <returns>Loading IEnumerator.</returns>
        internal static IEnumerator RemoveSkippedFromSimulation()
        {
            if (s_instance != null)
            {
                s_instance.RemoveSkippedFromSimulation<BuildingInfo>(Buildings);
                yield return null;
                s_instance.RemoveSkippedFromSimulation<VehicleInfo>(Vehicles);
                yield return null;
                s_instance.RemoveSkippedFromSimulation<PropInfo>(Props);
                yield return null;
                s_instance.RemoveSkippedFromSimulation<TreeInfo>(Trees);
                yield return null;
                s_instance.RemoveSkippedFromSimulation<NetInfo>(Nets);
                yield return null;
            }
        }

        /// <summary>
        /// Removes skipped prefabs from the given district style.
        /// </summary>
        /// <param name="style">District style.</param>
        internal static void RemoveSkippedFromStyle(DistrictStyle style)
        {
            // Get hashset of skipped prefabs, if any.
            HashSet<string> skippedPrefabs = s_instance?._skippedPrefabs[0];
            if (skippedPrefabs == null || skippedPrefabs.Count == 0)
            {
                return;
            }

            try
            {
                // Get style contents.
                BuildingInfo[] buildingInfos = style.GetBuildingInfos();

                // Clear all included infos and affected services - this is rebuild from the ground up.
                ((HashSet<BuildingInfo>)Util.Get(style, "m_Infos")).Clear();
                ((HashSet<int>)Util.Get(style, "m_AffectedServices")).Clear();

                // Iterate through each building in the style.
                foreach (BuildingInfo buildingInfo in buildingInfos)
                {
                    // Add any building not skipped back to the style.
                    if (buildingInfo != null && !skippedPrefabs.Contains(buildingInfo.name))
                    {
                        style.Add(buildingInfo);
                    }
                }

                // Clear the array.
                Array.Clear(buildingInfos, 0, buildingInfos.Length);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception removing skipped prefabs from district style ", style?.Name);
            }
        }

        /// <summary>
        /// Unloads skipped prefabs.
        /// </summary>
        internal static void UnloadSkipped()
        {
            // Don't do anything if no active instance.
            if (s_instance != null)
            {
                // Clear temp arrays.
                s_instance._keptProps.Clear();
                s_instance._keptProps = null;
                s_instance._keptTrees.Clear();
                s_instance._keptTrees = null;
                s_instance._keptNets.Clear();
                s_instance._keptNets = null;
                s_instance._simulationPrefabs?.Clear();
                s_instance._simulationPrefabs = null;

                // Log skipped counts.
                int[] skipCounts = LevelLoader.SkipCounts;
                if (skipCounts[Buildings] > 0)
                {
                    Logging.KeyMessage("Skipped ", skipCounts[Buildings], " building prefabs");
                }

                if (skipCounts[Vehicles] > 0)
                {
                    Logging.KeyMessage("Skipped ", skipCounts[Vehicles], " vehicle prefabs");
                }

                if (skipCounts[Props] > 0)
                {
                    Logging.KeyMessage("Skipped ", skipCounts[Props], " prop prefabs");
                }

                if (skipCounts[Trees] > 0)
                {
                    Logging.KeyMessage("Skipped ", skipCounts[Trees], " tree prefabs");
                }

                if (skipCounts[Nets] > 0)
                {
                    Logging.KeyMessage("Skipped ", skipCounts[Nets], " net prefabs");
                }

                try
                {
                    // Try to unload any unused assets.
                    Resources.UnloadUnusedAssets();
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception unloading unused assets");
                }
            }
        }

        /// <summary>
        /// Deploys the QueueLoadingAction Harmony patch.
        /// </summary>
        internal void Deploy()
        {
            Logging.KeyMessage("patching QueueLoadingAction");

            PatcherManager<Patcher>.Instance.PrefixMethod(typeof(LoadingManager), typeof(PrefabLoader), nameof(QueueLoadingAction));
        }

        /// <summary>
        /// Reverts the QueueLoadingAction Harmony patch.
        /// </summary>
        internal void Revert() => PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingManager), typeof(PrefabLoader), nameof(QueueLoadingAction));

        /// <summary>
        /// Looks up all simulation prefabs (used buildings).
        /// </summary>
        internal void PopulateSimulationPrefabs()
        {
            // Don't do anything if already executed.
            if (_simulationPrefabs != null)
            {
                return;
            }

            // Reset list.
            _simulationPrefabs = new HashSet<string>();

            try
            {
                // Iterate through all buildings in map.
                Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                int length = buildings.Length;
                for (int i = 1; i < length; ++i)
                {
                    // If building exists (non-zero flags), add the prefab to the list.
                    if (buildings[i].m_flags != 0)
                    {
                        string prefabName = PrefabCollection<BuildingInfo>.PrefabName(buildings[i].m_infoIndex);

                        // Ignore custom assets (with periods in their names).
                        if (!string.IsNullOrEmpty(prefabName) && prefabName.IndexOf('.') < 0)
                        {
                            // Add prefab to list.
                            _simulationPrefabs.Add(prefabName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception populating simulation building prefabs");
            }

            try
            {
                // Iterate through all network segments in map.
                NetSegment[] segments = Singleton<NetManager>.instance.m_segments.m_buffer;
                int length = segments.Length;
                for (int i = 1; i < length; ++i)
                {
                    // If segment exists (non-zero flags), add the prefab to the list.
                    if (segments[i].m_flags != 0)
                    {
                        string prefabName = PrefabCollection<NetInfo>.PrefabName(segments[i].m_infoIndex);

                        // Ignore custom assets (with periods in their names).
                        if (!string.IsNullOrEmpty(prefabName) && prefabName.IndexOf('.') < 0)
                        {
                            // Add prefab to list.
                            _simulationPrefabs.Add(prefabName);
                        }
                    }
                }

                // Iterate through all network nodes in map.
                NetNode[] nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
                length = nodes.Length;
                for (int i = 1; i < length; ++i)
                {
                    // If segment exists (non-zero flags), add the prefab to the list.
                    if (nodes[i].m_flags != 0)
                    {
                        string prefabName = PrefabCollection<NetInfo>.PrefabName(nodes[i].m_infoIndex);

                        // Ignore custom assets (with periods in their names).
                        if (!string.IsNullOrEmpty(prefabName) && prefabName.IndexOf('.') < 0)
                        {
                            // Add prefab to list.
                            _simulationPrefabs.Add(prefabName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception populating simulation network prefabs");
            }
        }

        /// <summary>
        /// Clears all data and disposes of the instance.
        /// </summary>
        internal void Dispose()
        {
            // CLear arrays.
            _simulationPrefabs?.Clear();
            _simulationPrefabs = null;

            // Report skipped prefabs to the level loader.
            LevelLoader.SetSkippedPrefabs(_skippedPrefabs);

            // Clear skipped prefab array.
            Array.Clear(_skippedPrefabs, 0, _skippedPrefabs.Length);

            // Clear instance reference.
            s_instance = null;
        }

        /// <summary>
        /// Harmony pre-emptive Prefix patch to LoadingManager.QueueLoadingAction to implement skipped prefab loading.
        /// </summary>
        /// <param name="__instance">LoadingManager instance.</param>
        /// <param name="action">Loading action.</param>
        /// <returns>Always false (never execute original method).</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
        private static bool QueueLoadingAction(LoadingManager __instance, IEnumerator action)
        {
            // Determine type index.
            Type declaringType = action.GetType().DeclaringType;
            int typeIndex = -1;
            if (declaringType == typeof(BuildingCollection))
            {
                typeIndex = Buildings;
            }
            else if (declaringType == typeof(VehicleCollection))
            {
                typeIndex = Vehicles;
            }
            else if (declaringType == typeof(PropCollection))
            {
                typeIndex = Props;
            }
            else if (declaringType == typeof(TreeCollection))
            {
                typeIndex = Trees;
            }
            else if (declaringType == typeof(NetCollection))
            {
                typeIndex = Nets;
            }

            // Wait for save to be deserialized before proceeding.
            if (typeIndex >= 0 && !s_instance._saveDeserialized)
            {
                while (!LevelLoader.IsSaveDeserialized)
                {
                    Thread.Sleep(60);
                }

                s_instance._saveDeserialized = true;
            }

            // Get loading lock.
            while (!Monitor.TryEnter(LevelLoader.s_loadingLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }

            try
            {
                // Get relevant skipping action.
                switch (typeIndex)
                {
                    case Buildings:
                        s_instance.Skip<BuildingInfo>(action, UpdateBuildingPrefabs, UpdateBuildingCollection, typeIndex);
                        break;
                    case Vehicles:
                        s_instance.Skip<VehicleInfo>(action, UpdateVehiclePrefabs, UpdateVehicleCollection, typeIndex);
                        break;
                    case Props:
                        s_instance.Skip<PropInfo>(action, UpdatePropPrefabs, UpdatePropCollection, typeIndex);
                        break;
                    case Trees:
                        s_instance.Skip<TreeInfo>(action, UpdateTreePrefabs, UpdateTreeCollection, typeIndex);
                        break;
                    case Nets:
                        s_instance.Skip<NetInfo>(action, UpdateNetPrefabs, UpdateNetCollection, typeIndex);
                        break;
                }

                // Enque skipping action.
                LevelLoader.s_mainThreadQueue.Enqueue(action);
                if (LevelLoader.s_mainThreadQueue.Count < 2)
                {
                    s_instance._hasQueuedActionsField.SetValue(__instance, true);
                }
            }
            finally
            {
                Monitor.Exit(LevelLoader.s_loadingLock);
            }

            // Never execute original method.
            return false;
        }

        /// <summary>
        /// Update building prefabs to remove skipped props.
        /// </summary>
        /// <param name="prefabs">Building prefab array.</param>
        private static void UpdateBuildingPrefabs(Array prefabs)
        {
            // Don't do anything if no props or trees skipped.
            if (!(s_instance.SkipMatcherHas(Props) || s_instance.SkipMatcherHas(Trees)))
            {
                return;
            }

            // Iterate through all building prefabs.
            if (prefabs is BuildingInfo[] buildings)
            {
                for (int i = 0; i < buildings.Length; ++i)
                {
                    s_instance.RemoveSkippedFromBuilding(buildings[i]);
                }
            }
        }

        /// <summary>
        /// Update network prefabs to remove skipped props.
        /// </summary>
        /// <param name="prefabs">Network prefab array.</param>
        private static void UpdateNetPrefabs(Array prefabs)
        {
            // Don't do anything if no props or trees skipped.
            if (!(s_instance.SkipMatcherHas(Props) || s_instance.SkipMatcherHas(Trees)))
            {
                return;
            }

            // Iterate through all network prefabs and remove skipped trees/props.
            if (prefabs is NetInfo[] networks)
            {
                for (int i = 0; i < networks.Length; ++i)
                {
                    s_instance.RemoveSkippedFromNet(networks[i]);
                }
            }
        }

        /// <summary>
        /// Update vehicle prefabs to remove skipped prefabs.
        /// </summary>
        /// <param name="prefabs">Vehicle prefab array.</param>
        private static void UpdateVehiclePrefabs(Array prefabs)
        {
            // Don't do anything if no vehicles skipped.
            if (!s_instance.SkipMatcherHas(Vehicles))
            {
                return;
            }

            // Iterate through all vehicle prefabs.
            if (prefabs is VehicleInfo[] vehicles)
            {
                for (int i = 0; i < vehicles.Length; ++i)
                {
                    s_instance.RemoveSkippedFromVehicle(vehicles[i]);
                }
            }
        }

        /// <summary>
        /// Update prop prefabs to remove skipped prefabs.
        /// </summary>
        /// <param name="prefabs">Prop prefab array.</param>
        private static void UpdatePropPrefabs(Array prefabs)
        {
            // Don't do anything if no props skipped.
            if (!s_instance.SkipMatcherHas(Props))
            {
                return;
            }

            // Iterate through all prop prefabs.
            if (prefabs is PropInfo[] props)
            {
                for (int i = 0; i < props.Length; ++i)
                {
                    s_instance.RemoveSkippedFromProp(props[i]);
                }
            }
        }

        /// <summary>
        /// Update tree prefabs to remove skipped prefabs.
        /// </summary>
        /// <param name="prefabs">Tree prefab array.</param>
        private static void UpdateTreePrefabs(Array prefabs)
        {
            // Don't do anything if no props skipped.
            if (!s_instance.SkipMatcherHas(Trees))
            {
                return;
            }

            // Iterate through all tree prefabs.
            if (prefabs is TreeInfo[] trees)
            {
                for (int i = 0; i < trees.Length; ++i)
                {
                    s_instance.RemoveSkippedFromTree(trees[i]);
                }
            }
        }

        /// <summary>
        /// Update the building collection to reflect replaced names.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="keptPrefabs">Kept prefabs.</param>
        /// <param name="replacedNames">Replaced names.</param>
        private static void UpdateBuildingCollection(string name, Array keptPrefabs, string[] replacedNames)
        {
            BuildingCollection buildingCollection = GameObject.Find(name)?.GetComponent<BuildingCollection>();
            if (buildingCollection != null)
            {
                // Set kept prefabs.
                buildingCollection.m_prefabs = keptPrefabs as BuildingInfo[];

                // Set replaced names.
                if (replacedNames != null)
                {
                    buildingCollection.m_replacedNames = replacedNames;
                }
            }
        }

        /// <summary>
        /// Update the vehicle collection to reflect replaced names.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="keptPrefabs">Kept prefabs.</param>
        /// <param name="replacedNames">Replaced names.</param>
        private static void UpdateVehicleCollection(string name, Array keptPrefabs, string[] replacedNames)
        {
            VehicleCollection vehicleCollection = GameObject.Find(name)?.GetComponent<VehicleCollection>();
            if (vehicleCollection != null)
            {
                // Set kept prefabs.
                vehicleCollection.m_prefabs = keptPrefabs as VehicleInfo[];

                // Set replaced names.
                if (replacedNames != null)
                {
                    vehicleCollection.m_replacedNames = replacedNames;
                }
            }
        }

        /// <summary>
        /// Update the prop collection to reflect replaced names.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="keptPrefabs">Kept prefabs.</param>
        /// <param name="replacedNames">Replaced names.</param>
        private static void UpdatePropCollection(string name, Array keptPrefabs, string[] replacedNames)
        {
            PropCollection propCollection = GameObject.Find(name)?.GetComponent<PropCollection>();
            if (propCollection != null)
            {
                // Set kept prefabs.
                propCollection.m_prefabs = keptPrefabs as PropInfo[];

                // Set replaced names.
                if (replacedNames != null)
                {
                    propCollection.m_replacedNames = replacedNames;
                }
            }
        }

        /// <summary>
        /// Update the tree collection to reflect replaced names.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="keptPrefabs">Kept prefabs.</param>
        /// <param name="replacedNames">Replaced names.</param>
        private static void UpdateTreeCollection(string name, Array keptPrefabs, string[] replacedNames)
        {
            TreeCollection treeCollection = GameObject.Find(name)?.GetComponent<TreeCollection>();
            if (treeCollection != null)
            {
                // Set kept prefabs.
                treeCollection.m_prefabs = keptPrefabs as TreeInfo[];

                // Set replaced names.
                if (replacedNames != null)
                {
                    treeCollection.m_replacedNames = replacedNames;
                }
            }
        }

        /// <summary>
        /// Update the net collection to reflect replaced names.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="keptPrefabs">Kept prefabs.</param>
        /// <param name="replacedNames">Replaced names.</param>
        private static void UpdateNetCollection(string name, Array keptPrefabs, string[] replacedNames)
        {
            NetCollection netCollection = GameObject.Find(name)?.GetComponent<NetCollection>();
            if (netCollection != null)
            {
                // Set kept prefabs.
                netCollection.m_prefabs = keptPrefabs as NetInfo[];

                // Set replaced names.
                if (replacedNames != null)
                {
                    netCollection.m_replacedNames = replacedNames;
                }
            }
        }

        /// <summary>
        /// Checks to see if we have skipping settings for the given type index.
        /// </summary>
        /// <param name="index">Skipping type index.</param>
        /// <returns>True if skipping entries exist for the given type, false otherwise.</returns>
        private bool SkipMatcherHas(int index) => Settings.SkipMatcher != null && Settings.SkipMatcher.Has[index];

        /// <summary>
        /// Skips prefabs for the specified type.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="action">IEnumerator coroutine loading action.</param>
        /// <param name="updateAll">Prefab array update delegate.</param>
        /// <param name="updateKept">Prefab collection to update.</param>
        /// <param name="index">Prefab index.</param>
        private void Skip<TPrefab>(IEnumerator action, UpdatePrefabsDelegate updateAll, UpdateCollectionDelegate updateKept, int index)
            where TPrefab : PrefabInfo
        {
            try
            {
                // Get existing prefab array.
                if (!(_prefabsField[index].GetValue(action) is TPrefab[] prefabs))
                {
                    _prefabsField[index].SetValue(action, new TPrefab[0]);
                    return;
                }

                // Update prefabs.
                updateAll(prefabs);

                // Don't do anything else if no skipping entries for this prefab type/
                if (!SkipMatcherHas(index))
                {
                    return;
                }

                // Determine prefabs in use if this is the first time through.
                if (_simulationPrefabs == null)
                {
                    PopulateSimulationPrefabs();
                }

                // Get list of replacements.
                if (!(_replacesField[index].GetValue(action) is string[] replacesArray))
                {
                    replacesArray = new string[0];
                }

                List<TPrefab> keptPrefabs = null;
                List<string> keptReplaces = null;
                for (int i = 0; i < prefabs.Length; ++i)
                {
                    // Get prefab and any replacement.
                    TPrefab prefab = prefabs[i];
                    string replace = (i >= replacesArray.Length) ? string.Empty : replacesArray[i]?.Trim();

                    // Check for skipping.
                    if (ShouldSkip(prefab, replace, index))
                    {
                        // Skip this one.
                        AddToSkipped(prefab, replace, index);
                        ++LevelLoader.SkipCounts[index];

                        // Populate initial kept prefabs array if required.
                        if (keptPrefabs == null)
                        {
                            keptPrefabs = prefabs.ToList(i);

                            // Also update replacements array if required.
                            if (i < replacesArray.Length)
                            {
                                keptReplaces = replacesArray.ToList(i);
                            }
                        }
                    }
                    else if (keptPrefabs != null)
                    {
                        // Not skipped - add this one to kept lists.
                        keptPrefabs.Add(prefab);
                        keptReplaces?.Add(replace);
                    }
                }

                // Process any kept prefabs.
                if (keptPrefabs != null)
                {
                    TPrefab[] keptPrefabArray = keptPrefabs.ToArray();
                    string[] keptReplaceArray = null;
                    _prefabsField[index].SetValue(action, keptPrefabArray);

                    if (keptReplaces != null)
                    {
                        keptReplaceArray = keptReplaces.ToArray();
                        _replacesField[index].SetValue(action, keptReplaceArray);
                    }

                    // Update kept arrays.
                    updateKept(_nameField[index].GetValue(action) as string, keptPrefabArray, keptReplaceArray);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception skipping prefabs of type ", typeof(TPrefab));
            }
        }

        /// <summary>
        /// Add the given prefab to the relevant skipped list.
        /// </summary>
        /// <param name="prefab">Prefab info.</param>
        /// <param name="replace">Replace string.</param>
        /// <param name="typeIndex">Type index.</param>
        private void AddToSkipped(PrefabInfo prefab, string replace, int typeIndex)
        {
            // Add to relevant hashset.
            HashSet<string> skips = _skippedPrefabs[typeIndex];
            skips.Add(prefab.name);

            // Don't do anything further if no replace string.
            if (string.IsNullOrEmpty(replace))
            {
                return;
            }

            // Check if replace string contains multiple entries.
            if (replace.IndexOf(',') >= 0)
            {
                // Multiple entries - split into comma delimited entries.
                string[] replaces = replace.Split(',');
                for (int i = 0; i < replaces.Length; ++i)
                {
                    skips.Add(replaces[i].Trim());
                }
            }
            else
            {
                // Single entry.
                skips.Add(replace);
            }
        }

        /// <summary>
        /// Checks to see if the given prefab should be skipped.
        /// </summary>
        /// <param name="prefab">Prefab.</param>
        /// <param name="replace">Replace string.</param>
        /// <param name="typeIndex">Type index.</param>
        /// <returns>True if the prefab should be skipped, false otherwise.</returns>
        private bool ShouldSkip(PrefabInfo prefab, string replace, int typeIndex)
        {
            // Check for matching skip entry.
            if (Settings.SkipMatcher != null && Settings.SkipMatcher.Matches(prefab, typeIndex))
            {
                string name = prefab.name;

                // Exempt used buildings from skipping.
                if ((typeIndex == Buildings || typeIndex == Nets) && IsSimulationPrefab(name, replace))
                {
                    Logging.KeyMessage(name, " -> not skipped because used in city");
                    return false;
                }

                // Exempt excepted entries.
                if (Settings.ExceptMatcher != null && Settings.ExceptMatcher.Matches(prefab, typeIndex))
                {
                    Logging.Message(name, " -> not skipped because excepted");
                    return false;
                }

                Logging.Message(name, " -> skipped");
                return true;
            }

            // If we got here, it shouldn't be skipped.
            return false;
        }

        /// <summary>
        /// Checks to see if the given prefab should be skipped.
        /// </summary>
        /// <param name="prefab">Prefab.</param>
        /// <param name="typeIndex">Type index.</param>
        /// <returns>True if the prefab should be skipped, false otherwise.</returns>
        private bool ShouldSkip(PrefabInfo prefab, int typeIndex)
        {
            // Exempt used buildings and nets from skipping.
            if ((typeIndex == Buildings || typeIndex == Nets) && _simulationPrefabs.Contains(prefab.name))
            {
                return false;
            }

            // Check for matching skip entry.
            if (Settings.SkipMatcher != null && Settings.SkipMatcher.Matches(prefab, typeIndex))
            {
                // Exempt excepted entries.
                return !(Settings.ExceptMatcher != null && Settings.ExceptMatcher.Matches(prefab, typeIndex));
            }

            // If we got here, it shouldn't be skipped.
            return false;
        }

        /// <summary>
        /// Checks to see if the given prop prefab should be skipped.
        /// </summary>
        /// <param name="tree">Prop prefab prefab.</param>
        /// <returns>True if the prop prefab should be skipped, false otherwise.</returns>
        private bool SkippedProp(PropInfo tree)
        {
            string name = tree.name;

            // If this prop is already kept, it shouldn't be skipped.
            if (_keptProps.Contains(name))
            {
                return false;
            }

            // If the prop is already skipped, it should be skipped.
            if (_skippedPrefabs[Props].Contains(name))
            {
                return true;
            }

            // Otherwise, new prop: determine skipping status.
            bool shouldSkip = ShouldSkip(tree, Props);
            if (shouldSkip)
            {
                // Skipped.
                _skippedPrefabs[Props].Add(name);
            }
            else
            {
                // Kept.
                _keptProps.Add(name);
            }

            return shouldSkip;
        }

        /// <summary>
        /// Checks to see if the given tree prefab should be skipped.
        /// </summary>
        /// <param name="tree">Tree prefab prefab.</param>
        /// <returns>True if the tree prefab should be skipped, false otherwise.</returns>
        private bool SkippedTree(TreeInfo tree)
        {
            string name = tree.name;

            // If this tree is already kept, it shouldn't be skipped.
            if (_keptTrees.Contains(name))
            {
                return false;
            }

            // If the tree is already skipped, it should be skipped.
            if (_skippedPrefabs[Trees].Contains(name))
            {
                return true;
            }

            // Otherwise, new tree: determine skipping status.
            bool shouldSkip = ShouldSkip(tree, Trees);
            if (shouldSkip)
            {
                // Skipped.
                _skippedPrefabs[Trees].Add(name);
            }
            else
            {
                // Kept.
                _keptTrees.Add(name);
            }

            return shouldSkip;
        }

        /// <summary>
        /// Checks to see if the given net prefab should be skipped.
        /// </summary>
        /// <param name="network">Net prefab.</param>
        /// <returns>True if the net prefab should be skipped, false otherwise.</returns>
        private bool SkippedNet(NetInfo network)
        {
            string name = network.name;

            // If this network is already kept, it shouldn't be skipped.
            if (_keptNets.Contains(name))
            {
                return false;
            }

            // If the network is already skipped, it should be skipped.
            if (_skippedPrefabs[Nets].Contains(name))
            {
                return true;
            }

            // Otherwise, new network: determine skipping status.
            bool shouldSkip = ShouldSkip(network, Nets);
            if (shouldSkip)
            {
                // Skipped.
                _skippedPrefabs[Nets].Add(name);
            }
            else
            {
                // Kept.
                _keptNets.Add(name);
            }

            return shouldSkip;
        }

        /// <summary>
        /// Checks to see if the given name/replace combination represents a simulation prefab.
        /// </summary>
        /// <param name="name">Prefab name.</param>
        /// <param name="replace">Replace string.</param>
        /// <returns>True if this is a simulation prefab, false otherwise.</returns>
        private bool IsSimulationPrefab(string name, string replace)
        {
            // Direct anme lookup.
            if (_simulationPrefabs.Contains(name))
            {
                return true;
            }

            // No name match - if no replace string, then no match.
            if (string.IsNullOrEmpty(replace))
            {
                return false;
            }

            // Check if replace string contains multiple entries.
            if (replace.IndexOf(',') >= 0)
            {
                // Multiple entries - split into comma delimited entries.
                string[] replaces = replace.Split(',');
                for (int i = 0; i < replaces.Length; ++i)
                {
                    // Check each entry for a match.
                    if (_simulationPrefabs.Contains(replaces[i].Trim()))
                    {
                        return true;
                    }
                }

                // If we got here, then no match was found.
                return false;
            }

            // Single replace entry only - use direct lookup.
            return _simulationPrefabs.Contains(replace);
        }

        /// <summary>
        /// Remove any skipped prefabs (props or trees) from the given building prefab.
        /// </summary>
        /// <param name="building">Building prefab.</param>
        private void RemoveSkippedFromBuilding(BuildingInfo building)
        {
            // Remove skipped props and/or trees.
            BuildingInfo.Prop[] props = building.m_props;
            if (props != null && props.Length > 0)
            {
                try
                {
                    // Removal data.
                    List<BuildingInfo.Prop> keptProps = new List<BuildingInfo.Prop>(props.Length);
                    bool removed = false;

                    // Iterate through each prop in building.
                    foreach (BuildingInfo.Prop prop in props)
                    {
                        // Skip null props.
                        if (prop != null)
                        {
                            if (prop.m_prop == null)
                            {
                                // Null prop; check for trees.
                                if (prop.m_tree == null)
                                {
                                    // Keep records with null prop and tree.
                                    keptProps.Add(prop);
                                }
                                else if (SkippedTree(prop.m_tree))
                                {
                                    // This tree is skipped - remove it.
                                    prop.m_tree = prop.m_finalTree = null;
                                    removed = true;
                                }
                                else
                                {
                                    // Otherwise, add this one to the kept list.
                                    keptProps.Add(prop);
                                }
                            }
                            else if (SkippedProp(prop.m_prop))
                            {
                                // This prop is skipped - remove it.
                                prop.m_prop = prop.m_finalProp = null;
                                removed = true;
                            }
                            else
                            {
                                // Otherwise, add this one to the kept list.
                                keptProps.Add(prop);
                            }
                        }
                    }

                    // Were any props removed?
                    if (removed)
                    {
                        // Yes - assign new shortened prop array back to building.
                        building.m_props = keptProps.ToArray();

                        // If no props remainng, suppress 'no props' warnings.
                        if (building.m_props.Length == 0)
                        {
                            CommonBuildingAI commonBuildingAI = building.m_buildingAI as CommonBuildingAI;

                            // Fallback attempt if direct assignment fails.
                            if (commonBuildingAI == null)
                            {
                                commonBuildingAI = building.GetComponent<BuildingAI>() as CommonBuildingAI;
                            }

                            // Suppress no props warning.
                            if (commonBuildingAI != null)
                            {
                                commonBuildingAI.m_ignoreNoPropsWarning = true;
                            }
                        }
                    }

                    // Clear list.
                    keptProps.Clear();
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception removing skipped props from building ", building?.name);
                }
            }

            // Remove skipped nets.
            BuildingInfo.PathInfo[] paths = building.m_paths;
            if (paths != null && paths.Length > 0)
            {
                try
                {
                    bool removed = false;
                    List<BuildingInfo.PathInfo> keptPaths = new List<BuildingInfo.PathInfo>(paths.Length);
                    foreach (BuildingInfo.PathInfo path in paths)
                    {
                        if (path == null)
                        {
                            continue;
                        }

                        if (path.m_netInfo == null)
                        {
                            // Keep records with null netInfo.
                            keptPaths.Add(path);
                        }
                        else if (SkippedNet(path.m_netInfo))
                        {
                            // This network is skipped - remove it.
                            path.m_netInfo = path.m_finalNetInfo = null;
                            removed = true;
                        }
                        else
                        {
                            // Otherwise, add this one to the kept list.
                            keptPaths.Add(path);
                        }
                    }

                    // Were any paths removed?
                    if (removed)
                    {
                        // Yes - assign new shortened path array back to building.
                        building.m_paths = keptPaths.ToArray();
                        Logging.Message("removed paths from building ", building.name);
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception removing skipped nets from building ", building?.name);
                }
            }
        }

        /// <summary>
        /// Remove any skipped prefabs (trailers) from the given vehicle prefab.
        /// </summary>
        /// <param name="vehicle">Vehicle prefab.</param>
        private void RemoveSkippedFromVehicle(VehicleInfo vehicle)
        {
            // Don't do anything if vehicle contains no trailers.
            VehicleInfo.VehicleTrailer[] trailers = vehicle.m_trailers;
            if (trailers == null || trailers.Length == 0)
            {
                return;
            }

            try
            {
                // Removal data.
                List<VehicleInfo.VehicleTrailer> keptTrailers = new List<VehicleInfo.VehicleTrailer>(trailers.Length);
                string previousTrailer = string.Empty;
                bool removed = false;
                bool skipThis = false;

                // Iterate through each trailer in vehicle.
                for (int i = 0; i < trailers.Length; ++i)
                {
                    VehicleInfo trailer = trailers[i].m_info;
                    if (trailer != null)
                    {
                        // If (and only if) this trailer is different from the previous trailer, check to see if this trailer should be skipped.
                        // This saves having to check the same trailer prefab more than once (e.g. for trains).
                        string name = trailer.name;
                        if (!previousTrailer.Equals(name))
                        {
                            skipThis = ShouldSkip(trailer, Vehicles);
                            previousTrailer = name;
                        }

                        if (skipThis)
                        {
                            // This trailer is skipped - remove it.
                            trailers[i].m_info = null;
                            removed = true;
                        }
                        else
                        {
                            // Otherwise, add this one to the kept list.
                            keptTrailers.Add(trailers[i]);
                        }
                    }
                }

                // Were any trailers removed?
                if (removed)
                {
                    // Yes - assign new shortened trailer array back to vehicle.
                    vehicle.m_trailers = (keptTrailers.Count > 0) ? keptTrailers.ToArray() : null;
                }

                // Clear list.
                keptTrailers.Clear();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception removing skipped trailers from vehicle ", vehicle?.name);
            }
        }

        /// <summary>
        /// Remove any skipped prefabs (prop variations) from the given prop prefab.
        /// </summary>
        /// <param name="prop">Prop prefab.</param>
        private void RemoveSkippedFromProp(PropInfo prop)
        {
            // Don't do anything if prop contains no variations.
            PropInfo.Variation[] variations = prop.m_variations;
            if (variations == null || variations.Length == 0)
            {
                return;
            }

            try
            {
                // Removal data.
                List<PropInfo.Variation> keptVariations = new List<PropInfo.Variation>(variations.Length);
                bool removed = false;

                // Iterate through each variation in prop.
                for (int i = 0; i < variations.Length; ++i)
                {
                    PropInfo variation = variations[i].m_prop;
                    if (variation != null)
                    {
                        if (SkippedProp(variation))
                        {
                            // This variation is skipped - remove it.
                            variations[i].m_prop = variations[i].m_finalProp = null;
                            removed = true;
                        }
                        else
                        {
                            // Otherwise, add this one to the kept list.
                            keptVariations.Add(variations[i]);
                        }
                    }
                }

                // Were any variations removed?
                if (removed)
                {
                    // Yes - assign new shortened variation array back to prop.
                    prop.m_variations = (keptVariations.Count > 0) ? keptVariations.ToArray() : null;
                }

                // Clear list.
                keptVariations.Clear();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception removing skipped variations from prop ", prop?.name);
            }
        }

        /// <summary>
        /// Remove any skipped prefabs (tree variations) from the given tree prefab.
        /// </summary>
        /// <param name="tree">Tree prefab.</param>
        private void RemoveSkippedFromTree(TreeInfo tree)
        {
            // Don't do anything if prop contains no variations.
            TreeInfo.Variation[] variations = tree.m_variations;
            if (variations == null || variations.Length == 0)
            {
                return;
            }

            try
            {
                // Removal data.
                List<TreeInfo.Variation> keptVariations = new List<TreeInfo.Variation>(variations.Length);
                bool removed = false;

                // Iterate through each variation in prop.
                for (int i = 0; i < variations.Length; ++i)
                {
                    TreeInfo variation = variations[i].m_tree;
                    if (variation != null)
                    {
                        if (SkippedTree(variation))
                        {
                            // This variation is skipped - remove it.
                            variations[i].m_tree = variations[i].m_finalTree = null;
                            removed = true;
                        }
                        else
                        {
                            // Otherwise, add this one to the kept list.
                            keptVariations.Add(variations[i]);
                        }
                    }
                }

                // Were any variations removed?
                if (removed)
                {
                    // Yes - assign new shortened variation array back to prop.
                    tree.m_variations = (keptVariations.Count > 0) ? keptVariations.ToArray() : null;
                }

                // Clear list.
                keptVariations.Clear();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception removing skipped variations from prop ", tree?.name);
            }
        }

        /// <summary>
        /// Remove any skipped prefabs (props or trees) from the given network prefab.
        /// </summary>
        /// <param name="network">Network prefab.</param>
        private void RemoveSkippedFromNet(NetInfo network)
        {
            try
            {
                // Local reference.
                NetInfo.Lane[] lanes = network?.m_lanes;

                // Skip empty nets.
                if (lanes == null)
                {
                    return;
                }

                // Iterate through each lane in network.
                List<NetLaneProps.Prop> keptProps = new List<NetLaneProps.Prop>(16);
                for (int i = 0; i < lanes.Length; ++i)
                {
                    NetLaneProps laneProps = lanes[i].m_laneProps;

                    // Skip lanes with no props.
                    if (laneProps == null || laneProps.m_props == null)
                    {
                        continue;
                    }

                    bool removed = false;

                    // Iterate through each prop in lane.
                    NetLaneProps.Prop[] lanePropsProps = laneProps.m_props;
                    for (int j = 0; j < lanePropsProps.Length; ++j)
                    {
                        NetLaneProps.Prop prop = lanePropsProps[j];
                        if (prop != null)
                        {
                            if (prop.m_prop == null)
                            {
                                // Null prop; check for trees.
                                if (prop.m_tree == null)
                                {
                                    // Keep records with null prop and tree (probably already done this).
                                    keptProps.Add(prop);
                                }
                                else if (SkippedTree(prop.m_tree))
                                {
                                    // This tree is skipped - remove it.
                                    prop.m_tree = prop.m_finalTree = null;
                                    removed = true;
                                }
                                else
                                {
                                    // Otherwise, add this one to the kept list.
                                    keptProps.Add(prop);
                                }
                            }
                            else if (SkippedProp(prop.m_prop))
                            {
                                // This prop is skipped - remove it.
                                prop.m_prop = prop.m_finalProp = null;
                                removed = true;
                            }
                            else
                            {
                                // Otherwise, add this one to the kept list.
                                keptProps.Add(prop);
                            }
                        }
                    }

                    // Were any props removed?
                    if (removed)
                    {
                        // Yes - assign new shortened prop array back to lane.
                        laneProps.m_props = keptProps.ToArray();
                    }

                    // Clear list.
                    keptProps.Clear();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception removing skipped props from networks");
            }
        }

        /// <summary>
        /// Removes skipped prefabs of the given type from the simulation.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="typeIndex">Type index.</param>
        private void RemoveSkippedFromSimulation<TPrefab>(int typeIndex)
            where TPrefab : PrefabInfo
        {
            // Get skipped prefab hashset.
            HashSet<string> skippedPrefabs = _skippedPrefabs[typeIndex];
            if (skippedPrefabs == null || skippedPrefabs.Count == 0)
            {
                return;
            }

            // Get prefab lock.
            object prefabLock = Util.GetStatic(typeof(PrefabCollection<TPrefab>), "m_prefabLock");
            while (!Monitor.TryEnter(prefabLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }

            try
            {
                // Get simulation prefabs.
                FastList<PrefabCollection<TPrefab>.PrefabData> prefabs = (FastList<PrefabCollection<TPrefab>.PrefabData>)Util.GetStatic(typeof(PrefabCollection<TPrefab>), "m_simulationPrefabs");
                int prefabCount = prefabs.m_size;
                PrefabCollection<TPrefab>.PrefabData[] buffer = prefabs.m_buffer;

                // Iterate through all prefabs.
                for (int i = 0; i < prefabCount; ++i)
                {
                    // Is this prefab skipped?
                    if (buffer[i].m_name != null && skippedPrefabs.Contains(buffer[i].m_name))
                    {
                        // Yes - prefix the name to indicate skipping.
                        buffer[i].m_name = SkipPrefix + (i + (typeIndex << 12));

                        // Clear reference count.
                        buffer[i].m_refcount = 0;
                    }
                }
            }
            finally
            {
                Monitor.Exit(prefabLock);
            }
        }
    }
}
