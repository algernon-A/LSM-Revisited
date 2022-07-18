using CitiesHarmony.API;
using ColossalFramework.UI;
using ICities;


namespace LoadingScreenModRevisited
{
	/// <summary>
	/// The base mod class for instantiation by the game.
	/// </summary>
	public sealed class LSMRMod : IUserMod
	{
		public static string ModName => "LSM Revisited";

		public string Name => "Loading Screen Mod Revisited " + ModUtils.CurrentVersion;
		public string Description => "Optimizes game loading";


		/// <summary>
		/// Called by the game when the mod is enabled.
		/// </summary>
		public void OnEnabled()
		{
			// Apply Harmony patches via Cities Harmony.
			// Called here instead of OnCreated to allow the auto-downloader to do its work prior to launch.
			HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

			// Attaching options panel event hook - check to see if UIView is ready.
			if (UIView.GetAView() != null)
			{
				// It's ready - attach the hook now.
				OptionsPanel.OptionsEventHook();
			}
			else
			{
				// Otherwise, queue the hook for when the intro's finished loading.
				LoadingManager.instance.m_introLoaded += OptionsPanel.OptionsEventHook;
			}
		}

		public void OnDisabled()
		{
			// Unapply Harmony patches via Cities Harmony.
			if (HarmonyHelper.IsHarmonyInstalled)
			{
				Patcher.UnpatchAll();
			}

			// Remove legacy settings helper.
			LoadingScreenMod.Settings.settings.helper = null;
		}


		/// <summary>
		/// Called by the game when the mod options panel is setup.
		/// </summary>
		public void OnSettingsUI(UIHelperBase helper)
		{
			// Create options panel.
			OptionsPanel.Setup(helper);
		}
	}
}
