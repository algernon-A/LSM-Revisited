namespace LoadingScreenModRevisited
{
	using AlgernonCommons;
	using AlgernonCommons.Patching;
	using AlgernonCommons.Translation;
	using AlgernonCommons.UI;
	using ColossalFramework.UI;
	using ICities;

	/// <summary>
	/// The base mod class for instantiation by the game.
	/// </summary>
	public sealed class Mod : PatcherMod,  IUserMod
	{
		// Mod name.
		private static readonly string ModName = "Loading Screen Mod Revisited";

		/// <summary>
		/// Gets the mod's name for logging purposes.
		/// </summary>
		public override string LogName => "LSM Revisited";

		/// <summary>
		/// Gets the mod's unique Harmony identfier.
		/// </summary>
		public override string HarmonyID => "com.github.algernon-A.csl.lsmr";

		/// <summary>
		/// Gets the mod's display name.
		/// </summary>
		public override string Name => ModName + ' ' + AssemblyUtils.TrimmedCurrentVersion;

		/// <summary>
		/// Gets the mod's description for display in the content manager.
		/// </summary>
		public string Description => Translations.Translate("MOD_DESCRIPTION");

		/// <summary>
		/// Called by the game when the mod is enabled.
		/// </summary>
		public override void OnEnabled()
		{
			base.OnEnabled();

			// Attaching options panel event hook - check to see if UIView is ready.
			if (UIView.GetAView() != null)
			{
				// It's ready - attach the hook now.
				OptionsPanelManager<OptionsPanel>.OptionsEventHook();
			}
			else
			{
				// Otherwise, queue the hook for when the intro's finished loading.
				LoadingManager.instance.m_introLoaded += OptionsPanelManager<OptionsPanel>.OptionsEventHook;
			}
		}

		public override void OnDisabled()
		{
			base.OnDisabled();

			// Remove legacy settings helper.
			LoadingScreenMod.Settings.settings.helper = null;
		}

		/// <summary>
		/// Called by the game when the mod options panel is setup.
		/// </summary>
		/// <param name="helper">UI helper instance.</param>
		public void OnSettingsUI(UIHelperBase helper)
		{
			// Create options panel.
			OptionsPanelManager<OptionsPanel>.Setup(helper);
		}

		/// <summary>
		/// Saves settings file.
		/// </summary>
		public override void SaveSettings() => LSMRSettings.Save();

		/// <summary>
		/// Loads settings file.
		/// </summary>
		public override void LoadSettings() => LSMRSettings.Load();

		/// <summary>
		/// Apply Harmony patches.
		/// </summary>
		protected override void ApplyPatches() => Patcher.Instance.PatchAll();

		/// <summary>
		/// Remove Harmony patches.
		/// </summary>
		protected override void RemovePatches() => Patcher.Instance.UnpatchAll();
	}
}
