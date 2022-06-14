using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ColossalFramework.Packaging;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class Reports : Instance<Reports>
	{
		internal const int ENABLED = 1;

		internal const int USEDDIR = 2;

		internal const int USEDIND = 4;

		internal const int FAILED = 8;

		internal const int AVAILABLE = 16;

		internal const int MISSING = 32;

		internal const int NAME_CHANGED = 64;

		internal const int USED = 6;

		private Dictionary<string, Item> assets = new Dictionary<string, Item>(256);

		private List<List<Package.Asset>> duplicates = new List<List<Package.Asset>>(4);

		private List<AssetError<int>> weirdTextures;

		private List<AssetError<int>> largeTextures;

		private List<AssetError<int>> largeMeshes;

		private List<AssetError<int>> extremeMeshes;

		private HashSet<Package> namingConflicts;

		private readonly string[] allHeadings = new string[9]
		{
			L10n.Get(70),
			L10n.Get(71),
			L10n.Get(72),
			L10n.Get(73),
			L10n.Get(74),
			L10n.Get(75),
			L10n.Get(76),
			L10n.Get(77),
			L10n.Get(78)
		};

		private readonly CustomAssetMetaData.Type[] allTypes = new CustomAssetMetaData.Type[9]
		{
			CustomAssetMetaData.Type.Building,
			CustomAssetMetaData.Type.Prop,
			CustomAssetMetaData.Type.Tree,
			CustomAssetMetaData.Type.Vehicle,
			CustomAssetMetaData.Type.Citizen,
			CustomAssetMetaData.Type.Road,
			CustomAssetMetaData.Type.Road,
			CustomAssetMetaData.Type.Prop,
			CustomAssetMetaData.Type.Tree
		};

		private int texturesShared;

		private int materialsShared;

		private int meshesShared;

		private readonly DateTime jsEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private string reportFilePath;

		private StreamWriter w;

		private static readonly char[] forbidden = new char[21]
		{
			':', '*', '?', '<', '>', '|', '#', '%', '&', '{',
			'}', '$', '!', '@', '+', '`', '=', '\\', '/', '"',
			'\''
		};

		private static readonly string[] jsEsc = new string[2] { "\\", "\"" };

		private const string spaces = "&nbsp;&nbsp;";

		private const string steamid = "<a target=\"_blank\" href=\"https://steamcommunity.com/sharedfiles/filedetails/?id=";

		private const string privateAssetLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/357284767251931800/\">";

		private const string weirdTextureLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562159404/\">";

		private const string largeTextureLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562181099/\">";

		private const string namingConflictLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562171733/\">";

		private const string largeMeshLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562193628/\">";

		private Reports()
		{
			if (Settings.settings.checkAssets)
			{
				weirdTextures = new List<AssetError<int>>();
				largeTextures = new List<AssetError<int>>();
				largeMeshes = new List<AssetError<int>>();
				extremeMeshes = new List<AssetError<int>>();
				namingConflicts = new HashSet<Package>();
			}
		}

		internal void Dispose()
		{
			Instance<Reports>.instance = null;
		}

		internal void ClearAssets()
		{
			assets.Clear();
			duplicates.Clear();
			assets = null;
			duplicates = null;
			weirdTextures = null;
			largeTextures = null;
			largeMeshes = null;
			extremeMeshes = null;
			namingConflicts = null;
		}

		internal void AssetFailed(Package.Asset assetRef)
		{
			Item item = FindItem(assetRef);
			if (item != null)
			{
				item.Failed = true;
			}
		}

		internal void Duplicate(List<Package.Asset> list)
		{
			duplicates.Add(list);
		}

		internal void AddPackage(Package.Asset mainAssetRef, CustomAssetMetaData.Type type, bool enabled, bool useddir)
		{
			string fullName = mainAssetRef.fullName;
			if (!string.IsNullOrEmpty(fullName))
			{
				assets[fullName] = new Available(mainAssetRef, type, enabled, useddir);
			}
		}

		internal void AddPackage(Package p)
		{
			Package.Asset asset = AssetLoader.FindMainAssetRef(p);
			string text = asset?.fullName;
			if (!string.IsNullOrEmpty(text) && !IsKnown(text))
			{
				assets.Add(text, new Available(asset, CustomAssetMetaData.Type.Unknown, enabled: false, useddir: false));
			}
		}

		internal bool IsKnown(Package.Asset assetRef)
		{
			return assets.ContainsKey(assetRef.fullName);
		}

		internal bool IsKnown(string fullName)
		{
			return assets.ContainsKey(fullName);
		}

		internal void AddReference(Package.Asset knownRef, string fullName, CustomAssetMetaData.Type type)
		{
			if (!assets.TryGetValue(fullName, out var value))
			{
				assets.Add(fullName, value = new Missing(fullName, type));
			}
			else
			{
				value.type = type;
			}
			assets[knownRef.fullName].Add(value);
		}

		internal void AddMissing(string fullName, CustomAssetMetaData.Type type)
		{
			if (!assets.TryGetValue(fullName, out var value))
			{
				assets.Add(fullName, new Missing(fullName, type, useddir: true));
			}
			else
			{
				value.UsedDir = true;
			}
		}

		internal void AddWeirdTexture(Package p, string c, int v)
		{
			weirdTextures.Add(new AssetError<int>(p, c, v));
		}

		internal void AddLargeTexture(Package p, string c, int v)
		{
			largeTextures.Add(new AssetError<int>(p, c, v));
		}

		internal void AddLargeMesh(Package p, string c, int v)
		{
			largeMeshes.Add(new AssetError<int>(p, c, v));
		}

		internal void AddExtremeMesh(Package p, string c, int v)
		{
			extremeMeshes.Add(new AssetError<int>(p, c, v));
		}

		internal void AddNamingConflict(Package p)
		{
			namingConflicts.Add(p);
		}

		internal bool IsUsed(Package.Asset mainAssetRef)
		{
			string text = mainAssetRef?.fullName;
			if (!string.IsNullOrEmpty(text) && assets.TryGetValue(text, out var value))
			{
				return value.Used;
			}
			return false;
		}

		internal string[] GetMissing()
		{
			return (from item in assets.Values.Which(32)
				select item.FullName).ToArray();
		}

		internal string[] GetDuplicates()
		{
			return duplicates.Select((List<Package.Asset> list) => list[0].fullName).ToArray();
		}

		internal void SetIndirectUsages()
		{
			foreach (Item value in assets.Values)
			{
				if (value.UsedDir)
				{
					SetIndirectUsages(value);
				}
			}
		}

		private static void SetIndirectUsages(Item item)
		{
			if (item.Uses == null)
			{
				return;
			}
			foreach (Item use in item.Uses)
			{
				if (!use.UsedInd)
				{
					use.UsedInd = true;
					SetIndirectUsages(use);
				}
			}
		}

		private static void SetNameChanges(Item[] missingItems)
		{
			foreach (Item item in missingItems)
			{
				if (item.HasPackageName && Instance<CustomDeserializer>.instance.HasPackages(item.packageName))
				{
					item.NameChanged = true;
				}
			}
		}

		private Dictionary<Item, List<Item>> GetUsedBy()
		{
			Dictionary<Item, List<Item>> dictionary = new Dictionary<Item, List<Item>>(assets.Count / 4);
			try
			{
				foreach (Item value2 in assets.Values)
				{
					if (value2.Uses == null)
					{
						continue;
					}
					foreach (Item use in value2.Uses)
					{
						if (dictionary.TryGetValue(use, out var value))
						{
							value.Add(value2);
							continue;
						}
						dictionary.Add(use, new List<Item>(2) { value2 });
					}
				}
				Comparison<Item> comparison = (Item a, Item b) => string.Compare(a.name, b.name);
				foreach (List<Item> value3 in dictionary.Values)
				{
					value3.Sort(comparison);
				}
				return dictionary;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return dictionary;
			}
		}

		internal void Save(HashSet<string> hidden, int textures, int materials, int meshes)
		{
			texturesShared = textures;
			materialsShared = materials;
			meshesShared = meshes;
			string text = AssetLoader.ShortName(Instance<LevelLoader>.instance.cityName);
			int millis = Profiling.Millis;
			try
			{
				char[] array = forbidden;
				foreach (char oldChar in array)
				{
					text = text.Replace(oldChar, 'x');
				}
				reportFilePath = Util.GetFileName(text + " - " + L10n.Get(10), "htm", Settings.settings.useReportDate);
				Util.DebugPrint("Saving assets report to", reportFilePath);
				w = new StreamWriter(reportFilePath);
				w.WriteLine("<!DOCTYPE html><html lang=\"" + L10n.Code + "\"><head><meta charset=\"UTF-8\"><title>" + L10n.Get(10) + "</title><style>");
				w.WriteLine("* {font-family:sans-serif;}");
				w.WriteLine("body {background-color:#f9f6ea;}");
				w.WriteLine("section {padding-right:24px;}");
				w.WriteLine("div {margin:5px 1px 0px 18px;}");
				w.WriteLine(".my {display:flex;}");
				w.WriteLine(".my .mi {margin:10px 0px;min-width:30%;}");
				w.WriteLine(".my .bx {line-height:133%;padding:8px 12px;background-color:#e8e5d4;border-radius:5px;margin:1px;min-width:58%;}");
				w.WriteLine(".my .st {font-style:italic;margin:0px;min-width:29%;}");
				w.WriteLine("h1 {margin-top:10px;padding:24px 18px;background-color:#e8e5d4;}");
				w.WriteLine("h2 {margin-top:0px;border-bottom:1px solid black;}");
				w.WriteLine("h3 {margin-top:25px;margin-left:18px;}");
				w.WriteLine("a:link {color:#0000e0;text-decoration:inherit;}");
				w.WriteLine("a:visited {color:#8000a0;text-decoration:inherit;}");
				w.WriteLine("a:hover {text-decoration:underline;}");
				w.WriteLine("</style></head><body>");
				H1(Enc(text));
				Italics(L10n.Get(11));
				Italics(L10n.Get(12));
				Br();
				Br();
				string[] headings = allHeadings.Take(6).ToArray();
				CustomAssetMetaData.Type[] types = allTypes.Take(6).ToArray();
				Sec("#f04040");
				H2(L10n.Get(13));
				Item[] array2 = assets.Values.Which(8).ToArray();
				if (array2.Length != 0)
				{
					Report(array2, headings, types);
					Array.Clear(array2, 0, array2.Length);
					array2 = null;
				}
				else
				{
					Italics(L10n.Get(14));
				}
				if (Settings.settings.checkAssets)
				{
					Br();
					Br();
					H2(L10n.Get(15));
					if (!ReportErrors())
					{
						Italics(L10n.Get(16));
					}
					Br();
					Br();
					H2(L10n.Get(17));
					if (!ReportWarnings())
					{
						Italics(L10n.Get(18));
					}
					weirdTextures.Clear();
					largeTextures.Clear();
					largeMeshes.Clear();
					extremeMeshes.Clear();
					namingConflicts.Clear();
				}
				SecOff();
				Sec("#f0a840");
				H2(L10n.Get(19));
				if (Settings.settings.loadUsed)
				{
					SetIndirectUsages();
					Item[] array3 = (from item in assets.Values.Which(32)
						where !hidden.Contains(item.FullName)
						select item).ToArray();
					SetNameChanges(array3);
					if (hidden.Count > 0)
					{
						Italics(L10n.Get(20));
					}
					if (array3.Length != 0)
					{
						Italics(L10n.Get(21));
						ReportMissing(array3, GetUsedBy(), allHeadings, allTypes, 2, 2, 2, 2, 2, 2, 0);
						Array.Clear(array3, 0, array3.Length);
						array3 = null;
					}
					else
					{
						Italics(L10n.Get(22));
					}
				}
				else
				{
					Italics(L10n.Get(23));
				}
				SecOff();
				Sec("#80e0f0");
				H2(L10n.Get(24));
				ReportDuplicates(hidden);
				SecOff();
				Sec("#60b030");
				H2(L10n.Get(25));
				if (Settings.settings.loadUsed)
				{
					Item[] array4 = assets.Values.Which(6).ToArray();
					if (array4.Length != 0)
					{
						Report(array4, allHeadings, allTypes, 2, 2, 2, 2, 2, 2, 4);
						Array.Clear(array4, 0, array4.Length);
						array4 = null;
					}
					else
					{
						Italics(L10n.Get(26));
					}
					SecOff();
					Sec("#c9c6ba");
					H2(L10n.Get(27));
					Item[] array5 = assets.Values.Where((Item item) => item.Enabled && !item.Used && !Instance<AssetLoader>.instance.IsIntersection(item.FullName)).ToArray();
					if (array5.Length != 0)
					{
						Italics(L10n.Get(28));
						Report(array5, headings, types);
						Array.Clear(array5, 0, array5.Length);
						array5 = null;
					}
					else
					{
						Italics(L10n.Get(29));
					}
				}
				else
				{
					Italics(L10n.Get(30));
				}
				SecOff();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			finally
			{
				w?.Dispose();
				w = null;
			}
			if (assets.Count > 0)
			{
				SaveBrowser(text);
			}
			Util.DebugPrint("Reports created in", Profiling.Millis - millis);
		}

		internal void SaveStats()
		{
			try
			{
				Util.DebugPrint("Saving stats to", reportFilePath);
				w = new StreamWriter(reportFilePath, append: true);
				H2(L10n.Get(31));
				H3(L10n.Get(32));
				Stat(L10n.Get(33), Instance<AssetLoader>.instance.assetCount, L10n.Get(34));
				int num = Instance<AssetLoader>.instance.lastMillis - Instance<AssetLoader>.instance.beginMillis;
				if (num > 0)
				{
					Stat(L10n.Get(35), ((float)Instance<AssetLoader>.instance.assetCount * 1000f / (float)num).ToString("F1"), L10n.Get(36));
				}
				Stat(L10n.Get(37), Profiling.TimeString(num + 500), L10n.Get(38));
				Stat(L10n.Get(39), Profiling.TimeString(Profiling.Millis + 500), L10n.Get(38));
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
				{
					H3(L10n.Get(40));
					Stat("RAM", ((float)MemoryAPI.wsMax / 1024f).ToString("F1"), "GB");
					Stat(L10n.Get(41), ((float)MemoryAPI.pfMax / 1024f).ToString("F1"), "GB");
				}
				H3(L10n.Get(42));
				Stat(L10n.Get(43), texturesShared, L10n.Get(44));
				Stat(L10n.Get(45), materialsShared, L10n.Get(44));
				Stat(L10n.Get(46), meshesShared, L10n.Get(44));
				H3(L10n.Get(47));
				int[] skipCounts = Instance<LevelLoader>.instance.skipCounts;
				Stat(L10n.Get(48), skipCounts[0], string.Empty);
				Stat(L10n.Get(49), skipCounts[1], string.Empty);
				Stat(L10n.Get(50), skipCounts[2], string.Empty);
				w.WriteLine("</body></html>");
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			finally
			{
				w?.Dispose();
				w = null;
			}
		}

		private void SaveBrowser(string cityName)
		{
			try
			{
				string fileName = Util.GetFileName(cityName + " - " + L10n.Get(51), "htm", Settings.settings.useReportDate);
				Util.DebugPrint("Saving assets browser to", fileName);
				Item[] array = assets.Values.OrderBy(Name).ToArray();
				Dictionary<Item, int> ids = new Dictionary<Item, int>(array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					ids.Add(array[i], i);
				}
				string text = "</option><option>";
				w = new StreamWriter(fileName);
				w.WriteLine("<!DOCTYPE html><html lang=\"" + L10n.Code + "\"><head><meta charset=\"UTF-8\"><title>" + L10n.Get(51) + "</title><style>");
				w.WriteLine("* {box-sizing:border-box;font-family:'Segoe UI',Verdana,sans-serif;}");
				w.WriteLine("body {background-color:#fafaf7;}");
				w.WriteLine("h1 {color:#00507a;}");
				w.WriteLine(".av {color:#f2f2ed;}");
				w.WriteLine(".mi {color:#f4a820;}");
				w.WriteLine(".fa {color:#f85800;}");
				w.WriteLine(".tg {background-color:#00628b;cursor:pointer;margin:8px 0 0;border:none;padding:6px 12px 6px 15px;width:100%;text-align:left;outline:none;font-size:16px;}");
				w.WriteLine(".tg:hover {filter:brightness(112%);}");
				w.WriteLine(".tg:after {content:'\\002B';font-weight:bold;float:right;}");
				w.WriteLine(".tg.ex:after {content:'\\2212';}");
				w.WriteLine("p {color:#7da291;margin-top:16px;margin-bottom:1px;font-style:italic;}");
				w.WriteLine("span {color:#8db5a2;margin-left:14px;font-style:italic;}");
				w.WriteLine("a:link {color:inherit;text-decoration:inherit;outline:none;}");
				w.WriteLine("a:visited {color:inherit;text-decoration:inherit;}");
				w.WriteLine("a:hover {text-decoration:underline;}");
				w.WriteLine(".cc {background-color:#f2f2ed;}");
				w.WriteLine(".ro {color:#f2f2ed;background-color:#7da291;padding:6px 0px 6px 15px;display:flex;}");
				w.WriteLine(".ce {flex:1;}");
				w.WriteLine(".nr {flex:0.75;}");
				w.WriteLine(".gr {border:1px solid #7da291;padding:0 8px 9px 15px;}");
				w.WriteLine("</style></head><body>");
				H1(L10n.Get(51) + " - " + Enc(cityName));
				w.WriteLine("<noscript><h2 style=\"color:red\">JavaScript is required.</h2></noscript>");
				w.WriteLine("<input id=\"sch\" style=\"margin:16px 16px 16px 0;\" type=\"search\" placeholder=\"" + L10n.Get(132) + "\">");
				w.WriteLine("<label for=\"sct\">" + L10n.Get(131) + "&nbsp;</label>");
				w.WriteLine("<select id=\"sct\"><option>" + L10n.Get(114) + text + L10n.Get(115) + text + L10n.Get(116) + text + L10n.Get(117) + text + L10n.Get(118) + text + L10n.Get(119) + text + L10n.Get(120) + text + L10n.Get(121) + text + L10n.Get(122) + text + L10n.Get(123) + text + L10n.Get(124) + text + L10n.Get(125) + text + L10n.Get(126) + text + L10n.Get(127) + text + L10n.Get(128) + "</option></select>");
				w.WriteLine("<div id=\"top\"></div>");
				w.WriteLine("<script src=\"https://code.jquery.com/jquery-3.3.1.min.js\"></script>");
				w.WriteLine("<script>const zh=" + ((L10n.Code == "zh") ? "1" : "0") + "</script>");
				w.WriteLine("<script src=\"https://thale5.github.io/js/browse4.js\"></script>");
				w.WriteLine("<script>");
				StringBuilder stringBuilder = new StringBuilder(640);
				stringBuilder.Append("const d=[");
				bool loadUsed = Settings.settings.loadUsed;
				for (int j = 0; j < array.Length; j++)
				{
					Item item = array[j];
					stringBuilder.Append('[').Append(j).Append(',')
						.Append((int)item.type);
					stringBuilder.Append(',').Append(GetStatus(item));
					GetDateAndSize(item, out var date, out var size);
					stringBuilder.Append(',').Append(date).Append(',')
						.Append(size);
					stringBuilder.Append(',').Append(GetInCity(item, loadUsed));
					stringBuilder.Append(',').Append(GetId(item));
					stringBuilder.Append(",\"").Append(JsEnc(item.name)).Append("\",[");
					if (item.Uses != null)
					{
						int[] array2 = item.Uses.Select((Item it) => ids[it]).ToArray();
						Array.Sort(array2);
						stringBuilder.Append(string.Join(",", array2.Select((int u) => u.ToString()).ToArray()));
					}
					stringBuilder.Append("]]");
					if (j < array.Length - 1)
					{
						stringBuilder.Append(',');
						if (stringBuilder.Length > 500)
						{
							w.WriteLine(stringBuilder);
							stringBuilder.Length = 0;
						}
					}
				}
				w.WriteLine(stringBuilder.Append(']'));
				w.WriteLine("$(ini)");
				w.WriteLine("</script></body></html>");
				Array.Clear(array, 0, array.Length);
				ids.Clear();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			finally
			{
				w?.Dispose();
				w = null;
			}
		}

		private void Report(IEnumerable<Item> items, string[] headings, CustomAssetMetaData.Type[] types, params int[] usages)
		{
			int usage = 0;
			for (int i = 0; i < headings.Length; i++)
			{
				if (i < usages.Length)
				{
					usage = usages[i];
				}
				Item[] array = items.Which(types[i], usage).OrderBy(Name).ToArray();
				if (array.Length != 0)
				{
					H3(headings[i]);
					Item[] array2 = array;
					foreach (Item item in array2)
					{
						Div(Ref(item));
					}
				}
			}
		}

		private void ReportMissing(IEnumerable<Item> items, Dictionary<Item, List<Item>> usedBy, string[] headings, CustomAssetMetaData.Type[] types, params int[] usages)
		{
			StringBuilder stringBuilder = new StringBuilder(1024);
			int num = 0;
			for (int i = 0; i < headings.Length; i++)
			{
				if (i < usages.Length)
				{
					num = usages[i];
				}
				Item[] array = ((num != 2) ? (from item in items.Which(types[i])
					where usedBy.ContainsKey(item)
					select item).OrderBy(Name).ToArray() : items.Which(types[i], num).OrderBy(Name).ToArray());
				if (array.Length == 0)
				{
					continue;
				}
				H3(headings[i]);
				Item[] array2 = array;
				foreach (Item item2 in array2)
				{
					string text = Ref(item2);
					string text2 = (item2.NameChanged ? GetNameChangedDesc(item2) : string.Empty);
					if (num == 2)
					{
						if (!string.IsNullOrEmpty(text2))
						{
							text = text + "&nbsp;&nbsp;" + "<i>" + text2.Replace("<br>", " ") + "</i>";
						}
						Div(text);
						continue;
					}
					stringBuilder.Length = 0;
					stringBuilder.Append(L10n.Get(130)).Append(':');
					int num2 = 0;
					foreach (Item item3 in usedBy[item2])
					{
						stringBuilder.Append("<br>" + Ref(item3));
						if (num2 < 2 && FromWorkshop(item3))
						{
							num2++;
						}
					}
					if (string.IsNullOrEmpty(text2) && !FromWorkshop(item2))
					{
						if (!item2.HasPackageName)
						{
							text2 = ((!item2.FullName.EndsWith("_Data")) ? (Enc(item2.name) + " " + L10n.Get(56)) : (Enc(item2.name) + " " + L10n.Get(55)));
						}
						else if (num2 > 0)
						{
							string text3 = ((num2 == 1) ? L10n.Get(53) : L10n.Get(52));
							text2 = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/357284767251931800/\">" + text3 + ":</a> " + L10n.Get(54) + " (" + Enc(item2.FullName) + ")";
						}
					}
					if (!string.IsNullOrEmpty(text2))
					{
						stringBuilder.Append("<br><br><i>" + text2 + "</i>");
					}
					Div("my", Cl("mi", text) + Cl("bx", stringBuilder.ToString()));
				}
			}
		}

		private void ReportDuplicates(HashSet<string> hidden)
		{
			duplicates.Sort((List<Package.Asset> a, List<Package.Asset> b) => string.Compare(a[0].fullName, b[0].fullName));
			StringBuilder stringBuilder = new StringBuilder(512);
			int num = 0;
			if (hidden.Count > 0)
			{
				Italics(L10n.Get(20));
			}
			foreach (List<Package.Asset> duplicate in duplicates)
			{
				string fullName = duplicate[0].fullName;
				if (hidden.Contains(fullName))
				{
					continue;
				}
				Item[] array = (from a in duplicate
					select FindItem(a) into item
					where item != null
					select item).ToArray();
				if (array.Length > 1)
				{
					string text = Enc(fullName);
					stringBuilder.Length = 0;
					stringBuilder.Append(L10n.Get(57) + " (" + text + ") " + L10n.Get(58) + ":");
					Item[] array2 = array;
					foreach (Item item2 in array2)
					{
						stringBuilder.Append("<br>" + Ref(item2));
					}
					Div("my", Cl("mi", text) + Cl("bx", stringBuilder.ToString()));
					num++;
				}
			}
			if (num == 0)
			{
				Italics(L10n.Get(59));
			}
		}

		private void ReportEWs(IEnumerable<AssetError<string>> all)
		{
			foreach (IGrouping<Item, AssetError<string>> item in from e in all
				group e by FindItem(e.package) into g
				where g.Key != null
				orderby g.Key.name
				select g)
			{
				Div("my", Cl("mi", Ref(item.Key)) + Cl("bx", "<i>" + string.Join("<br>", (from e in item.Distinct()
					select e.value).ToArray()) + "</i>"));
			}
		}

		private bool ReportErrors()
		{
			List<AssetError<int>> list = new List<AssetError<int>>(extremeMeshes.Count);
			foreach (AssetError<int> extremeMesh in extremeMeshes)
			{
				if (HasExtremeVertices(extremeMesh))
				{
					list.Add(extremeMesh);
				}
				else
				{
					largeMeshes.Add(extremeMesh);
				}
			}
			string wLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562159404/\">" + L10n.Get(61) + "</a> ";
			string nLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562171733/\">" + L10n.Get(62) + "</a>";
			IEnumerable<AssetError<string>> enumerable = weirdTextures.Select((AssetError<int> e) => e.Map((int v) => wLink + DecodeTextureSize(v))).Concat(namingConflicts.Select((Package p) => new AssetError<string>(p, string.Empty, nLink))).Concat(list.Select((AssetError<int> e) => e.Map<string>(ExtremeMesh)));
			if (enumerable.Any())
			{
				Italics(L10n.Get(60));
				ReportEWs(enumerable);
				return true;
			}
			return false;
		}

		private bool ReportWarnings()
		{
			string link = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562181099/\">" + L10n.Get(64) + "</a> ";
			IEnumerable<AssetError<string>> enumerable = largeTextures.Select((AssetError<int> e) => e.Map((int v) => link + DecodeTextureSize(v))).Concat(largeMeshes.Select((AssetError<int> e) => e.Map<string>(MeshSize)));
			if (enumerable.Any())
			{
				Italics(L10n.Get(63));
				ReportEWs(enumerable);
				return true;
			}
			return false;
		}

		private static string GetNameChangedDesc(Item missing)
		{
			List<Package> packages = Instance<CustomDeserializer>.instance.GetPackages(missing.packageName);
			Package.Asset asset = ((packages.Count == 1) ? AssetLoader.FindMainAssetRef(packages[0]) : null);
			string text = ((asset != null) ? Ref(asset.package.packageName, AssetLoader.ShortName(asset.name)) : Ref(missing.packageName));
			return L10n.Get(65) + " " + text + " " + L10n.Get(66) + " " + Enc(missing.name) + ".<br><a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/141136086940263481/\">" + L10n.Get(67) + "</a> " + L10n.Get(68);
		}

		private static int GetStatus(Item item)
		{
			if (item.Failed)
			{
				return 0;
			}
			if (item.Available)
			{
				return 1;
			}
			if (item.NameChanged)
			{
				return 3;
			}
			return 2;
		}

		private void GetDateAndSize(Item item, out int date, out int size)
		{
			Available available = item as Available;
			if (available != null)
			{
				try
				{
					FileInfo fileInfo = new FileInfo(available.mainAssetRef.package.packagePath);
					long num = (long)(fileInfo.LastWriteTimeUtc - jsEpoch).TotalMilliseconds;
					date = (int)(num >> 20) - 1350000;
					size = (int)(fileInfo.Length + 512 >> 10);
					return;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			date = 0;
			size = 0;
		}

		private int GetInCity(Item item, bool loadUsed)
		{
			if (!loadUsed)
			{
				return 0;
			}
			if (!item.UsedDir)
			{
				if (!item.UsedInd)
				{
					return 1;
				}
				return 2;
			}
			return 3;
		}

		private string GetId(Item item)
		{
			if (FromWorkshop(item) && !item.NameChanged)
			{
				return item.packageName;
			}
			if (item.HasPackageName)
			{
				return "0";
			}
			if (!item.FullName.EndsWith("_Data"))
			{
				return "-2";
			}
			return "-1";
		}

		private void Sec(string color)
		{
			w.Write("<section style=\"border-right:12px solid " + color + ";\">");
		}

		private void SecOff()
		{
			w.Write("<br><br></section>");
		}

		private void Div(string line)
		{
			w.WriteLine("<div>" + line + "</div>");
		}

		private void Div(string cl, string line)
		{
			w.WriteLine(Cl(cl, line));
		}

		private void Italics(string line)
		{
			Div("<i>" + line + "</i>");
		}

		private void H1(string line)
		{
			w.WriteLine("<h1>" + line + "</h1>");
		}

		private void H2(string line)
		{
			w.WriteLine("<h2>" + line + "</h2>");
		}

		private void H3(string line)
		{
			w.WriteLine("<h3>" + line + "</h3>");
		}

		private void Stat(string stat, object value, string unit)
		{
			Div("my", Cl("st", stat) + Cl(value.ToString() + "&nbsp;&nbsp;" + unit));
		}

		private void Br()
		{
			w.Write("<br>");
		}

		private static bool FromWorkshop(Item item)
		{
			return FromWorkshop(item.packageName);
		}

		private static bool FromWorkshop(string packageName)
		{
			if (ulong.TryParse(packageName, out var result))
			{
				return result > 99999999;
			}
			return false;
		}

		private static string Ref(Item item)
		{
			if (!item.NameChanged)
			{
				return Ref(item.packageName, item.name);
			}
			return Enc(item.name);
		}

		private static string Ref(string packageName, string name)
		{
			if (!FromWorkshop(packageName))
			{
				return Enc(name);
			}
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/sharedfiles/filedetails/?id=" + packageName + "\">" + Enc(name) + "</a>";
		}

		private static string Ref(string packageName)
		{
			if (!FromWorkshop(packageName))
			{
				return Enc(packageName);
			}
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/sharedfiles/filedetails/?id=" + packageName + "\">" + L10n.Get(129) + " " + packageName + "</a>";
		}

		private static string Cl(string cl, string s)
		{
			return "<div class=\"" + cl + "\">" + s + "</div>";
		}

		private static string Cl(string s)
		{
			return "<div>" + s + "</div>";
		}

		private static string Name(Item item)
		{
			return item.name;
		}

		private static string DecodeTextureSize(int v)
		{
			return (v >> 16) + " x " + (v & 0xFFFF);
		}

		private static string MeshSize(int v)
		{
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562193628/\">" + L10n.Get(69) + "</a> " + ((v < 0) ? (-v + " " + L10n.Get(133)) : (v + " " + L10n.Get(134)));
		}

		private static string ExtremeMesh(int v)
		{
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562193628/\">" + L10n.Get(138) + "</a> " + v + " " + L10n.Get(134);
		}

		private Item FindItem(Package.Asset assetRef)
		{
			return FindItem(assetRef.package);
		}

		private Item FindItem(Package package)
		{
			string text = AssetLoader.FindMainAssetRef(package)?.fullName;
			if (string.IsNullOrEmpty(text) || !assets.TryGetValue(text, out var value))
			{
				return null;
			}
			return value;
		}

		private bool HasExtremeVertices(AssetError<int> e)
		{
			Item item = FindItem(e.package);
			if (item != null)
			{
				CustomAssetMetaData.Type type = item.type;
				if (type == CustomAssetMetaData.Type.Prop || type == CustomAssetMetaData.Type.Vehicle || type == CustomAssetMetaData.Type.Citizen)
				{
					return e.value > 4062;
				}
				return e.value > 8125;
			}
			return false;
		}

		private static string Enc(string s)
		{
			int length = s.Length;
			if (length > 200)
			{
				return Enc(L10n.Get(135));
			}
			bool flag = false;
			for (int i = 0; i < length; i++)
			{
				char c = s[i];
				if (c == '&' || c == '"' || c == '<' || c == '>' || c > '\u009f' || c == '\'')
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return s;
			}
			StringBuilder stringBuilder = new StringBuilder(length + 12);
			for (int j = 0; j < length; j++)
			{
				char c2 = s[j];
				switch (c2)
				{
				case '&':
					stringBuilder.Append("&amp;");
					break;
				case '>':
					stringBuilder.Append("&gt;");
					break;
				case '<':
					stringBuilder.Append("&lt;");
					break;
				case '"':
					stringBuilder.Append("&quot;");
					break;
				case '\'':
					stringBuilder.Append("&#39;");
					break;
				case '＜':
					stringBuilder.Append("&#65308;");
					break;
				case '＞':
					stringBuilder.Append("&#65310;");
					break;
				default:
					stringBuilder.Append(c2);
					break;
				}
			}
			return stringBuilder.ToString();
		}

		private static string JsEnc(string s)
		{
			if (s.Length > 100)
			{
				s = s.Substring(0, 100);
			}
			string[] array = jsEsc;
			foreach (string text in array)
			{
				s = s.Replace(text, "\\" + text);
			}
			return s;
		}
	}
}
