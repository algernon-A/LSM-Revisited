using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ColossalFramework.Importers;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using UnityEngine;

namespace LoadingScreenMod
{
    // Based on PackageHelper.CustomDeserialize.
    public sealed class CustomDeserializer : Instance<CustomDeserializer>
    {
        internal const string SKIP_PREFIX = "lsm___";

        private Package.Asset[] assets;

        private Dictionary<string, object> packages = new Dictionary<string, object>(256);

        private Dictionary<Type, int> types;

        private AtlasObj prevAtlasObj;

        private Texture2D largeSprite;

        private Texture2D smallSprite;

        private Texture2D halfSprite;

        private ConcurrentQueue<AtlasObj> atlasIn;

        private ConcurrentQueue<AtlasObj> atlasOut;

        private HashSet<string> skippedProps;

        private readonly bool loadUsed = LoadingScreenModRevisited.LSMRSettings.LoadUsed;

        private readonly bool recordUsed = LoadingScreenModRevisited.LSMRSettings.RecordAssets & LoadingScreenModRevisited.LSMRSettings.LoadUsed;

        private readonly bool optimizeThumbs = LoadingScreenModRevisited.LSMRSettings.OptimizeThumbs;

        private bool skipProps = LoadingScreenModRevisited.LSMRSettings.SkipPrefabs;

        private const int THUMBW = 109;

        private const int THUMBH = 100;

        private const int TIPW = 492;

        private const int TIPH = 147;

        private const int HALFW = 66;

        private const int HALFH = 66;

        private const int MODINFO = 0;

        private const int INT_32 = 3;

        private const int STRING = 6;

        private const int BUILDINGINFO_PROP = 10;

        private const int PACKAGE_ASSET = 13;

        private const int VECTOR3 = 16;

        private const int NETINFO_LANE = 20;

        private const int ITEMCLASS = 23;

        private const int DATETIME = 26;

        private const int UITEXTUREATLAS = 29;

        private const int VECTOR4 = 32;

        private const int BUILDINGINFO_PATHINFO = 35;

        private const int NETINFO_NODE = 39;

        private const int NETINFO_SEGMENT = 42;

        private const int NETINFO = 45;

        private const int MILESTONE = 48;

        private const int VECTOR2 = 52;

        private const int MESSAGEINFO = 56;

        private const int VEHICLEINFO_EFFECT = 59;

        private const int TRANSPORTINFO = 62;

        private const int VEHICLEINFO_VEHICLETRAILER = 65;

        private const int VEHICLEINFO_VEHICLEDOOR = 70;

        private const int BUILDINGINFO = 74;

        private const int BUILDINGINFO_SUBINFO = 77;

        private const int DEPOTAI_SPAWNPOINT = 81;

        private const int PROPINFO_EFFECT = 86;

        private const int PROPINFO_VARIATION = 89;

        private const int VEHICLEINFO_MESHINFO = 95;

        private const int BUILDINGINFO_MESHINFO = 103;

        private const int PROPINFO_SPECIALPLACE = 109;

        private const int TREEINFO_VARIATION = 116;

        private const int DICT_STRING_BYTE_ARRAY = 125;

        private const int PROPINFO_PARKINGSPACE = 3232;

        private const int DISASTERPROPERTIES_DISASTERSETTINGS = 11386;

        private static Package.Asset[] Assets
        {
            get
            {
                if (Instance<CustomDeserializer>.instance.assets == null)
                {
                    Instance<CustomDeserializer>.instance.assets = FilterAssets(Package.AssetType.Object);
                }
                return Instance<CustomDeserializer>.instance.assets;
            }
        }

        private HashSet<string> SkippedProps
        {
            get
            {
                if (skippedProps == null)
                {
                    skippedProps = Instance<PrefabLoader>.instance?.SkippedProps;
                    if (skippedProps == null || skippedProps.Count == 0)
                    {
                        skipProps = false;
                        skippedProps = new HashSet<string>();
                    }
                }
                return skippedProps;
            }
        }

        private CustomDeserializer()
        {
        }

        internal void Setup()
        {
            types = new Dictionary<Type, int>(64)
            {
                [typeof(ModInfo)] = 0,
                [typeof(TerrainModify.Surface)] = 3,
                [typeof(SteamHelper.DLC_BitMask)] = 3,
                [typeof(ItemClass.Availability)] = 3,
                [typeof(ItemClass.Placement)] = 3,
                [typeof(ItemClass.Service)] = 3,
                [typeof(ItemClass.SubService)] = 3,
                [typeof(ItemClass.Level)] = 3,
                [typeof(CustomAssetMetaData.Type)] = 3,
                [typeof(VehicleInfo.VehicleType)] = 3,
                [typeof(BuildingInfo.PlacementMode)] = 3,
                [typeof(BuildingInfo.ZoningMode)] = 3,
                [typeof(Vehicle.Flags)] = 3,
                [typeof(CitizenInstance.Flags)] = 3,
                [typeof(NetInfo.ConnectGroup)] = 3,
                [typeof(PropInfo.DoorType)] = 3,
                [typeof(LightEffect.BlinkType)] = 3,
                [typeof(EventManager.EventType)] = 3,
                [typeof(string)] = 6,
                [typeof(BuildingInfo.Prop)] = 10,
                [typeof(Package.Asset)] = 13,
                [typeof(Vector3)] = 16,
                [typeof(NetInfo.Lane)] = 20,
                [typeof(ItemClass)] = 23,
                [typeof(DateTime)] = 26,
                [typeof(UITextureAtlas)] = 29,
                [typeof(Vector4)] = 32,
                [typeof(BuildingInfo.PathInfo)] = 35,
                [typeof(NetInfo.Node)] = 39,
                [typeof(NetInfo.Segment)] = 42,
                [typeof(NetInfo)] = 45,
                [typeof(ManualMilestone)] = 48,
                [typeof(CombinedMilestone)] = 48,
                [typeof(Vector2)] = 52,
                [typeof(MessageInfo)] = 56,
                [typeof(VehicleInfo.Effect)] = 59,
                [typeof(TransportInfo)] = 62,
                [typeof(VehicleInfo.VehicleTrailer)] = 65,
                [typeof(VehicleInfo.VehicleDoor)] = 70,
                [typeof(BuildingInfo)] = 74,
                [typeof(BuildingInfo.SubInfo)] = 77,
                [typeof(DepotAI.SpawnPoint)] = 81,
                [typeof(PropInfo.Effect)] = 86,
                [typeof(PropInfo.Variation)] = 89,
                [typeof(VehicleInfo.MeshInfo)] = 95,
                [typeof(BuildingInfo.MeshInfo)] = 103,
                [typeof(PropInfo.SpecialPlace)] = 109,
                [typeof(TreeInfo.Variation)] = 116,
                [typeof(Dictionary<string, byte[]>)] = 125,
                [typeof(PropInfo.ParkingSpace)] = 3232,
                [typeof(DisasterProperties.DisasterSettings)] = 11386
            };
            if (optimizeThumbs)
            {
                largeSprite = new Texture2D(492, 147, TextureFormat.ARGB32, mipmap: false, linear: false);
                smallSprite = new Texture2D(109, 100, TextureFormat.ARGB32, mipmap: false, linear: false);
                halfSprite = new Texture2D(66, 66, TextureFormat.ARGB32, mipmap: false, linear: false);
                largeSprite.name = "tooltip";
                smallSprite.name = "thumb";
                halfSprite.name = "thumbDisabled";
                smallSprite.SetPixels32(Enumerable.Repeat(new Color32(64, 64, 64, byte.MaxValue), 10900).ToArray());
                smallSprite.Apply(updateMipmaps: false);
                atlasIn = new ConcurrentQueue<AtlasObj>(64);
                atlasOut = new ConcurrentQueue<AtlasObj>(32);
                new Thread(AtlasWorker).Start();
            }
        }

