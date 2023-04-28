// <copyright file="CustomDeserializer.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using AlgernonCommons;
    using ColossalFramework.Importers;
    using ColossalFramework.Packaging;
    using ColossalFramework.UI;
    using LoadingScreenMod;
    using UnityEngine;

    /// <summary>
    /// Custom asset deserializer.
    /// Based on PackageHelper.CustomDeserialize.
    /// </summary>
    public sealed class CustomDeserializer
    {
        // Asset type constants.
        private const int TypeModInfo = 0;
        private const int TypeInt32 = 3;
        private const int TypeString = 6;
        private const int TypeBuildingInfoProp = 10;
        private const int TypePackageAsset = 13;
        private const int TypeVector3 = 16;
        private const int TypeNetInfoLane = 20;
        private const int TypeItemClass = 23;
        private const int TypeDateTime = 26;
        private const int TypeTextureAtlas = 29;
        private const int TypeVector4 = 32;
        private const int TypeBuildingInfoPath = 35;
        private const int TypeNetInfoNode = 39;
        private const int TypeNetInfoSegment = 42;
        private const int TypeNetInfo = 45;
        private const int TypeMilestone = 48;
        private const int TypeVector2 = 52;
        private const int TypeMessageInfo = 56;
        private const int TypeVehicleInfoEffect = 59;
        private const int TypeTransportInfo = 62;
        private const int TypeVehicleInfoTrailer = 65;
        private const int TypeVehicleInfoDoor = 70;
        private const int TypeBuildingInfo = 74;
        private const int TypeBuildingInfoSub = 77;
        private const int TypeBuildingSpawnPoint = 80;
        private const int TypeDepotSpawnPoint = 81;
        private const int TypePropInfo = 85;
        private const int TypePropInfoEffect = 86;
        private const int TypePropInfoVariation = 89;
        private const int TypeVehicleInfoMesh = 95;
        private const int TypeBuildingInfoMesh = 103;
        private const int TypeSpecialPlace = 109;
        private const int TypeTreeInfoVariation = 116;
        private const int TypeDictStringByte = 125;
        private const int TypeParkingSpace = 3232;
        private const int TypeDisasterSettings = 11386;

        // Active instance.
        private static CustomDeserializer s_instance;

        // Settings.
        private readonly bool _loadUsed = LSMRSettings.LoadUsed;
        private readonly bool _recordUsed = LSMRSettings.RecordAssets & LSMRSettings.LoadUsed;
        private readonly bool _optimizeThumbs = LSMRSettings.OptimizeThumbs;

        // Package data.
        private Package.Asset[] _assets;
        private Dictionary<string, object> _packages = new Dictionary<string, object>(256);
        private Dictionary<Type, int> _types;

        // Textures.
        private Texture2D _largeSprite;
        private Texture2D _smallSprite;
        private Texture2D _halfSprite;
        private AtlasObj _prevAtlasObj;
        private ConcurrentQueue<AtlasObj> _atlasIn;
        private ConcurrentQueue<AtlasObj> _atlasOut;

        // Skipping.
        private bool _skipProps = LSMRSettings.SkipPrefabs;
        private HashSet<string> _skippedProps;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDeserializer"/> class.
        /// </summary>
        internal CustomDeserializer()
        {
            // Assign instance reference.
            s_instance = this;

            // Initialize type dictionary.
            _types = new Dictionary<Type, int>(64)
            {
                [typeof(ModInfo)] = TypeModInfo,
                [typeof(TerrainModify.Surface)] = TypeInt32,
#pragma warning disable CS0618 // Type or member is obsolete
                [typeof(SteamHelper.DLC_BitMask)] = TypeInt32,
#pragma warning restore CS0618 // Type or member is obsolete
                [typeof(ItemClass.Availability)] = TypeInt32,
                [typeof(ItemClass.Placement)] = TypeInt32,
                [typeof(ItemClass.Service)] = TypeInt32,
                [typeof(ItemClass.SubService)] = TypeInt32,
                [typeof(ItemClass.Level)] = TypeInt32,
                [typeof(CustomAssetMetaData.Type)] = TypeInt32,
                [typeof(VehicleInfo.VehicleType)] = TypeInt32,
                [typeof(BuildingInfo.PlacementMode)] = TypeInt32,
                [typeof(BuildingInfo.ZoningMode)] = TypeInt32,
                [typeof(Vehicle.Flags)] = TypeInt32,
                [typeof(CitizenInstance.Flags)] = TypeInt32,
                [typeof(NetInfo.ConnectGroup)] = TypeInt32,
                [typeof(PropInfo.DoorType)] = TypeInt32,
                [typeof(LightEffect.BlinkType)] = TypeInt32,
                [typeof(EventManager.EventType)] = TypeInt32,
                [typeof(string)] = TypeString,
                [typeof(BuildingInfo.Prop)] = TypeBuildingInfoProp,
                [typeof(Package.Asset)] = TypePackageAsset,
                [typeof(Vector3)] = TypeVector3,
                [typeof(NetInfo.Lane)] = TypeNetInfoLane,
                [typeof(ItemClass)] = TypeItemClass,
                [typeof(DateTime)] = TypeDateTime,
                [typeof(UITextureAtlas)] = TypeTextureAtlas,
                [typeof(Vector4)] = TypeVector4,
                [typeof(BuildingInfo.PathInfo)] = TypeBuildingInfoPath,
                [typeof(NetInfo.Node)] = TypeNetInfoNode,
                [typeof(NetInfo.Segment)] = TypeNetInfoSegment,
                [typeof(NetInfo)] = TypeNetInfo,
                [typeof(ManualMilestone)] = TypeMilestone,
                [typeof(CombinedMilestone)] = TypeMilestone,
                [typeof(Vector2)] = TypeVector2,
                [typeof(MessageInfo)] = TypeMessageInfo,
                [typeof(VehicleInfo.Effect)] = TypeVehicleInfoEffect,
                [typeof(TransportInfo)] = TypeTransportInfo,
                [typeof(VehicleInfo.VehicleTrailer)] = TypeVehicleInfoTrailer,
                [typeof(VehicleInfo.VehicleDoor)] = TypeVehicleInfoDoor,
                [typeof(BuildingInfo)] = TypeBuildingInfo,
                [typeof(BuildingInfo.SubInfo)] = TypeBuildingInfoSub,
                [typeof(BuildingAI.SpawnPoint)] = TypeBuildingSpawnPoint,
                [typeof(DepotAI.SpawnPoint)] = TypeDepotSpawnPoint,
                [typeof(PropInfo)] = TypePropInfo,
                [typeof(PropInfo.Effect)] = TypePropInfoEffect,
                [typeof(PropInfo.Variation)] = TypePropInfoVariation,
                [typeof(VehicleInfo.MeshInfo)] = TypeVehicleInfoMesh,
                [typeof(BuildingInfo.MeshInfo)] = TypeBuildingInfoMesh,
                [typeof(PropInfo.SpecialPlace)] = TypeSpecialPlace,
                [typeof(TreeInfo.Variation)] = TypeTreeInfoVariation,
                [typeof(Dictionary<string, byte[]>)] = TypeDictStringByte,
                [typeof(PropInfo.ParkingSpace)] = TypeParkingSpace,
                [typeof(DisasterProperties.DisasterSettings)] = TypeDisasterSettings,
            };

            // Setup thumbnal optimization, if enabled.
            if (_optimizeThumbs)
            {
                _largeSprite = new Texture2D(492, 147, TextureFormat.ARGB32, mipmap: false, linear: false);
                _smallSprite = new Texture2D(109, 100, TextureFormat.ARGB32, mipmap: false, linear: false);
                _halfSprite = new Texture2D(66, 66, TextureFormat.ARGB32, mipmap: false, linear: false);
                _largeSprite.name = "tooltip";
                _smallSprite.name = "thumb";
                _halfSprite.name = "thumbDisabled";
                _smallSprite.SetPixels32(Enumerable.Repeat(new Color32(64, 64, 64, byte.MaxValue), 10900).ToArray());
                _smallSprite.Apply(updateMipmaps: false);
                _atlasIn = new ConcurrentQueue<AtlasObj>(64);
                _atlasOut = new ConcurrentQueue<AtlasObj>(32);
                new Thread(AtlasWorker).Start();
            }
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        internal static CustomDeserializer Instance => s_instance;

        /// <summary>
        /// Gets the active asset array.
        /// </summary>
        internal static Package.Asset[] Assets
        {
            get
            {
                // Ensure instance.
                if (s_instance == null)
                {
                    s_instance = new CustomDeserializer();
                }

                // Create new list if it hasn't already been done.
                if (s_instance._assets == null)
                {
                    s_instance._assets = s_instance.FilterAssets(Package.AssetType.Object);
                }

                return s_instance._assets;
            }
        }

        /// <summary>
        /// Gets the current collection of recorded packages.
        /// </summary>
        internal ICollection<object> AllPackages => _packages.Values;

        /// <summary>
        /// Gets the hashset of skipped props.
        /// </summary>
        private HashSet<string> SkippedProps
        {
            get
            {
                // Create new list if it hasn't already been done.
                if (_skippedProps == null)
                {
                    _skippedProps = PrefabLoader.Instance?.SkippedProps;
                    if (_skippedProps == null || _skippedProps.Count == 0)
                    {
                        // Disable skipping if there's no skip list.
                        _skipProps = false;
                        _skippedProps = new HashSet<string>();
                    }
                }

                return _skippedProps;
            }
        }

        /// <summary>
        /// Attempts to find an asset by name.
        /// </summary>
        /// <param name="fullName">Asset name.</param>
        /// <returns>Matching Package.Asset (null if not found).</returns>
        public static Package.Asset FindAsset(string fullName)
        {
            if (LevelLoader.HasAssetFailed(fullName))
            {
                return null;
            }

            // Check for package number in fullName.
            int periodIndex = fullName.IndexOf('.');
            if (periodIndex >= 0)
            {
                // Try recorded packages.
                string assetName = fullName.Substring(periodIndex + 1);
                if (s_instance._packages.TryGetValue(fullName.Substring(0, periodIndex), out object packageObject))
                {
                    // If result is a single package, return that.
                    if (packageObject is Package package)
                    {
                        return package.Find(assetName);
                    }

                    // If result wisas a list of packages, iterate through to find the target.
                    if (packageObject is List<Package> packageList)
                    {
                        for (int i = 0; i < packageList.Count; ++i)
                        {
                            if (packageList[i].Find(assetName) is Package.Asset asset)
                            {
                                return asset;
                            }
                        }
                    }
                }
            }
            else
            {
                // No package number in fullName - search asset array.
                Package.Asset[] array = Assets;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (fullName.Equals(array[i].name))
                    {
                        return array[i];
                    }
                }
            }

            // If we got here, we didn't get a result; return null.
            return null;
        }

        /// <summary>
        /// Attempts to find a loaded PrefabInfo by name.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="fullName">Full prefab name.</param>
        /// <param name="tryName">True to permit a name search via assets.</param>
        /// <returns>PrefabInfo (null if unavailable).</returns>
        internal static TPrefab FindLoaded<TPrefab>(string fullName, bool tryName = true)
            where TPrefab : PrefabInfo
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            // Try dictionary first.
            Dictionary<string, PrefabCollection<TPrefab>.PrefabData> prefabDict = Fetch<TPrefab>.PrefabDict;
            if (prefabDict.TryGetValue(fullName, out PrefabCollection<TPrefab>.PrefabData prefab))
            {
                return prefab.m_prefab;
            }

            // If tryName is set and the fullanme contains a package number, and the asset hasn't failed, then serach assets for a name match.
            if (tryName && fullName.IndexOf('.') < 0 && !LevelLoader.HasAssetFailed(fullName))
            {
                Package.Asset[] array = Assets;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (fullName == array[i].name && prefabDict.TryGetValue(array[i].package.packageName + "." + fullName, out prefab))
                    {
                        return prefab.m_prefab;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks to see if all known prefabs of the given type are loaded (excluding missing or failed prefabs).
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="fullNames">Hashset of full prefab names.</param>
        /// <param name="ignore">Hashset of names to ignore.</param>
        /// <returns>True if all known prefabs of the given type are loaded, missing, or failed; false otherwise (loading not yet completed).</returns>
        internal static bool AllAvailable<TPrefab>(HashSet<string> fullNames, HashSet<string> ignore)
            where TPrefab : PrefabInfo
        {
            // Iterate through each name provided.
            foreach (string fullName in fullNames)
            {
                // If we're not ignoring this name, and we can't find the matching prefab, then we're still going.
                if (!ignore.Contains(fullName) && FindLoaded<TPrefab>(fullName, tryName: false) == null)
                {
                    Logging.Message("not loaded yet:", fullName);
                    return false;
                }
            }

            // If we got here, all matches were found; we're done.
            return true;
        }

        /// <summary>
        /// Adds a package to the list of packages to deserialize.
        /// </summary>
        /// <param name="package">Package to add.</param>
        internal void AddPackage(Package package)
        {
            string packageName = package.packageName;

            if (string.IsNullOrEmpty(packageName))
            {
                Logging.Error("no package name for package ", package.packagePath);
            }

            // Check for existing entry.
            else if (_packages.TryGetValue(packageName, out object value))
            {
                // If there's an exising list, add this package to it.
                if (value is List<Package> packageList)
                {
                    packageList.Add(package);
                    return;
                }

                // No existing list - create a new one.
                _packages[packageName] = new List<Package>(4)
                {
                    value as Package,
                    package,
                };
            }
            else
            {
                // No existing entry - create one.
                _packages.Add(packageName, package);
            }
        }

        /// <summary>
        /// Checks to see if the package list contains the given package.
        /// </summary>
        /// <param name="packageName">Package name to check.</param>
        /// <returns>True if there's already an entry for this package name, false otherwise.</returns>
        internal bool HasPackages(string packageName) => _packages.ContainsKey(packageName);

        /// <summary>
        /// Returns the list of packages associated with the given package name.
        /// </summary>
        /// <param name="packageName">Package name.</param>
        /// <returns>List of packages (null if none).</returns>
        internal List<Package> GetPackages(string packageName)
        {
            if (_packages.TryGetValue(packageName, out object value))
            {
                // Convert any single package values to a list.
                if (value is Package package)
                {
                    return new List<Package>(1) { package };
                }

                // Otherwise just return the list.
                return value as List<Package>;
            }

            // No matching package entry - return null.
            return null;
        }

        /// <summary>
        /// Completes loading.
        /// </summary>
        internal void SetCompleted()
        {
            if (_optimizeThumbs)
            {
                _atlasIn.SetCompleted();
            }
        }

        /// <summary>
        /// Clears all data and disposes of the instance.
        /// </summary>
        internal void Dispose()
        {
            Fetch<BuildingInfo>.Dispose();
            Fetch<PropInfo>.Dispose();
            Fetch<TreeInfo>.Dispose();
            Fetch<VehicleInfo>.Dispose();
            Fetch<CitizenInfo>.Dispose();
            Fetch<NetInfo>.Dispose();
            _types?.Clear();
            _types = null;
            _atlasIn = null;
            _atlasOut = null;
            _prevAtlasObj = null;
            _largeSprite = null;
            _smallSprite = null;
            _halfSprite = null;
            _assets = null;
            _packages.Clear();
            _packages = null;
            _skippedProps = null;
            s_instance = null;
        }

        /// <summary>
        /// Deserializes a package.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="type">Asset type.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>Deserialized object.</returns>
        internal object CustomDeserialize(Package package, Type type, PackageReader reader)
        {
            // Check for supported type.
            if (!_types.TryGetValue(type, out var value))
            {
                return null;
            }

            // Deserialize based on type.
            switch (value)
            {
                case TypeModInfo:
                    ModInfo modInfo = default;
                    modInfo.modName = reader.ReadString();
                    modInfo.modWorkshopID = reader.ReadUInt64();
                    return modInfo;

                case TypeInt32:
                    return reader.ReadInt32();

                case TypeString:
                    return reader.ReadString();

                case TypeBuildingInfoProp:
                    return ReadBuildingInfoProp(reader);

                case TypePackageAsset:
                    return ReadPackageAsset(package, reader);

                case TypeVector3:
                    return reader.ReadVector3();

                case TypeNetInfoLane:
                    return ReadNetInfoLane(package, reader);

                case TypeItemClass:
                    return ReadItemClass(reader);

                case TypeDateTime:
                    return reader.ReadDateTime();

                case TypeTextureAtlas:
                    if (!_optimizeThumbs)
                    {
                        return PackageHelper.CustomDeserialize(package, type, reader);
                    }

                    return ReadUITextureAtlas(package, reader);

                case TypeVector4:
                    return reader.ReadVector4();

                case TypeBuildingInfoPath:
                    return ReadBuildingInfoPathInfo(package, reader);

                case TypeNetInfoNode:
                    return ReadNetInfoNode(package, reader);

                case TypeNetInfoSegment:
                    return ReadNetInfoSegment(package, reader);

                case TypeNetInfo:
                    return ReadNetInfo(package, reader);

                case TypeMilestone:
                    return ReadMilestone(reader);

                case TypeVector2:
                    return reader.ReadVector2();

                case TypeMessageInfo:
                    return ReadMessageInfo(reader);

                case TypeVehicleInfoEffect:
                    return ReadVehicleInfoEffect(package, reader);

                case TypeTransportInfo:
                    return ReadTransportInfo(reader);

                case TypeVehicleInfoTrailer:
                    return ReadVehicleInfoVehicleTrailer(package, reader);

                case TypeVehicleInfoDoor:
                    return ReadVehicleInfoVehicleDoor(reader);

                case TypeBuildingInfo:
                    return ReadBuildingInfo(package, reader);

                case TypeBuildingInfoSub:
                    return ReadBuildingInfoSubInfo(package, reader);

                case TypeBuildingSpawnPoint:
                    return ReadBuildingAISpawnPoint(reader);

                case TypeDepotSpawnPoint:
                    return ReadDepotAISpawnPoint(reader);

                case TypePropInfo:
                    return ReadPropInfo(package, reader);

                case TypePropInfoEffect:
                    return ReadPropInfoEffect(reader);

                case TypePropInfoVariation:
                    return ReadPropInfoVariation(package, reader);

                case TypeVehicleInfoMesh:
                    return ReadVehicleInfoMeshInfo(package, reader);

                case TypeBuildingInfoMesh:
                    return ReadBuildingInfoMeshInfo(package, reader);

                case TypeSpecialPlace:
                    return ReadPropInfoSpecialPlace(reader);

                case TypeTreeInfoVariation:
                    return ReadTreeInfoVariation(package, reader);

                case TypeDictStringByte:
                    return ReadDictStringByteArray(reader);

                case TypeParkingSpace:
                    return ReadPropInfoParkingSpace(reader);

                case TypeDisasterSettings:
                    return ReadDisasterPropertiesDisasterSettings(reader);

                default:
                    // Unsupported type.
                    return null;
            }
        }

        /// <summary>
        /// Receives available thumbnail atlases.
        /// </summary>
        internal void ReceiveAvailableThumbnails()
        {
            AtlasObj[] array = _atlasOut.DequeueAll();
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ReceiveAtlas(array[i]);
                }
            }
        }

        /// <summary>
        /// Receives remaining thumbnail atlases.
        /// </summary>
        internal void ReceiveRemainingThumbnails()
        {
            while (_atlasOut.Dequeue(out AtlasObj atlasObj))
            {
                ReceiveAtlas(atlasObj);
            }
        }

        /// <summary>
        /// Receives an atlas object for processing.
        /// </summary>
        /// <param name="atlasObj">Atlas object to receive.</param>
        private static void ReceiveAtlas(AtlasObj atlasObj)
        {
            UITextureAtlas atlas = atlasObj.atlas;
            if (atlasObj.bytes != null && atlas.material != null)
            {
                Texture2D texture2D = new Texture2D(atlasObj.width, atlasObj.height, atlasObj.format, mipmap: false, linear: false);
                texture2D.LoadRawTextureData(atlasObj.bytes);
                texture2D.Apply(updateMipmaps: false);
                atlas.material.mainTexture = texture2D;
                atlas.AddSprites(atlasObj.sprites);
            }
        }

        /// <summary>
        /// Deserializes a BuildingInfo.Prop.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New BuildingInfo.Prop.</returns>
        private BuildingInfo.Prop ReadBuildingInfoProp(PackageReader reader)
        {
            string propName = reader.ReadString();
            string treeName = reader.ReadString();
            PropInfo propInfo = GetProp(propName);
            TreeInfo treeInfo = Get<TreeInfo>(treeName);

            // LSM insertion - recording the used asset.
            if (_recordUsed)
            {
                if (!string.IsNullOrEmpty(propName))
                {
                    AddReference(propInfo, propName, CustomAssetMetaData.Type.Prop);
                }

                if (!string.IsNullOrEmpty(treeName))
                {
                    AddReference(treeInfo, treeName, CustomAssetMetaData.Type.Tree);
                }
            }

            return new BuildingInfo.Prop
            {
                m_prop = propInfo,
                m_tree = treeInfo,
                m_position = reader.ReadVector3(),
                m_angle = reader.ReadSingle(),
                m_probability = reader.ReadInt32(),
                m_fixedHeight = reader.ReadBoolean(),
            };
        }

        /// <summary>
        /// Deserializes a Package.Asset.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New Package.Asset.</returns>
        private Package.Asset ReadPackageAsset(Package package, PackageReader reader)
        {
            string checksum = reader.ReadString();
            Package.Asset asset = package.FindByChecksum(checksum);
            if (!(asset == null) || package.version >= 3)
            {
                return asset;
            }

            return PackageManager.FindAssetByChecksum(checksum);
        }

        /// <summary>
        /// Deserializes a NetInfo.Lane.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetInfo.Lane.</returns>
        private NetInfo.Lane ReadNetInfoLane(Package package, PackageReader reader)
        {
            NetInfo.Lane lane = new NetInfo.Lane
            {
                m_position = reader.ReadSingle(),
                m_width = reader.ReadSingle(),
                m_verticalOffset = reader.ReadSingle(),
                m_stopOffset = reader.ReadSingle(),
                m_speedLimit = reader.ReadSingle(),
                m_direction = (NetInfo.Direction)reader.ReadInt32(),
                m_laneType = (NetInfo.LaneType)reader.ReadInt32(),
                m_vehicleType = (VehicleInfo.VehicleType)reader.ReadInt32(),
                m_stopType = (VehicleInfo.VehicleType)reader.ReadInt32(),
                m_laneProps = ReadNetLaneProps(package, reader),
                m_allowConnect = reader.ReadBoolean(),
                m_useTerrainHeight = reader.ReadBoolean(),
                m_centerPlatform = reader.ReadBoolean(),
                m_elevated = reader.ReadBoolean(),
            };

            // 1.15.1 addition.
            if (package.version >= 9)
            {
                lane.m_vehicleCategoryPart1 = (VehicleInfo.VehicleCategoryPart1)reader.ReadInt32();
                lane.m_vehicleCategoryPart2 = (VehicleInfo.VehicleCategoryPart2)reader.ReadInt32();
            }
            else
            {
                lane.m_vehicleCategoryPart1 = VehicleInfo.VehicleCategoryPart1.All;
                lane.m_vehicleCategoryPart2 = VehicleInfo.VehicleCategoryPart2.All;
            }

            return lane;
        }

        /// <summary>
        /// Deserializes an ItemClass.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New ItemClass.</returns>
        private ItemClass ReadItemClass(PackageReader reader) => ItemClassCollection.FindClass(reader.ReadString());

        /// <summary>
        /// Deserializes a UITexture.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetInfo.Lane.</returns>
        private UITextureAtlas ReadUITextureAtlas(Package package, PackageReader reader)
        {
            Package.Asset current = AssetLoader.Instance.Current;

            // Check if this one has already been read.
            if ((object)current == _prevAtlasObj?.asset)
            {
                SkipUITextureAtlas(package, reader);
                return _prevAtlasObj.atlas;
            }

            // Atlas.
            UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            uITextureAtlas.name = reader.ReadString();
            AtlasObj atlasObj = _prevAtlasObj = new AtlasObj
            {
                asset = current,
                atlas = uITextureAtlas,
            };

            if (package.version > 3)
            {
                atlasObj.bytes = reader.ReadBytes(reader.ReadInt32());
            }
            else
            {
                atlasObj.width = reader.ReadInt32();
                atlasObj.height = reader.ReadInt32();
                atlasObj.bytes = ReadColorArray(reader);
            }

            // Shader.
            string shaderName = reader.ReadString();
            Shader shader = Shader.Find(shaderName);

            // Material.
            Material material = null;
            if (shader != null)
            {
                material = new Material(shader);
            }
            else
            {
                Logging.Error("texture atlas shader *", shaderName, "* not found.");
            }

            uITextureAtlas.material = material;
            uITextureAtlas.padding = reader.ReadInt32();

            // Sprites.
            int numSprites = reader.ReadInt32();
            atlasObj.sprites = new List<UITextureAtlas.SpriteInfo>(numSprites);
            for (int i = 0; i < numSprites; i++)
            {
                Rect region = new Rect(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo
                {
                    name = reader.ReadString(),
                    region = region,
                };
                atlasObj.sprites.Add(item);
            }

            // Add to queue.
            _atlasIn.Enqueue(atlasObj);
            ReceiveAvailableThumbnails();
            return uITextureAtlas;
        }

        /// <summary>
        /// Skips deserializing a UI texture atlas (if it's already been deserialized).
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        private void SkipUITextureAtlas(Package package, PackageReader reader)
        {
            reader.ReadString();
            if (package.version > 3)
            {
                reader.ReadBytes(reader.ReadInt32());
            }
            else
            {
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadBytes(reader.ReadInt32() << 4);
            }

            reader.ReadString();
            reader.ReadInt32();
            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadString();
            }
        }

        /// <summary>
        /// Deserializes a color array.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New color array.</returns>
        private byte[] ReadColorArray(PackageReader reader)
        {
            int length = reader.ReadInt32();
            byte[] array = new byte[length << 2];
            int index = 0;
            for (int i = 0; i < length; i++)
            {
                array[index++] = (byte)(reader.ReadSingle() * 255f);
                array[index++] = (byte)(reader.ReadSingle() * 255f);
                array[index++] = (byte)(reader.ReadSingle() * 255f);
                array[index++] = (byte)(reader.ReadSingle() * 255f);
            }

            return array;
        }

        /// <summary>
        /// Deserializes a BuildingInfo.PathInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New BuildingInfo.PathInfo.</returns>
        private BuildingInfo.PathInfo ReadBuildingInfoPathInfo(Package package, PackageReader reader)
        {
            string netName = reader.ReadString();
            NetInfo netInfo = Get<NetInfo>(netName);
            if (_recordUsed && !string.IsNullOrEmpty(netName))
            {
                AddReference(netInfo, netName, CustomAssetMetaData.Type.Road);
            }

            BuildingInfo.PathInfo pathInfo = new BuildingInfo.PathInfo()
            {
                m_netInfo = netInfo,
                m_nodes = reader.ReadVector3Array(),
                m_curveTargets = reader.ReadVector3Array(),
                m_invertSegments = reader.ReadBoolean(),
                m_maxSnapDistance = reader.ReadSingle(),
            };

            if (package.version >= 5)
            {
                pathInfo.m_forbidLaneConnection = reader.ReadBooleanArray();
                pathInfo.m_trafficLights = (BuildingInfo.TrafficLights[])(object)reader.ReadInt32Array();
                pathInfo.m_yieldSigns = reader.ReadBooleanArray();
            }

            return pathInfo;
        }

        /// <summary>
        /// Deserializes a NetInfo.Node.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetInfo.Node.</returns>
        private NetInfo.Node ReadNetInfoNode(Package package, PackageReader reader)
        {
            NetInfo.Node node = new NetInfo.Node();

            // Implement sharing.
            Sharing sharing = Instance<Sharing>.instance;
            string text = reader.ReadString();
            node.m_mesh = string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, package, isMain: true);
            text = reader.ReadString();
            node.m_material = string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, package, isMain: true);
            text = reader.ReadString();
            node.m_lodMesh = string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, package, isMain: false);
            text = reader.ReadString();
            node.m_lodMaterial = string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, package, isMain: false);

            // Flags etc.
            node.m_flagsRequired = (NetNode.Flags)reader.ReadInt32();
            node.m_flagsForbidden = (NetNode.Flags)reader.ReadInt32();
            node.m_connectGroup = (NetInfo.ConnectGroup)reader.ReadInt32();
            node.m_directConnect = reader.ReadBoolean();
            node.m_emptyTransparent = reader.ReadBoolean();

            // 1.15.1 addition.
            if (package.version >= 9)
            {
                node.m_flagsRequired2 = (NetNode.Flags2)reader.ReadInt32();
                node.m_flagsForbidden2 = (NetNode.Flags2)reader.ReadInt32();
                node.m_tagsRequired = reader.ReadStringArray();
                node.m_tagsForbidden = reader.ReadStringArray();
                node.m_forbidAnyTags = reader.ReadBoolean();
                node.m_minSameTags = reader.ReadByte();
                node.m_maxSameTags = reader.ReadByte();
                node.m_minOtherTags = reader.ReadByte();
                node.m_maxOtherTags = reader.ReadByte();
            }
            else
            {
                node.m_flagsRequired2 = NetNode.Flags2.None;
                node.m_flagsForbidden2 = NetNode.Flags2.None;
                node.m_tagsRequired = new string[0];
                node.m_tagsForbidden = new string[0];
                node.m_forbidAnyTags = false;
                node.m_minSameTags = 0;
                node.m_maxSameTags = 7;
                node.m_minOtherTags = 0;
                node.m_maxOtherTags = 7;
            }

            return node;
        }

        /// <summary>
        /// Deserializes a NetInfo.Segment.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetInfo.Segment.</returns>
        private NetInfo.Segment ReadNetInfoSegment(Package package, PackageReader reader)
        {
            NetInfo.Segment segment = new NetInfo.Segment();

            // Implement sharing.
            Sharing sharing = Instance<Sharing>.instance;
            string text = reader.ReadString();
            segment.m_mesh = string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, package, isMain: true);
            text = reader.ReadString();
            segment.m_material = string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, package, isMain: true);
            text = reader.ReadString();
            segment.m_lodMesh = string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, package, isMain: false);
            text = reader.ReadString();
            segment.m_lodMaterial = string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, package, isMain: false);

            // Flags etc.
            segment.m_forwardRequired = (NetSegment.Flags)reader.ReadInt32();
            segment.m_forwardForbidden = (NetSegment.Flags)reader.ReadInt32();
            segment.m_backwardRequired = (NetSegment.Flags)reader.ReadInt32();
            segment.m_backwardForbidden = (NetSegment.Flags)reader.ReadInt32();
            segment.m_emptyTransparent = reader.ReadBoolean();
            segment.m_disableBendNodes = reader.ReadBoolean();

            return segment;
        }

        /// <summary>
        /// Deserializes a NetInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetInfo.</returns>
        private NetInfo ReadNetInfo(Package package, PackageReader reader)
        {
            string netName = reader.ReadString();

            // Roads.
            if (AssetLoader.Instance.GetPackageTypeFor(package) == CustomAssetMetaData.Type.Road)
            {
                return Get<NetInfo>(package, netName);
            }

            // Other nets.
            return Get<NetInfo>(netName);
        }

        /// <summary>
        /// Deserializes a MilestoneInfo.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New MilestoneInfo.</returns>
        private MilestoneInfo ReadMilestone(PackageReader reader) => MilestoneCollection.FindMilestone(reader.ReadString());

        /// <summary>
        /// Deserializes a MessageInfo.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New MessageInfo.</returns>
        private MessageInfo ReadMessageInfo(PackageReader reader)
        {
            // Read data.
            MessageInfo messageInfo = new MessageInfo
            {
                m_firstID1 = reader.ReadString(),
                m_firstID2 = reader.ReadString(),
                m_repeatID1 = reader.ReadString(),
                m_repeatID2 = reader.ReadString(),
            };

            // Convert empty strings to null.
            if (messageInfo.m_firstID1.Equals(string.Empty))
            {
                messageInfo.m_firstID1 = null;
            }

            if (messageInfo.m_firstID2.Equals(string.Empty))
            {
                messageInfo.m_firstID2 = null;
            }

            if (messageInfo.m_repeatID1.Equals(string.Empty))
            {
                messageInfo.m_repeatID1 = null;
            }

            if (messageInfo.m_repeatID2.Equals(string.Empty))
            {
                messageInfo.m_repeatID2 = null;
            }

            return messageInfo;
        }

        /// <summary>
        /// Deserializes a VehicleInfo.Effect.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New VehicleInfo.Effect.</returns>
        private VehicleInfo.Effect ReadVehicleInfoEffect(Package package, PackageReader reader)
        {
            VehicleInfo.Effect effect = new VehicleInfo.Effect
            {
                m_effect = EffectCollection.FindEffect(reader.ReadString()),
                m_parkedFlagsForbidden = (VehicleParked.Flags)reader.ReadInt32(),
                m_parkedFlagsRequired = (VehicleParked.Flags)reader.ReadInt32(),
                m_vehicleFlagsForbidden = (Vehicle.Flags)reader.ReadInt32(),
                m_vehicleFlagsRequired = (Vehicle.Flags)reader.ReadInt32(),
            };

            // 1.16.1.
            if (package.version >= 10)
            {
                effect.m_vehicleFlagsForbidden2 = (Vehicle.Flags2)reader.ReadInt32();
                effect.m_vehicleFlagsRequired2 = (Vehicle.Flags2)reader.ReadInt32();
            }
            else
            {
                effect.m_vehicleFlagsForbidden2 = 0;
                effect.m_vehicleFlagsRequired2 = 0;
            }

            return effect;
        }

        /// <summary>
        /// Deserializes a TransportInfo.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New TransportInfo.</returns>
        private TransportInfo ReadTransportInfo(PackageReader reader) => PrefabCollection<TransportInfo>.FindLoaded(reader.ReadString());

        /// <summary>
        /// Deserializes a VehicleInfo.VehicleTrailer.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New VehicleInfo.VehicleTrailer.</returns>
        private VehicleInfo.VehicleTrailer ReadVehicleInfoVehicleTrailer(Package package, PackageReader reader)
        {
            string vehicleName = reader.ReadString();
            return new VehicleInfo.VehicleTrailer
            {
                m_info = Get<VehicleInfo>(package, package.packageName + "." + vehicleName, vehicleName, tryName: false),
                m_probability = reader.ReadInt32(),
                m_invertProbability = reader.ReadInt32(),
            };
        }

        /// <summary>
        /// Deserializes a VehicleInfo.VehicleDoor.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New VehicleInfo.VehicleDoor.</returns>
        private VehicleInfo.VehicleDoor ReadVehicleInfoVehicleDoor(PackageReader reader)
        {
            return new VehicleInfo.VehicleDoor
            {
                m_type = (VehicleInfo.DoorType)reader.ReadInt32(),
                m_location = reader.ReadVector3(),
            };
        }

        /// <summary>
        /// Deserializes a BuildingInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New BuildingInfo.</returns>
        private BuildingInfo ReadBuildingInfo(Package package, PackageReader reader)
        {
            string buildingName = reader.ReadString();

            // Yes, really.
            if (AssetLoader.Instance.GetPackageTypeFor(package) == CustomAssetMetaData.Type.Road)
            {
                return Get<BuildingInfo>(package, buildingName);
            }

            return Get<BuildingInfo>(buildingName);
        }

        /// <summary>
        /// Deserializes a BuildingInfo.SubInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New BuildingInfo.SubInfo.</returns>
        private BuildingInfo.SubInfo ReadBuildingInfoSubInfo(Package package, PackageReader reader)
        {
            string buildingName = reader.ReadString();
            string fullName = package.packageName + "." + buildingName;
            BuildingInfo buildingInfo = null;

            // Recursion check.
            if (fullName == AssetLoader.Instance.Current.fullName || buildingName == AssetLoader.Instance.Current.fullName)
            {
                Logging.Error(fullName, "wants to be a sub-building for itself");
            }
            else
            {
                buildingInfo = Get<BuildingInfo>(package, fullName, buildingName, tryName: true);
            }

            return new BuildingInfo.SubInfo
            {
                m_buildingInfo = buildingInfo,
                m_position = reader.ReadVector3(),
                m_angle = reader.ReadSingle(),
                m_fixedHeight = reader.ReadBoolean(),
            };
        }

        /// <summary>
        /// Deserializes a BuildingAI.SpawnPoint.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New BuildingAI.SpawnPoint.</returns>
        private BuildingAI.SpawnPoint ReadBuildingAISpawnPoint(PackageReader reader)
        {
            return new BuildingAI.SpawnPoint
            {
                m_position = reader.ReadVector3(),
                m_target = reader.ReadVector3(),
                m_vehicleCategoryPart1 = (VehicleInfo.VehicleCategoryPart1)reader.ReadInt32(),
                m_vehicleCategoryPart2 = (VehicleInfo.VehicleCategoryPart2)reader.ReadInt32(),
            };
        }

        /// <summary>
        /// Deserializes a DepotAI.SpawnPoint.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New DepotAI.SpawnPoint.</returns>
        private DepotAI.SpawnPoint ReadDepotAISpawnPoint(PackageReader reader)
        {
            return new DepotAI.SpawnPoint
            {
                m_position = reader.ReadVector3(),
                m_target = reader.ReadVector3(),
            };
        }

        /// <summary>
        /// Deserializes a PropInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New PropInfo.</returns>
        private PropInfo ReadPropInfo(Package package, PackageReader reader)
        {
            // 1.15.1 addition.
            if (package.version >= 9)
            {
                return PrefabCollection<PropInfo>.FindLoaded(reader.ReadString());
            }

            return null;
        }

        /// <summary>
        /// Deserializes a PropInfo.Effect.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New PropInfo.Effect.</returns>
        private PropInfo.Effect ReadPropInfoEffect(PackageReader reader)
        {
            return new PropInfo.Effect
            {
                m_effect = EffectCollection.FindEffect(reader.ReadString()),
                m_position = reader.ReadVector3(),
                m_direction = reader.ReadVector3(),
            };
        }

        /// <summary>
        /// Deserializes a PropInfo.Variation.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New PropInfo.Variation.</returns>
        private PropInfo.Variation ReadPropInfoVariation(Package package, PackageReader reader)
        {
            string propName = reader.ReadString();
            string fullName = package.packageName + "." + propName;
            PropInfo prop = null;

            // Recursion check.
            if (fullName == AssetLoader.Instance.Current.fullName)
            {
                Util.DebugPrint("Warning:", fullName, "wants to be a prop variation for itself");
            }
            else
            {
                prop = Get<PropInfo>(package, fullName, propName, tryName: false);
            }

            return new PropInfo.Variation
            {
                m_prop = prop,
                m_probability = reader.ReadInt32(),
            };
        }

        /// <summary>
        /// Deserializes a VehicleInfo.MeshInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New VehicleInfo.MeshInfo.</returns>
        private VehicleInfo.MeshInfo ReadVehicleInfoMeshInfo(Package package, PackageReader reader)
        {
            VehicleInfo.MeshInfo meshInfo = new VehicleInfo.MeshInfo();
            string checksum = reader.ReadString();
            if (!string.IsNullOrEmpty(checksum))
            {
                GameObject gameObject = AssetDeserializer.Instantiate(package.FindByChecksum(checksum), isMain: true, isTop: false) as GameObject;
                meshInfo.m_subInfo = gameObject.GetComponent<VehicleInfoBase>();
                gameObject.SetActive(value: false);
                meshInfo.m_subInfo.m_lodObject?.SetActive(value: false);
            }
            else
            {
                meshInfo.m_subInfo = null;
            }

            // Flags etc.
            meshInfo.m_vehicleFlagsForbidden = (Vehicle.Flags)reader.ReadInt32();
            meshInfo.m_vehicleFlagsRequired = (Vehicle.Flags)reader.ReadInt32();
            meshInfo.m_parkedFlagsForbidden = (VehicleParked.Flags)reader.ReadInt32();
            meshInfo.m_parkedFlagsRequired = (VehicleParked.Flags)reader.ReadInt32();

            return meshInfo;
        }

        /// <summary>
        /// Deserializes a BuildingInfo.MeshInfo.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New BuildingInfo.MeshInfo.</returns>
        private BuildingInfo.MeshInfo ReadBuildingInfoMeshInfo(Package package, PackageReader reader)
        {
            BuildingInfo.MeshInfo meshInfo = new BuildingInfo.MeshInfo();
            string checksum = reader.ReadString();
            if (!string.IsNullOrEmpty(checksum))
            {
                GameObject gameObject = AssetDeserializer.Instantiate(package.FindByChecksum(checksum), isMain: true, isTop: false) as GameObject;
                meshInfo.m_subInfo = gameObject.GetComponent<BuildingInfoBase>();
                gameObject.SetActive(value: false);
                meshInfo.m_subInfo.m_lodObject?.SetActive(value: false);
            }
            else
            {
                meshInfo.m_subInfo = null;
            }

            // Flags etc.
            meshInfo.m_flagsForbidden = (Building.Flags)reader.ReadInt32();
            meshInfo.m_flagsRequired = (Building.Flags)reader.ReadInt32();

            // 1.15.1 addition.
            if (package.version >= 9)
            {
                meshInfo.m_flagsRequired2 = (Building.Flags2)reader.ReadInt32();
            }
            else
            {
                meshInfo.m_flagsRequired2 = Building.Flags2.None;
            }

            meshInfo.m_position = reader.ReadVector3();
            meshInfo.m_angle = reader.ReadSingle();

            return meshInfo;
        }

        /// <summary>
        /// Deserializes a PropInfo.SpecialPlace.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New PropInfo.SpecialPlace.</returns>
        private PropInfo.SpecialPlace ReadPropInfoSpecialPlace(PackageReader reader)
        {
            return new PropInfo.SpecialPlace
            {
                m_specialFlags = (CitizenInstance.Flags)reader.ReadInt32(),
                m_position = reader.ReadVector3(),
                m_direction = reader.ReadVector3(),
            };
        }

        /// <summary>
        /// Deserializes a TreeInfo.Variation.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New TreeInfo.Variation.</returns>
        private TreeInfo.Variation ReadTreeInfoVariation(Package package, PackageReader reader)
        {
            string treeName = reader.ReadString();
            string fullName = package.packageName + "." + treeName;
            TreeInfo tree = null;

            // Recursion check.
            if (fullName == AssetLoader.Instance.Current.fullName)
            {
                Util.DebugPrint("Warning:", fullName, "wants to be a tree variation for itself");
            }
            else
            {
                tree = Get<TreeInfo>(package, fullName, treeName, tryName: false);
            }

            return new TreeInfo.Variation
            {
                m_tree = tree,
                m_probability = reader.ReadInt32(),
            };
        }

        /// <summary>
        /// Deserializes a dictionary (string, byte[]).
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New dictionary.</returns>
        private Dictionary<string, byte[]> ReadDictStringByteArray(PackageReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>(count);

            // Read keys and values.
            for (int i = 0; i < count; ++i)
            {
                string key = reader.ReadString();
                dictionary[key] = reader.ReadBytes(reader.ReadInt32());
            }

            return dictionary;
        }

        /// <summary>
        /// Deserializes a PropInfo.ParkingSpace.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New PropInfo.ParkingSpace.</returns>
        private PropInfo.ParkingSpace ReadPropInfoParkingSpace(PackageReader reader)
        {
            return new PropInfo.ParkingSpace
            {
                m_position = reader.ReadVector3(),
                m_direction = reader.ReadVector3(),
                m_size = reader.ReadVector3(),
            };
        }

        /// <summary>
        /// Deserializes a DisasterProperties.DisasterSettings.
        /// </summary>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New DisasterProperties.DisasterSettings.</returns>
        private DisasterProperties.DisasterSettings ReadDisasterPropertiesDisasterSettings(PackageReader reader)
        {
            return new DisasterProperties.DisasterSettings
            {
                m_disasterName = reader.ReadString(),
                m_randomProbability = reader.ReadInt32(),
            };
        }

        /// <summary>
        /// Deserializes a NetLaneProps.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetLaneProps.</returns>
        private NetLaneProps ReadNetLaneProps(Package package, PackageReader reader)
        {
            int count = reader.ReadInt32();
            NetLaneProps netLaneProps = ScriptableObject.CreateInstance<NetLaneProps>();
            netLaneProps.m_props = new NetLaneProps.Prop[count];
            for (int i = 0; i < count; ++i)
            {
                netLaneProps.m_props[i] = ReadNetLaneProp(package, reader);
            }

            return netLaneProps;
        }

        /// <summary>
        /// Deserializes a NetLaneProp.
        /// </summary>
        /// <param name="package">Package to deserialize.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <returns>New NetLaneProp.</returns>
        private NetLaneProps.Prop ReadNetLaneProp(Package package, PackageReader reader)
        {
            string propName;
            string treeName;
            NetLaneProps.Prop prop = new NetLaneProps.Prop
            {
                m_flagsRequired = (NetLane.Flags)reader.ReadInt32(),
                m_flagsForbidden = (NetLane.Flags)reader.ReadInt32(),
                m_startFlagsRequired = (NetNode.Flags)reader.ReadInt32(),
                m_startFlagsForbidden = (NetNode.Flags)reader.ReadInt32(),
                m_endFlagsRequired = (NetNode.Flags)reader.ReadInt32(),
                m_endFlagsForbidden = (NetNode.Flags)reader.ReadInt32(),
                m_colorMode = (NetLaneProps.ColorMode)reader.ReadInt32(),
                m_prop = GetProp(propName = reader.ReadString()),
                m_tree = Get<TreeInfo>(treeName = reader.ReadString()),
                m_position = reader.ReadVector3(),
                m_angle = reader.ReadSingle(),
                m_segmentOffset = reader.ReadSingle(),
                m_repeatDistance = reader.ReadSingle(),
                m_minLength = reader.ReadSingle(),
                m_cornerAngle = reader.ReadSingle(),
                m_probability = reader.ReadInt32(),
            };

            // Record used assets, if enabled.
            if (_recordUsed)
            {
                if (!string.IsNullOrEmpty(propName))
                {
                    AddReference(prop.m_prop, propName, CustomAssetMetaData.Type.Prop);
                }

                if (!string.IsNullOrEmpty(treeName))
                {
                    AddReference(prop.m_tree, treeName, CustomAssetMetaData.Type.Tree);
                }
            }

            // Upgradeable flag (1.14 road tree replacement).
            if (package.version >= 8)
            {
                prop.m_upgradable = reader.ReadBoolean();
            }
            else
            {
                prop.m_upgradable = prop.m_tree != null && prop.m_repeatDistance > 0f;
            }

            // 1.15.1 additions.
            if (package.version >= 9)
            {
                prop.m_startFlagsRequired2 = (NetNode.Flags2)reader.ReadInt32();
                prop.m_startFlagsForbidden2 = (NetNode.Flags2)reader.ReadInt32();
                prop.m_endFlagsRequired2 = (NetNode.Flags2)reader.ReadInt32();
                prop.m_endFlagsForbidden2 = (NetNode.Flags2)reader.ReadInt32();
            }
            else
            {
                prop.m_startFlagsRequired2 = NetNode.Flags2.None;
                prop.m_startFlagsForbidden2 = NetNode.Flags2.None;
                prop.m_endFlagsRequired2 = NetNode.Flags2.None;
                prop.m_endFlagsForbidden2 = NetNode.Flags2.None;
            }

            return prop;
        }

        /// <summary>
        /// Gets a PropInfo from the full name provided, unless it's skipped.
        /// </summary>
        /// <param name="fullName">Full prop name.</param>
        /// <returns>PropInfo (null if unavailable or skipped).</returns>
        private PropInfo GetProp(string fullName)
        {
            if (string.IsNullOrEmpty(fullName) || (_skipProps && SkippedProps.Contains(fullName)))
            {
                return null;
            }

            return Get<PropInfo>(fullName);
        }

        /// <summary>
        /// Gets a PrefabInfo from the full name provided, including any legacy prefab name conversion for NetInfos.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="fullName">Full prefab name.</param>
        /// <returns>PrefabInfo (null if unavailable).</returns>
        private TPrefab Get<TPrefab>(string fullName)
            where TPrefab : PrefabInfo
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            TPrefab prefab = FindLoaded<TPrefab>(fullName);

            // If there's a missing NetInfo, call ResolveLegacyPrefab to see if it maps to a new prefab name.
            if (prefab == null && typeof(TPrefab) == typeof(NetInfo))
            {
                string newName = BuildConfig.ResolveLegacyPrefab(fullName);
                prefab = FindLoaded<TPrefab>(newName);
            }

            // Not loaded - this asset is used, so try to load it (if 'load used assets' is enabled).
            if (prefab == null && s_instance.Load(ref fullName, FindAsset(fullName), typeof(TPrefab)))
            {
                prefab = FindLoaded<TPrefab>(fullName);
            }

            return prefab;
        }

        /// <summary>
        /// Gets a PrefabInfo from the asset name provided from the specified packaage.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="package">Package.</param>
        /// <param name="name">Asset name.</param>
        /// <returns>PrefabInfo (null if unavailable).</returns>
        private TPrefab Get<TPrefab>(Package package, string name)
            where TPrefab : PrefabInfo
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            string assetName = PackageHelper.StripName(name);
            TPrefab prefab = FindLoaded<TPrefab>(package.packageName + "." + assetName);

            if (prefab == null)
            {
                // No prefab found; search the package directly.
                Package.Asset asset = package.Find(assetName);
                if (asset != null)
                {
                    // Not loaded - this asset is used, so try to load it (if 'load used assets' is enabled).
                    string fullName = asset.fullName;
                    if (s_instance.Load(ref fullName, asset, typeof(TPrefab)))
                    {
                        prefab = FindLoaded<TPrefab>(fullName);
                    }
                }
                else
                {
                    prefab = Get<TPrefab>(name);
                }
            }

            return prefab;
        }

        /// <summary>
        /// Gets a PrefabInfo, trying multiple approaches if necessary.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="package">Package.</param>
        /// <param name="fullName">Full prefab name.</param>
        /// <param name="name">Asset name.</param>
        /// <param name="tryName">True to permit a name search.</param>
        /// <returns>PrefabInfo (null if unavailable).</returns>
        private TPrefab Get<TPrefab>(Package package, string fullName, string name, bool tryName)
            where TPrefab : PrefabInfo
        {
            TPrefab prefab = FindLoaded<TPrefab>(fullName);

            // If no prefab found, try a name search if that option is enabled.
            if (prefab == null && tryName)
            {
                prefab = FindLoaded<TPrefab>(name);
            }

            if (prefab == null)
            {
                // No prefab found - try the package.
                Package.Asset asset = package.Find(name);
                if (tryName && asset == null)
                {
                    asset = FindAsset(name);
                }

                // Determine fullname based on whether a matching asset was found.
                if (asset != null)
                {
                    fullName = asset.fullName;
                }
                else if (name.IndexOf('.') >= 0)
                {
                    fullName = name;
                }

                // Not loaded - this asset is used, so try to load it (if 'load used assets' is enabled).
                if (s_instance.Load(ref fullName, asset, typeof(TPrefab)))
                {
                    prefab = FindLoaded<TPrefab>(fullName);
                }
            }

            return prefab;
        }

        /// <summary>
        /// Attempts to load a prefab from an asset if 'load used assets' is enabled.
        /// </summary>
        /// <param name="fullName">Prefab full name.</param>
        /// <param name="asset">Asset.</param>
        /// <param name="type">Prefab type (used for error reporting).</param>
        /// <returns>True if loading was successful, false otherwise.</returns>
        private bool Load(ref string fullName, Package.Asset asset, Type type)
        {
            // Only attempt to load if the 'load used assets' setting is enabled.
            if (_loadUsed)
            {
                // Check to see if the asset exists.
                if (asset != null)
                {
                    try
                    {
                        fullName = asset.fullName;
                        if (fullName != AssetLoader.Instance.Current.fullName && !LevelLoader.HasAssetFailed(fullName))
                        {
                            // Record used asset if we're doing so.
                            if (_recordUsed)
                            {
                                Instance<Reports>.instance.AddPackage(asset.package);
                            }

                            // Load the asset and return successful loading.
                            AssetLoader.Instance.LoadImpl(asset);
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        // Asset loading failed - report failed asset.
                        AssetLoader.Instance.AssetFailed(asset, asset.package, e);
                    }
                }
                else
                {
                    // Asset doesn't exist - report missing asset.
                    AssetLoader.Instance.AssetMissing(fullName, type);
                }
            }
            else
            {
                // Not attempting to load used assets - report failed asset.
                LevelLoader.AddFailed(fullName);
            }

            // If we got here, loading wasn't successful.
            return false;
        }

        /// <summary>
        /// Records a custom asset prefab reference.
        /// </summary>
        /// <param name="prefab">Prefab info (null to match on name and type).</param>
        /// <param name="fullName">Prefab full name.</param>
        /// <param name="type">Asset type.</param>
        private void AddReference(PrefabInfo prefab, string fullName, CustomAssetMetaData.Type type)
        {
            if (prefab == null)
            {
                // Handle any prop skipping.
                if (type != CustomAssetMetaData.Type.Prop || !_skipProps || !SkippedProps.Contains(fullName))
                {
                    // Try to find containing asset.
                    Package.Asset asset = FindContainer();
                    if (asset != null)
                    {
                        Instance<Reports>.instance.AddReference(asset, fullName, type);
                    }
                }
            }
            else
            {
                // Only interested in custom content.
                if (!prefab.m_isCustomContent)
                {
                    return;
                }

                // Try to find containing asset.
                string prefabName = prefab.name;
                Package.Asset asset = FindContainer();
                if (asset != null && !string.IsNullOrEmpty(prefabName))
                {
                    string packageName = asset.package.packageName;
                    int periodIndex = prefabName.IndexOf('.');
                    string mainFullName;
                    if (periodIndex >= 0 && (periodIndex != packageName.Length || !prefabName.StartsWith(packageName)) && (mainFullName = FindMain(prefabName)) != null)
                    {
                        Instance<Reports>.instance.AddReference(asset, mainFullName, type);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the current package asset.
        /// </summary>
        /// <returns>Current package asset.</returns>
        private Package.Asset FindContainer()
        {
            Package.Asset current = AssetLoader.Instance.Current;
            if (Instance<Reports>.instance.IsKnown(current))
            {
                return current;
            }

            return KnownMainAsset(current.package);
        }

        /// <summary>
        /// Finds the main asset name of the current package asset.
        /// </summary>
        /// <param name="fullName">Prefab full name.</param>
        /// <returns>Name of main package asset (null if unsuccessful).</returns>
        private string FindMain(string fullName)
        {
            if (Instance<Reports>.instance.IsKnown(fullName))
            {
                return fullName;
            }

            Package.Asset asset = FindAsset(fullName);
            if (asset != null)
            {
                return KnownMainAsset(asset.package)?.fullName;
            }

            return null;
        }

        /// <summary>
        /// Attempts to find the known main asset for the given package.
        /// </summary>
        /// <param name="package">Package.</param>
        /// <returns>Known main asset (null if none recorded).</returns>
        private Package.Asset KnownMainAsset(Package package)
        {
            Package.Asset asset = AssetLoader.FindMainAsset(package);
            if (string.IsNullOrEmpty(asset?.fullName) || !Instance<Reports>.instance.IsKnown(asset))
            {
                return null;
            }

            return asset;
        }

        /// <summary>
        /// Filters the current asset array.
        /// </summary>
        /// <param name="assetType">Asset type to match.</param>
        /// <returns>Array of filtered assets.</returns>
        private Package.Asset[] FilterAssets(Package.AssetType assetType)
        {
            List<Package.Asset> list = new List<Package.Asset>(256);
            try
            {
                foreach (Package.Asset item in PackageManager.FilterAssets(assetType))
                {
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception filtering asset array");
            }

            Package.Asset[] result = list.ToArray();
            list.Clear();
            return result;
        }

        /// <summary>
        /// Altas worker thread.
        /// </summary>
        private void AtlasWorker()
        {
            Thread.CurrentThread.Name = "AtlasWorker";

            // Process through the incoming queue.
            while (_atlasIn.Dequeue(out AtlasObj atlas))
            {
                try
                {
                    ProcessAtlas(atlas);
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "AtlasWorker exception processing ", atlas.asset.fullName);
                    atlas.bytes = null;
                }

                // Add atlast to outgoing queue.
                _atlasOut.Enqueue(atlas);
            }

            // Set atlas processing as completed.
            _atlasOut.SetCompleted();
        }

        /// <summary>
        /// Process a thumbnail atlas.
        /// </summary>
        /// <param name="atlas">Atlas to process.</param>
        private void ProcessAtlas(AtlasObj atlas)
        {
            // Atlas data and dimensions.
            byte[] atlasBytes;
            int width;
            int height;

            if (atlas.width == 0)
            {
                Image image = new Image(atlas.bytes);
                atlasBytes = image.GetAllPixels();
                width = image.width;
                height = image.height;
                atlas.format = image.format;
            }
            else
            {
                atlasBytes = atlas.bytes;
                width = atlas.width;
                height = atlas.height;
                atlas.format = TextureFormat.RGBA32;
            }

            // Sprites.
            List<UITextureAtlas.SpriteInfo> sprites = atlas.sprites;
            UITextureAtlas.SpriteInfo spriteInfo = null;
            UITextureAtlas.SpriteInfo spriteInfo2 = null;
            int num = 0;
            int num2 = 0;
            int num3 = 9999;
            for (int i = 0; i < sprites.Count; ++i)
            {
                UITextureAtlas.SpriteInfo spriteInfo3 = sprites[i];
                int spriteWidth = Mathf.FloorToInt((float)width * spriteInfo3.region.width);
                int spriteHeight = Mathf.FloorToInt((float)height * spriteInfo3.region.height);
                if (spriteWidth == 109 && spriteHeight == 100)
                {
                    num++;
                    spriteInfo3.texture = _smallSprite;
                    if (spriteInfo3.name.Length < num3)
                    {
                        num3 = spriteInfo3.name.Length;
                        spriteInfo = spriteInfo3;
                    }
                }
                else if (spriteWidth == 492 && spriteHeight == 147)
                {
                    num2++;
                    spriteInfo3.texture = _largeSprite;
                    spriteInfo2 = spriteInfo3;
                }
                else
                {
                    num2++;
                    spriteInfo3.texture = _smallSprite;
                }
            }

            int padding = atlas.atlas.padding;
            if (spriteInfo == null || num > 5 || num2 > 1 || padding > 2 || width > 512 || (spriteInfo2 == null && num2 > 0))
            {
                Logging.Message("processing atlas ", atlas.asset.fullName);
                atlas.bytes = atlasBytes;
                atlas.width = width;
                atlas.height = height;
                return;
            }

            int num6 = (spriteInfo2 != null) ? 256 : 128;
            byte[] array2 = new byte[num6 << 11];
            int num7 = 0;
            int num8 = 0;
            if (spriteInfo2 != null)
            {
                CopySprite(atlasBytes, width, Mathf.FloorToInt((float)width * spriteInfo2.region.x), Mathf.FloorToInt((float)height * spriteInfo2.region.y), array2, 512, 0, 0, 75264, 1);
                SetRect(spriteInfo2, 0, 0, 492, 147, 512f, num6);
                num8 = 147 + padding;
            }

            for (int j = 0; j < sprites.Count; j++)
            {
                UITextureAtlas.SpriteInfo spriteInfo4 = sprites[j];
                if (spriteInfo4 != spriteInfo2 && spriteInfo4.name.StartsWith(spriteInfo.name))
                {
                    if (spriteInfo4.name.EndsWith("Disabled") && spriteInfo4 != spriteInfo)
                    {
                        CopyHalf(atlasBytes, width, Mathf.FloorToInt((float)width * spriteInfo4.region.x), Mathf.FloorToInt((float)height * spriteInfo4.region.y), array2, 512, num7, num8, 66, 66);
                        SetRect(spriteInfo4, num7, num8, 66, 66, 512f, num6);
                        spriteInfo4.texture = _halfSprite;
                        num7 += 66 + padding;
                    }
                    else
                    {
                        CopySprite(atlasBytes, width, Mathf.FloorToInt((float)width * spriteInfo4.region.x), Mathf.FloorToInt((float)height * spriteInfo4.region.y), array2, 512, num7, num8, 109, 100);
                        SetRect(spriteInfo4, num7, num8, 109, 100, 512f, num6);
                        num7 += 109 + padding;
                    }
                }
            }

            atlas.bytes = array2;
            atlas.width = 512;
            atlas.height = num6;
        }

        /// <summary>
        /// Sets a sprite rectangle.
        /// </summary>
        /// <param name="sprite">SpriteInfo.</param>
        /// <param name="x">Sprite X-position.</param>
        /// <param name="y">Sprite Y-position.</param>
        /// <param name="width">Sprite width.</param>
        /// <param name="height">Sprite height.</param>
        /// <param name="atlasWidth">Atlas width.</param>
        /// <param name="atlasHeight">Atlas height.</param>
        private void SetRect(UITextureAtlas.SpriteInfo sprite, int x, int y, int width, int height, float atlasWidth, float atlasHeight)
        {
            sprite.region = new Rect(x / atlasWidth, y / atlasHeight, width / atlasWidth, height / atlasHeight);
        }

        /// <summary>
        /// Copies a sprite from one byte array to another.
        /// </summary>
        /// <param name="source">Source byte array.</param>
        /// <param name="sourceWidth">Source array width.</param>
        /// <param name="sourceX">Source X-position.</param>
        /// <param name="sourceY">Source Y-position.</param>
        /// <param name="destination">Destination byte array.</param>
        /// <param name="destinationWidth">Destination array width.</param>
        /// <param name="destinationX">Destination X-position.</param>
        /// <param name="destinationY">Destination Y-position.</param>
        /// <param name="width">Sprite width.</param>
        /// <param name="height">Sprite height.</param>
        private void CopySprite(byte[] source, int sourceWidth, int sourceX, int sourceY, byte[] destination, int destinationWidth, int destinationX, int destinationY, int width, int height)
        {
            // Four bytes per pixel.
            int sourceWidthSize = sourceWidth << 2;
            int destinationWidthSize = destinationWidth << 2;

            // Calculate offsets.
            int sourceOffset = ((sourceY * sourceWidth) + sourceX) << 2;
            int destinationOffset = ((destinationY * destinationWidth) + destinationX) << 2;
            int count = width << 2;

            // Line-by-line copy.
            for (int i = 0; i < height; ++i)
            {
                Buffer.BlockCopy(source, sourceOffset, destination, destinationOffset, count);
                sourceOffset += sourceWidthSize;
                destinationOffset += destinationWidthSize;
            }
        }

        /// <summary>
        /// Copies half a sprite (width-wise) from one byte array to another.
        /// </summary>
        /// <param name="source">Source byte array.</param>
        /// <param name="sourceWidth">Source array width.</param>
        /// <param name="sourceX">Source X-position.</param>
        /// <param name="sourceY">Source Y-position.</param>
        /// <param name="destination">Destination byte array.</param>
        /// <param name="destinationWidth">Destination array width.</param>
        /// <param name="destinationX">Destination X-position.</param>
        /// <param name="destinationY">Destination Y-position.</param>
        /// <param name="width">Sprite width.</param>
        /// <param name="height">Sprite height.</param>
        private void CopyHalf(byte[] source, int sourceWidth, int sourceX, int sourceY, byte[] destination, int destinationWidth, int destinationX, int destinationY, int width, int height)
        {
            // Four bytes per pixel.
            int sourceWidthSize = (sourceWidth - width - (width >> 1)) << 2;
            int destinationWidthSize = (destinationWidth - width) << 2;
            int souceOffset = ((sourceY * sourceWidth) + sourceX + 4) << 2;
            int destinationOffset = ((destinationY * destinationWidth) + destinationX) << 2;

            int halfWidth = width >> 1;
            int count = 1;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < halfWidth; ++x)
                {
                    destination[destinationOffset++] = source[souceOffset++];
                    destination[destinationOffset++] = source[souceOffset++];
                    destination[destinationOffset++] = source[souceOffset++];
                    destination[destinationOffset++] = source[souceOffset];
                    souceOffset += 5;
                    destination[destinationOffset++] = source[souceOffset++];
                    destination[destinationOffset++] = source[souceOffset++];
                    destination[destinationOffset++] = source[souceOffset++];
                    destination[destinationOffset++] = source[souceOffset++];
                }

                souceOffset += sourceWidthSize;
                destinationOffset += destinationWidthSize;
                if (++count == 2)
                {
                    count = 0;
                    souceOffset += sourceWidth << 2;
                }
            }
        }
    }
}
