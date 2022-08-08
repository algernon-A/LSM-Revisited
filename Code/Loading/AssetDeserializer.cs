namespace LoadingScreenModRevisited
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Runtime.CompilerServices;
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
		private readonly Package package;
		private readonly PackageReader reader;
		private bool isMain;
		private readonly bool isTop;


		/// <summary>
		/// Deserializes an asset from a sharing stream.
		/// </summary>
		/// <param name="asset">Asset to deserialise</param>
		/// <param name="isMain">True if this assset is the main package asset, false otherwise</param>
		/// <param name="isTop">True if this is the top asset, false otherwise</param>
		/// <returns>Deserialized object</returns>
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
		/// <param name="package">Containing package</param>
		/// <param name="bytes">True if this is the top asset, false otherwise</param>
		/// <param name="isMain">True if the assset is the main package asset, false otherwise</param>
		/// <returns>Deserialized object</returns>
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
		/// <param name="asset">Asset to deserialise</param>
		/// <param name="isMain">True if this assset is the main package asset, false otherwise</param>
		/// <param name="isTop">True if this is the top asset, false otherwise</param>
		/// <returns>Deserialized object</returns>
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
		/// Harmomny reverse-patched method stub for AssetSerializer.DeserializeHeader(out Type, PackageReader).
		/// </summary>
		/// <param name="type">Asset type</param>
		/// <param name="reader">PackageReader instance</param>
		/// <returns>True if a known type was extracted, false otherwise (after throwing an exception)</returns>
		/// <exception cref="NotImplementedException">Thrown if reverse patch wasn't successful</exception>
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool DeserializeHeader(out Type type, PackageReader reader)
		{
			Logging.Error("DeserializeHeader reverse Harmony patch wasn't applied with params ", reader);
			throw new NotImplementedException();
		}

		/// <summary>
		/// Harmomny reverse-patched method stub for AssetSerializer.DeserializeHeader(out Type, out string, PackageReader).
		/// </summary>
		/// <param name="type">Asset type</param
		/// <param name="name">Asset name</param>
		/// <param name="reader">PackageReader instance</param>
		/// <returns>True if a known type was extracted, false otherwise (after throwing an exception)</returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool DeserializeHeaderName(out Type type, out string name, PackageReader reader)
		{
			Logging.Error("DeserializeHeaderName reverse Harmony patch wasn't applied with params ", reader);
			throw new NotImplementedException();
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="package">Package</param>
		/// <param name="reader">PackageReader instance</param>
		/// <param name="isMain">True if this assset is the main package asset, false otherwise</param>
		/// <param name="isTop">True if this is the top asset, false otherwise</param>
		private AssetDeserializer(Package package, PackageReader reader, bool isMain, bool isTop)
		{
			this.package = package;
			this.reader = reader;
			this.isMain = isMain;
			this.isTop = isTop;
		}


		/// <summary>
		/// Gets a reader for the specified stream.
		/// </summary>
		/// <param name="stream">Stream to read from</param>
		/// <returns>New MemReader (if stream is MemStream) or PackageReader (otherwise)</returns>
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
		/// <returns>Deserialized object</returns>
		private object Deserialize()
		{
			if (!DeserializeHeader(out Type type, reader))
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
		/// <param name="type">Object type</param>
		/// <param name="expectedType">Expected object type</param>
		/// <returns>Deserialized object</returns>
		private object DeserializeSingleObject(Type type, Type expectedType)
		{
			// Attempt custom deserialization.
			object obj = Instance<CustomDeserializer>.instance.CustomDeserialize(package, type, reader);
			if (obj != null)
			{
				// Success - return object.
				return obj;
			}

			// Follow gamecode, but call FindByChecksum directly rather than via intermediate steps.
			if (typeof(ScriptableObject).IsAssignableFrom(type))
			{
				return Instantiate(package.FindByChecksum(reader.ReadString()), isMain, isTop: false);
			}
			if (typeof(GameObject).IsAssignableFrom(type))
			{
				return Instantiate(package.FindByChecksum(reader.ReadString()), isMain, isTop: false);
			}

			try
			{
				// Gamecode.
				if (package.version < 3 && expectedType != null && expectedType == typeof(Package.Asset))
				{
					return reader.ReadUnityType(expectedType);
				}
				return reader.ReadUnityType(type, package);
			}
			catch (MissingMethodException)
			{
				Logging.Error("unsupported type for deserialization: ", type.Name);
				return null;
			}
		}


		/// <summary>
		/// Deserializes an object's fields.
		/// Implements rapid reads of known simple types.
		/// </summary>
		/// <param name="obj">Object to deserialize</param>
		/// <param name="type">Object type</param>
		/// <param name="resolveMember">True to resolve any legacy members</param>
		private void DeserializeFields(object obj, Type type, bool resolveMember)
		{
			int fieldCount = reader.ReadInt32();
			for (int i = 0; i < fieldCount; ++i)
			{
				if (!DeserializeHeaderName(out Type headerType, out string name, reader))
				{
					continue;
				}
				FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field == null && resolveMember)
				{
					field = type.GetField(ResolveLegacyMember(headerType, type, name), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}

				// Rapid reads of simple types.
				object value;
				if (headerType == typeof(bool))
				{
					value = reader.ReadBoolean();
				}
				else if (headerType == typeof(int))
				{
					value = reader.ReadInt32();
				}
				else if (headerType == typeof(float))
				{
					value = reader.ReadSingle();
				}
				else if (headerType.IsArray)
				{
					int arraySize = reader.ReadInt32();
					Type elementType = headerType.GetElementType();
					if (elementType == typeof(Vector2))
					{
						Vector2[] vectors = new Vector2[arraySize];
						value = vectors;
						for (int j = 0; j < arraySize; ++j)
						{
							vectors[j] = reader.ReadVector2();
						}
					}
					else if (elementType == typeof(float))
					{
						float[] floats = new float[arraySize];
						value = floats;
						for (int j = 0; j < arraySize; ++j)
						{
							floats[j] = reader.ReadSingle();
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
		/// <returns>Deserialized GameObject</returns>
		/// <exception cref="InvalidDataException">Thrown if GameObject type is unknown</exception>
		private UnityEngine.Object DeserializeGameObject()
		{
			GameObject gameObject = new GameObject(reader.ReadString());
			gameObject.tag = reader.ReadString();
			gameObject.layer = reader.ReadInt32();
			gameObject.SetActive(reader.ReadBoolean());
			int count = reader.ReadInt32();

			// Insert to gamecode.
			isMain = isTop || count > 3;

			// Unwrapped ColossalFramework.Packaging.PackageDeserializer.DeserializeComponent.
			for (int i = 0; i < count; ++i)
			{
				if (!DeserializeHeader(out Type type, reader))
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
		/// <returns>Deserialized mesh</returns>
		private UnityEngine.Object DeserializeMesh()
		{
			Mesh mesh = new Mesh();
			mesh.name = reader.ReadString();
			mesh.vertices = reader.ReadVector3Array();
			mesh.colors = reader.ReadColorArray();
			mesh.uv = reader.ReadVector2Array();
			mesh.normals = reader.ReadVector3Array();
			mesh.tangents = reader.ReadVector4Array();
			mesh.boneWeights = reader.ReadBoneWeightsArray();
			mesh.bindposes = reader.ReadMatrix4x4Array();
			mesh.subMeshCount = reader.ReadInt32();
			for (int i = 0; i < mesh.subMeshCount; ++i)
			{
				mesh.SetTriangles(reader.ReadInt32Array(), i);
			}
			return mesh;
		}


		/// <summary>
		/// Deserializes a material.
		/// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeMaterial.
		/// </summary>
		/// <returns>Deserialized material</returns>
		private MaterialData DeserializeMaterial()
		{
			string name = reader.ReadString();
			Material material = new Material(Shader.Find(reader.ReadString()));
			material.name = name;
			int num = reader.ReadInt32();
			int num2 = 0;
			Sharing instance = Instance<Sharing>.instance;
			Texture2D texture2D = null;
			for (int i = 0; i < num; i++)
			{
				switch (reader.ReadInt32())
				{
					case 0:
						material.SetColor(reader.ReadString(), reader.ReadColor());
						break;
					case 1:
						material.SetVector(reader.ReadString(), reader.ReadVector4());
						break;
					case 2:
						material.SetFloat(reader.ReadString(), reader.ReadSingle());
						break;
					case 3:
						{
							string name2 = reader.ReadString();
							if (!reader.ReadBoolean())
							{
								string checksum = reader.ReadString();
								texture2D = instance.GetTexture(checksum, package, isMain);
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
			if (instance.checkAssets && !isMain && texture2D != null)
			{
				instance.Check(materialData, texture2D);
			}
			return materialData;
		}


		/// <summary>
		/// Deserializes a texture.
		/// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeTexture.
		/// </summary>
		/// <param name="animator">Animator to deserialize</param>
		private UnityEngine.Object DeserializeTexture()
		{
			string name = reader.ReadString();
			bool linear = reader.ReadBoolean();
			int anisoLevel = ((package.version < 6) ? 1 : reader.ReadInt32());
			int count = reader.ReadInt32();
			Texture2D texture2D = new Image(reader.ReadBytes(count)).CreateTexture(linear);
			texture2D.name = name;
			texture2D.anisoLevel = anisoLevel;
			return texture2D;
		}


		/// <summary>
		/// Deserializes a scriptable object.
		/// Based on ColossalFramework.Packaging.PackageDeserializer.DeserializeScriptableObject.
		/// </summary>
		/// <param name="type">Object type</param>
		/// <returns>Deserialized object</returns>
		private UnityEngine.Object DeserializeScriptableObject(Type type)
		{
			// Skip gamecode custom deserializer check.
			ScriptableObject scriptableObject = ScriptableObject.CreateInstance(type);
			scriptableObject.name = reader.ReadString();
			DeserializeFields(scriptableObject, type, resolveMember: false);
			return scriptableObject;
		}


		/// <summary>
		/// Deserializes a generic object.
		/// </summary>
		/// <param name="type">Object type</param>
		/// <returns>Deserializaed fields</returns>
		private object DeserializeObject(Type type)
		{
			// Skips gamecode custom deserializer check.
			object obj = Activator.CreateInstance(type);
			reader.ReadString();
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
			transform.localPosition = reader.ReadVector3();
			transform.localRotation = reader.ReadQuaternion();
			transform.localScale = reader.ReadVector3();
		}


		/// <summary>
		/// Deserializes a mesh filter.
		/// Implements mesh sharing.
		/// </summary>
		/// <param name="meshFilter">Mesh filter to deserialize</param>
		private void DeserializeMeshFilter(MeshFilter meshFilter)
		{
			meshFilter.sharedMesh = Instance<Sharing>.instance.GetMesh(reader.ReadString(), package, isMain);
		}


		/// <summary>
		/// Deserializes a mesh renderer.
		/// Implements mesh sharing.
		/// </summary>
		/// <param name="renderer">Renderer to deserialize</param>
		private void DeserializeMeshRenderer(MeshRenderer renderer)
		{
			int materialCount = reader.ReadInt32();
			Material[] materials = new Material[materialCount];
			Sharing instance = Instance<Sharing>.instance;
			for (int i = 0; i < materialCount; ++i)
			{
				materials[i] = instance.GetMaterial(reader.ReadString(), package, isMain);
			}
			renderer.sharedMaterials = materials;
		}


		/// <summary>
		/// Deserializes a generic MonoBehaviour object.
		/// Local implementation of ColossalFramework.Packaging.PackageDeserializer.DeserializeMonoBehaviour with slightly faster performance.
		/// </summary>
		/// <param name="behaviour">MonoBehaviour to deserialize</param>
		private void DeserializeMonoBehaviour(MonoBehaviour behaviour)
		{
			// DeserializeFields speeds loads with known data types.
			DeserializeFields(behaviour, behaviour.GetType(), resolveMember: false);
		}


		/// <summary>
		/// Deserializaes a skinned mesh renderer.
		/// Implements mesh and material sharing.
		/// </summary>
		/// <param name="renderer">Renderer to deserialize</param>
		private void DeserializeSkinnedMeshRenderer(SkinnedMeshRenderer renderer)
		{
			int materialCount = reader.ReadInt32();
			Material[] materials = new Material[materialCount];
			for (int i = 0; i < materialCount; ++i)
			{
				materials[i] = Instance<Sharing>.instance.GetMaterial(reader.ReadString(), package, isMain);
			}
			renderer.sharedMaterials = materials;
			renderer.sharedMesh = Instance<Sharing>.instance.GetMesh(reader.ReadString(), package, isMain);
		}


		/// <summary>
		/// Deserializes an animator.
		/// Local implementation of ColossalFramework.Packaging.PackageDeserializer.
		/// </summary>
		/// <param name="animator">Animator to deserialize</param>
		private void DeserializeAnimator(Animator animator)
		{
			animator.applyRootMotion = reader.ReadBoolean();
			animator.updateMode = (AnimatorUpdateMode)reader.ReadInt32();
			animator.cullingMode = (AnimatorCullingMode)reader.ReadInt32();
		}





		/// <summary>
		/// Harmomny reverse-patched method stub for PackageDeserializer.ResolveLegacyMember.
		/// Attempts to resolve any legacy members using the game's legacy member handler.
		/// </summary>
		/// <param name="fieldType">Field type to resolve</param>
		/// <param name="classType">Class type to resolve</param>
		/// <param name="member">Member name</param>
		/// <returns>Updated member name (if available), or original text</returns>
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(PackageDeserializer), "ResolveLegacyMember")] 
		[MethodImpl(MethodImplOptions.NoInlining)]
		private string ResolveLegacyMember(Type fieldType, Type classType, string member)
		{
			Logging.Error("DeserializeHeaderName reverse Harmony patch wasn't applied with params ", fieldType, classType, member);
			throw new NotImplementedException();
		}
	}
}