        internal void SetCompleted()
        {
            if (optimizeThumbs)
            {
                atlasIn.SetCompleted();
            }
        }

        internal void Dispose()
        {
            Fetch<BuildingInfo>.Dispose();
            Fetch<PropInfo>.Dispose();
            Fetch<TreeInfo>.Dispose();
            Fetch<VehicleInfo>.Dispose();
            Fetch<CitizenInfo>.Dispose();
            Fetch<NetInfo>.Dispose();
            types?.Clear();
            types = null;
            atlasIn = null;
            atlasOut = null;
            prevAtlasObj = null;
            largeSprite = null;
            smallSprite = null;
            halfSprite = null;
            assets = null;
            packages.Clear();
            packages = null;
            skippedProps = null;
            Instance<CustomDeserializer>.instance = null;
        }

        internal object CustomDeserialize(Package p, Type t, PackageReader r)
        {
            if (!types.TryGetValue(t, out var value))
            {
                return null;
            }
            switch (value)
            {
                case 0:
                    {
                        ModInfo modInfo = default(ModInfo);
                        modInfo.modName = r.ReadString();
                        modInfo.modWorkshopID = r.ReadUInt64();
                        return modInfo;
                    }
                case 3:
                    return r.ReadInt32();
                case 6:
                    return r.ReadString();
                case 10:
                    return ReadBuildingInfoProp(r);
                case 13:
                    return ReadPackageAsset(p, r);
                case 16:
                    return r.ReadVector3();
                case 20:
                    return ReadNetInfoLane(p, r);
                case 23:
                    return ReadItemClass(r);
                case 26:
                    return r.ReadDateTime();
                case 29:
                    if (!optimizeThumbs)
                    {
                        return PackageHelper.CustomDeserialize(p, t, r);
                    }
                    return ReadUITextureAtlas(p, r);
                case 32:
                    return r.ReadVector4();
                case 35:
                    return ReadBuildingInfoPathInfo(p, r);
                case 39:
                    return ReadNetInfoNode(p, r);
                case 42:
                    return ReadNetInfoSegment(p, r);
                case 45:
                    return ReadNetInfo(p, r);
                case 48:
                    return ReadMilestone(r);
                case 52:
                    return r.ReadVector2();
                case 56:
                    return ReadMessageInfo(r);
                case 59:
                    return ReadVehicleInfoEffect(r);
                case 62:
                    return ReadTransportInfo(r);
                case 65:
                    return ReadVehicleInfoVehicleTrailer(p, r);
                case 70:
                    return ReadVehicleInfoVehicleDoor(r);
                case 74:
                    return ReadBuildingInfo(p, r);
                case 77:
                    return ReadBuildingInfoSubInfo(p, r);
                case 81:
                    return ReadDepotAISpawnPoint(r);
                case 86:
                    return ReadPropInfoEffect(r);
                case 89:
                    return ReadPropInfoVariation(p, r);
                case 95:
                    return ReadVehicleInfoMeshInfo(p, r);
                case 103:
                    return ReadBuildingInfoMeshInfo(p, r);
                case 109:
                    return ReadPropInfoSpecialPlace(r);
                case 116:
                    return ReadTreeInfoVariation(p, r);
                case 125:
                    return ReadDictStringByteArray(r);
                case 3232:
                    return ReadPropInfoParkingSpace(r);
                case 11386:
                    return ReadDisasterPropertiesDisasterSettings(r);
                default:
                    return null;
            }
        }

