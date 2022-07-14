using System;
using System.IO;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Importers;
using ColossalFramework.Packaging;
using UnityEngine;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
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
		/// Deserializes an object.
		/// Based on ColossalFramework.Packaging.PackageDeserializer.
		/// </summary>
		/// <returns>Deserialized object</returns>
		private object Deserialize()
		{
			if (!DeserializeHeader(out Type type))
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
		/// <returns></returns>
		private object DeserializeSingleObject(Type type, Type expectedType)
		{
			object obj = Instance<CustomDeserializer>.instance.CustomDeserialize(package, type, reader);
			if (obj != null)
			{
				return obj;
			}
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


		// Replace with reverse patch to ColossalFramework.Packaging.PackageDeserializer.DeserializeScriptableObject?
		private UnityEngine.Object DeserializeScriptableObject(Type type)
		{
			ScriptableObject scriptableObject = ScriptableObject.CreateInstance(type);
			scriptableObject.name = reader.ReadString();
			DeserializeFields(scriptableObject, type, resolveMember: false);
			return scriptableObject;
		}

		// Replace with gamecode from DeserializeObject/DeserializeScriptableObject?
		private void DeserializeFields(object obj, Type type, bool resolveMember)
		{
			int num = reader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				if (!DeserializeHeader(out var type2, out var name))
				{
					continue;
				}
				FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field == null && resolveMember)
				{
					field = type.GetField(ResolveLegacyMember(type2, type, name), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				object value;
				if (type2 == typeof(bool))
				{
					value = reader.ReadBoolean();
				}
				else if (type2 == typeof(int))
				{
					value = reader.ReadInt32();
				}
				else if (type2 == typeof(float))
				{
					value = reader.ReadSingle();
				}
				else if (type2.IsArray)
				{
					int num2 = reader.ReadInt32();
					Type elementType = type2.GetElementType();
					if (elementType == typeof(Vector2))
					{
						Vector2[] array = new Vector2[num2];
						value = array;
						for (int j = 0; j < num2; j++)
						{
							array[j] = reader.ReadVector2();
						}
					}
					else if (elementType == typeof(float))
					{
						float[] array2 = new float[num2];
						value = array2;
						for (int k = 0; k < num2; k++)
						{
							array2[k] = reader.ReadSingle();
						}
					}
					else
					{
						Array array3 = Array.CreateInstance(elementType, num2);
						value = array3;
						for (int l = 0; l < num2; l++)
						{
							array3.SetValue(DeserializeSingleObject(elementType, field?.FieldType), l);
						}
					}
				}
				else
				{
					value = DeserializeSingleObject(type2, field?.FieldType);
				}
				field?.SetValue(obj, value);
			}
		}


		/// <summary>
		/// Deserializes a GameObject.
		/// Based on ColossalFramework.Packaging.PackageDeserializer.DeserializeGameObject.
		/// Custom Asset Loader Postfixes this.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidDataException"></exception>
		private UnityEngine.Object DeserializeGameObject()
		{
			GameObject gameObject = new GameObject(reader.ReadString());
			gameObject.tag = reader.ReadString();
			gameObject.layer = reader.ReadInt32();
			gameObject.SetActive(reader.ReadBoolean());
			int num = reader.ReadInt32();

			// Insert to gamecode.
			isMain = isTop || num > 3;

			// Unwrapped ColossalFramework.Packaging.PackageDeserializer.DeserializeComponent.
			for (int i = 0; i < num; i++)
			{
				if (!DeserializeHeader(out var type))
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


		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		//internal static void DeserializeAnimator(Package package, Animator animator, PackageReader reader)

		private void DeserializeAnimator(Animator animator)
		{
			animator.applyRootMotion = reader.ReadBoolean();
			animator.updateMode = (AnimatorUpdateMode)reader.ReadInt32();
			animator.cullingMode = (AnimatorCullingMode)reader.ReadInt32();
		}


		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		// internal static Object DeserializeTexture(Package package, PackageReader reader)
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


		// ColossalFramework.Packaging.PackageDeserializer
		// internal static Object DeserializeMaterial(Package package, PackageReader reader)
		// LSM sharing inserts
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


		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		// internal static void DeserializeTransform(Package package, Transform transform, PackageReader reader)
		private void DeserializeTransform(Transform transform)
		{
			transform.localPosition = reader.ReadVector3();
			transform.localRotation = reader.ReadQuaternion();
			transform.localScale = reader.ReadVector3();
		}



		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		// internal static void DeserializeMeshFilter(Package package, MeshFilter meshFilter, PackageReader reader)
		private void DeserializeMeshFilter(MeshFilter meshFilter)
		{
			meshFilter.sharedMesh = Instance<Sharing>.instance.GetMesh(reader.ReadString(), package, isMain);
		}


		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		// internal static void DeserializeMonoBehaviour(Package package, MonoBehaviour behaviour, PackageReader reader)
		// Note that deserializefields is the same as object and scriptableobject.
		private void DeserializeMonoBehaviour(MonoBehaviour behaviour)
		{
			DeserializeFields(behaviour, behaviour.GetType(), resolveMember: false);
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
		/// Deserializaes a mesh renderer.
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


		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		// internal static Object DeserializeMesh(Package package, PackageReader reader)
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
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				mesh.SetTriangles(reader.ReadInt32Array(), i);
			}
			return mesh;
		}
		
		
		// ColossalFramework.Packaging.AssetSerializer
		// Reverse patch
		private bool DeserializeHeader(out Type type)
		{
			type = null;
			if (reader.ReadBoolean())
			{
				return false;
			}
			string text = reader.ReadString();
			type = Type.GetType(text);
			if (type == null)
			{
				type = Type.GetType(ResolveLegacyType(text));
				if (type == null)
				{
					if (HandleUnknownType(text) < 0)
					{
						throw new InvalidDataException("Unknown type to deserialize " + text);
					}
					return false;
				}
			}
			return true;
		}


		// Replace with reverse patch.
		// ColossalFramework.Packaging.AssetSerializer
		// public static bool DeserializeHeader(out Type type, out string name, PackageReader reader)
		private bool DeserializeHeader(out Type type, out string name)
		{
			type = null;
			name = null;
			if (reader.ReadBoolean())
			{
				return false;
			}
			string text = reader.ReadString();
			type = Type.GetType(text);
			name = reader.ReadString();
			if (type == null)
			{
				type = Type.GetType(ResolveLegacyType(text));
				if (type == null)
				{
					if (HandleUnknownType(text) < 0)
					{
						throw new InvalidDataException("Unknown type to deserialize " + text);
					}
					return false;
				}
			}
			return true;
		}


		// Replace with reverse patch.
		// ColossalFramework.Packaging.AssetSerializer
		// private static int HandleUnknownType(string type, PackageReader reader)
		private int HandleUnknownType(string type)
		{
			int num = PackageHelper.UnknownTypeHandler(type);
			CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, "Unexpected type '" + type + "' detected. No resolver handled this type. Skipping " + num + " bytes.");
			if (num > 0)
			{
				reader.ReadBytes(num);
				return num;
			}
			return -1;
		}


		/// <summary>
		/// Attempts to resolve any legacy types using the game's legacy type handler.
		/// Replace with reverse patch to ColossalFramework.Packaging.PackageDeserializer
		/// internal static string ResolveLegacyType(string type)
		/// </summary>
		/// <param name="type">Type to resolve</param>
		/// <returns>Resolved type text (unchanged if no conversion was found)</returns>
		private static string ResolveLegacyType(string type)
		{
			string text = PackageHelper.ResolveLegacyTypeHandler(type);
			CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, "Unkown type detected. Attempting to resolve from '" + type + "' to '" + text + "'");
			return text;
		}


		// Replace with reverse patch.
		// ColossalFramework.Packaging.PackageDeserializer
		// internal static string ResolveLegacyMember(Type fieldType, Type classType, string member)
		private static string ResolveLegacyMember(Type fieldType, Type classType, string member)
		{
			string text = PackageHelper.ResolveLegacyMemberHandler(classType, member);
			CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, "Unkown member detected of type " + fieldType.FullName + " in " + classType.FullName + ". Attempting to resolve from '" + member + "' to '" + text + "'");
			return text;
		}
	}
}
