using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ColossalFramework.Importers;
using ColossalFramework.Packaging;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class Sharing : Instance<Sharing>
	{
		private const int maxData = 600;

		private const int evictData = 500;

		private const int maxMaterial = 70;

		private const int indexHistory = 9;

		private const int targetCost = 600;

		private const int evictCost = 1000;

		private const int SHIFT = 18;

		private const int MASK = 262143;

		private const int COSTMASK = -262144;

		private const int OBJ = 1;

		private const int MAT = 2;

		private const int TEX = 3;

		private const int MSH = 4;

		private const int OBJSHIFT = 13;

		private const int MTSHIFT = 16;

		private const int OBJMAXSIZE = 409600;

		private LinkedHashMap<string, Triple> data = new LinkedHashMap<string, Triple>(300);

		private readonly object mutex = new object();

		private int currentCosts;

		private int loaderIndex;

		private int mainIndex;

		private int currentCount;

		private int maxCount;

		private int readyIndex = -1;

		private readonly ConcurrentQueue<Triple> mtQueue = new ConcurrentQueue<Triple>(32);

		private readonly Atomic<int> readySlot = new Atomic<int>();

		private List<Package.Asset> loadList;

		private List<Triple> loadedList;

		internal int texhit;

		internal int mathit;

		internal int meshit;

		private int texpre;

		private int texload;

		private int matpre;

		private int matload;

		private int mespre;

		private int mesload;

		private Dictionary<string, Texture2D> texturesMain = new Dictionary<string, Texture2D>(256);

		private Dictionary<string, Texture2D> texturesLod = new Dictionary<string, Texture2D>(256);

		private Dictionary<string, MaterialData> materialsMain = new Dictionary<string, MaterialData>(128);

		private Dictionary<string, MaterialData> materialsLod = new Dictionary<string, MaterialData>(128);

		private Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>(256);

		private Dictionary<MaterialData, int> weirdMaterials;

		private Dictionary<MaterialData, int> largeMaterials;

		private readonly bool shareTextures;

		private readonly bool shareMaterials;

		private readonly bool shareMeshes;

		private readonly bool mustPrune;

		internal readonly bool checkAssets;

		internal int LoaderAhead => loaderIndex - mainIndex;

		internal string Status => currentCount + " " + currentCosts;

		internal int Misses => texload + matload + mesload;

		private static bool Supports(int type)
		{
			return type <= 4 && type >= 1;
		}

		private void Prune()
		{
			int num = currentCosts;
			int num2 = mainIndex;
			int num3 = num2 - 9;
			for (int num4 = data.Count; num4 > 0; num4--)
			{
				int code = data.Eldest.code;
				int num5 = code & 0x3FFFF;
				if ((num3 < num5 && num4 < 500 && num < 1000) || num2 <= num5)
				{
					break;
				}
				num -= code >> 18;
				data.RemoveEldest();
			}
			currentCosts = num;
		}

		private void Send(int acc)
		{
			if (loadedList.Count <= 0)
			{
				return;
			}
			lock (mutex)
			{
				foreach (Triple loaded in loadedList)
				{
					string key = loaded.obj as string;
					if (!data.ContainsKey(key))
					{
						loaded.obj = null;
						data.Add(key, loaded);
					}
				}
				currentCosts += acc;
			}
			loadedList.Clear();
		}

		private void LoadPackage(int firstIndex, int lastIndex, Package package, Package.Asset[] q)
		{
			loadList.Clear();
			int num = firstIndex;
			bool flag;
			lock (mutex)
			{
				loaderIndex = firstIndex;
				Prune();
				int num2 = 0;
				int num3 = Mathf.Min(70, 600 - data.Count);
				foreach (Package.Asset item in package)
				{
					string name = item.name;
					int num4 = item.type;
					if (!Supports(num4) || name.EndsWith("_SteamPreview") || name.EndsWith("_Snapshot") || name == "UserAssetData")
					{
						continue;
					}
					string checksum = item.checksum;
					if ((num4 == 3 && (texturesMain.ContainsKey(checksum) || texturesLod.ContainsKey(checksum))) || (num4 == 4 && meshes.ContainsKey(checksum)) || (num4 == 2 && (materialsMain.ContainsKey(checksum) || materialsLod.ContainsKey(checksum) || ++num2 > num3)))
					{
						continue;
					}
					if (num4 == 1 && name.StartsWith(" - "))
					{
						break;
					}
					if (data.TryGetValue(checksum, out var val))
					{
						val.code = (val.code & -262144) | lastIndex;
						data.Reinsert(checksum);
						if (num < lastIndex && (object)item == q[num])
						{
							num++;
						}
					}
					else
					{
						loadList.Add(item);
					}
				}
				flag = CanLoad();
			}
			if (loadList.Count == 0)
			{
				return;
			}
			using (FileStream fileStream = new FileStream(package.packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192))
			{
				if (!flag)
				{
					fileStream.Position = loadList[0].offset;
					lock (mutex)
					{
						while (!CanLoad())
						{
							Monitor.Wait(mutex);
						}
					}
				}
				loadedList.Clear();
				int code = (lastIndex - num + 1 << 18) | lastIndex;
				int num5 = 0;
				for (int i = 0; i < loadList.Count; i++)
				{
					Package.Asset asset2 = loadList[i];
					byte[] bytes = LoadAsset(fileStream, asset2);
					int num6 = asset2.type;
					int num7 = lastIndex;
					if (num6 > 1)
					{
						if (num6 > 2)
						{
							mtQueue.Enqueue(new Triple(asset2, bytes, code));
							int num8 = asset2.size >> 16;
							num7 |= num8 << 18;
							num5 += num8;
						}
						loadedList.Add(new Triple(asset2.checksum, bytes, num7));
						continue;
					}
					int size = asset2.size;
					if (size > 32768)
					{
						int num9 = Mathf.Min(size, 409600) >> 13;
						num7 |= num9 << 18;
						num5 += num9;
					}
					loadedList.Add(new Triple(asset2.checksum, bytes, num7));
					if (num < lastIndex && (object)asset2 == q[num])
					{
						int num10 = num - firstIndex;
						if (num10 < 3 || (num10 & 3) == 0)
						{
							Send(num5);
							num5 = 0;
							code = (lastIndex - num << 18) | lastIndex;
						}
						num++;
					}
				}
				Send(num5);
			}
			loadList.Clear();
		}

		private static byte[] LoadAsset(FileStream fs, Package.Asset asset)
		{
			int num = asset.size;
			if (num > 222444000 || num < 0)
			{
				throw new IOException("Asset " + asset.fullName + " size: " + num);
			}
			long offset = asset.offset;
			if (offset != fs.Position)
			{
				fs.Position = offset;
			}
			byte[] array = new byte[num];
			int num2 = 0;
			while (num > 0)
			{
				int num3 = fs.Read(array, num2, num);
				if (num3 == 0)
				{
					throw new IOException("Unexpected end of file: " + asset.fullName);
				}
				num2 += num3;
				num -= num3;
			}
			return array;
		}

		private static int Forward(int index, Package p, Package.Asset[] q)
		{
			while (++index < q.Length && (object)p == q[index].package)
			{
			}
			return index - 1;
		}

		private void LoadWorker(object param)
		{
			Thread.CurrentThread.Name = "LoadWorker";
			Package.Asset[] array = (Package.Asset[])param;
			loadList = new List<Package.Asset>(64);
			loadedList = new List<Triple>(64);
			int num = 0;
			while (num < array.Length)
			{
				Package package = array[num].package;
				int num2 = Forward(num, package, array);
				try
				{
					LoadPackage(num, num2, package, array);
				}
				catch (Exception ex)
				{
					Util.DebugPrint("LoadWorker", package.packageName, ex.Message);
				}
				mtQueue.Enqueue(new Triple(num2));
				num = num2 + 1;
			}
			mtQueue.SetCompleted();
			loadList = null;
			loadedList = null;
			array = null;
		}

		private void MTWorker()
		{
			Thread.CurrentThread.Name = "MTWorker";
			int num = -1;
			Triple result;
			while (mtQueue.Dequeue(out result))
			{
				int code = result.code;
				int num2 = code & 0x3FFFF;
				int num3 = num2 - (int)((uint)code >> 18);
				if (num < num3)
				{
					num = num3;
					readySlot.Set(num3);
				}
				Package.Asset asset = result.obj as Package.Asset;
				if ((object)asset == null)
				{
					continue;
				}
				try
				{
					byte[] bytes = result.bytes;
					if (asset.type == 3)
					{
						DeserializeTextObj(asset, bytes, num2);
					}
					else
					{
						DeserializeMeshObj(asset, bytes, num2);
					}
				}
				catch (Exception ex)
				{
					Util.DebugPrint("MTWorker", asset.fullName, ex.Message);
				}
			}
			readySlot.Set(int.MaxValue);
		}

		private void DeserializeMeshObj(Package.Asset asset, byte[] bytes, int index)
		{
			MeshObj obj;
			using (MemStream stream = new MemStream(bytes, 0))
			{
				using (MemReader memReader = new MemReader(stream))
				{
					if (DeserializeHeader(memReader) != typeof(Mesh))
					{
						throw new IOException("Asset " + asset.fullName + " should be Mesh");
					}
					string name = memReader.ReadString();
					Vector3[] vertices = memReader.ReadVector3Array();
					Color[] colors = memReader.ReadColorArray();
					Vector2[] uv = memReader.ReadVector2Array();
					Vector3[] normals = memReader.ReadVector3Array();
					Vector4[] tangents = memReader.ReadVector4Array();
					BoneWeight[] boneWeights = memReader.ReadBoneWeightsArray();
					Matrix4x4[] bindposes = memReader.ReadMatrix4x4Array();
					int num = memReader.ReadInt32();
					int[][] array = new int[num][];
					for (int i = 0; i < num; i++)
					{
						array[i] = memReader.ReadInt32Array();
					}
					obj = new MeshObj
					{
						name = name,
						vertices = vertices,
						colors = colors,
						uv = uv,
						normals = normals,
						tangents = tangents,
						boneWeights = boneWeights,
						bindposes = bindposes,
						triangles = array
					};
				}
			}
			string checksum = asset.checksum;
			lock (mutex)
			{
				if (data.TryGetValue(checksum, out var val))
				{
					val.obj = obj;
					val.bytes = null;
				}
				else
				{
					data.Add(checksum, new Triple(obj, (asset.size >> 16 << 18) | index));
				}
			}
		}

		private void DeserializeTextObj(Package.Asset asset, byte[] bytes, int index)
		{
			TextObj obj;
			using (MemStream stream = new MemStream(bytes, 0))
			{
				using (MemReader memReader = new MemReader(stream))
				{
					Type type = DeserializeHeader(memReader);
					if (type != typeof(Texture2D) && type != typeof(Image))
					{
						throw new IOException("Asset " + asset.fullName + " should be Texture2D or Image");
					}
					string name = memReader.ReadString();
					bool linear = memReader.ReadBoolean();
					int anisoLevel = ((asset.package.version < 6) ? 1 : memReader.ReadInt32());
					int count = memReader.ReadInt32();
					Image image = new Image(memReader.ReadBytes(count));
					byte[] allPixels = image.GetAllPixels();
					obj = new TextObj
					{
						name = name,
						pixels = allPixels,
						width = image.width,
						height = image.height,
						anisoLevel = anisoLevel,
						format = image.format,
						mipmap = (image.mipmapCount > 1),
						linear = linear
					};
					image = null;
				}
			}
			string checksum = asset.checksum;
			lock (mutex)
			{
				if (data.TryGetValue(checksum, out var val))
				{
					val.obj = obj;
					val.bytes = null;
				}
				else
				{
					data.Add(checksum, new Triple(obj, (asset.size >> 16 << 18) | index));
				}
			}
		}

		private static Type DeserializeHeader(MemReader reader)
		{
			if (reader.ReadBoolean())
			{
				return null;
			}
			return Type.GetType(reader.ReadString());
		}

		internal void WaitForWorkers(int index)
		{
			lock (mutex)
			{
				mainIndex = index;
				if (mustPrune)
				{
					Prune();
				}
				currentCount = data.Count;
				if (CanLoad())
				{
					Monitor.Pulse(mutex);
				}
			}
			for (maxCount = Mathf.Max(currentCount, maxCount); readyIndex < index; readyIndex = readySlot.Get())
			{
			}
		}

		private bool CanLoad()
		{
			if (currentCosts >= 600 || data.Count >= 500)
			{
				return loaderIndex - mainIndex < 4;
			}
			return true;
		}

		internal Stream GetStream(Package.Asset asset)
		{
			string checksum = asset.checksum;
			int size = asset.size;
			bool flag = size > 32768 || asset.name.EndsWith("_Data");
			Triple val;
			lock (mutex)
			{
				if (flag)
				{
					val = data.Remove(checksum);
					int num = ((val != null) ? (val.code >> 18) : 0);
					if (num > 0)
					{
						int num2 = currentCosts;
						int num3 = (currentCosts = num2 - num);
						if (num2 >= 600 && num3 < 600)
						{
							Monitor.Pulse(mutex);
						}
					}
				}
				else
				{
					data.TryGetValue(checksum, out val);
				}
			}
			byte[] array = val?.bytes;
			if (array != null)
			{
				return new MemStream(array, 0);
			}
			return new FileStream(asset.package.packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, Mathf.Min(size, 8192))
			{
				Position = asset.offset
			};
		}

		internal Mesh GetMesh(string checksum, Package package, bool isMain)
		{
			Triple val;
			Mesh value;
			lock (mutex)
			{
				if (meshes.TryGetValue(checksum, out value))
				{
					meshit++;
					if (checkAssets && !isMain)
					{
						Check(package, value, checksum);
					}
					return value;
				}
				data.TryGetValue(checksum, out val);
			}
			MeshObj meshObj = val?.obj as MeshObj;
			if (meshObj != null)
			{
				value = new Mesh();
				value.name = meshObj.name;
				value.vertices = meshObj.vertices;
				value.colors = meshObj.colors;
				value.uv = meshObj.uv;
				value.normals = meshObj.normals;
				value.tangents = meshObj.tangents;
				value.boneWeights = meshObj.boneWeights;
				value.bindposes = meshObj.bindposes;
				for (int i = 0; i < meshObj.triangles.Length; i++)
				{
					value.SetTriangles(meshObj.triangles[i], i);
				}
				mespre++;
			}
			else
			{
				byte[] array = val?.bytes;
				if (array != null)
				{
					value = AssetDeserializer.Instantiate(package, array, isMain) as Mesh;
					mesload++;
				}
				else
				{
					value = AssetDeserializer.InstantiateOne(package.FindByChecksum(checksum), isMain, isTop: false) as Mesh;
					mesload++;
				}
			}
			if (checkAssets && !isMain)
			{
				Check(package, value, checksum);
			}
			if (shareMeshes)
			{
				lock (mutex)
				{
					meshes[checksum] = value;
					Triple triple = data.Remove(checksum);
					int num = ((triple != null) ? (triple.code >> 18) : 0);
					if (num <= 0)
					{
						return value;
					}
					int num2 = currentCosts;
					int num3 = (currentCosts = num2 - num);
					if (num2 < 600)
					{
						return value;
					}
					if (num3 >= 600)
					{
						return value;
					}
					Monitor.Pulse(mutex);
					return value;
				}
			}
			return value;
		}

		internal Texture2D GetTexture(string checksum, Package package, bool isMain)
		{
			Triple val;
			Texture2D value;
			lock (mutex)
			{
				if (isMain && texturesMain.TryGetValue(checksum, out value))
				{
					texhit++;
					return value;
				}
				if (!isMain && (texturesLod.TryGetValue(checksum, out value) || texturesMain.TryGetValue(checksum, out value)))
				{
					texpre++;
					return UnityEngine.Object.Instantiate(value);
				}
				data.TryGetValue(checksum, out val);
			}
			TextObj textObj = val?.obj as TextObj;
			if (textObj != null)
			{
				value = new Texture2D(textObj.width, textObj.height, textObj.format, textObj.mipmap, textObj.linear);
				value.LoadRawTextureData(textObj.pixels);
				value.Apply();
				value.name = textObj.name;
				value.anisoLevel = textObj.anisoLevel;
				texpre++;
			}
			else
			{
				byte[] array = val?.bytes;
				if (array != null)
				{
					value = AssetDeserializer.Instantiate(package, array, isMain) as Texture2D;
					texload++;
				}
				else
				{
					value = AssetDeserializer.InstantiateOne(package.FindByChecksum(checksum), isMain, isTop: false) as Texture2D;
					texload++;
				}
			}
			if (shareTextures)
			{
				lock (mutex)
				{
					if (isMain)
					{
						texturesMain[checksum] = value;
					}
					else
					{
						texturesLod[checksum] = value;
					}
					Triple triple = data.Remove(checksum);
					int num = ((triple != null) ? (triple.code >> 18) : 0);
					if (num <= 0)
					{
						return value;
					}
					int num2 = currentCosts;
					int num3 = (currentCosts = num2 - num);
					if (num2 < 600)
					{
						return value;
					}
					if (num3 >= 600)
					{
						return value;
					}
					Monitor.Pulse(mutex);
					return value;
				}
			}
			return value;
		}

		internal Material GetMaterial(string checksum, Package package, bool isMain)
		{
			Triple val;
			MaterialData value;
			lock (mutex)
			{
				if (isMain && materialsMain.TryGetValue(checksum, out value))
				{
					mathit++;
					texhit += value.textureCount;
					return value.material;
				}
				if (!isMain && materialsLod.TryGetValue(checksum, out value))
				{
					matpre++;
					texpre += value.textureCount;
					if (checkAssets)
					{
						Check(package, value, checksum);
					}
					return new Material(value.material);
				}
				data.TryGetValue(checksum, out val);
			}
			byte[] array = val?.bytes;
			if (array != null)
			{
				value = AssetDeserializer.Instantiate(package, array, isMain) as MaterialData;
				matpre++;
			}
			else
			{
				value = AssetDeserializer.InstantiateOne(package.FindByChecksum(checksum), isMain, isTop: false) as MaterialData;
				matload++;
			}
			if (checkAssets && !isMain)
			{
				Check(package, value, checksum);
			}
			if (shareMaterials)
			{
				lock (mutex)
				{
					data.Remove(checksum);
					if (isMain)
					{
						materialsMain[checksum] = value;
					}
					else
					{
						materialsLod[checksum] = value;
					}
				}
			}
			return value.material;
		}

		private void Check(Package package, Mesh mesh, string checksum)
		{
			int v;
			if ((v = mesh.vertices.Length) > 4062)
			{
				Instance<Reports>.instance.AddExtremeMesh(package, checksum, v);
			}
			else if ((v = mesh.triangles.Length) >= 1800)
			{
				Instance<Reports>.instance.AddLargeMesh(package, checksum, -v / 3);
			}
			else if ((v = mesh.vertices.Length) >= 1000)
			{
				Instance<Reports>.instance.AddLargeMesh(package, checksum, v);
			}
		}

		internal void Check(MaterialData mat, Texture2D texture2D)
		{
			int width = texture2D.width;
			int height = texture2D.height;
			if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height))
			{
				weirdMaterials.Add(mat, (width << 16) | height);
			}
			if (width * height >= 262144)
			{
				largeMaterials.Add(mat, (width << 16) | height);
			}
		}

		private void Check(Package package, MaterialData mat, string checksum)
		{
			if (weirdMaterials.TryGetValue(mat, out var value))
			{
				Instance<Reports>.instance.AddWeirdTexture(package, checksum, value);
			}
			if (largeMaterials.TryGetValue(mat, out value))
			{
				Instance<Reports>.instance.AddLargeTexture(package, checksum, value);
			}
		}

		private static bool IsPowerOfTwo(int i)
		{
			return (i & (i - 1)) == 0;
		}

		private Sharing()
		{
			shareTextures = Settings.settings.shareTextures;
			shareMaterials = Settings.settings.shareMaterials;
			shareMeshes = Settings.settings.shareMeshes;
			mustPrune = !(shareTextures & shareMaterials & shareMeshes);
			checkAssets = Settings.settings.checkAssets;
			if (checkAssets)
			{
				weirdMaterials = new Dictionary<MaterialData, int>();
				largeMaterials = new Dictionary<MaterialData, int>();
			}
		}

		internal void Dispose()
		{
			Util.DebugPrint("Textures / Materials / Meshes shared:", texhit, "/", mathit, "/", meshit, "pre-loaded:", texpre, "/", matpre, "/", mespre, "loaded:", texload, "/", matload, "/", mesload);
			Util.DebugPrint("Max cache", maxCount);
			lock (mutex)
			{
				data.Clear();
				data = null;
				texturesMain.Clear();
				texturesLod.Clear();
				materialsMain.Clear();
				materialsLod.Clear();
				meshes.Clear();
				texturesMain = null;
				texturesLod = null;
				materialsMain = null;
				materialsLod = null;
				meshes = null;
				weirdMaterials = null;
				largeMaterials = null;
				Instance<Sharing>.instance = null;
			}
		}

		internal void Start(Package.Asset[] queue)
		{
			new Thread(LoadWorker).Start(queue);
			new Thread(MTWorker).Start();
		}
	}
}
