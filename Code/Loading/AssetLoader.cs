using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using ICities;
using UnityEngine;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
	/// <summary>
	/// Custom content loader; called from LevelLoader.
	/// </summary>
	public sealed class AssetLoader
	{
		// Instance reference.
		private static AssetLoader instance;


		/// <summary>
		/// Instance getter.
		/// </summary>
		public static AssetLoader Instance => instance;


		/// <summary>
		/// Custom asset types.
		/// </summary>
		private readonly CustomAssetMetaData.Type[] typeMap = new CustomAssetMetaData.Type[12]
		{
			CustomAssetMetaData.Type.Building,
			CustomAssetMetaData.Type.Prop,
			CustomAssetMetaData.Type.Tree,
			CustomAssetMetaData.Type.Vehicle,
			CustomAssetMetaData.Type.Vehicle,
			CustomAssetMetaData.Type.Building,
			CustomAssetMetaData.Type.Building,
			CustomAssetMetaData.Type.Prop,
			CustomAssetMetaData.Type.Citizen,
			CustomAssetMetaData.Type.Road,
			CustomAssetMetaData.Type.Road,
			CustomAssetMetaData.Type.Building
		};

		// Loading order queue index for loading each of the above asset types.
		// Organized so related and identical assets are close to each other == more texture/mesh cache hits
		// 0: Tree and citizen
		// 1: Props
		// 2: Roads, elevations, and pillars
		// 3: Buildings and sub-buildings
		// 4: Vehicles and trailers
		private readonly int[] loadQueueIndex = new int[12]
		{
			3, 1, 0, 4, 4, 3, 3, 1, 0, 2, 2, 2
		};


		private HashSet<string> loadedIntersections = new HashSet<string>();

		private HashSet<string> hiddenAssets = new HashSet<string>();


		private Dictionary<Package, CustomAssetMetaData.Type> packageTypes = new Dictionary<Package, CustomAssetMetaData.Type>(256);

		private Dictionary<string, SomeMetaData> metaDatas = new Dictionary<string, SomeMetaData>(128);

		private Dictionary<string, CustomAssetMetaData> citizenMetaDatas = new Dictionary<string, CustomAssetMetaData>();

		private Dictionary<string, List<Package.Asset>>[] suspects;

		private Dictionary<string, bool> boolValues;

		internal readonly Stack<Package.Asset> stack = new Stack<Package.Asset>(4);

		internal int beginMillis;

		internal int lastMillis;

		internal int assetCount;

		private float progress;

		private readonly bool recordAssets = LoadingScreenMod.Settings.settings.RecordAssets;

		private readonly bool checkAssets = LoadingScreenMod.Settings.settings.checkAssets;

		private readonly bool hasAssetDataExtensions;

		internal const int yieldInterval = 350;

		internal Package.Asset Current
		{
			get
			{
				if (stack.Count <= 0)
				{
					return null;
				}
				return stack.Peek();
			}
		}

		internal bool IsIntersection(string fullName)
		{
			return loadedIntersections.Contains(fullName);
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		private AssetLoader()
		{
			// Loading queues.
			Dictionary<string, List<Package.Asset>> buildingQueue = new Dictionary<string, List<Package.Asset>>(4);
			Dictionary<string, List<Package.Asset>> propQueue = new Dictionary<string, List<Package.Asset>>(4);
			Dictionary<string, List<Package.Asset>> treeQueue = new Dictionary<string, List<Package.Asset>>(4);
			Dictionary<string, List<Package.Asset>> vehicleQueue = new Dictionary<string, List<Package.Asset>>(4);
			Dictionary<string, List<Package.Asset>> citizenQueue = new Dictionary<string, List<Package.Asset>>(4);
			Dictionary<string, List<Package.Asset>> netQueue = new Dictionary<string, List<Package.Asset>>(4);

			suspects = new Dictionary<string, List<Package.Asset>>[12]
			{
				buildingQueue, propQueue, treeQueue, vehicleQueue, vehicleQueue, buildingQueue, buildingQueue, propQueue, citizenQueue, netQueue,
				netQueue, buildingQueue
			};
			SettingsFile settingsFile = GameSettings.FindSettingsFileByName(PackageManager.assetStateSettingsFile);
			boolValues = (Dictionary<string, bool>)Util.Get(settingsFile, "m_SettingsBoolValues");
			List<IAssetDataExtension> list = (List<IAssetDataExtension>)Util.Get(Singleton<LoadingManager>.instance.m_AssetDataWrapper, "m_AssetDataExtensions");
			hasAssetDataExtensions = list.Count > 0;
			if (hasAssetDataExtensions)
			{
				Util.DebugPrint("IAssetDataExtensions:", list.Count);
			}

			Instance<CustomDeserializer>.instance.Setup();
			Instance<Sharing>.Create();
			if (recordAssets)
			{
				Instance<Reports>.Create();
			}
			if (LoadingScreenMod.Settings.settings.hideAssets)
			{
				LoadingScreenMod.Settings.settings.LoadHiddenAssets(hiddenAssets);
			}
		}


		/// <summary>
		/// Creates the instance.
		/// </summary>
		public static void Create()
		{
			// Dispose of any existing instance.
			Dispose();

			// Create new instance.
			instance = new AssetLoader();
		}


		/// <summary>
		/// Clears all data and disposes of the instance.
		/// </summary>
		public static void Dispose()
		{
			// Safety check.
			if (instance == null)
			{
				return;
			}

			// Save assets report if set to do so.
			if (LoadingScreenMod.Settings.settings.reportAssets)
			{
				Instance<Reports>.instance.SaveStats();
			}

			// Dispost of any asset reporting instance.
			if (instance.recordAssets)
			{
				Instance<Reports>.instance.Dispose();
			}

			// Dispose of instances.
			Instance<UsedAssets>.instance?.Dispose();
			Instance<Sharing>.instance?.Dispose();

			// Clear collections.
			instance.loadedIntersections.Clear();
			instance.hiddenAssets.Clear();
			instance.packageTypes.Clear();
			instance.metaDatas.Clear();
			instance.citizenMetaDatas.Clear();
			instance.loadedIntersections = null;
			instance.hiddenAssets = null;
			instance.packageTypes = null;
			instance.metaDatas = null;
			instance.citizenMetaDatas = null;
			Array.Clear(instance.suspects, 0, instance.suspects.Length);
			instance.suspects = null;
			instance.boolValues = null;

			// Finally, clear this instance.
			instance = null;
		}


		/// <summary>
		/// The custom content loader iteslf.
		/// </summary>
		/// <returns></returns>
		public IEnumerator LoadCustomContent()
		{
			// Local reference.
			LoadingManager loadingManager = Singleton<LoadingManager>.instance;


			// Gamecode.
			loadingManager.m_loadingProfilerMain.BeginLoading("LoadCustomContent");
			loadingManager.m_loadingProfilerCustomContent.Reset();
			loadingManager.m_loadingProfilerCustomAsset.Reset();
			loadingManager.m_loadingProfilerCustomContent.BeginLoading("District Styles");
			loadingManager.m_loadingProfilerCustomAsset.PauseLoading();

			// LSM.
			LevelLoader.assetLoadingStarted = true;

			// Gamecode.
			List<DistrictStyle> districtStyles = new List<DistrictStyle>();
			HashSet<string> hashSet = new HashSet<string>();
			FastList<DistrictStyleMetaData> cachedStyles = new FastList<DistrictStyleMetaData>();
			FastList<Package> cachedStylePackages = new FastList<Package>();

			// Gamecode equivalent.
			Package.Asset europeanStyles = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanStyleName);
			if (europeanStyles != null && europeanStyles.isEnabled)
			{
				DistrictStyle districtStyle = new DistrictStyle(DistrictStyle.kEuropeanStyleName, builtIn: true);
				Util.InvokeVoid(Singleton<LoadingManager>.instance, "AddChildrenToBuiltinStyle", GameObject.Find("European Style new"), districtStyle, false);
				Util.InvokeVoid(Singleton<LoadingManager>.instance, "AddChildrenToBuiltinStyle", GameObject.Find("European Style others"), districtStyle, true);
				if (LoadingScreenMod.Settings.settings.SkipPrefabs)
				{
					PrefabLoader.RemoveSkippedFromStyle(districtStyle);
				}
				districtStyles.Add(districtStyle);
			}
			if (LevelLoader.DLC(715190))
			{
				Package.Asset europeanSuburbiaStyle = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanSuburbiaStyleName);
				if (europeanSuburbiaStyle != null && europeanSuburbiaStyle.isEnabled)
				{
					DistrictStyle districtStyle = new DistrictStyle(DistrictStyle.kEuropeanSuburbiaStyleName, builtIn: true);
					Util.InvokeVoid(Singleton<LoadingManager>.instance, "AddChildrenToBuiltinStyle", GameObject.Find("Modder Pack 3"), districtStyle, false);
					if (LoadingScreenMod.Settings.settings.SkipPrefabs)
					{
						PrefabLoader.RemoveSkippedFromStyle(districtStyle);
					}
					districtStyles.Add(districtStyle);
				}
			}
			if (LevelLoader.DLC(1148020))
			{
				Package.Asset cityCenterStyle = PackageManager.FindAssetByName("System." + DistrictStyle.kModderPack5StyleName);
				if (cityCenterStyle != null && cityCenterStyle.isEnabled)
				{
					DistrictStyle districtStyle = new DistrictStyle(DistrictStyle.kModderPack5StyleName, builtIn: true);
					Util.InvokeVoid(Singleton<LoadingManager>.instance, "AddChildrenToBuiltinStyle", GameObject.Find("Modder Pack 5"), districtStyle, false);
					if (LoadingScreenMod.Settings.settings.SkipPrefabs)
					{
						PrefabLoader.RemoveSkippedFromStyle(districtStyle);
					}
					districtStyles.Add(districtStyle);
				}
			}

			// LSM insert.
			// Unload any skipped assets.
			if (LoadingScreenMod.Settings.settings.SkipPrefabs)
			{
				PrefabLoader.UnloadSkipped();
			}

			// Gamecode.
			foreach (Package.Asset item in PackageManager.FilterAssets(UserAssetType.DistrictStyleMetaData))
			{
				try
				{
					if (!(item != null) || !item.isEnabled)
					{
						continue;
					}
					DistrictStyleMetaData districtStyleMetaData = item.Instantiate<DistrictStyleMetaData>();
					if (districtStyleMetaData == null || districtStyleMetaData.builtin)
					{
						continue;
					}
					cachedStyles.Add(districtStyleMetaData);
					cachedStylePackages.Add(item.package);
					if (districtStyleMetaData.assets != null)
					{
						for (int l = 0; l < districtStyleMetaData.assets.Length; l++)
						{
							hashSet.Add(districtStyleMetaData.assets[l]);
						}
					}
				}
				catch (Exception ex)
				{
					CODebugBase<LogChannel>.Warn(LogChannel.Modding, string.Concat(ex.GetType(), ": Loading custom district style failed[", item, "]\n", ex.Message));
				}
			}
			loadingManager.m_loadingProfilerCustomAsset.ContinueLoading();
			loadingManager.m_loadingProfilerCustomContent.EndLoading();

			// LSM insert.
			// Create used asset instance if required.
			if (LoadingScreenMod.Settings.settings.loadUsed)
			{
				Instance<UsedAssets>.Create();
			}
			LoadingScreen.instance.DualSource.Add(Translations.Translate("CUSTOM_ASSETS"));

			// Gamecode.
			loadingManager.m_loadingProfilerCustomContent.BeginLoading("Calculating asset load order");

			// LSM - replaces game loading queue calculation.
			LogStatus();
			Package.Asset[] queue = GetLoadQueue(hashSet);
			Util.DebugPrint("LoadQueue", queue.Length, Profiling.Millis);

			// Gamecode.
			loadingManager.m_loadingProfilerCustomContent.EndLoading();
			loadingManager.m_loadingProfilerCustomContent.BeginLoading("Loading Custom Assets");

			// LSM - replace game custom asset loading.
			Instance<Sharing>.instance.Start(queue);
			beginMillis = (lastMillis = Profiling.Millis);
			for (int k = 0; k < queue.Length; k++)
			{
				if ((k & 0x3F) == 0)
				{
					LogStatus(k);
				}
				Instance<Sharing>.instance.WaitForWorkers(k);
				stack.Clear();
				Package.Asset asset4 = queue[k];
				try
				{
					LoadImpl(asset4);
				}
				catch (Exception e)
				{
					AssetFailed(asset4, asset4.package, e);
				}
				if (Profiling.Millis - lastMillis > 350)
				{
					lastMillis = Profiling.Millis;
					progress = 0.15f + (float)(k + 1) * 0.7f / (float)queue.Length;
					LoadingScreen.instance.SetProgress(progress, progress, assetCount, assetCount - k - 1 + queue.Length, beginMillis, lastMillis);
					yield return null;
				}
			}
			lastMillis = Profiling.Millis;
			LoadingScreen.instance.SetProgress(0.85f, 1f, assetCount, assetCount, beginMillis, lastMillis);
			loadingManager.m_loadingProfilerCustomContent.EndLoading();
			Util.DebugPrint(assetCount, "custom assets loaded in", lastMillis - beginMillis);
			Instance<CustomDeserializer>.instance.SetCompleted();
			LogStatus();
			stack.Clear();
			Report();

			// Gamecode.
			loadingManager.m_loadingProfilerCustomContent.BeginLoading("Finalizing District Styles");
			loadingManager.m_loadingProfilerCustomAsset.PauseLoading();
			for (int k = 0; k < cachedStyles.m_size; k++)
			{
				try
				{
					DistrictStyleMetaData districtStyleMetaData = cachedStyles.m_buffer[k];
					DistrictStyle districtStyle = new DistrictStyle(districtStyleMetaData.name, builtIn: false);
					if (cachedStylePackages.m_buffer[k].GetPublishedFileID() != PublishedFileId.invalid)
					{
						districtStyle.PackageName = cachedStylePackages.m_buffer[k].packageName;
					}
					if (districtStyleMetaData.assets == null)
					{
						continue;
					}
					for (int l = 0; l < districtStyleMetaData.assets.Length; l++)
					{
						BuildingInfo buildingInfo = CustomDeserializer.FindLoaded<BuildingInfo>(districtStyleMetaData.assets[l] + "_Data");
						if (buildingInfo != null)
						{
							districtStyle.Add(buildingInfo);
							if (districtStyleMetaData.builtin)
							{
								buildingInfo.m_dontSpawnNormally = !districtStyleMetaData.assetRef.isEnabled;
							}
						}
						else
						{
							CODebugBase<LogChannel>.Warn(LogChannel.Modding, "Warning: Missing asset (" + districtStyleMetaData.assets[l] + ") in style " + districtStyleMetaData.name);
						}
					}
					districtStyles.Add(districtStyle);
				}
				catch (Exception ex2)
				{
					CODebugBase<LogChannel>.Warn(LogChannel.Modding, ex2.GetType()?.ToString() + ": Loading district style failed\n" + ex2.Message);
				}
			}
			Singleton<DistrictManager>.instance.m_Styles = districtStyles.ToArray();
			if (Singleton<BuildingManager>.exists)
			{
				Singleton<BuildingManager>.instance.InitializeStyleArray(districtStyles.Count);
			}

			// LSM insert.
			if (LoadingScreenMod.Settings.settings.enableDisable)
			{
				Util.DebugPrint("Going to enable and disable assets");
				LoadingScreen.instance.DualSource.Add(Translations.Translate("ENABLING_AND_DISABLING"));
				yield return null;
				EnableDisableAssets();
			}

			// Gamecode.
			loadingManager.m_loadingProfilerCustomAsset.ContinueLoading();
			loadingManager.m_loadingProfilerCustomContent.EndLoading();
			loadingManager.m_loadingProfilerMain.EndLoading();

			// LSM insert.
			LevelLoader.assetsFinished = true;
		}



		/// <summary>
		/// Retrieves the main asset of a package.
		/// </summary>
		/// <param name="package">Package</param>
		/// <returns>Main asset</returns>
		internal static Package.Asset FindMainAssetRef(Package package) => package.FilterAssets(Package.AssetType.Object).LastOrDefault((Package.Asset a) => a.name.EndsWith("_Data"));


		/// <summary>
		/// Logs memory usage and other stats.
		/// </summary>
		/// <param name="queueCount">Asset loading queue counter</param>
		internal static void LogStatus(int queueCount = -1)
		{
			StringBuilder logMessage = new StringBuilder();
			logMessage.Append("status: ");
			if (queueCount >= 0)
			{
				logMessage.Append("assets: ");
				logMessage.Append(queueCount);
				logMessage.Append(' ');
			}
			logMessage.Append("millis: ");
			logMessage.Append(Profiling.Millis);

			try
			{
				// Include memory usage if on Windows.
				if (Application.platform == RuntimePlatform.WindowsPlayer)
				{
					MemoryAPI.GetUsage(out var pfMegas, out var wsMegas);
					logMessage.Append(" RAM: ");
					logMessage.Append(wsMegas);
					logMessage.Append(" Page: ");
					logMessage.Append(pfMegas);
				}

				// Include sharing status.
				if (Instance<Sharing>.HasInstance)
				{
					Sharing sharing = Instance<Sharing>.instance;
					logMessage.Append(" Sharing status: ");
					logMessage.Append(sharing.Status);
					logMessage.Append(" Sharing misses: ");
					logMessage.Append(sharing.Misses);
					logMessage.Append(" Loader ahead: ");
					logMessage.Append(sharing.LoaderAhead);
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception logging status");
			}

			// Write to log.
			Logging.KeyMessage(logMessage);
		}


		/// <summary>
		/// Removes trailing "_Data" (if any) from the provided name.
		/// </summary>
		/// <param name="name_Data">Name</param>
		/// <returns>Name with trailing "_Data" removed (unchanged input if the original didn't end with "_Data")</returns>
		internal static string ShortName(string name_Data)
		{
			if (name_Data.Length <= 5 || !name_Data.EndsWith("_Data"))
			{
				return name_Data;
			}
			return name_Data.Substring(0, name_Data.Length - 5);
		}


		// Transpiled by Intersection Marking Tool - leave for now!
		internal void LoadImpl(Package.Asset assetRef)
		{
			try
			{
				stack.Push(assetRef);
				string name = assetRef.name;
				Singleton<LoadingManager>.instance.m_loadingProfilerCustomAsset.BeginLoading(ShortName(name));
				GameObject gameObject = AssetDeserializer.Instantiate(assetRef, isMain: true, isTop: true) as GameObject;
				if (gameObject == null)
				{
					throw new Exception(assetRef.fullName + ": no GameObject");
				}
				Package package = assetRef.package;
				CustomAssetMetaData.Type value;
				bool num = packageTypes.TryGetValue(package, out value);
				bool flag = value == CustomAssetMetaData.Type.Road;
				if (checkAssets && name != gameObject.name)
				{
					Instance<Reports>.instance.AddNamingConflict(package);
				}
				string text2 = (gameObject.name = (((num && !flag) || !name.Contains(".") || !IsPillarOrElevation(assetRef, flag)) ? assetRef.fullName : PillarOrElevationName(package.packageName, name)));
				gameObject.SetActive(value: false);
				PrefabInfo prefabInfo = gameObject.GetComponent<PrefabInfo>();
				prefabInfo.m_isCustomContent = true;
				if (prefabInfo.m_Atlas != null && !string.IsNullOrEmpty(prefabInfo.m_InfoTooltipThumbnail))
				{
					prefabInfo.m_InfoTooltipAtlas = prefabInfo.m_Atlas;
				}
				PropInfo component;
				TreeInfo component2;
				BuildingInfo component3;
				VehicleInfo component4;
				CitizenInfo component5;
				NetInfo component6;
				if ((component = gameObject.GetComponent<PropInfo>()) != null)
				{
					if (component.m_lodObject != null)
					{
						component.m_lodObject.SetActive(value: false);
					}
					Initialize(component);
				}
				else if ((component2 = gameObject.GetComponent<TreeInfo>()) != null)
				{
					Initialize(component2);
				}
				else if ((component3 = gameObject.GetComponent<BuildingInfo>()) != null)
				{
					if (component3.m_lodObject != null)
					{
						component3.m_lodObject.SetActive(value: false);
					}
					if (package.version < 7)
					{
						LegacyMetroUtils.PatchBuildingPaths(component3);
					}
					Initialize(component3);
					if (component3.GetAI() is IntersectionAI)
					{
						loadedIntersections.Add(text2);
					}
				}
				else if ((component4 = gameObject.GetComponent<VehicleInfo>()) != null)
				{
					if (component4.m_lodObject != null)
					{
						component4.m_lodObject.SetActive(value: false);
					}
					Initialize(component4);
				}
				else if ((component5 = gameObject.GetComponent<CitizenInfo>()) != null)
				{
					if (component5.m_lodObject != null)
					{
						component5.m_lodObject.SetActive(value: false);
					}
					if (citizenMetaDatas.TryGetValue(text2, out var value2))
					{
						citizenMetaDatas.Remove(text2);
					}
					else
					{
						value2 = GetMetaDataFor(assetRef);
					}
					if (value2 != null && component5.InitializeCustomPrefab(value2))
					{
						component5.gameObject.SetActive(value: true);
						Initialize(component5);
					}
					else
					{
						prefabInfo = null;
						CODebugBase<LogChannel>.Warn(LogChannel.Modding, "Custom citizen [" + text2 + "] template not available in selected theme. Asset not added in game.");
					}
				}
				else if ((component6 = gameObject.GetComponent<NetInfo>()) != null)
				{
					Initialize(component6);
				}
				else
				{
					prefabInfo = null;
				}
				if (hasAssetDataExtensions && prefabInfo != null)
				{
					CallExtensions(assetRef, prefabInfo);
				}
			}
			finally
			{
				stack.Pop();
				assetCount++;
				Singleton<LoadingManager>.instance.m_loadingProfilerCustomAsset.EndLoading();
			}
		}


		/// <summary>
		/// Gets the asset metdata type for the given package.
		/// </summary>
		/// <param name="package">Pakcage</param>
		/// <returns>Asset metadata type (defaults to Building if type cannot be obtained)</returns>
		internal CustomAssetMetaData.Type GetPackageTypeFor(Package package)
		{
			// See if we've already done this one.
			if (packageTypes.TryGetValue(package, out CustomAssetMetaData.Type packageType))
			{
				return packageType;
			}

			// No existing record - get main asset metadata.
			CustomAssetMetaData mainMetaDataFor = GetMainMetaDataFor(package);
			if (mainMetaDataFor != null)
			{
				// Found it - add to dictionary.
				packageType = typeMap[(int)mainMetaDataFor.type];
				packageTypes.Add(package, packageType);
				return packageType;
			}

			// Fallback to buildng.
			Logging.Error("cannot get package type for package ", package.packagePath);
			return CustomAssetMetaData.Type.Building;
		}


		/// <summary>
		/// Reports a failed asset.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="package">Package</param>
		/// <param name="e">Asset execption</param>
		internal void AssetFailed(Package.Asset assetRef, Package package, Exception e)
		{
			// Get asset name.
			string text = assetRef?.fullName;
			if (text == null)
			{
				assetRef = FindMainAssetRef(package);
				text = assetRef?.fullName;
			}

			// Check that text isn't null and we haven't already recorded this asset as missing or having failed.
			if (text != null && LevelLoader.AddFailed(text))
			{
				// Add asset to list of recorded failures, if we're doing so.
				if (recordAssets)
				{
					Instance<Reports>.instance.AssetFailed(assetRef);
				}

				// Log and display failure.
				Logging.Error("asset failed: ", text);
				LoadingScreen.instance.DualSource?.CustomAssetFailed(ShortAssetName(text));
			}

			// Log the exception details as well.
			if (e != null)
			{
				Logging.LogException(e, "asset failure exception");
			}
		}


		/// <summary>
		/// Reports a missing asset.
		/// </summary>
		/// <param name="fullName">Asset full name</param>
		internal void AssetMissing(string fullName)
		{
			// Check that text isn't null and we haven't already recorded this asset as missing or having failed.
			if (fullName != null && LevelLoader.AddFailed(fullName))
			{
				Logging.KeyMessage("asset missing: ", fullName);

				// Display missing asset name unless we're supressing this one as a known missing asset.
				if (!hiddenAssets.Contains(fullName))
				{
					LoadingScreen.instance.DualSource?.CustomAssetNotFound(ShortAssetName(fullName));
				}
			}
		}


		/// <summary>
		/// Initializes prefabs.
		/// </summary>
		/// <typeparam name="T">Prefab type to initialize</typeparam>
		/// <param name="info">Prefab instance</param>
		/// <exception cref="Exception">PrefabLoadingException if prefab cannot be loaded</exception>
		private void Initialize<T>(T info) where T : PrefabInfo
		{
			// Local reference.
			LoadingManager loadingManager = Singleton<LoadingManager>.instance;

			// Save broken assets list.
			string brokenAssets = loadingManager.m_brokenAssets;

			// Initialize custom prefabs.
			PrefabCollection<T>.InitializePrefabs("Custom Assets", info, null);

			// Restore broken assets list.
			loadingManager.m_brokenAssets = brokenAssets;

			// Confirm prefab loaded.
			string name = info.name;
			if ((UnityEngine.Object)CustomDeserializer.FindLoaded<T>(name, tryName: false) == (UnityEngine.Object)null)
			{
				// Prefab not loaded - throw exception.
				Logging.Error(typeof(T).Name, " prefab ", name, " failed");
				throw new Exception(typeof(T).Name + " " + name + " failed");
			}
		}


		/// <summary>
		/// Compares two packages and determines load order.
		/// </summary>
		/// <param name="a">Package a</param>
		/// <param name="b">package b</param>
		/// <returns>Negative integer if a is first, positive integer if b is first, 0 if no difference</returns>
		private int PackageComparison(Package a, Package b)
		{
			// Compare names.
			int sortOrder = string.Compare(a.packageName, b.packageName);
			if (sortOrder != 0)
			{
				// Strings aren't identical; return sort order (< 0 if a lexically preceeds b, > 0 if b lexically preceeds a).
				return sortOrder;
			}

			// Names are identical; retrieve package main assets.
			Package.Asset assetA = a.Find(a.packageMainAsset);
			Package.Asset assetB = b.Find(b.packageMainAsset);

			// If either main asset is null, then order is irrelevant; return 0.
			if (assetA == null | assetB == null)
			{
				return 0;
			}

			// Check enabled status.
			bool aEnabled = IsEnabled(assetA);
			bool bEnabled = IsEnabled(assetB);
			if (aEnabled != bEnabled)
			{
				// Flags differ; if A is disabled, then return 1 (b first).
				if (!aEnabled)
				{
					return 1;
				}

				// Otherwise, return -1 (a first).
				return -1;
			}

			// Othewise, the package with the greatest offset goes first.
			return (int)assetB.offset - (int)assetA.offset;
		}


		/// <summary>
		/// Calculates asset loading queue.
		/// </summary>
		/// <param name="styleBuildings">Buildings in current district style</param>
		/// <returns>Package asset load queue as array</returns>
		private Package.Asset[] GetLoadQueue(HashSet<string> styleBuildings)
		{
			// Retrieve all custom asset packages to load.
			Package[] packages = new Package[0];
			try
			{
				packages = PackageManager.allPackages.Where((Package p) => p.FilterAssets(UserAssetType.CustomAssetMetaData).Any()).ToArray();

				// Sort list by package comparison.
				Array.Sort(packages, PackageComparison);
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception retrieving custom asset package list");
			}

			// Establish loading queues.
			// 0: Tree and citizen
			// 1: Props
			// 2: Roads, elevations, and pillars
			// 3: Buildings and sub-buildings
			// 4: Vehicles and trailers
			List<Package.Asset>[] queues = new List<Package.Asset>[5]
			{
				new List<Package.Asset>(32),
				new List<Package.Asset>(128),
				new List<Package.Asset>(32),
				new List<Package.Asset>(128),
				new List<Package.Asset>(64)
			};

			List<Package.Asset> assetRefList = new List<Package.Asset>(8);
			HashSet<string> assetNames = new HashSet<string>();
			string previousPackageName = string.Empty;
			SteamHelper.DLC_BitMask dLC_BitMask = ~SteamHelper.GetOwnedDLCMask();

			// 'Load enabled' and 'load used' settings.
			bool loadEnabled = LoadingScreenMod.Settings.settings.loadEnabled & !LoadingScreenMod.Settings.settings.enableDisable;
			bool loadUsed = LoadingScreenMod.Settings.settings.loadUsed;
			
			// Iterate through each package.
			foreach (Package package in packages)
			{
				Package.Asset finalAsset = null;
				try
				{
					Instance<CustomDeserializer>.instance.AddPackage(package);
					Package.Asset mainAsset = package.Find(package.packageMainAsset);
					string packageName = package.packageName;

					// Package is enabled if 'load enabled' is active and the main asset is enabled, or if the district style contains the main asset.
					bool enabled = (loadEnabled && IsEnabled(mainAsset)) || styleBuildings.Contains(mainAsset.fullName);

					// If not enabled, skip (unless we're loading used assets, or this package is already recorded as being in use).
					if (!enabled && (!loadUsed || !Instance<UsedAssets>.instance.GotPackage(packageName)))
					{
						continue;
					}
					
					CustomAssetMetaData assetRefs = GetAssetRefs(mainAsset, assetRefList);
					int assetCount = assetRefList.Count;
					finalAsset = assetRefList[assetCount - 1];
					CustomAssetMetaData.Type type = typeMap[(int)assetRefs.type];
					packageTypes.Add(package, type);

					// Check if the first asset in the package is in use.
					bool isUsed = loadUsed && Instance<UsedAssets>.instance.IsUsed(finalAsset, type);

					// Disable asset if relevant DLC isn't active.
					enabled &= (AssetImporterAssetTemplate.GetAssetDLCMask(assetRefs) & dLC_BitMask) == 0;

					// If we're loading used assets, and the main asset isn't used, but there are other assets in the package - check if any of the other assets are used.
					if (assetCount > 1 & !isUsed & loadUsed)
					{
						// Iterate through each asset in package.
						for (int i = 0; i < assetCount - 1; ++i)
						{
							if ((type != CustomAssetMetaData.Type.Road && Instance<UsedAssets>.instance.IsUsed(assetRefList[i], type)) ||
								(type == CustomAssetMetaData.Type.Road && Instance<UsedAssets>.instance.IsUsed(assetRefList[i], CustomAssetMetaData.Type.Road, CustomAssetMetaData.Type.Building)))
							{
								// Secondary asset is in use; mark the package as being in use.
								isUsed = true;
								break;
							}
						}
					}

					// If not enabled or in use, skip.
					if (!(enabled || isUsed))
					{
						continue;
					}

					// Record asset if we're doing so.
					if (recordAssets)
					{
						Instance<Reports>.instance.AddPackage(finalAsset, type, enabled, isUsed);
					}

					// Update previous package name reference.
					if (packageName != previousPackageName)
					{
						previousPackageName = packageName;

						// Finished with this package; clear asset names list. 
						assetNames.Clear();
					}

					// Iterate through all asset references in this package and add to queue (unless it's a duplicate).
					List<Package.Asset> assetQueue = queues[loadQueueIndex[(int)type]];
					for (int i = 0; i < assetCount - 1; ++i)
					{
						Package.Asset thisAsset = assetRefList[i];
						if (assetNames.Add(thisAsset.name) || !IsDuplicate(thisAsset, type, queues, isMainAssetRef: false))
						{
							assetQueue.Add(thisAsset);
						}
					}

					// Add final asset to queue, if not a duplicate.
					if (assetNames.Add(finalAsset.name) || !IsDuplicate(finalAsset, type, queues, isMainAssetRef: true))
					{
						assetQueue.Add(finalAsset);
						if (hasAssetDataExtensions)
						{
							metaDatas[finalAsset.fullName] = new SomeMetaData(assetRefs.userDataRef, assetRefs.name);
						}
						if (type == CustomAssetMetaData.Type.Citizen)
						{
							citizenMetaDatas[finalAsset.fullName] = assetRefs;
						}
					}
				}
				catch (Exception e)
				{
					AssetFailed(finalAsset, package, e);
				}
			}

			// Report duplicates.
			CheckSuspects();

			// Clear hashset.
			assetNames.Clear();
			assetNames = null;

			// Generate return queue.
			Package.Asset[] queue = new Package.Asset[queues.Sum((List<Package.Asset> assetList) => assetList.Count)];
			int index = 0;
			for (int i = 0; i < queues.Length; ++i)
			{
				queues[i].CopyTo(queue, index);
				index += queues[i].Count;

				// Clear each queue after copying.
				queues[i].Clear();
				queues[i] = null;
			}
			queues = null;
			return queue;
		}


		/// <summary>
		/// Gets asset metadata and references for the provided main asset.
		/// </summary>
		/// <param name="mainAsset">Main asset</param>
		/// <param name="assetRefs">List of asset references (will be cleared before new references are added)</param>
		/// <returns>Custom asset metadata</returns>
		private CustomAssetMetaData GetAssetRefs(Package.Asset mainAsset, List<Package.Asset> assetRefs)
		{
			// Read metadata and extract initial reference.
			CustomAssetMetaData customAssetMetaData = AssetDeserializer.InstantiateOne(mainAsset) as CustomAssetMetaData;
			Package.Asset assetRef = customAssetMetaData.assetRef;
			Package.Asset asset = null;
			assetRefs.Clear();

			// Iterate through each asset in package looking for suitable asset references.
			foreach (Package.Asset item in mainAsset.package)
			{
				if (item.type == Package.AssetType.Object)
				{
					asset = item;
					if ((object)item == assetRef)
					{
						break;
					}
				}
				else if (item.type == UserAssetType.CustomAssetMetaData)
				{
					if (asset != null)
					{
						string name = asset.name;
						int length = name.Length;
						// Any metadata names with " - " starting at length - 35 need secondary asset ref resolution.
						if (length < 35 || name[length - 34] != '-' || name[length - 35] != ' ' || name[length - 33] != ' ')
						{
							assetRefs.Add(asset);
							asset = null;
							break;
						}
						Logging.Message("getting secondary asset refs for asset ", name);
						GetSecondaryAssetRefs(mainAsset, assetRefs);
					}
					else
					{
						GetSecondaryAssetRefs(mainAsset, assetRefs);
					}
					break;
				}
			}

			// If we found a reference, add it to the list.
			if (assetRef != null)
			{
				assetRefs.Add(assetRef);
			}
			return customAssetMetaData;
		}


		/// <summary>
		/// Gets secondar asset references for the specified main asset.
		/// </summary>
		/// <param name="mainAsset">Main asset</param>
		/// <param name="assetRefs">List of secondary asset references (will be cleared before new references are added)</param>
		private void GetSecondaryAssetRefs(Package.Asset mainAsset, List<Package.Asset> assetRefs)
		{
			Logging.Message("!GetSecondaryAssetRefs: ", mainAsset.fullName);
			assetRefs.Clear();

			// Iterate through each CustomAssetMetaData asset in package.
			foreach (Package.Asset item in mainAsset.package.FilterAssets(UserAssetType.CustomAssetMetaData))
			{
				// If this is not the main asset, try to deserialise it and extract the reference.
				if ((object)item != mainAsset)
				{
					Package.Asset assetRef = (AssetDeserializer.InstantiateOne(item) as CustomAssetMetaData).assetRef;
					if (assetRef != null)
					{
						assetRefs.Add(assetRef);
						continue;
					}
					Logging.Error(" NULL asset ", mainAsset.fullName);
				}
			}
		}


		/// <summary>
		/// Returns the metadata type for the given asset and package type.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="packageType">Package type</param>
		/// <returns>Custom meta data type</returns>
		private CustomAssetMetaData.Type GetMetaTypeFor(Package.Asset assetRef, CustomAssetMetaData.Type packageType)
		{
			if (packageType != CustomAssetMetaData.Type.Road || IsMainAssetRef(assetRef))
			{
				return packageType;
			}
			return typeMap[(int)GetMetaDataFor(assetRef).type];
		}


		/// <summary>
		/// Returns the metadata type for the given asset and package type, with known main asset status.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="packageType">Package type</param>
		/// <param name="isMainAssetRef">True if the provided asset is the main asset, false otherwise</param>
		/// <returns>Custom meta data type</returns>
		private CustomAssetMetaData.Type GetMetaTypeFor(Package.Asset assetRef, CustomAssetMetaData.Type packageType, bool isMainAssetRef)
		{
			if (isMainAssetRef || packageType != CustomAssetMetaData.Type.Road)
			{
				return packageType;
			}
			return typeMap[(int)GetMetaDataFor(assetRef).type];
		}


		/// <summary>
		/// Gets the metadata for the specified asset.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <returns>Custom asset metadata (null if metadata was unable to be retrieved)</returns>
		private CustomAssetMetaData GetMetaDataFor(Package.Asset assetRef)
		{
			bool seeking = true;
			foreach (Package.Asset item in assetRef.package)
			{
				if (seeking)
				{
					if ((object)item == assetRef)
					{
						seeking = false;
					}
				}
				else if (item.type.m_Value == UserAssetType.CustomAssetMetaData)
				{
					CustomAssetMetaData customAssetMetaData = AssetDeserializer.InstantiateOne(item) as CustomAssetMetaData;
					if ((object)customAssetMetaData.assetRef == assetRef)
					{
						return customAssetMetaData;
					}
					break;
				}
			}

			Logging.Message("assetRef mismatch for asset ", assetRef.fullName);

			// Iterate through all custom asset metadata in package.
			foreach (Package.Asset item in assetRef.package.FilterAssets(UserAssetType.CustomAssetMetaData))
			{
				CustomAssetMetaData customAssetMetaData = AssetDeserializer.InstantiateOne(item) as CustomAssetMetaData;
				if ((object)customAssetMetaData.assetRef == assetRef)
				{
					return customAssetMetaData;
				}
			}

			Logging.Error("cannot get metadata for asset ", assetRef.fullName);
			return null;
		}


		/// <summary>
		/// Gets the main asset metadata for the specified package.
		/// </summary>
		/// <param name="package">Package</param>
		/// <returns>Custom asset metadata (null if metadata was unable to be retrieved)</returns>
		private CustomAssetMetaData GetMainMetaDataFor(Package package)
		{
			// Look for main asset.
			Package.Asset asset = package.Find(package.packageMainAsset);
			if (!(asset != null))
			{
				return null;
			}

			// Found it - deserialize and return.
			return AssetDeserializer.InstantiateOne(asset) as CustomAssetMetaData;
		}


		/// <summary>
		/// Checks to see if the given asset is a duplicate.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="packageType">Package type</param>
		/// <param name="loadingQueues">Asset loading queues</param>
		/// <param name="isMainAssetRef">True if this is the package's main asset, false otherwise</param>
		/// <returns>True if the given asset is a duplicate, false otherwise</returns>
		private bool IsDuplicate(Package.Asset assetRef, CustomAssetMetaData.Type packageType, List<Package.Asset>[] loadingQueues, bool isMainAssetRef)
		{
			// Get metadata.
			CustomAssetMetaData.Type metaTypeFor = GetMetaTypeFor(assetRef, packageType, isMainAssetRef);
			Dictionary<string, List<Package.Asset>> duplicateSuspects = suspects[(int)metaTypeFor];
			string fullName = assetRef.fullName;

			// See if we already have an entry for this asset.
			if (duplicateSuspects.TryGetValue(fullName, out List<Package.Asset> duplicateList))
			{
				// Existing entry found - add this asset to it.
				duplicateList.Add(assetRef);
			}
			else
			{
				// No existing entry.
				duplicateList = new List<Package.Asset>(2);

				// Check for any duplicates.
				FindDuplicates(assetRef, metaTypeFor, loadingQueues[loadQueueIndex[(int)metaTypeFor]], duplicateList);
				if (metaTypeFor == CustomAssetMetaData.Type.Building)
				{
					FindDuplicates(assetRef, metaTypeFor, loadingQueues[loadQueueIndex[9]], duplicateList);
				}

				// Did we find any duplicates?
				if (duplicateList.Count == 0)
				{
					// No duplicates found - return false.
					return false;
				}

				// Duplicates found - add to suspects dictionary.
				duplicateList.Add(assetRef);
				duplicateSuspects.Add(fullName, duplicateList);
			}

			// If we got here, duplicates were found - return true.
			return true;
		}


		/// <summary>
		/// Find any duplicates of the given asset.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="type">Asset type</param>
		/// <param name="loadingQueue">Loading queue</param>
		/// <param name="duplicates">List of duplicate assets (duplicates will be added to the list)</param>
		private void FindDuplicates(Package.Asset assetRef, CustomAssetMetaData.Type type, List<Package.Asset> loadingQueue, List<Package.Asset> duplicates)
		{
			string name = assetRef.name;
			string packageName = assetRef.package.packageName;

			// Iterate through the queue in reverse.
			int queueIndex = loadingQueue.Count - 1;
			while (queueIndex >= 0)
			{
				Package.Asset asset = loadingQueue[queueIndex];
				Package package = asset.package;

				// Check for package name match.
				if (package.packageName == packageName)
				{
					// If asset name and type match, this is a duplicate.
					if (asset.name == name && GetMetaTypeFor(asset, packageTypes[package]) == type)
					{
						duplicates.Insert(0, asset);
					}

					// Move onto next asset in queue.
					--queueIndex;
					continue;
				}

				// If package name no longer matches, stop.
				break;
			}
		}


		/// <summary>
		/// Checks for duplicate assets and reports them.
		/// </summary>
		private void CheckSuspects()
		{
			// Types to check.
			CustomAssetMetaData.Type[] array = new CustomAssetMetaData.Type[6]
			{
				CustomAssetMetaData.Type.Building,
				CustomAssetMetaData.Type.Prop,
				CustomAssetMetaData.Type.Tree,
				CustomAssetMetaData.Type.Vehicle,
				CustomAssetMetaData.Type.Citizen,
				CustomAssetMetaData.Type.Road
			};

			// Iterate through each type.
			foreach (CustomAssetMetaData.Type type in array)
			{
				// Iterate through all duplicate suspects recorded for that type.
				foreach (KeyValuePair<string, List<Package.Asset>> item in suspects[(int)type])
				{
					// Check for multiple entries where more than one is enabled.
					List<Package.Asset> duplicateList = item.Value;
					if (duplicateList.Select((Package.Asset a) => a.checksum).Distinct().Count() > 1 && duplicateList.Where((Package.Asset a) => IsEnabled(a.package)).Count() != 1)
					{
						// Report duplicate.
						ReportDuplicate(item.Key, duplicateList);
					}
				}
			}
		}


		/// <summary>
		/// Checks to see if the given package is enabled.
		/// </summary>
		/// <param name="package">Package</param>
		/// <returns>True if enabled, false otherwise</returns>
		private bool IsEnabled(Package package)
		{
			Package.Asset asset = package.Find(package.packageMainAsset);
			if (!(asset == null))
			{
				return IsEnabled(asset);
			}
			return true;
		}


		/// <summary>
		/// Checks to see if the given main asset is enabled.
		/// </summary>
		/// <param name="mainAsset">Package main asset</param>
		/// <returns>True if enabled, false otherwise</returns>
		private bool IsEnabled(Package.Asset mainAsset) => !boolValues.TryGetValue(mainAsset.checksum + ".enabled", out bool value) | value;


		/// <summary>
		/// Removes asset metadata record and calls OnAssetLoaded extensions as required.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="info">Prefab info</param>

		private void CallExtensions(Package.Asset assetRef, PrefabInfo info)
		{
			// Check for recorded metadata.
			string fullName = assetRef.fullName;
			if (metaDatas.TryGetValue(fullName, out SomeMetaData metaData))
			{
				// Metadata retrieved - remove from dictionary (no longer needed).
				metaDatas.Remove(fullName);
			}
			else if (IsMainAssetRef(assetRef))
			{
				// No metadata record found and the asset is the main asset - get the metadata for this asset.
				CustomAssetMetaData mainMetaDataFor = GetMainMetaDataFor(assetRef.package);
				metaData = new SomeMetaData(mainMetaDataFor.userDataRef, mainMetaDataFor.name);
			}

			// Was metadata found?
			if (metaData.userDataRef != null)
			{
				// Metadata found - deserialize the asset data.
				AssetDataWrapper.UserAssetData userAssetData = AssetDeserializer.InstantiateOne(metaData.userDataRef) as AssetDataWrapper.UserAssetData;
				if (userAssetData == null)
				{
					// Create empty instance if none was deserialized.
					userAssetData = new AssetDataWrapper.UserAssetData();
				}

				// Execute OnAssetLoaded extensions.
				Singleton<LoadingManager>.instance.m_AssetDataWrapper.OnAssetLoaded(metaData.name, info, userAssetData);
			}
		}


		/// <summary>
		/// Checks to see if the given asset is a pillar or road elevation.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="knownRoad">True if the asset is a road type, false otherwise</param>
		/// <returns>True if the asset is a pillar or road elevation, false otherwise</returns>
		private bool IsPillarOrElevation(Package.Asset assetRef, bool knownRoad)
		{
			// If this is a known road asset, then anything other than the main asset is a pillar or elevation.
			if (knownRoad)
			{
				return !IsMainAssetRef(assetRef);
			}

			// Iterate through each custom asset item in the package.
			int counter = 0;
			foreach (Package.Asset item in assetRef.package.FilterAssets(UserAssetType.CustomAssetMetaData))
			{
				_ = item;
				if (++counter > 1)
				{
					break;
				}
			}

			// If more than one item was returned, return based on the metadata type of the provided asset.
			if (counter != 1)
			{
				return GetMetaDataFor(assetRef).type >= CustomAssetMetaData.Type.RoadElevation;
			}

			// Default is false (not a pillar or elevation).
			return false;
		}


		/// <summary>
		/// Returns the name of a pillar or elevation.
		/// </summary>
		/// <param name="packageName">Package name</param>
		/// <param name="name">Asset name</param>
		/// <returns></returns>
		private string PillarOrElevationName(string packageName, string assetName) => packageName + "." + PackageHelper.StripName(assetName);


		/// <summary>
		/// Checks to see if the provided asset is the package's main asset.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <returns>True if the asset is the package's main asset, false otherwise</returns>
		private bool IsMainAssetRef(Package.Asset assetRef) => (object)FindMainAssetRef(assetRef.package) == assetRef;


		/// <summary>
		/// Triggers LSM report generation and disposes of sharing instance.
		/// </summary>
		private void Report()
		{
			LoadingScreenMod.Settings settings = LoadingScreenMod.Settings.settings;
			if (settings.loadUsed)
			{
				Instance<UsedAssets>.instance.ReportMissingAssets();
			}
			if (recordAssets)
			{
				if (settings.reportAssets)
				{
					Instance<Reports>.instance.Save(hiddenAssets, Instance<Sharing>.instance.texhit, Instance<Sharing>.instance.mathit, Instance<Sharing>.instance.meshit);
				}
				if (settings.hideAssets)
				{
					settings.SaveHiddenAssets(hiddenAssets, Instance<Reports>.instance.GetMissing(), Instance<Reports>.instance.GetDuplicates());
				}
				if (!settings.enableDisable)
				{
					Instance<Reports>.instance.ClearAssets();
				}
			}

			// Dispose of sharing instance (no longer needed).
			Instance<Sharing>.instance.Dispose();
		}


		/// <summary>
		/// Struips leading package number from the given name.
		/// </summary>
		/// <param name="fullName_Data">Name</param>
		/// <returns>Name stripped of leading package number and period (unchanged input if no leading package number)</returns>
		private string ShortAssetName(string fullName_Data)
		{
			int periodIndex = fullName_Data.IndexOf('.');
			if (periodIndex >= 0 && periodIndex < fullName_Data.Length - 1)
			{
				fullName_Data = fullName_Data.Substring(periodIndex + 1);
			}
			return ShortName(fullName_Data);
		}



		/// <summary>
		/// Reports duplicate assets.
		/// </summary>
		/// <param name="fullName">Asset full name</param>
		/// <param name="assets">List of duplicates</param>
		private void ReportDuplicate(string fullName, List<Package.Asset> assets)
		{
			// Report duplicate assets if we're doing that.
			if (recordAssets)
			{
				Instance<Reports>.instance.Duplicate(assets);
			}

			// Log message
			Logging.Message("duplicate name", fullName);

			// Display missing asset name unless we're supressing this one as a known missing asset.
			if (!hiddenAssets.Contains(fullName))
			{
				LoadingScreen.instance.DualSource?.CustomAssetDuplicate(ShortAssetName(fullName));
			}
		}


		/// <summary>
		/// Enables all assets used in the current save, and disables all others.
		/// </summary>
		private void EnableDisableAssets()
		{
			try
			{
				// Report if we're set to do so.
				if (!LoadingScreenMod.Settings.settings.reportAssets)
				{
					Instance<Reports>.instance.SetIndirectUsages();
				}

				// Iterate through all packages.
				foreach (object item in Instance<CustomDeserializer>.instance.AllPackages())
				{
					// Single-package items.
					if (item is Package singlePackage)
					{
						EnableDisableAssets(singlePackage);
						continue;
					}

					// Otherwise, iterate through each item in a multi-package list
					foreach (Package package in item as List<Package>)
					{
						EnableDisableAssets(package);
					}
				}

				// Report cleared assets.
				Instance<Reports>.instance.ClearAssets();

				// Mark game settigns file as dirty.
				GameSettings.FindSettingsFileByName(PackageManager.assetStateSettingsFile).MarkDirty();
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception enabling/disabling assset");
			}
		}


		/// <summary>
		/// Enables or disables assets in the given package.
		/// </summary>
		/// <param name="package">Package</param>
		private void EnableDisableAssets(Package package)
		{
			// Determine if package is in use.
			bool isUsed = Instance<Reports>.instance.IsUsed(FindMainAssetRef(package));

			// Enable/disable each item in the package based on used status.
			foreach (Package.Asset item in package.FilterAssets(UserAssetType.CustomAssetMetaData))
			{
				string key = item.checksum + ".enabled";
				if (isUsed)
				{
					boolValues.Remove(key);
				}
				else
				{
					boolValues[key] = false;
				}
			}
		}
	}
}
