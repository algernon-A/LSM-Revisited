using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
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

		private static string HiddenAssetsFile => Path.Combine(Path.Combine(DataLocation.localApplicationData, "HiddenAssets"), "hide.txt");

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

		private void Save()
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
					streamWriter.WriteLine(L10n.Get(79));
					streamWriter.WriteLine(L10n.Get(80));
					if (hidden.Count > 0)
					{
						WriteLines(streamWriter, L10n.Get(81), hidden.ToArray(), tag: false);
					}
					string[] array = missing.Where((string s) => !hidden.Contains(s)).ToArray();
					string[] array2 = duplicates.Where((string s) => !hidden.Contains(s)).ToArray();
					if (array.Length != 0 || array2.Length != 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine(L10n.Get(82));
						WriteLines(streamWriter, L10n.Get(83), array, tag: true);
						WriteLines(streamWriter, L10n.Get(84), array2, tag: true);
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
					streamWriter.WriteLine(L10n.Get(85));
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
			L10n.SetCurrent();
			if (!BuildConfig.applicationVersion.StartsWith("1.14"))
			{
				CreateGroup(newHelper, L10n.Get(9), L10n.Get(86));
				return;
			}
			UIComponent uIComponent = Self(helper);
			if (uIComponent != null)
			{
				uIComponent.eventVisibilityChanged -= OnVisibilityChanged;
			}
			helper = newHelper;
			string tooltip = L10n.Get(94);
			UIHelper group = CreateGroup(newHelper, L10n.Get(87), L10n.Get(88));
			Check(group, L10n.Get(89), L10n.Get(90), loadEnabled, delegate(bool b)
			{
				loadEnabled = b;
				Instance<LevelLoader>.instance?.Reset();
				Save();
			});
			Check(group, L10n.Get(91), L10n.Get(92), loadUsed, delegate(bool b)
			{
				loadUsed = b;
				Instance<LevelLoader>.instance?.Reset();
				Save();
			});
			Check(group, L10n.Get(93), tooltip, shareTextures, delegate(bool b)
			{
				shareTextures = b;
				Save();
			});
			Check(group, L10n.Get(95), tooltip, shareMaterials, delegate(bool b)
			{
				shareMaterials = b;
				Save();
			});
			Check(group, L10n.Get(96), tooltip, shareMeshes, delegate(bool b)
			{
				shareMeshes = b;
				Save();
			});
			Check(group, L10n.Get(97), L10n.Get(98), optimizeThumbs, delegate(bool b)
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
			UIHelper group = CreateGroup(helper, L10n.Get(99));
			UICheckBox reportCheck = null;
			UICheckBox checkCheck = null;
			reportCheck = Check(group, L10n.Get(100), L10n.Get(101), reportAssets, delegate(bool b)
			{
				reportAssets = b;
				checkAssets = checkAssets && b;
				checkCheck.isChecked = checkAssets;
				Save();
			});
			TextField(group, reportDir, OnReportDirChanged);
			checkCheck = Check(group, L10n.Get(102), null, checkAssets, delegate(bool b)
			{
				checkAssets = b;
				reportAssets = reportAssets || b;
				reportCheck.isChecked = reportAssets;
				Save();
			});
			Check(group, L10n.Get(103), null, hideAssets, delegate(bool b)
			{
				hideAssets = b;
				Save();
			});
			Button(group, L10n.Get(104), L10n.Get(105) + " " + HiddenAssetsFile, OnAssetsButton);
			group = CreateGroup(helper, L10n.Get(106), L10n.Get(107));
			Check(group, L10n.Get(108), null, skipPrefabs, delegate(bool b)
			{
				skipPrefabs = b;
				Save();
			});
			TextField(group, skipFile, OnSkipFileChanged);
			group = CreateGroup(helper, L10n.Get(109), L10n.Get(110));
			Check(group, L10n.Get(111), null, removeVehicles, delegate(bool b)
			{
				removeVehicles = b;
			});
			Check(group, L10n.Get(112), null, removeCitizenInstances, delegate(bool b)
			{
				removeCitizenInstances = b;
			});
			Check(group, L10n.Get(113), null, recover, delegate(bool b)
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

		private void OnSkipFileChanged(string text)
		{
			if (text != skipFile)
			{
				skipFile = text;
				Matcher matcher3 = (SkipMatcher = (ExceptMatcher = null));
				skipFileTimestamp = DateTime.MinValue;
				Save();
			}
		}

		private void OnAssetsButton()
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
