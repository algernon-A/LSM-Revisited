using ICities;
using CitiesHarmony.API;

namespace LoadingScreenMod
{
	public sealed class Mod : IUserMod, ILoadingExtension
	{
		private static bool created;

		public static string ModName => "LSM Revisited";

		public string Name => "Loading Screen Mod Revisited";

		public string Description => "Optimizes game loading";

		public void OnSettingsUI(UIHelperBase helper)
		{
			Settings.settings.OnSettingsUI(helper);
		}

		public void OnCreated(ILoading loading)
		{
		}

		public void OnReleased()
		{
		}

		public void OnLevelLoaded(LoadMode mode)
		{
		}

		public void OnLevelUnloading()
		{
		}

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
	}
}
