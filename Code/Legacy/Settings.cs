using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AlgernonCommons.Translation;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace LoadingScreenMod
{
	public class Settings
	{
		private const string FILENAME = "LoadingScreenMod.xml";

		internal const string SUPPORTED = "1.14";

		private const int VERSION = 10;

		public int version = 10;

		public bool loadEnabled = true;

		public bool loadUsed = true;

		public bool shareTextures = true;

		public bool shareMaterials = true;

		public bool shareMeshes = true;

		public bool optimizeThumbs = true;

		public bool reportAssets;

		public bool checkAssets;

		public string reportDir = string.Empty;

		public bool skipPrefabs;

		public string skipFile = string.Empty;

		public bool hideAssets;

		public bool useReportDate = true;

		private DateTime skipFileTimestamp = DateTime.MinValue;

		internal bool enableDisable;

		internal bool removeVehicles;

		internal bool removeCitizenInstances;

		internal bool recover;

		private static Settings singleton;

		internal UIHelperBase helper;

		internal bool SkipPrefabs
		{
			get
			{
				if (skipPrefabs && SkipMatcher != null)
				{
					return ExceptMatcher != null;
				}
				return false;
			}
		}

		internal bool RecordAssets => reportAssets | hideAssets | enableDisable;

		internal bool Removals => removeVehicles | removeCitizenInstances;

		internal Matcher SkipMatcher { get; private set; }

		internal Matcher ExceptMatcher { get; private set; }

		internal static string DefaultSavePath => Path.Combine(Path.Combine(DataLocation.localApplicationData, "Report"), "LoadingScreenMod");

		private static string DefaultSkipFile => Path.Combine(Path.Combine(DataLocation.localApplicationData, "SkippedPrefabs"), "skip.txt");

		internal static string HiddenAssetsFile => Path.Combine(Path.Combine(DataLocation.localApplicationData, "HiddenAssets"), "hide.txt");

		public static Settings settings
		{
			get
			{
				if (singleton == null)
				{
					singleton = Load();
				}
				return singleton;
			}
		}

		private Settings()
		{
		}

		private static Settings Load()
		{
			Settings settings;
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
				using (StreamReader textReader = new StreamReader("LoadingScreenMod.xml"))
				{
					settings = (Settings)xmlSerializer.Deserialize(textReader);
				}
			}
			catch (Exception)
			{
				settings = new Settings();
			}
			if (string.IsNullOrEmpty(settings.reportDir = settings.reportDir?.Trim()))
			{
				settings.reportDir = DefaultSavePath;
			}
			if (string.IsNullOrEmpty(settings.skipFile = settings.skipFile?.Trim()))
			{
				settings.skipFile = DefaultSkipFile;
			}
			settings.checkAssets &= settings.reportAssets;
			settings.version = 10;
			return settings;
		}

		internal void Save()
		{
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
				using (StreamWriter textWriter = new StreamWriter("LoadingScreenMod.xml"))
				{
					xmlSerializer.Serialize(textWriter, this);
				}
			}
			catch (Exception exception)
			{
				Util.DebugPrint("Settings.Save");
				UnityEngine.Debug.LogException(exception);
			}
		}

		internal DateTime LoadSkipFile()
		{
			try
			{
				if (skipPrefabs)
				{
					bool flag = File.Exists(skipFile);
					DateTime lastWriteTimeUtc;
					if (flag && skipFileTimestamp != (lastWriteTimeUtc = File.GetLastWriteTimeUtc(skipFile)))
					{
						Matcher[] array = Matcher.Load(skipFile);
						SkipMatcher = array[0];
						ExceptMatcher = array[1];
						skipFileTimestamp = lastWriteTimeUtc;
					}
					else if (!flag)
					{
						Util.DebugPrint("Skip file", skipFile, "does not exist");
					}
				}
			}
			catch (Exception exception)
			{
				Util.DebugPrint("Settings.LoadSkipFile");
				UnityEngine.Debug.LogException(exception);
				Matcher matcher3 = (SkipMatcher = (ExceptMatcher = null));
				skipFileTimestamp = DateTime.MinValue;
			}
			if (!SkipPrefabs)
			{
				return DateTime.MinValue;
			}
			return skipFileTimestamp;
		}

		internal void LoadHiddenAssets(HashSet<string> hidden)
		{
			try
			{
				using (StreamReader streamReader = new StreamReader(HiddenAssetsFile))
				{
					string text;
					while ((text = streamReader.ReadLine()) != null)
					{
						string text2 = text.Trim();
						if (!string.IsNullOrEmpty(text2) && !text2.StartsWith("#"))
						{
							hidden.Add(text2);
						}
					}
				}
			}
			catch (Exception)
			{
				Util.DebugPrint("Cannot read from " + HiddenAssetsFile);
			}
		}

		internal void SaveHiddenAssets(HashSet<string> hidden, string[] missing, string[] duplicates)
		{
			if (hidden.Count == 0 && missing.Length == 0 && duplicates.Length == 0)
			{
				CreateHiddenAssetsFile();
				return;
			}
			string hiddenAssetsFile = HiddenAssetsFile;
			Util.DebugPrint("Saving hidden assets to", hiddenAssetsFile);
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(hiddenAssetsFile));
				using (StreamWriter streamWriter = new StreamWriter(hiddenAssetsFile))
				{
					streamWriter.WriteLine(Translations.Translate("AS_YOU_KNOW"));
					streamWriter.WriteLine(Translations.Translate("USING_THIS_FILE"));
					if (hidden.Count > 0)
					{
						WriteLines(streamWriter, Translations.Translate("THESE_ARE_NOT_REPORTED"), hidden.ToArray(), tag: false);
					}
					string[] array = missing.Where((string s) => !hidden.Contains(s)).ToArray();
					string[] array2 = duplicates.Where((string s) => !hidden.Contains(s)).ToArray();
					if (array.Length != 0 || array2.Length != 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine(Translations.Translate("LSM_REPORTED_THESE"));
						WriteLines(streamWriter, Translations.Translate("REPORTED_MISSING"), array, tag: true);
						WriteLines(streamWriter, Translations.Translate("REPORTED_DUPLICATE"), array2, tag: true);
					}
				}
			}
			catch (Exception)
			{
				Util.DebugPrint("Cannot write to " + hiddenAssetsFile);
			}
		}

		private static void WriteLines(StreamWriter w, string header, string[] lines, bool tag)
		{
			if (lines.Length == 0)
			{
				return;
			}
			w.WriteLine();
			w.WriteLine(header);
			Array.Sort(lines);
			foreach (string text in lines)
			{
				if (tag)
				{
					w.WriteLine("#" + text);
				}
				else
				{
					w.WriteLine(text);
				}
			}
		}

		private void CreateHiddenAssetsFile()
		{
			try
			{
				string hiddenAssetsFile = HiddenAssetsFile;
				Directory.CreateDirectory(Path.GetDirectoryName(hiddenAssetsFile));
				using (StreamWriter streamWriter = new StreamWriter(hiddenAssetsFile))
				{
					streamWriter.WriteLine(Translations.Translate("GO_AHEAD"));
				}
			}
			catch (Exception)
			{
				Util.DebugPrint("Cannot write to " + HiddenAssetsFile);
			}
		}

		private static UIComponent Self(UIHelperBase h)
		{
			return (h as UIHelper)?.self as UIComponent;
		}

		private static void OnVisibilityChanged(UIComponent comp, bool visible)
		{
			if (visible && comp == Self(settings.helper) && comp.childCount == 1)
			{
				settings.LateSettingsUI(settings.helper);
			}
		}

		internal void OnSettingsUI(UIHelperBase newHelper)
		{
			if (!BuildConfig.applicationVersion.StartsWith("1.14"))
			{
				CreateGroup(newHelper, Translations.Translate("MAJOR_GAME_UPDATE"), Translations.Translate("INCOMPATIBLE_VERSION"));
				return;
			}
			UIComponent uIComponent = Self(helper);
			if (uIComponent != null)
			{
				uIComponent.eventVisibilityChanged -= OnVisibilityChanged;
			}
			helper = newHelper;
			string tooltip = Translations.Translate("REPLACE_DUPLICATES");
			UIHelper group = CreateGroup(newHelper, Translations.Translate("LOADING_OPTIONS_FOR_ASSETS"), Translations.Translate("CUSTOM_MEANS"));
			Check(group, Translations.Translate("LOAD_ENABLED_ASSETS"), Translations.Translate("LOAD_ENABLED_IN_CM"), loadEnabled, delegate(bool b)
			{
				loadEnabled = b;
				LoadingScreenModRevisited.LevelLoader.Reset();
				Save();
			});
			Check(group, Translations.Translate("LOAD_USED_ASSETS"), Translations.Translate("LOAD_USED_IN_YOUR_CITY"), loadUsed, delegate(bool b)
			{
				loadUsed = b;
				LoadingScreenModRevisited.LevelLoader.Reset();
				Save();
			});
			Check(group, Translations.Translate("SHARE_TEXTURES"), tooltip, shareTextures, delegate(bool b)
			{
				shareTextures = b;
				Save();
			});
			Check(group, Translations.Translate("SHARE_MATERIALS"), tooltip, shareMaterials, delegate(bool b)
			{
				shareMaterials = b;
				Save();
			});
			Check(group, Translations.Translate("SHARE_MESHES"), tooltip, shareMeshes, delegate(bool b)
			{
				shareMeshes = b;
				Save();
			});
			Check(group, Translations.Translate("OPTIMIZE_THUMBNAILS"), Translations.Translate("OPTIMIZE_TEXTURES"), optimizeThumbs, delegate(bool b)
			{
				optimizeThumbs = b;
				Save();
			});
			uIComponent = Self(newHelper);
			uIComponent.eventVisibilityChanged -= OnVisibilityChanged;
			uIComponent.eventVisibilityChanged += OnVisibilityChanged;
		}

		private void LateSettingsUI(UIHelperBase helper)
		{
			UIHelper group = CreateGroup(helper, Translations.Translate("REPORTING"));
			UICheckBox reportCheck = null;
			UICheckBox checkCheck = null;
			reportCheck = Check(group, Translations.Translate("SAVE_REPORTS_IN_DIRECTORY"), Translations.Translate("SAVE_REPORTS_OF_ASSETS"), reportAssets, delegate(bool b)
			{
				reportAssets = b;
				checkAssets = checkAssets && b;
				checkCheck.isChecked = checkAssets;
				Save();
			});
			TextField(group, reportDir, OnReportDirChanged);
			checkCheck = Check(group, Translations.Translate("CHECK_FOR_ERRORS"), null, checkAssets, delegate(bool b)
			{
				checkAssets = b;
				reportAssets = reportAssets || b;
				reportCheck.isChecked = reportAssets;
				Save();
			});
			Check(group, Translations.Translate("DO_NOT_REPORT_THESE"), null, hideAssets, delegate(bool b)
			{
				hideAssets = b;
				Save();
			});
			Button(group, Translations.Translate("OPEN_FILE"), Translations.Translate("CLICK_TO_OPEN") + ' ' + HiddenAssetsFile, OnAssetsButton);
			group = CreateGroup(helper, Translations.Translate("PREFAB_SKIPPING"), Translations.Translate("PREFAB_MEANS"));
			Check(group, Translations.Translate("SKIP_THESE"), null, skipPrefabs, delegate(bool b)
			{
				skipPrefabs = b;
				Save();
			});
			TextField(group, skipFile, OnSkipFileChanged);
			group = CreateGroup(helper, Translations.Translate("SAFE_MODE"), Translations.Translate("AUTOMATICALLY_DISABLED"));
			Check(group, Translations.Translate("REMOVE_VEHICLE_AGENTS"), null, removeVehicles, delegate(bool b)
			{
				removeVehicles = b;
			});
			Check(group, Translations.Translate("REMOVE_CITIZEN_AGENTS"), null, removeCitizenInstances, delegate(bool b)
			{
				removeCitizenInstances = b;
			});
			Check(group, Translations.Translate("TRY_TO_RECOVER"), null, recover, delegate(bool b)
			{
				recover = b;
			});
		}

		private UIHelper CreateGroup(UIHelperBase parent, string name, string tooltip = null)
		{
			UIHelper obj = parent.AddGroup(name) as UIHelper;
			UIPanel uIPanel = obj.self as UIPanel;
			RectOffset rectOffset = ((uIPanel != null) ? uIPanel.autoLayoutPadding : null);
			if (rectOffset != null)
			{
				rectOffset.bottom /= 2;
			}
			UIPanel uIPanel2 = ((uIPanel != null) ? uIPanel.parent : null) as UIPanel;
			RectOffset rectOffset2 = ((uIPanel2 != null) ? uIPanel2.autoLayoutPadding : null);
			if (rectOffset2 != null)
			{
				rectOffset2.bottom /= 2;
			}
			if (!string.IsNullOrEmpty(tooltip))
			{
				UILabel uILabel = ((uIPanel2 != null) ? uIPanel2.Find<UILabel>("Label") : null);
				if (uILabel != null)
				{
					uILabel.tooltip = tooltip;
				}
			}
			return obj;
		}

		private UICheckBox Check(UIHelper group, string text, string tooltip, bool setChecked, OnCheckChanged action)
		{
			try
			{
				UICheckBox uICheckBox = group.AddCheckbox(text, setChecked, action) as UICheckBox;
				if (tooltip != null)
				{
					uICheckBox.tooltip = tooltip;
				}
				return uICheckBox;
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogException(exception);
				return null;
			}
		}

		private void TextField(UIHelper group, string text, OnTextChanged action)
		{
			try
			{
				UITextField obj = group.AddTextfield(" ", " ", action, null) as UITextField;
				obj.text = text;
				obj.width *= 2.8f;
				UIComponent parent = obj.parent;
				UILabel uILabel = ((parent != null) ? parent.Find<UILabel>("Label") : null);
				if (uILabel != null)
				{
					float height = uILabel.height;
					uILabel.height = 0f;
					uILabel.Hide();
					parent.height -= height;
				}
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogException(exception);
			}
		}

		private void Button(UIHelper group, string text, string tooltip, OnButtonClicked action)
		{
			try
			{
				UIButton obj = group.AddButton(text, action) as UIButton;
				obj.textScale = 0.875f;
				obj.tooltip = tooltip;
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogException(exception);
			}
		}

		private void OnReportDirChanged(string text)
		{
			if (text != reportDir)
			{
				reportDir = text;
				Save();
			}
		}

		internal void OnSkipFileChanged(string text)
		{
			if (text != skipFile)
			{
				skipFile = text;
				Matcher matcher3 = (SkipMatcher = (ExceptMatcher = null));
				skipFileTimestamp = DateTime.MinValue;
				Save();
			}
		}

		internal void OnAssetsButton()
		{
			string hiddenAssetsFile = HiddenAssetsFile;
			if (!File.Exists(hiddenAssetsFile))
			{
				CreateHiddenAssetsFile();
			}
			else
			{
				try
				{
					if (new FileInfo(hiddenAssetsFile).Length < 100)
					{
						CreateHiddenAssetsFile();
					}
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogException(exception);
				}
			}
			Process.Start(hiddenAssetsFile);
		}
	}
}
