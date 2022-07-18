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
			LoadingScreenModRevisited.Translations.Translate("BUILDINGS_AND_PARKS"),
			LoadingScreenModRevisited.Translations.Translate("PROPS"),
			LoadingScreenModRevisited.Translations.Translate("TREES"),
			LoadingScreenModRevisited.Translations.Translate("VEHICLES"),
			LoadingScreenModRevisited.Translations.Translate("CITIZENS"),
			LoadingScreenModRevisited.Translations.Translate("NETS"),
			LoadingScreenModRevisited.Translations.Translate("NETS_IN_BUILDINGS"),
			LoadingScreenModRevisited.Translations.Translate("PROPS_IN_BUILDINGS"),
			LoadingScreenModRevisited.Translations.Translate("TREES_IN_BUILDINGS")
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
			Package.Asset asset = LoadingScreenModRevisited.AssetLoader.FindMainAssetRef(p);
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
			string text = LoadingScreenModRevisited.AssetLoader.ShortName(LoadingScreenModRevisited.LevelLoader.cityName);
			int millis = Profiling.Millis;
			try
			{
				char[] array = forbidden;
				foreach (char oldChar in array)
				{
					text = text.Replace(oldChar, 'x');
				}
				reportFilePath = Util.GetFileName(text + " - " + LoadingScreenModRevisited.Translations.Translate("ASSETS_REPORT"), "htm", Settings.settings.useReportDate); ;
				Util.DebugPrint("Saving assets report to", reportFilePath);
				w = new StreamWriter(reportFilePath);
				w.WriteLine("<!DOCTYPE html><html lang=\"" + LoadingScreenModRevisited.Translations.CurrentLanguage + "\"><head><meta charset=\"UTF-8\"><title>" + LoadingScreenModRevisited.Translations.Translate("ASSETS_REPORT") + "</title><style>");
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
				Italics(LoadingScreenModRevisited.Translations.Translate("ASSETS_REPORT_FOR_CS"));
				Italics(LoadingScreenModRevisited.Translations.Translate("TO_STOP_SAVING"));
				Br();
				Br();
				string[] headings = allHeadings.Take(6).ToArray();
				CustomAssetMetaData.Type[] types = allTypes.Take(6).ToArray();
				Sec("#f04040");
				H2(LoadingScreenModRevisited.Translations.Translate("ASSETS_THAT_FAILED"));
				Item[] array2 = assets.Values.Which(8).ToArray();
				if (array2.Length != 0)
				{
					Report(array2, headings, types);
					Array.Clear(array2, 0, array2.Length);
					array2 = null;
				}
				else
				{
					Italics(LoadingScreenModRevisited.Translations.Translate("NO_FAILED_ASSETS"));
				}
				if (Settings.settings.checkAssets)
				{
					Br();
					Br();
					H2(LoadingScreenModRevisited.Translations.Translate("ASSET_ERRORS"));
					if (!ReportErrors())
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("NO_ERRORS_FOUND"));
					}
					Br();
					Br();
					H2(LoadingScreenModRevisited.Translations.Translate("ASSET_WARNINGS"));
					if (!ReportWarnings())
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("NO_WARNINGS"));
					}
					weirdTextures.Clear();
					largeTextures.Clear();
					largeMeshes.Clear();
					extremeMeshes.Clear();
					namingConflicts.Clear();
				}
				SecOff();
				Sec("#f0a840");
				H2(LoadingScreenModRevisited.Translations.Translate("ASSETS_THAT_ARE_MISSING"));
				if (Settings.settings.loadUsed)
				{
					SetIndirectUsages();
					Item[] array3 = (from item in assets.Values.Which(32)
									 where !hidden.Contains(item.FullName)
									 select item).ToArray();
					SetNameChanges(array3);
					if (hidden.Count > 0)
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("SECTION_MIGHT_BE_INCOMPLETE"));
					}
					if (array3.Length != 0)
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("PLACED_BUT_MISSING"));
						ReportMissing(array3, GetUsedBy(), allHeadings, allTypes, 2, 2, 2, 2, 2, 2, 0);
						Array.Clear(array3, 0, array3.Length);
						array3 = null;
					}
					else
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("NO_MISSING_ASSETS"));
					}
				}
				else
				{
					Italics(LoadingScreenModRevisited.Translations.Translate("TO_TRACK_MISSING"));
				}
				SecOff();
				Sec("#80e0f0");
				H2(LoadingScreenModRevisited.Translations.Translate("DUPLICATE_NAMES"));
				Italics(LoadingScreenModRevisited.Translations.Translate("DUPLICATE_NAMES_EXPLAIN_1"));
				Italics(LoadingScreenModRevisited.Translations.Translate("DUPLICATE_NAMES_EXPLAIN_2"));
				ReportDuplicates(hidden);
				SecOff();
				Sec("#60b030");
				H2(LoadingScreenModRevisited.Translations.Translate("THESE_ARE_USED"));
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
						Italics(LoadingScreenModRevisited.Translations.Translate("NO_USED_ASSETS"));
					}
					SecOff();
					Sec("#c9c6ba");
					H2(LoadingScreenModRevisited.Translations.Translate("THESE_ARE_UNNECESSARY"));
					Item[] array5 = assets.Values.Where((Item item) => item.Enabled && !item.Used && !LoadingScreenModRevisited.AssetLoader.Instance.IsIntersection(item.FullName)).ToArray();
					if (array5.Length != 0)
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("ENABLED_BUT_UNNECESSARY"));
						Report(array5, headings, types);
						Array.Clear(array5, 0, array5.Length);
						array5 = null;
					}
					else
					{
						Italics(LoadingScreenModRevisited.Translations.Translate("NO_UNNECESSARY_ASSETS"));
					}
				}
				else
				{
					Italics(LoadingScreenModRevisited.Translations.Translate("TO_TRACK_USED"));
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
				H2(LoadingScreenModRevisited.Translations.Translate("LOADING_STATS"));
				H3(LoadingScreenModRevisited.Translations.Translate("PERFORMANCE"));
				Stat(LoadingScreenModRevisited.Translations.Translate("CUSTOM_ASSETS_LOADED"), LoadingScreenModRevisited.AssetLoader.Instance.assetCount, LoadingScreenModRevisited.Translations.Translate("ASSETS"));
				int num = LoadingScreenModRevisited.AssetLoader.Instance.lastMillis - LoadingScreenModRevisited.AssetLoader.Instance.beginMillis;
				if (num > 0)
				{
					Stat(LoadingScreenModRevisited.Translations.Translate("LOADING_SPEED"), ((float)LoadingScreenModRevisited.AssetLoader.Instance.assetCount * 1000f / (float)num).ToString("F1"), LoadingScreenModRevisited.Translations.Translate("ASSETS_PER_SECOND"));
				}
				Stat(LoadingScreenModRevisited.Translations.Translate("ASSETS_LOADING_TIME"), Profiling.TimeString(num + 500), LoadingScreenModRevisited.Translations.Translate("MINUTES_SECONDS"));
				Stat(LoadingScreenModRevisited.Translations.Translate("TOTAL_LOADING_TIME"), Profiling.TimeString(Profiling.Millis + 500), LoadingScreenModRevisited.Translations.Translate("MINUTES_SECONDS"));
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
				{
					H3(LoadingScreenModRevisited.Translations.Translate("PEAK_MEMORY_USAGE"));
					Stat("RAM", ((float)MemoryAPI.wsMax / 1024f).ToString("F1"), "GB");
					Stat(LoadingScreenModRevisited.Translations.Translate("VIRTUAL_MEMORY"), ((float)MemoryAPI.pfMax / 1024f).ToString("F1"), "GB");
				}
				H3(LoadingScreenModRevisited.Translations.Translate("SHARING_OF_RESOURCES"));
				Stat(LoadingScreenModRevisited.Translations.Translate("TEXTURES"), texturesShared, LoadingScreenModRevisited.Translations.Translate("TIMES"));
				Stat(LoadingScreenModRevisited.Translations.Translate("MATERIALS"), materialsShared, LoadingScreenModRevisited.Translations.Translate("TIMES"));
				Stat(LoadingScreenModRevisited.Translations.Translate("MESHES"), meshesShared, LoadingScreenModRevisited.Translations.Translate("TIMES"));
				H3(LoadingScreenModRevisited.Translations.Translate("SKIPPED_PREFABS"));
				int[] skipCounts = LoadingScreenModRevisited.LevelLoader.skipCounts;
				Stat(LoadingScreenModRevisited.Translations.Translate("BUILDING_PREFABS"), skipCounts[0], string.Empty);
				Stat(LoadingScreenModRevisited.Translations.Translate("VEHICLE_PREFABS"), skipCounts[1], string.Empty);
				Stat(LoadingScreenModRevisited.Translations.Translate("PROP_PREFABS"), skipCounts[2], string.Empty);
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
				string fileName = Util.GetFileName(cityName + " - " + LoadingScreenModRevisited.Translations.Translate("ASSETS_BROWSER"), "htm", Settings.settings.useReportDate);
				Util.DebugPrint("Saving assets browser to", fileName);
				Item[] array = assets.Values.OrderBy(Name).ToArray();
				Dictionary<Item, int> ids = new Dictionary<Item, int>(array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					ids.Add(array[i], i);
				}
				string text = "</option><option>";
				w = new StreamWriter(fileName);
				w.WriteLine("<!DOCTYPE html><html lang=\"" + LoadingScreenModRevisited.Translations.CurrentLanguage + "\"><head><meta charset=\"UTF-8\"><title>" + LoadingScreenModRevisited.Translations.Translate("ASSETS_BROWSER") + "</title><style>");
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
				H1(LoadingScreenModRevisited.Translations.Translate("ASSETS_BROWSER") + " - " + Enc(cityName));
				w.WriteLine("<noscript><h2 style=\"color:red\">JavaScript is required.</h2></noscript>");
				w.WriteLine("<input id=\"sch\" style=\"margin:16px 16px 16px 0;\" type=\"search\" placeholder=\"" + LoadingScreenModRevisited.Translations.Translate("FIND_ASSETS") + "\">");
				w.WriteLine("<label for=\"sct\">" + LoadingScreenModRevisited.Translations.Translate("ORDER_BY") + "&nbsp;</label>");
				w.WriteLine("<select id=\"sct\"><option>" + LoadingScreenModRevisited.Translations.Translate("NAME") + text +
					LoadingScreenModRevisited.Translations.Translate("TYPE") + text +
					LoadingScreenModRevisited.Translations.Translate("STATUS") + text +
					LoadingScreenModRevisited.Translations.Translate("WORKSHOP_ID") + text +
					LoadingScreenModRevisited.Translations.Translate("DATE") + text +
					LoadingScreenModRevisited.Translations.Translate("SIZE") + text +
					LoadingScreenModRevisited.Translations.Translate("USES_COUNT") + text +
					LoadingScreenModRevisited.Translations.Translate("USED_BY_COUNT") + text +
					LoadingScreenModRevisited.Translations.Translate("TYPE_AND_STATUS") + text +
					LoadingScreenModRevisited.Translations.Translate("TYPE_AND_SIZE") + text +
					LoadingScreenModRevisited.Translations.Translate("TYPE_AND_USED_BY_COUNT") + text +
					LoadingScreenModRevisited.Translations.Translate("STATUS_AND_USED_BY_COUNT") + text +
					LoadingScreenModRevisited.Translations.Translate("USED_BY_COUNT_AND_SIZE") + text +
					LoadingScreenModRevisited.Translations.Translate("USED_IN_CITY") + text +
					LoadingScreenModRevisited.Translations.Translate("USED_IN_CITY_AND_SIZE") + "</option></select>");
				w.WriteLine("<div id=\"top\"></div>");
				w.WriteLine("<script src=\"https://code.jquery.com/jquery-3.3.1.min.js\"></script>");
				w.WriteLine("<script>const zh=" + ((LoadingScreenModRevisited.Translations.CurrentLanguage.StartsWith("zh")) ? "1" : "0") + "</script>");
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
					stringBuilder.Append(LoadingScreenModRevisited.Translations.Translate("USED_BY")).Append(':');
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
							text2 = ((!item2.FullName.EndsWith("_Data")) ? (Enc(item2.name) + " " + LoadingScreenModRevisited.Translations.Translate("IS_POSSIBLY_DLC")) : (Enc(item2.name) + " " + LoadingScreenModRevisited.Translations.Translate("NO_LINK_IS_AVAILABLE")));
						}
						else if (num2 > 0)
						{
							string text3 = ((num2 == 1) ? LoadingScreenModRevisited.Translations.Translate("ASSET_BUG") : LoadingScreenModRevisited.Translations.Translate("ASSET_BUGS"));
							text2 = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/357284767251931800/\">" + text3 + ":</a> " + LoadingScreenModRevisited.Translations.Translate("ASSET_USES_PRIVATE_ASSET") + " (" + Enc(item2.FullName) + ")";
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
				Italics(LoadingScreenModRevisited.Translations.Translate("SECTION_MIGHT_BE_INCOMPLETE"));
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
					stringBuilder.Append(LoadingScreenModRevisited.Translations.Translate("SAME_ASSET_NAME") + " (" + text + ") " + LoadingScreenModRevisited.Translations.Translate("IN_ALL_OF_THESE") + ':');
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
				Italics(LoadingScreenModRevisited.Translations.Translate("NO_DUPLICATES"));
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
			string wLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562159404/\">" + LoadingScreenModRevisited.Translations.Translate("INVALID_LOD_TEXTURE_SIZE") + "</a> ";
			string nLink = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562171733/\">" + LoadingScreenModRevisited.Translations.Translate("ASSET_NAMING_CONFLICT") + "</a>";
			IEnumerable<AssetError<string>> enumerable = weirdTextures.Select((AssetError<int> e) => e.Map((int v) => wLink + DecodeTextureSize(v))).Concat(namingConflicts.Select((Package p) => new AssetError<string>(p, string.Empty, nLink))).Concat(list.Select((AssetError<int> e) => e.Map<string>(ExtremeMesh)));
			if (enumerable.Any())
			{
				Italics(LoadingScreenModRevisited.Translations.Translate("PROBLEMS_WERE_DETECTED"));
				ReportEWs(enumerable);
				return true;
			}
			return false;
		}

		private bool ReportWarnings()
		{
			string link = "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562181099/\">" + LoadingScreenModRevisited.Translations.Translate("VERY_LARGE_LOD_TEXTURE") + "</a> ";
			IEnumerable<AssetError<string>> enumerable = largeTextures.Select((AssetError<int> e) => e.Map((int v) => link + DecodeTextureSize(v))).Concat(largeMeshes.Select((AssetError<int> e) => e.Map<string>(MeshSize)));
			if (enumerable.Any())
			{
				Italics(LoadingScreenModRevisited.Translations.Translate("OBSERVATIONS_WERE_MADE"));
				ReportEWs(enumerable);
				return true;
			}
			return false;
		}

		private static string GetNameChangedDesc(Item missing)
		{
			List<Package> packages = Instance<CustomDeserializer>.instance.GetPackages(missing.packageName);
			Package.Asset asset = ((packages.Count == 1) ? LoadingScreenModRevisited.AssetLoader.FindMainAssetRef(packages[0]) : null);
			string text = ((asset != null) ? Ref(asset.package.packageName, LoadingScreenModRevisited.AssetLoader.ShortName(asset.name)) : Ref(missing.packageName));
			return LoadingScreenModRevisited.Translations.Translate("YOU_HAVE") + ' ' + text + ' ' +
				LoadingScreenModRevisited.Translations.Translate("DOES_NOT_CONTAIN") + " " + Enc(missing.name) +
				".<br><a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/141136086940263481/\">" +
				LoadingScreenModRevisited.Translations.Translate("NAME_PROBABLY_CHANGED") + "</a> " +
				LoadingScreenModRevisited.Translations.Translate("BY_THE_AUTHOR");
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
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/sharedfiles/filedetails/?id=" + packageName + "\">" + LoadingScreenModRevisited.Translations.Translate("WORKSHOP_ITEM") + ' ' + packageName + "</a>";
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
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562193628/\">" +
				LoadingScreenModRevisited.Translations.Translate("VERY_LARGE_LOD_MESH") + "</a> " +
				((v < 0) ? (-v + " " + LoadingScreenModRevisited.Translations.Translate("TRIANGLES")) : (v + " " +LoadingScreenModRevisited.Translations.Translate("VERTICES")));
		}

		private static string ExtremeMesh(int v)
		{
			return "<a target=\"_blank\" href=\"https://steamcommunity.com/workshop/filedetails/discussion/667342976/1639789306562193628/\">" +
				LoadingScreenModRevisited.Translations.Translate("EXTREMELY_LARGE_LOD_MESH") + "</a> " + v + " " +
				LoadingScreenModRevisited.Translations.Translate("VERTICES");
		}

		private Item FindItem(Package.Asset assetRef)
		{
			return FindItem(assetRef.package);
		}

		private Item FindItem(Package package)
		{
			string text = LoadingScreenModRevisited.AssetLoader.FindMainAssetRef(package)?.fullName;
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
				return Enc(LoadingScreenModRevisited.Translations.Translate("LONG_NAME"));
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
