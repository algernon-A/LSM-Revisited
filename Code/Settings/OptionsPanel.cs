namespace LoadingScreenModRevisited
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Loading Screen Mod Revisited options panel.
    /// </summary>
    public sealed class OptionsPanel : UIPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        public OptionsPanel()
        {
            // Add tabstrip.
            UITabstrip tabStrip = UITabstrips.AddTabStrip(this, 0f, 0f, OptionsPanelManager<OptionsPanel>.PanelWidth, OptionsPanelManager<OptionsPanel>.PanelHeight, out UITabContainer _);
            tabStrip.clipChildren = false;

            // Add tabs and panels.
            new GeneralOptions(tabStrip, 0);
            new ReportingOptions(tabStrip, 1);
            new ImageOptions(tabStrip, 2);

            // Select first tab.
            tabStrip.selectedIndex = -1;
            tabStrip.selectedIndex = 0;
        }
    }
}