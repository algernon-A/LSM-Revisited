namespace LoadingScreenModRevisited
{
	using AlgernonCommons.UI;
    using ICities;

    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public sealed class Loading : LoadingExtensionBase
	{
		/// <summary>
		/// Called by the game when level loading is complete.
		/// </summary>
		/// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);

			// Set up options panel event handler (need to redo this now that options panel has been reset after loading into game).
			OptionsPanelManager<OptionsPanel>.OptionsEventHook();
		}
	}
}