        // Inserts RecordUsed
        private object ReadBuildingInfoProp(PackageReader r)
        {
            string text = r.ReadString();
            string text2 = r.ReadString();
            PropInfo prop = GetProp(text);
            TreeInfo treeInfo = Get<TreeInfo>(text2);
            if (recordUsed)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    AddRef(prop, text, CustomAssetMetaData.Type.Prop);
                }
                if (!string.IsNullOrEmpty(text2))
                {
                    AddRef(treeInfo, text2, CustomAssetMetaData.Type.Tree);
                }
            }
            return new BuildingInfo.Prop
            {
                m_prop = prop,
                m_tree = treeInfo,
                m_position = r.ReadVector3(),
                m_angle = r.ReadSingle(),
                m_probability = r.ReadInt32(),
                m_fixedHeight = r.ReadBoolean()
            };
        }

        private static object ReadPackageAsset(Package p, PackageReader r)
        {
            string checksum = r.ReadString();
            Package.Asset asset = p.FindByChecksum(checksum);
            if (!(asset == null) || p.version >= 3)
            {
                return asset;
            }
            return PackageManager.FindAssetByChecksum(checksum);
        }

        private object ReadNetInfoLane(Package p, PackageReader r)
        {
            return new NetInfo.Lane
            {
                m_position = r.ReadSingle(),
                m_width = r.ReadSingle(),
                m_verticalOffset = r.ReadSingle(),
                m_stopOffset = r.ReadSingle(),
                m_speedLimit = r.ReadSingle(),
                m_direction = (NetInfo.Direction)r.ReadInt32(),
                m_laneType = (NetInfo.LaneType)r.ReadInt32(),
                m_vehicleType = (VehicleInfo.VehicleType)r.ReadInt32(),
                m_stopType = (VehicleInfo.VehicleType)r.ReadInt32(),
                m_laneProps = ReadNetLaneProps(p, r),
                //m_laneProps = (NetLaneProps)PackageHelper.CustomDeserialize(p, typeof(NetLaneProps), r),
                m_allowConnect = r.ReadBoolean(),
                m_useTerrainHeight = r.ReadBoolean(),
                m_centerPlatform = r.ReadBoolean(),
                m_elevated = r.ReadBoolean()
            };
        }

        private static object ReadItemClass(PackageReader r)
        {
            return ItemClassCollection.FindClass(r.ReadString());
        }

        private object ReadUITextureAtlas(Package p, PackageReader r)
        {
            Package.Asset current = LoadingScreenModRevisited.AssetLoader.Instance.Current;
            if ((object)current == prevAtlasObj?.asset)
            {
                ReadOutUITextureAtlas(p, r);
                return prevAtlasObj.atlas;
            }
            UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            uITextureAtlas.name = r.ReadString();
            AtlasObj atlasObj = (prevAtlasObj = new AtlasObj
            {
                asset = current,
                atlas = uITextureAtlas
            });
            if (p.version > 3)
            {
                atlasObj.bytes = r.ReadBytes(r.ReadInt32());
            }
            else
            {
                atlasObj.width = r.ReadInt32();
                atlasObj.height = r.ReadInt32();
                atlasObj.bytes = ReadColorArray(r);
            }
            string text = r.ReadString();
            Shader shader = Shader.Find(text);
            Material material = null;
            if (shader != null)
            {
                material = new Material(shader);
            }
            else
            {
                Debug.Log("Warning: texture atlas shader *" + text + "* not found.");
            }
            uITextureAtlas.material = material;
            uITextureAtlas.padding = r.ReadInt32();
            int num = r.ReadInt32();
            atlasObj.sprites = new List<UITextureAtlas.SpriteInfo>(num);
            for (int i = 0; i < num; i++)
            {
                Rect region = new Rect(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo
                {
                    name = r.ReadString(),
                    region = region
                };
                atlasObj.sprites.Add(item);
            }
            atlasIn.Enqueue(atlasObj);
            ReceiveAvailable();
            return uITextureAtlas;
        }

        private static void ReadOutUITextureAtlas(Package p, PackageReader r)
        {
            r.ReadString();
            if (p.version > 3)
            {
                r.ReadBytes(r.ReadInt32());
            }
            else
            {
                r.ReadInt32();
                r.ReadInt32();
                r.ReadBytes(r.ReadInt32() << 4);
            }
            r.ReadString();
            r.ReadInt32();
            int num = r.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                r.ReadInt32();
                r.ReadInt32();
                r.ReadInt32();
                r.ReadInt32();
                r.ReadString();
            }
        }

        // Inserts recordused
        private object ReadBuildingInfoPathInfo(Package p, PackageReader r)
        {
            string text = r.ReadString();
            NetInfo netInfo = Get<NetInfo>(text);
            if (recordUsed && !string.IsNullOrEmpty(text))
            {
                AddRef(netInfo, text, CustomAssetMetaData.Type.Road);
            }
            BuildingInfo.PathInfo pathInfo = new BuildingInfo.PathInfo();
            pathInfo.m_netInfo = netInfo;
            pathInfo.m_nodes = r.ReadVector3Array();
            pathInfo.m_curveTargets = r.ReadVector3Array();
            pathInfo.m_invertSegments = r.ReadBoolean();
            pathInfo.m_maxSnapDistance = r.ReadSingle();
            if (p.version >= 5)
            {
                pathInfo.m_forbidLaneConnection = r.ReadBooleanArray();
                pathInfo.m_trafficLights = (BuildingInfo.TrafficLights[])(object)r.ReadInt32Array();
                pathInfo.m_yieldSigns = r.ReadBooleanArray();
            }
            return pathInfo;
        }

        private static object ReadNetInfoNode(Package p, PackageReader r)
        {
            NetInfo.Node node = new NetInfo.Node();
            Sharing sharing = Instance<Sharing>.instance;
            string text = r.ReadString();
            node.m_mesh = (string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, p, isMain: true));
            text = r.ReadString();
            node.m_material = (string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, p, isMain: true));
            text = r.ReadString();
            node.m_lodMesh = (string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, p, isMain: false));
            text = r.ReadString();
            node.m_lodMaterial = (string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, p, isMain: false));
            node.m_flagsRequired = (NetNode.Flags)r.ReadInt32();
            node.m_flagsForbidden = (NetNode.Flags)r.ReadInt32();
            node.m_connectGroup = (NetInfo.ConnectGroup)r.ReadInt32();
            node.m_directConnect = r.ReadBoolean();
            node.m_emptyTransparent = r.ReadBoolean();
            return node;
        }

        private static object ReadNetInfoSegment(Package p, PackageReader r)
        {
            NetInfo.Segment segment = new NetInfo.Segment();
            Sharing sharing = Instance<Sharing>.instance;
            string text = r.ReadString();
            segment.m_mesh = (string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, p, isMain: true));
            text = r.ReadString();
            segment.m_material = (string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, p, isMain: true));
            text = r.ReadString();
            segment.m_lodMesh = (string.IsNullOrEmpty(text) ? null : sharing.GetMesh(text, p, isMain: false));
            text = r.ReadString();
            segment.m_lodMaterial = (string.IsNullOrEmpty(text) ? null : sharing.GetMaterial(text, p, isMain: false));
            segment.m_forwardRequired = (NetSegment.Flags)r.ReadInt32();
            segment.m_forwardForbidden = (NetSegment.Flags)r.ReadInt32();
            segment.m_backwardRequired = (NetSegment.Flags)r.ReadInt32();
            segment.m_backwardForbidden = (NetSegment.Flags)r.ReadInt32();
            segment.m_emptyTransparent = r.ReadBoolean();
            segment.m_disableBendNodes = r.ReadBoolean();
            return segment;
        }

        private static object ReadNetInfo(Package p, PackageReader r)
        {
            string text = r.ReadString();
            if (LoadingScreenModRevisited.AssetLoader.Instance.GetPackageTypeFor(p) == CustomAssetMetaData.Type.Road)
            {
                return Get<NetInfo>(p, text);
            }
            return Get<NetInfo>(text);
        }

        private static object ReadMilestone(PackageReader r)
        {
            return MilestoneCollection.FindMilestone(r.ReadString());
        }


        // Vanilla.
        private static object ReadMessageInfo(PackageReader r)
        {
            MessageInfo messageInfo = new MessageInfo();
            messageInfo.m_firstID1 = r.ReadString();
            if (messageInfo.m_firstID1.Equals(string.Empty))
            {
                messageInfo.m_firstID1 = null;
            }
            messageInfo.m_firstID2 = r.ReadString();
            if (messageInfo.m_firstID2.Equals(string.Empty))
            {
                messageInfo.m_firstID2 = null;
            }
            messageInfo.m_repeatID1 = r.ReadString();
            if (messageInfo.m_repeatID1.Equals(string.Empty))
            {
                messageInfo.m_repeatID1 = null;
            }
            messageInfo.m_repeatID2 = r.ReadString();
            if (messageInfo.m_repeatID2.Equals(string.Empty))
            {
                messageInfo.m_repeatID2 = null;
            }
            return messageInfo;
        }

        private static object ReadVehicleInfoEffect(PackageReader r)
        {
            VehicleInfo.Effect effect = default(VehicleInfo.Effect);
            effect.m_effect = EffectCollection.FindEffect(r.ReadString());
            effect.m_parkedFlagsForbidden = (VehicleParked.Flags)r.ReadInt32();
            effect.m_parkedFlagsRequired = (VehicleParked.Flags)r.ReadInt32();
            effect.m_vehicleFlagsForbidden = (Vehicle.Flags)r.ReadInt32();
            effect.m_vehicleFlagsRequired = (Vehicle.Flags)r.ReadInt32();
            return effect;
        }

        private static object ReadTransportInfo(PackageReader r)
        {
            return PrefabCollection<TransportInfo>.FindLoaded(r.ReadString());
        }

        private static object ReadVehicleInfoVehicleTrailer(Package p, PackageReader r)
        {
            string text = r.ReadString();
            VehicleInfo.VehicleTrailer vehicleTrailer = default(VehicleInfo.VehicleTrailer);
            vehicleTrailer.m_info = Get<VehicleInfo>(p, p.packageName + "." + text, text, tryName: false);
            vehicleTrailer.m_probability = r.ReadInt32();
            vehicleTrailer.m_invertProbability = r.ReadInt32();
            return vehicleTrailer;
        }

        private static object ReadVehicleInfoVehicleDoor(PackageReader r)
        {
            VehicleInfo.VehicleDoor vehicleDoor = default(VehicleInfo.VehicleDoor);
            vehicleDoor.m_type = (VehicleInfo.DoorType)r.ReadInt32();
            vehicleDoor.m_location = r.ReadVector3();
            return vehicleDoor;
        }

        private static object ReadBuildingInfo(Package p, PackageReader r)
        {
            string text = r.ReadString();
            if (LoadingScreenModRevisited.AssetLoader.Instance.GetPackageTypeFor(p) == CustomAssetMetaData.Type.Road)
            {
                return Get<BuildingInfo>(p, text);
            }
            return Get<BuildingInfo>(text);
        }

        private static object ReadBuildingInfoSubInfo(Package p, PackageReader r)
        {
            string text = r.ReadString();
            string text2 = p.packageName + "." + text;
            BuildingInfo buildingInfo = null;
            if (text2 == LoadingScreenModRevisited.AssetLoader.Instance.Current.fullName || text == LoadingScreenModRevisited.AssetLoader.Instance.Current.fullName)
            {
                Util.DebugPrint("Warning:", text2, "wants to be a sub-building for itself");
            }
            else
            {
                buildingInfo = Get<BuildingInfo>(p, text2, text, tryName: true);
            }
            return new BuildingInfo.SubInfo
            {
                m_buildingInfo = buildingInfo,
                m_position = r.ReadVector3(),
                m_angle = r.ReadSingle(),
                m_fixedHeight = r.ReadBoolean()
            };
        }

        private static object ReadDepotAISpawnPoint(PackageReader r)
        {
            DepotAI.SpawnPoint spawnPoint = default(DepotAI.SpawnPoint);
            spawnPoint.m_position = r.ReadVector3();
            spawnPoint.m_target = r.ReadVector3();
            return spawnPoint;
        }

        private static PropInfo.Effect ReadPropInfoEffect(PackageReader r)
        {
            PropInfo.Effect result = default(PropInfo.Effect);
            result.m_effect = EffectCollection.FindEffect(r.ReadString());
            result.m_position = r.ReadVector3();
            result.m_direction = r.ReadVector3();
            return result;
        }

        // Inserts warning about recursive variation
        private static object ReadPropInfoVariation(Package p, PackageReader r)
        {
            string text = r.ReadString();
            string text2 = p.packageName + "." + text;
            PropInfo prop = null;
            if (text2 == LoadingScreenModRevisited.AssetLoader.Instance.Current.fullName)
            {
                Util.DebugPrint("Warning:", text2, "wants to be a prop variation for itself");
            }
            else
            {
                prop = Get<PropInfo>(p, text2, text, tryName: false);
            }
            PropInfo.Variation variation = default(PropInfo.Variation);
            variation.m_prop = prop;
            variation.m_probability = r.ReadInt32();
            return variation;
        }

        private static object ReadVehicleInfoMeshInfo(Package p, PackageReader r)
        {
            VehicleInfo.MeshInfo meshInfo = new VehicleInfo.MeshInfo();
            string text = r.ReadString();
            if (!string.IsNullOrEmpty(text))
            {
                GameObject gameObject = LoadingScreenModRevisited.AssetDeserializer.Instantiate(p.FindByChecksum(text), isMain: true, isTop: false) as GameObject;
                meshInfo.m_subInfo = gameObject.GetComponent<VehicleInfoBase>();
                gameObject.SetActive(value: false);
                if (meshInfo.m_subInfo.m_lodObject != null)
                {
                    meshInfo.m_subInfo.m_lodObject.SetActive(value: false);
                }
            }
            else
            {
                meshInfo.m_subInfo = null;
            }
            meshInfo.m_vehicleFlagsForbidden = (Vehicle.Flags)r.ReadInt32();
            meshInfo.m_vehicleFlagsRequired = (Vehicle.Flags)r.ReadInt32();
            meshInfo.m_parkedFlagsForbidden = (VehicleParked.Flags)r.ReadInt32();
            meshInfo.m_parkedFlagsRequired = (VehicleParked.Flags)r.ReadInt32();
            return meshInfo;
        }

        private static object ReadBuildingInfoMeshInfo(Package p, PackageReader r)
        {
            BuildingInfo.MeshInfo meshInfo = new BuildingInfo.MeshInfo();
            string text = r.ReadString();
            if (!string.IsNullOrEmpty(text))
            {
                GameObject gameObject = LoadingScreenModRevisited.AssetDeserializer.Instantiate(p.FindByChecksum(text), isMain: true, isTop: false) as GameObject;
                meshInfo.m_subInfo = gameObject.GetComponent<BuildingInfoBase>();
                gameObject.SetActive(value: false);
                if (meshInfo.m_subInfo.m_lodObject != null)
                {
                    meshInfo.m_subInfo.m_lodObject.SetActive(value: false);
                }
            }
            else
            {
                meshInfo.m_subInfo = null;
            }
            meshInfo.m_flagsForbidden = (Building.Flags)r.ReadInt32();
            meshInfo.m_flagsRequired = (Building.Flags)r.ReadInt32();
            meshInfo.m_position = r.ReadVector3();
            meshInfo.m_angle = r.ReadSingle();
            return meshInfo;
        }

        private static object ReadPropInfoSpecialPlace(PackageReader r)
        {
            PropInfo.SpecialPlace specialPlace = default(PropInfo.SpecialPlace);
            specialPlace.m_specialFlags = (CitizenInstance.Flags)r.ReadInt32();
            specialPlace.m_position = r.ReadVector3();
            specialPlace.m_direction = r.ReadVector3();
            return specialPlace;
        }

        // Adds warning message re recursive variations
        private static object ReadTreeInfoVariation(Package p, PackageReader r)
        {
            string text = r.ReadString();
            string text2 = p.packageName + "." + text;
            TreeInfo tree = null;
            if (text2 == LoadingScreenModRevisited.AssetLoader.Instance.Current.fullName)
            {
                Util.DebugPrint("Warning:", text2, "wants to be a tree variation for itself");
            }
            else
            {
                tree = Get<TreeInfo>(p, text2, text, tryName: false);
            }
            return new TreeInfo.Variation
            {
                m_tree = tree,
                m_probability = r.ReadInt32()
            };
        }

        private static object ReadDictStringByteArray(PackageReader r)
        {
            int num = r.ReadInt32();
            Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>(num);
            for (int i = 0; i < num; i++)
            {
                string key = r.ReadString();
                dictionary[key] = r.ReadBytes(r.ReadInt32());
            }
            return dictionary;
        }

        private static object ReadPropInfoParkingSpace(PackageReader r)
        {
            PropInfo.ParkingSpace parkingSpace = default(PropInfo.ParkingSpace);
            parkingSpace.m_position = r.ReadVector3();
            parkingSpace.m_direction = r.ReadVector3();
            parkingSpace.m_size = r.ReadVector3();
            return parkingSpace;
        }

        private static object ReadDisasterPropertiesDisasterSettings(PackageReader r)
        {
            DisasterProperties.DisasterSettings disasterSettings = default(DisasterProperties.DisasterSettings);
            disasterSettings.m_disasterName = r.ReadString();
            disasterSettings.m_randomProbability = r.ReadInt32();
            return disasterSettings;
        }

        private NetLaneProps ReadNetLaneProps(Package p, PackageReader r)
        {
            int num = r.ReadInt32();
            NetLaneProps netLaneProps = ScriptableObject.CreateInstance<NetLaneProps>();
            netLaneProps.m_props = new NetLaneProps.Prop[num];
            for (int i = 0; i < num; i++)
            {
                netLaneProps.m_props[i] = ReadNetLaneProp(p, r);
            }
            return netLaneProps;
        }

        private NetLaneProps.Prop ReadNetLaneProp(Package p, PackageReader r)
        {
            string text;
            string text2;
            NetLaneProps.Prop prop = new NetLaneProps.Prop
            {
                m_flagsRequired = (NetLane.Flags)r.ReadInt32(),
                m_flagsForbidden = (NetLane.Flags)r.ReadInt32(),
                m_startFlagsRequired = (NetNode.Flags)r.ReadInt32(),
                m_startFlagsForbidden = (NetNode.Flags)r.ReadInt32(),
                m_endFlagsRequired = (NetNode.Flags)r.ReadInt32(),
                m_endFlagsForbidden = (NetNode.Flags)r.ReadInt32(),
                m_colorMode = (NetLaneProps.ColorMode)r.ReadInt32(),
                m_prop = GetProp(text = r.ReadString()),
                m_tree = Get<TreeInfo>(text2 = r.ReadString()),
                m_position = r.ReadVector3(),
                m_angle = r.ReadSingle(),
                m_segmentOffset = r.ReadSingle(),
                m_repeatDistance = r.ReadSingle(),
                m_minLength = r.ReadSingle(),
                m_cornerAngle = r.ReadSingle(),
                m_probability = r.ReadInt32()
            };
            if (recordUsed)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    AddRef(prop.m_prop, text, CustomAssetMetaData.Type.Prop);
                }
                if (!string.IsNullOrEmpty(text2))
                {
                    AddRef(prop.m_tree, text2, CustomAssetMetaData.Type.Tree);
                }
            }

            if (p.version >= 8)
            {
                prop.m_upgradable = r.ReadBoolean();
            }
            else
            {
                prop.m_upgradable = prop.m_tree != null && prop.m_repeatDistance > 0f;
            }
            return prop;
        }

        private PropInfo GetProp(string fullName)
        {
            if (string.IsNullOrEmpty(fullName) || (skipProps && SkippedProps.Contains(fullName)))
            {
                return null;
            }
            return Get<PropInfo>(fullName);
        }

        private static T Get<T>(string fullName) where T : PrefabInfo
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }
            T val = FindLoaded<T>(fullName);

            // If there's a missing NetInfo, call ResolveLegacyPrefab to see if it maps to a new prefab name.
            if (val == null && typeof(T) == typeof(NetInfo))
            {
                string newName = BuildConfig.ResolveLegacyPrefab(fullName);
                val = FindLoaded<T>(newName);
            }

            if ((UnityEngine.Object)val == (UnityEngine.Object)null && Instance<CustomDeserializer>.instance.Load(ref fullName, FindAsset(fullName)))
            {
                val = FindLoaded<T>(fullName);
            }
            return val;
        }

        private static T Get<T>(Package package, string name) where T : PrefabInfo
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            string text = PackageHelper.StripName(name);
            T val = FindLoaded<T>(package.packageName + "." + text);
            if ((UnityEngine.Object)val == (UnityEngine.Object)null)
            {
                Package.Asset asset = package.Find(text);
                if (asset != null)
                {
                    string fullName = asset.fullName;
                    if (Instance<CustomDeserializer>.instance.Load(ref fullName, asset))
                    {
                        val = FindLoaded<T>(fullName);
                    }
                }
                else
                {
                    val = Get<T>(name);
                }
            }
            return val;
        }

        private static T Get<T>(Package package, string fullName, string name, bool tryName) where T : PrefabInfo
        {
            T val = FindLoaded<T>(fullName);
            if (tryName && (UnityEngine.Object)val == (UnityEngine.Object)null)
            {
                val = FindLoaded<T>(name);
            }
            if ((UnityEngine.Object)val == (UnityEngine.Object)null)
            {
                Package.Asset asset = package.Find(name);
                if (tryName && asset == null)
                {
                    asset = FindAsset(name);
                }
                if (asset != null)
                {
                    fullName = asset.fullName;
                }
                else if (name.IndexOf('.') >= 0)
                {
                    fullName = name;
                }
                if (Instance<CustomDeserializer>.instance.Load(ref fullName, asset))
                {
                    val = FindLoaded<T>(fullName);
                }
            }
            return val;
        }

        internal static T FindLoaded<T>(string fullName, bool tryName = true) where T : PrefabInfo
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }
            Dictionary<string, PrefabCollection<T>.PrefabData> prefabDict = Fetch<T>.PrefabDict;
            if (prefabDict.TryGetValue(fullName, out var value))
            {
                return value.m_prefab;
            }
            if (tryName && fullName.IndexOf('.') < 0 && !LoadingScreenModRevisited.LevelLoader.HasAssetFailed(fullName))
            {
                Package.Asset[] array = Assets;
                for (int i = 0; i < array.Length; i++)
                {
                    if (fullName == array[i].name && prefabDict.TryGetValue(array[i].package.packageName + "." + fullName, out value))
                    {
                        return value.m_prefab;
                    }
                }
            }
            return null;
        }

        public static Package.Asset FindAsset(string fullName)
        {
            if (LoadingScreenModRevisited.LevelLoader.HasAssetFailed(fullName))
            {
                return null;
            }
            int num = fullName.IndexOf('.');
            if (num >= 0)
            {
                string assetName = fullName.Substring(num + 1);
                if (Instance<CustomDeserializer>.instance.packages.TryGetValue(fullName.Substring(0, num), out var value))
                {
                    Package package = value as Package;
                    if ((object)package != null)
                    {
                        return package.Find(assetName);
                    }
                    List<Package> list = value as List<Package>;
                    for (int i = 0; i < list.Count; i++)
                    {
                        Package.Asset result;
                        if ((result = list[i].Find(assetName)) != null)
                        {
                            return result;
                        }
                    }
                }
            }
            else
            {
                Package.Asset[] array = Assets;
                for (int j = 0; j < array.Length; j++)
                {
                    if (fullName == array[j].name)
                    {
                        return array[j];
                    }
                }
            }
            return null;
        }

        private bool Load(ref string fullName, Package.Asset data)
        {
            if (loadUsed)
            {
                if (data != null)
                {
                    try
                    {
                        fullName = data.fullName;
                        if (fullName != LoadingScreenModRevisited.AssetLoader.Instance.Current.fullName && !LoadingScreenModRevisited.LevelLoader.HasAssetFailed(fullName))
                        {
                            if (recordUsed)
                            {
                                Instance<Reports>.instance.AddPackage(data.package);
                            }
                            LoadingScreenModRevisited.AssetLoader.Instance.LoadImpl(data);
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        LoadingScreenModRevisited.AssetLoader.Instance.AssetFailed(data, data.package, e);
                    }
                }
                else
                {
                    LoadingScreenModRevisited.AssetLoader.Instance.AssetMissing(fullName);
                }
            }
            else
            {
                LoadingScreenModRevisited.LevelLoader.AddFailed(fullName);
            }
            return false;
        }

        private void AddRef(PrefabInfo info, string fullName, CustomAssetMetaData.Type type)
        {
            if (info == null)
            {
                if (type != CustomAssetMetaData.Type.Prop || !skipProps || !SkippedProps.Contains(fullName))
                {
                    Package.Asset asset = FindContainer();
                    if (asset != null)
                    {
                        Instance<Reports>.instance.AddReference(asset, fullName, type);
                    }
                }
            }
            else
            {
                if (!info.m_isCustomContent)
                {
                    return;
                }
                string name = info.name;
                Package.Asset asset2 = FindContainer();
                if (!string.IsNullOrEmpty(name) && asset2 != null)
                {
                    string packageName = asset2.package.packageName;
                    int num = name.IndexOf('.');
                    string fullName2;
                    if (num >= 0 && (num != packageName.Length || !name.StartsWith(packageName)) && (fullName2 = FindMain(name)) != null)
                    {
                        Instance<Reports>.instance.AddReference(asset2, fullName2, type);
                    }
                }
            }
        }

        private static Package.Asset FindContainer()
        {
            Package.Asset current = LoadingScreenModRevisited.AssetLoader.Instance.Current;
            if (Instance<Reports>.instance.IsKnown(current))
            {
                return current;
            }
            return KnownMainAssetRef(current.package);
        }

        private static string FindMain(string fullName)
        {
            if (Instance<Reports>.instance.IsKnown(fullName))
            {
                return fullName;
            }
            Package.Asset asset = FindAsset(fullName);
            if (asset != null)
            {
                return KnownMainAssetRef(asset.package)?.fullName;
            }
            return null;
        }

        private static Package.Asset KnownMainAssetRef(Package p)
        {
            Package.Asset asset = LoadingScreenModRevisited.AssetLoader.FindMainAssetRef(p);
            if (string.IsNullOrEmpty(asset?.fullName) || !Instance<Reports>.instance.IsKnown(asset))
            {
                return null;
            }
            return asset;
        }

        //[HarmonyPatch(typeof(BuildConfig), "ResolveCustomAssetName")]
        //[HarmonyPrefix]
        private static bool ResolveCustomAssetName(ref string __result, string name)
        {
            if (name.IndexOf('.') < 0 && !name.StartsWith("lsm___") && !LoadingScreenModRevisited.LevelLoader.HasAssetFailed(name))
            {
                Package.Asset[] array = Assets;
                for (int i = 0; i < array.Length; i++)
                {
                    if (name == array[i].name)
                    {
                        __result = array[i].package.packageName + "." + name;
                        return false;
                    }
                }
            }
            __result = name;
            return false;
        }

        private static Package.Asset[] FilterAssets(Package.AssetType assetType)
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
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            Package.Asset[] result = list.ToArray();
            list.Clear();
            return result;
        }

        internal void AddPackage(Package p)
        {
            string packageName = p.packageName;
            object value;
            if (string.IsNullOrEmpty(packageName))
            {
                Util.DebugPrint(p.packagePath, " Error : no package name");
            }
            else if (packages.TryGetValue(packageName, out value))
            {
                List<Package> list = value as List<Package>;
                if (list != null)
                {
                    list.Add(p);
                    return;
                }
                packages[packageName] = new List<Package>(4)
                {
                    value as Package,
                    p
                };
            }
            else
            {
                packages.Add(packageName, p);
            }
        }

        internal bool HasPackages(string packageName)
        {
            return packages.ContainsKey(packageName);
        }

        internal ICollection<object> AllPackages()
        {
            return packages.Values;
        }

        internal List<Package> GetPackages(string packageName)
        {
            if (packages.TryGetValue(packageName, out var value))
            {
                Package package = value as Package;
                if ((object)package != null)
                {
                    return new List<Package>(1) { package };
                }
                return value as List<Package>;
            }
            return null;
        }

        internal static bool AllAvailable<P>(HashSet<string> fullNames, HashSet<string> ignore) where P : PrefabInfo
        {
            foreach (string fullName in fullNames)
            {
                if (!ignore.Contains(fullName) && (UnityEngine.Object)FindLoaded<P>(fullName, tryName: false) == (UnityEngine.Object)null)
                {
                    Util.DebugPrint("Must load:", fullName);
                    return false;
                }
            }
            return true;
        }

        private static byte[] ReadColorArray(PackageReader r)
        {
            int num = r.ReadInt32();
            byte[] array = new byte[num << 2];
            int i = 0;
            int num2 = 0;
            for (; i < num; i++)
            {
                array[num2++] = (byte)(r.ReadSingle() * 255f);
                array[num2++] = (byte)(r.ReadSingle() * 255f);
                array[num2++] = (byte)(r.ReadSingle() * 255f);
                array[num2++] = (byte)(r.ReadSingle() * 255f);
            }
            return array;
        }

        private void ProcessAtlas(AtlasObj ao)
        {
            byte[] array;
            int width;
            int height;
            if (ao.width == 0)
            {
                Image image = new Image(ao.bytes);
                array = image.GetAllPixels();
                width = image.width;
                height = image.height;
                ao.format = image.format;
            }
            else
            {
                array = ao.bytes;
                width = ao.width;
                height = ao.height;
                ao.format = TextureFormat.RGBA32;
            }
            List<UITextureAtlas.SpriteInfo> sprites = ao.sprites;
            UITextureAtlas.SpriteInfo spriteInfo = null;
            UITextureAtlas.SpriteInfo spriteInfo2 = null;
            int num = 0;
            int num2 = 0;
            int num3 = 9999;
            for (int i = 0; i < sprites.Count; i++)
            {
                UITextureAtlas.SpriteInfo spriteInfo3 = sprites[i];
                int num4 = Mathf.FloorToInt((float)width * spriteInfo3.region.width);
                int num5 = Mathf.FloorToInt((float)height * spriteInfo3.region.height);
                if (num4 == 109 && num5 == 100)
                {
                    num++;
                    spriteInfo3.texture = smallSprite;
                    if (spriteInfo3.name.Length < num3)
                    {
                        num3 = spriteInfo3.name.Length;
                        spriteInfo = spriteInfo3;
                    }
                }
                else if (num4 == 492 && num5 == 147)
                {
                    num2++;
                    spriteInfo3.texture = largeSprite;
                    spriteInfo2 = spriteInfo3;
                }
                else
                {
                    num2++;
                    spriteInfo3.texture = smallSprite;
                }
            }
            int padding = ao.atlas.padding;
            if (spriteInfo == null || num > 5 || num2 > 1 || padding > 2 || width > 512 || (spriteInfo2 == null && num2 > 0))
            {
                Util.DebugPrint("!ProcessAtlas", ao.asset.fullName);
                ao.bytes = array;
                ao.width = width;
                ao.height = height;
                return;
            }
            int num6 = ((spriteInfo2 != null) ? 256 : 128);
            byte[] array2 = new byte[num6 << 11];
            int num7 = 0;
            int num8 = 0;
            if (spriteInfo2 != null)
            {
                CopySprite(array, width, Mathf.FloorToInt((float)width * spriteInfo2.region.x), Mathf.FloorToInt((float)height * spriteInfo2.region.y), array2, 512, 0, 0, 75264, 1);
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
                        CopyHalf(array, width, Mathf.FloorToInt((float)width * spriteInfo4.region.x), Mathf.FloorToInt((float)height * spriteInfo4.region.y), array2, 512, num7, num8, 66, 66);
                        SetRect(spriteInfo4, num7, num8, 66, 66, 512f, num6);
                        spriteInfo4.texture = halfSprite;
                        num7 += 66 + padding;
                    }
                    else
                    {
                        CopySprite(array, width, Mathf.FloorToInt((float)width * spriteInfo4.region.x), Mathf.FloorToInt((float)height * spriteInfo4.region.y), array2, 512, num7, num8, 109, 100);
                        SetRect(spriteInfo4, num7, num8, 109, 100, 512f, num6);
                        num7 += 109 + padding;
                    }
                }
            }
            ao.bytes = array2;
            ao.width = 512;
            ao.height = num6;
        }

        private static void CopySprite(byte[] src, int srcWidth, int x0, int y0, byte[] dst, int dstWidth, int x1, int y1, int w, int h)
        {
            int num = srcWidth << 2;
            int num2 = dstWidth << 2;
            int num3 = y0 * srcWidth + x0 << 2;
            int num4 = y1 * dstWidth + x1 << 2;
            int count = w << 2;
            for (int i = 0; i < h; i++)
            {
                Buffer.BlockCopy(src, num3, dst, num4, count);
                num3 += num;
                num4 += num2;
            }
        }

        private static void CopyHalf(byte[] src, int srcWidth, int x0, int y0, byte[] dst, int dstWidth, int x1, int y1, int w, int h)
        {
            int num = srcWidth - w - (w >> 1) << 2;
            int num2 = dstWidth - w << 2;
            int num3 = y0 * srcWidth + x0 + 4 << 2;
            int num4 = y1 * dstWidth + x1 << 2;
            int num5 = w >> 1;
            int i = 0;
            int num6 = 1;
            for (; i < h; i++)
            {
                for (int j = 0; j < num5; j++)
                {
                    dst[num4++] = src[num3++];
                    dst[num4++] = src[num3++];
                    dst[num4++] = src[num3++];
                    dst[num4++] = src[num3];
                    num3 += 5;
                    dst[num4++] = src[num3++];
                    dst[num4++] = src[num3++];
                    dst[num4++] = src[num3++];
                    dst[num4++] = src[num3++];
                }
                num3 += num;
                num4 += num2;
                if (++num6 == 2)
                {
                    num6 = 0;
                    num3 += srcWidth << 2;
                }
            }
        }

        private static void SetRect(UITextureAtlas.SpriteInfo sprite, int x, int y, int w, int h, float atlasWidth, float atlasHeight)
        {
            sprite.region = new Rect((float)x / atlasWidth, (float)y / atlasHeight, (float)w / atlasWidth, (float)h / atlasHeight);
        }

        internal void ReceiveAvailable()
        {
            AtlasObj[] array = atlasOut.DequeueAll();
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ReceiveAtlas(array[i]);
                }
            }
        }

        internal void ReceiveRemaining()
        {
            AtlasObj result;
            while (atlasOut.Dequeue(out result))
            {
                ReceiveAtlas(result);
            }
        }

        private static void ReceiveAtlas(AtlasObj ao)
        {
            UITextureAtlas atlas = ao.atlas;
            if (ao.bytes != null && atlas.material != null)
            {
                Texture2D texture2D = new Texture2D(ao.width, ao.height, ao.format, mipmap: false, linear: false);
                texture2D.LoadRawTextureData(ao.bytes);
                texture2D.Apply(updateMipmaps: false);
                atlas.material.mainTexture = texture2D;
                atlas.AddSprites(ao.sprites);
            }
        }

        private void AtlasWorker()
        {
            Thread.CurrentThread.Name = "AtlasWorker";
            AtlasObj result;
            while (atlasIn.Dequeue(out result))
            {
                try
                {
                    ProcessAtlas(result);
                }
                catch (Exception ex)
                {
                    Util.DebugPrint("AtlasWorker", result.asset.fullName, ex.Message);
                    result.bytes = null;
                }
                atlasOut.Enqueue(result);
            }
            atlasOut.SetCompleted();
        }
    }
}
