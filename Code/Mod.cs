using ICities;
using CitiesHarmony.API;


namespace LoadingScreenMod
{
	/// <summary>
	/// The base mod class for instantiation by the game.
	/// </summary>
	public sealed class LSMRMod : IUserMod
	{
		public static string ModName => "LSM Revisited";
		public static string Version => "0.0.1";

		public string Name => "Loading Screen Mod Revisited " + Version;
		public string Description => "Optimizes game loading";

		// Status flag.
		private bool created = false;


		/// <summary>
		/// Called by the game when the mod is enabled.
		/// </summary>
		public void OnEnabled()
		{
			// Apply Harmony patches via Cities Harmony.
			// Called here instead of OnCreated to allow the auto-downloader to do its work prior to launch.
			HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

			L10n.SetCurrent();
			if (!created)
			{
				if (BuildConfig.applicationVersion.StartsWith("1.14"))
				{
					Instance<LevelLoader>.Create().Deploy();
					created = true;
				}
				else
				{
					Util.DebugPrint(L10n.Get(9));
				}
			}
		}

		public void OnDisabled()
		{
			// Unapply Harmony patches via Cities Harmony.
			if (HarmonyHelper.IsHarmonyInstalled)
			{
				Patcher.UnpatchAll();
			}
			Instance<LevelLoader>.instance?.Dispose();
			Settings.settings.helper = null;
			created = false;
		}


		/// <summary>
		/// Called by the game when the mod options panel is setup.
		/// </summary>
		public void OnSettingsUI(UIHelperBase helper)
		{
			Settings.settings.OnSettingsUI(helper);
		}
	}
}
