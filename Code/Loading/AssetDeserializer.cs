// <copyright file="AssetDeserializer.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.IO;
    using System.Reflection;
    using AlgernonCommons;
    using ColossalFramework.Importers;
    using ColossalFramework.Packaging;
    using HarmonyLib;
    using LoadingScreenMod;
    using UnityEngine;

    /// <summary>
    /// Asset deserialization.
    /// </summary>
    internal sealed class AssetDeserializer
    {
        // Delegates to game private methods.
        private static DeserializeHeaderDelegate s_deserializeHeader;
        private static DeserializeHeaderNameDelegate s_deserializeHeaderName;
        private static ResolveLegacyMemberDelegate s_resolveLegacyMember;

        // Package fields.
        private readonly Package _package;
        private readonly PackageReader _reader;
        private readonly bool _isTop;
        private bool _isMain;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDeserializer"/> class.
        /// </summary>
        /// <param name="package">Package.</param>
        /// <param name="reader">PackageReader instance.</param>
        /// <param name="isMain">True if this assset is the main package asset, false otherwise.</param>
        /// <param name="isTop">True if this is the top asset, false otherwise.</param>
        private AssetDeserializer(Package package, PackageReader reader, bool isMain, bool isTop)
        {
            _package = package;
            _reader = reader;
            _isMain = isMain;
            _isTop = isTop;
        }

        // Delegate for private method ColossalFramework.Packaging.AssetSerializer.DeserializeHeader.
        private delegate bool DeserializeHeaderDelegate(out Type type, PackageReader reader);

        // Delegate for private method ColossalFramework.Packaging.AssetSerializer.DeserializeHeader.
        private delegate bool DeserializeHeaderNameDelegate(out Type type, out string name, PackageReader reader);

        // Delegate for private method ColossalFramework.Packaging.PackageDeserializer.ResolveLegacyMember.
        private delegate string ResolveLegacyMemberDelegate(Type fieldType, Type classType, string member);

        /// <summary>
        /// Initializes delegates to game private methods.
        /// </summary>
        internal static void SetDelegates()
        {
            s_deserializeHeader = (DeserializeHeaderDelegate)Delegate.CreateDelegate(
                typeof(DeserializeHeaderDelegate),
                AccessTools.Method(
                    Type.GetType("ColossalFramework.Packaging.AssetSerializer,ColossalManaged"),
                    "DeserializeHeader",
                    new Type[] { typeof(Type).MakeByRefType(), typeof(PackageReader) }));

            s_deserializeHeaderName = (DeserializeHeaderNameDelegate)Delegate.CreateDelegate(
                typeof(DeserializeHeaderNameDelegate),
                AccessTools.Method(
                    Type.GetType("ColossalFramework.Packaging.AssetSerializer,ColossalManaged"),
                    "DeserializeHeader",
                    new Type[] { typeof(Type).MakeByRefType(), typeof(string).MakeByRefType(), typeof(PackageReader) }));

            s_resolveLegacyMember = (ResolveLegacyMemberDelegate)Delegate.CreateDelegate(
                typeof(ResolveLegacyMemberDelegate),
                AccessTools.Method(
                    typeof(PackageDeserializer),
                    "ResolveLegacyMember"));
        }

        /// <summary>
        /// Deserializes an asset from a sharing stream.
        /// </summary>
        /// <param name="asset">Asset to deserialise.</param>
        /// <param name="isMain">True if this assset is the main package asset, false otherwise.</param>
        /// <param name="isTop">True if this is the top asset, false otherwise.</param>
        /// <returns>Deserialized object.</returns>
        internal static object Instantiate(Package.Asset asset, bool isMain, bool isTop)
        {
            using (Stream stream = Instance<Sharing>.instance.GetStream(asset))
            {
                using (PackageReader packageReader = GetReader(stream))
                {
                    return new AssetDeserializer(asset.package, packageReader, isMain, isTop).Deserialize();
                }
            }
        }

        /// <summary>
        /// Deserializes an asset from a byte array.
        /// </summary>
        /// <param name="package">Containing package.</param>
        /// <param name="bytes">True if this is the top asset, false otherwise.</param>
        /// <param name="isMain">True if the assset is the main package asset, false otherwise.</param>
        /// <returns>Deserialized object.</returns>
        internal static object Instantiate(Package package, byte[] bytes, bool isMain)
        {
            using (MemStream stream = new MemStream(bytes, 0))
            {
                using (PackageReader packageReader = new MemReader(stream))
                {
                    return new AssetDeserializer(package, packageReader, isMain, isTop: false).Deserialize();
                }
            }
        }

        /// <summary>
        /// Deserializes an asset from a package file.
        /// </summary>
        /// <param name="asset">Asset to deserialise.</param>
        /// <param name="isMain">True if this assset is the main package asset, false otherwise.</param>
        /// <param name="isTop">True if this is the top asset, false otherwise.</param>
        /// <returns>Deserialized object.</returns>
        internal static object InstantiateOne(Package.Asset asset, bool isMain = true, bool isTop = true)
        {
            Package package = asset.package;
            using (FileStream fileStream = new FileStream(package.packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, Mathf.Min(asset.size, 8192)))
            {
                fileStream.Position = asset.offset;
                using (PackageReader packageReader = new PackageReader(fileStream))
                {
                    return new AssetDeserializer(package, packageReader, isMain, isTop).Deserialize();
                }
            }
        }

        /// <summary>
        /// Gets a reader for the specified stream.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>New MemReader (if stream is MemStream) or PackageReader (otherwise.)</returns>
        private static PackageReader GetReader(Stream stream)
        {
            // If this is a MemStream, return a new MemReader.
            if (stream is MemStream memStream)
            {
                return new MemReader(memStream);
            }

            // Otherwise, return a new PackageReader.
            return new PackageReader(stream);
        }

        /// <summary>
        /// Deserializes an object.
        /// Based on ColossalFramework.Packaging.PackageDeserializer.
        /// </summary>
        /// <returns>Deserialized object.</returns>
        private object Deserialize()
        {
            if (!s_deserializeHeader(out Type type, _reader))
            {
                return null;
            }

            if (type == typeof(GameObject))
            {
                // Slight modification to game code.
                return DeserializeGameObject();
            }

            if (type == typeof(Mesh))
            {
                return DeserializeMesh();
            }

            if (type == typeof(Material))
            {
                return DeserializeMaterial();
            }

            if (type == typeof(Texture2D) || type == typeof(Image))
            {
                return DeserializeTexture();
            }

            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return DeserializeScriptableObject(type);
            }

            return DeserializeObject(type);
        }

        /// <summary>
        /// Deserializes a single object.
        /// Based on ColossalFramework.Packaging.PackageDeserializer.DeserializeSingleObject.
        /// </summary>
        /// <param name="type">Object type.</param>
        /// <param name="expectedType">Expected object type.</param>
        /// <returns>Deserialized object.</returns>
        private object DeserializeSingleObject(Type type, Type expectedType)
        {
            // Attempt custom deserialization.
            object obj = CustomDeserializer.Instance.CustomDeserialize(_package, type, _reader);
            if (obj != null)
            {
                // Success - return object.
                return obj;
            }

            // Follow gamecode, but call FindByChecksum directly rather than via intermediate steps.
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return Instantiate(_package.FindByChecksum(_reader.ReadString()), _isMain, isTop: false);
            }

            if (typeof(GameObject).IsAssignableFrom(type))
            {
                return Instantiate(_package.FindByChecksum(_reader.ReadString()), _isMain, isTop: false);
            }

            try
            {
                // Gamecode.
                if (_package.version < 3 && expectedType != null && expectedType == typeof(Package.Asset))
                {
                    return _reader.ReadUnityType(expectedType);
                }

                return _reader.ReadUnityType(type, _package);
            }
            catch (MissingMethodException)
            {
                Logging.Error("unsupported type for deserialization: ", type.Name, " in package ", _package.packageName);
                return null;
            }
        }

        /// <summary>
        /// Deserializes an object's fields.
        /// Implements rapid reads of known simple types.
        /// </summary>
        /// <param name="obj">Object to deserialize.</param>
        /// <param name="type">Object type.</param>
        /// <param name="resolveMember">True to resolve any legacy members.</param>
        private void DeserializeFields(object obj, Type type, bool resolveMember)
        {
            int fieldCount = _reader.ReadInt32();
            for (int i = 0; i < fieldCount; ++i)
            {
                if (!s_deserializeHeaderName(out Type headerType, out string name, _reader))
                {
                    continue;
                }

                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null && resolveMember)
                {
                    field = type.GetField(s_resolveLegacyMember(headerType, type, name), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                // Rapid reads of simple types.
                object value;
                if (headerType == typeof(bool))
                {
                    value = _reader.ReadBoolean();
                }
                else if (headerType == typeof(int))
                {
                    value = _reader.ReadInt32();
                }
                else if (headerType == typeof(float))
                {
                    value = _reader.ReadSingle();
                }
                else if (headerType.IsArray)
                {
                    int arraySize = _reader.ReadInt32();
                    Type elementType = headerType.GetElementType();
                    if (elementType == typeof(Vector2))
                    {
                        Vector2[] vectors = new Vector2[arraySize];
                        value = vectors;
                        for (int j = 0; j < arraySize; ++j)
                        {
                            vectors[j] = _reader.ReadVector2();
                        }
                    }
                    else if (elementType == typeof(float))
                    {
                        float[] floats = new float[arraySize];
                        value = floats;
                        for (int j = 0; j < arraySize; ++j)
                        {
                            floats[j] = _reader.ReadSingle();
                        }
                    }
                    else
                    {
                        Array array = Array.CreateInstance(elementType, arraySize);
                        value = array;
                        for (int j = 0; j < arraySize; ++j)
                        {
                            array.SetValue(DeserializeSingleObject(elementType, field?.FieldType), j);
                        }
                    }
                }
                else
                {
                    value = DeserializeSingleObject(headerType, field?.FieldType);
                }

                field?.SetValue(obj, value);
            }
        }

        /// <summary>
        /// Deserializes a GameObject.
        /// Based on ColossalFramework.Packaging.PackageDeserializer.DeserializeGameObject.
        /// Custom Asset Loader Postfixes this.
        /// </summary>
        /// <returns>Deserialized GameObject.</returns>
        /// <exception cref="InvalidDataException">Thrown if GameObject type is unknown.</exception>
        private UnityEngine.Object DeserializeGameObject()
        {
            GameObject gameObject = new GameObject(_reader.ReadString())
            {
                tag = _reader.ReadString(),
                layer = _reader.ReadInt32(),
            };

            gameObject.SetActive(_reader.ReadBoolean());
            int count = _reader.ReadInt32();

            // Insert to gamecode.
            _isMain = _isTop | count > 3;

            // Unwrapped ColossalFramework.Packaging.PackageDeserializer.DeserializeComponent.
            for (int i = 0; i < count; ++i)
            {
                if (!s_deserializeHeader(out Type type, _reader))
                {
                    continue;
                }

                if (type == typeof(Transform))
                {
                    DeserializeTransform(gameObject.transform);
                    continue;
                }

                if (type == typeof(MeshFilter))
                {
                    DeserializeMeshFilter(gameObject.AddComponent(type) as MeshFilter);
                    continue;
                }

                if (type == typeof(MeshRenderer))
                {
                    DeserializeMeshRenderer(gameObject.AddComponent(type) as MeshRenderer);
                    continue;
                }

                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    DeserializeMonoBehaviour((MonoBehaviour)gameObject.AddComponent(type));
                    continue;
                }

                if (type == typeof(SkinnedMeshRenderer))
                {
                    DeserializeSkinnedMeshRenderer(gameObject.AddComponent(type) as SkinnedMeshRenderer);
                    continue;
                }

                if (type == typeof(Animator))
                {
                    DeserializeAnimator(gameObject.AddComponent(type) as Animator);
                    continue;
                }

                throw new InvalidDataException("Unknown type to deserialize " + type.Name);
            }

            return gameObject;
        }

        /// <summary>
        /// Deserializes a mesh.
        /// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeMesh.
        /// </summary>
        /// <returns>Deserialized mesh.</returns>
        private UnityEngine.Object DeserializeMesh()
        {
            Mesh mesh = new Mesh
            {
                name = _reader.ReadString(),
                vertices = _reader.ReadVector3Array(),
                colors = _reader.ReadColorArray(),
                uv = _reader.ReadVector2Array(),
                normals = _reader.ReadVector3Array(),
                tangents = _reader.ReadVector4Array(),
                boneWeights = _reader.ReadBoneWeightsArray(),
                bindposes = _reader.ReadMatrix4x4Array(),
                subMeshCount = _reader.ReadInt32(),
            };

            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                mesh.SetTriangles(_reader.ReadInt32Array(), i);
            }

            return mesh;
        }

        /// <summary>
        /// Deserializes a material.
        /// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeMaterial.
        /// </summary>
        /// <returns>Deserialized material.</returns>
        private MaterialData DeserializeMaterial()
        {
            string name = _reader.ReadString();
            Material material = new Material(Shader.Find(_reader.ReadString()))
            {
                name = name,
            };

            int num = _reader.ReadInt32();
            int num2 = 0;
            Sharing instance = Instance<Sharing>.instance;
            Texture2D texture2D = null;
            for (int i = 0; i < num; i++)
            {
                switch (_reader.ReadInt32())
                {
                    case 0:
                        material.SetColor(_reader.ReadString(), _reader.ReadColor());
                        break;
                    case 1:
                        material.SetVector(_reader.ReadString(), _reader.ReadVector4());
                        break;
                    case 2:
                        material.SetFloat(_reader.ReadString(), _reader.ReadSingle());
                        break;
                    case 3:
                        {
                            string name2 = _reader.ReadString();
                            if (!_reader.ReadBoolean())
                            {
                                string checksum = _reader.ReadString();
                                texture2D = instance.GetTexture(checksum, _package, _isMain);
                                material.SetTexture(name2, texture2D);
                                num2++;
                            }
                            else
                            {
                                material.SetTexture(name2, null);
                            }

                            break;
                        }
                }
            }

            MaterialData materialData = new MaterialData(material, num2);
            if (instance.checkAssets && !_isMain && texture2D != null)
            {
                instance.Check(materialData, texture2D);
            }

            return materialData;
        }

        /// <summary>
        /// Deserializes a texture.
        /// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeTexture.
        /// </summary>
        private UnityEngine.Object DeserializeTexture()
        {
            string name = _reader.ReadString();
            bool linear = _reader.ReadBoolean();
            int anisoLevel = (_package.version < 6) ? 1 : _reader.ReadInt32();
            int count = _reader.ReadInt32();
            Texture2D texture2D = new Image(_reader.ReadBytes(count)).CreateTexture(linear);
            texture2D.name = name;
            texture2D.anisoLevel = anisoLevel;
            return texture2D;
        }

        /// <summary>
        /// Deserializes a scriptable object.
        /// Based on ColossalFramework.Packaging.PackageDeserializer.DeserializeScriptableObject.
        /// </summary>
        /// <param name="type">Object type.</param>
        /// <returns>Deserialized object.</returns>
        private UnityEngine.Object DeserializeScriptableObject(Type type)
        {
            // Skip gamecode custom deserializer check.
            ScriptableObject scriptableObject = ScriptableObject.CreateInstance(type);
            scriptableObject.name = _reader.ReadString();
            DeserializeFields(scriptableObject, type, resolveMember: false);
            return scriptableObject;
        }

        /// <summary>
        /// Deserializes a generic object.
        /// </summary>
        /// <param name="type">Object type.</param>
        /// <returns>Deserializaed fields.</returns>
        private object DeserializeObject(Type type)
        {
            // Skips gamecode custom deserializer check.
            object obj = Activator.CreateInstance(type);
            _reader.ReadString();
            DeserializeFields(obj, type, resolveMember: true);
            return obj;
        }

        /// <summary>
        /// Deserializes a transform.
        /// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeTransform.
        /// </summary>
        /// <param name="transform">Transform to deserialize.</param>
        private void DeserializeTransform(Transform transform)
        {
            transform.localPosition = _reader.ReadVector3();
            transform.localRotation = _reader.ReadQuaternion();
            transform.localScale = _reader.ReadVector3();
        }

        /// <summary>
        /// Deserializes a mesh filter.
        /// Implements mesh sharing.
        /// </summary>
        /// <param name="meshFilter">Mesh filter to deserialize.</param>
        private void DeserializeMeshFilter(MeshFilter meshFilter)
        {
            meshFilter.sharedMesh = Instance<Sharing>.instance.GetMesh(_reader.ReadString(), _package, _isMain);
        }

        /// <summary>
        /// Deserializes a mesh renderer.
        /// Implements mesh sharing.
        /// </summary>
        /// <param name="renderer">Renderer to deserialize.</param>
        private void DeserializeMeshRenderer(MeshRenderer renderer)
        {
            int materialCount = _reader.ReadInt32();
            Material[] materials = new Material[materialCount];
            Sharing instance = Instance<Sharing>.instance;
            for (int i = 0; i < materialCount; ++i)
            {
                materials[i] = instance.GetMaterial(_reader.ReadString(), _package, _isMain);
            }

            renderer.sharedMaterials = materials;
        }

        /// <summary>
        /// Deserializes a generic MonoBehaviour object.
        /// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeMonoBehaviour with slightly faster performance.
        /// </summary>
        /// <param name="behaviour">MonoBehaviour to deserialize.</param>
        private void DeserializeMonoBehaviour(MonoBehaviour behaviour)
        {
            // DeserializeFields speeds loads with known data types.
            DeserializeFields(behaviour, behaviour.GetType(), resolveMember: false);
        }

        /// <summary>
        /// Deserializaes a skinned mesh renderer.
        /// Implements mesh and material sharing.
        /// </summary>
        /// <param name="renderer">Renderer to deserialize.</param>
        private void DeserializeSkinnedMeshRenderer(SkinnedMeshRenderer renderer)
        {
            int materialCount = _reader.ReadInt32();
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materialCount; ++i)
            {
                materials[i] = Instance<Sharing>.instance.GetMaterial(_reader.ReadString(), _package, _isMain);
            }

            renderer.sharedMaterials = materials;
            renderer.sharedMesh = Instance<Sharing>.instance.GetMesh(_reader.ReadString(), _package, _isMain);
        }

        /// <summary>
        /// Deserializes an animator.
        /// Local implementation of ColossalFramework.Packaging.PackageDeserializer.
        /// </summary>
        /// <param name="animator">Animator to deserialize.</param>
        private void DeserializeAnimator(Animator animator)
        {
            animator.applyRootMotion = _reader.ReadBoolean();
            animator.updateMode = (AnimatorUpdateMode)_reader.ReadInt32();
            animator.cullingMode = (AnimatorCullingMode)_reader.ReadInt32();
        }
    }
}
