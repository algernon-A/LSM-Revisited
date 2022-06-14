using System;
using System.IO;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Importers;
using ColossalFramework.Packaging;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class AssetDeserializer
	{
		private readonly Package package;

		private readonly PackageReader reader;

		private bool isMain;

		private readonly bool isTop;

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

		private AssetDeserializer(Package package, PackageReader reader, bool isMain, bool isTop)
		{
			this.package = package;
			this.reader = reader;
			this.isMain = isMain;
			this.isTop = isTop;
		}

		private object Deserialize()
		{
			if (!DeserializeHeader(out var type))
			{
				return null;
			}
			if (type == typeof(GameObject))
			{
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

		private object DeserializeSingleObject(Type type, Type expectedType)
		{
			object obj = Instance<CustomDeserializer>.instance.CustomDeserialize(package, type, reader);
			if (obj != null)
				return obj;
			if (typeof(ScriptableObject).IsAssignableFrom(type))
				//return Instantiate(FindAsset(reader.ReadString()), isMain);
				return Instantiate(package.FindByChecksum(reader.ReadString()), isMain, isTop: false);
			if (typeof(GameObject).IsAssignableFrom(type))
				//return Instantiate(FindAsset(reader.ReadString()), isMain);
				return Instantiate(package.FindByChecksum(reader.ReadString()), isMain, isTop: false);

			try
			{
				if (package.version < 3 && expectedType != null && expectedType == typeof(Package.Asset))
					return reader.ReadUnityType(expectedType);

				return reader.ReadUnityType(type, package);
			}
			catch (MissingMethodException)
			{
				Util.DebugPrint("Unsupported type for deserialization:", type.Name);
				return null;
			}
		}

		private UnityEngine.Object DeserializeScriptableObject(Type type)
		{
			ScriptableObject scriptableObject = ScriptableObject.CreateInstance(type);
			scriptableObject.name = reader.ReadString();
			DeserializeFields(scriptableObject, type, resolveMember: false);
			return scriptableObject;
		}

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

		private UnityEngine.Object DeserializeGameObject()
		{
			GameObject gameObject = new GameObject(reader.ReadString());
			gameObject.tag = reader.ReadString();
			gameObject.layer = reader.ReadInt32();
			gameObject.SetActive(reader.ReadBoolean());
			int num = reader.ReadInt32();
			isMain = isTop || num > 3;
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

		private void DeserializeAnimator(Animator animator)
		{
			animator.applyRootMotion = reader.ReadBoolean();
			animator.updateMode = (AnimatorUpdateMode)reader.ReadInt32();
			animator.cullingMode = (AnimatorCullingMode)reader.ReadInt32();
		}

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

		private void DeserializeTransform(Transform transform)
		{
			transform.localPosition = reader.ReadVector3();
			transform.localRotation = reader.ReadQuaternion();
			transform.localScale = reader.ReadVector3();
		}

		private void DeserializeMeshFilter(MeshFilter meshFilter)
		{
			meshFilter.sharedMesh = Instance<Sharing>.instance.GetMesh(reader.ReadString(), package, isMain);
		}

		private void DeserializeMonoBehaviour(MonoBehaviour behaviour)
		{
			DeserializeFields(behaviour, behaviour.GetType(), resolveMember: false);
		}

		private object DeserializeObject(Type type)
		{
			object obj = Activator.CreateInstance(type);
			reader.ReadString();
			DeserializeFields(obj, type, resolveMember: true);
			return obj;
		}

		private void DeserializeMeshRenderer(MeshRenderer renderer)
		{
			int num = reader.ReadInt32();
			Material[] array = new Material[num];
			Sharing instance = Instance<Sharing>.instance;
			for (int i = 0; i < num; i++)
			{
				array[i] = instance.GetMaterial(reader.ReadString(), package, isMain);
			}
			renderer.sharedMaterials = array;
		}

		private void DeserializeSkinnedMeshRenderer(SkinnedMeshRenderer smr)
		{
			int num = reader.ReadInt32();
			Material[] array = new Material[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = Instance<Sharing>.instance.GetMaterial(reader.ReadString(), package, isMain);
			}
			smr.sharedMaterials = array;
			smr.sharedMesh = Instance<Sharing>.instance.GetMesh(reader.ReadString(), package, isMain);
		}

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

		private static PackageReader GetReader(Stream stream)
		{
			MemStream memStream = stream as MemStream;
			if (memStream == null)
			{
				return new PackageReader(stream);
			}
			return new MemReader(memStream);
		}

		private static bool IsPowerOfTwo(int i)
		{
			return (i & (i - 1)) == 0;
		}

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

		private static string ResolveLegacyType(string type)
		{
			string text = PackageHelper.ResolveLegacyTypeHandler(type);
			CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, "Unkown type detected. Attempting to resolve from '" + type + "' to '" + text + "'");
			return text;
		}

		private static string ResolveLegacyMember(Type fieldType, Type classType, string member)
		{
			string text = PackageHelper.ResolveLegacyMemberHandler(classType, member);
			CODebugBase<InternalLogChannel>.Warn(InternalLogChannel.Packer, "Unkown member detected of type " + fieldType.FullName + " in " + classType.FullName + ". Attempting to resolve from '" + member + "' to '" + text + "'");
			return text;
		}
	}
}
