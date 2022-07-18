namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Global LSM Revisited settings.
    /// </summary>
    internal static class ModSettings
    {
        internal enum ImageMode : int
        {
            StandardBackground = 0,
            ImgurCuratedBackground,
            ImgurRandomBackground
        }


        private static ImageMode backgroundMode = ImageMode.StandardBackground;


        /// <summary>
        /// Background image mode.
        /// </summary>
        internal static ImageMode BackgroundImageMode
        {
            get => backgroundMode;

            set
            {
                backgroundMode = value;

                if (value == ImageMode.ImgurRandomBackground)
                {
                    // Ensure random image list is populated.
                    BackgroundImage.PopulatImgurRandomList();
                }
            }
        }
    }
}
