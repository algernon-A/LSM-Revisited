// <copyright file="BackgroundImage.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Text.RegularExpressions;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Custom background image handling.
    /// </summary>
    public static class BackgroundImage
    {
        // List of image URLs from imgur.
        private static readonly List<string> RandomImages = new List<string>();

        // Cureated imgur images.
        private static readonly List<string> CuratedImages = new List<string>
        {
            "rgxqLV0",
            "jNyKCHa",
            "lgcI6OY",
            "vUhu5jP",
            "B7uZk9h",
            "ZDnQ9E6",
            "VXq6pkf",
            "55r4CU3",
            "6vJwZMc",
            "1XFZ7R4",
            "ZFYmaxe",
            "J4iPKta",
            "flntFdZ",
            "eHxXttn",
            "wRb2zpm",
            "6v3CNXO",
            "KENCQU6",
            "IcyyO0Y",
            "SopTi2R",
            "VsemQRg",
            "CrXy9hx",
            "FsDd12C",
        };

        /// <summary>
        /// Simple Mono https/ssl certificate validation override to ensure that all certificates are accepted,
        /// because Mono has no certificates by default and https will automatically fail if we don't do this.
        /// We don't do anything more sophisticated because we really don't care about site validation for this.
        /// You could argue that even checking the common name is overkill here, but we'll do it anyway.
        /// </summary>
        private static readonly RemoteCertificateValidationCallback CertificateValidationFudge = (sender, cert, chain, sslPolicyErrors) => cert.Subject.Contains("CN=*.imgur.com");

        // Local random image directory.
        private static string s_imageDir = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "LoadingImages");

        // Current background image mode.
        private static ImageMode s_imageMode = ImageMode.Standard;

        // Current background image scaling mode.
        private static ScaleMode s_scaleMode = ScaleMode.ScaleToFit;

        /// <summary>
        /// Loading image mode enum.
        /// </summary>
        public enum ImageMode : int
        {
            /// <summary>
            /// Standard (game) background image.
            /// </summary>
            Standard = 0,

            /// <summary>
            /// Curated image from r/CitiesSkylines on imgur
            /// </summary>
            ImgurCurated,

            /// <summary>
            /// Random image from r/CitiesSkylines on imgur
            /// </summary>
            ImgurRandom,

            /// <summary>
            /// Random image from a local directory.
            /// </summary>
            LocalRandom,
        }

        /// <summary>
        /// Gets or sets the local image directory.
        /// </summary>
        internal static string ImageDirectory { get => s_imageDir; set => s_imageDir = value; }

        /// <summary>
        /// Gets or sets the current background image mode.
        /// </summary>
        internal static ImageMode CurrentImageMode
        {
            get => s_imageMode;

            set
            {
                s_imageMode = value;

                if (value == ImageMode.ImgurRandom)
                {
                    // Ensure random image list is populated.
                    PopulateImgurRandomList();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current background image scaling mode.
        /// </summary>
        internal static ScaleMode ImageScaling { get => s_scaleMode; set => s_scaleMode = value; }

        /// <summary>
        /// Attempts to replace the given material with one according to custom settings.
        /// </summary>
        /// <param name="material">Original material.</param>
        /// <returns>New material based on original with new texture, or null if failed.</returns>
        internal static Material GetImage(Material material)
        {
            switch (s_imageMode)
            {
                case ImageMode.LocalRandom:
                    return GetLocalImage(material);

                case ImageMode.ImgurCurated:
                    return GetImgurImage(material, CuratedImages);

                case ImageMode.ImgurRandom:
                    // Try to populate the random list if we haven't already.
                    if (RandomImages.Count == 0)
                    {
                        PopulateImgurRandomList();
                    }

                    return GetImgurImage(material, RandomImages);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Attempts to replace the given material with one from a randomly selected file in a local image directory.
        /// </summary>
        /// <param name="material">Original material.</param>
        /// <returns>New material based on original with new texture, or null if failed.</returns>
        private static Material GetLocalImage(Material material)
        {
            // Check that the specified directory exists.
            if (!Directory.Exists(s_imageDir))
            {
                Logging.KeyMessage("local image directory not found: ", s_imageDir);
                return null;
            }

            // Background texture base.
            Texture2D newTexture = new Texture2D(1, 1);

            // Get list of files.
            System.Random random = new System.Random();
            string[] fileNames = Directory.GetFiles(s_imageDir);

            // Iterate through filenames in random list order.
            foreach (string imageFileName in fileNames.ToList().OrderBy(x => random.Next()))
            {
                try
                {
                    // Skip anything that isn't png or jpg.
                    if (imageFileName == null || (!imageFileName.EndsWith(".png") && !imageFileName.EndsWith(".jpg")))
                    {
                        continue;
                    }

                    Logging.Message("found image file ", imageFileName);

                    // Read file and convert to texture.
                    byte[] imageData = File.ReadAllBytes(imageFileName);
                    newTexture.LoadImage(imageData);

                    // TODO: Removed image size check for local files.
                    // Need minimum image size of 1920x1080.
                    // if (newTexture.width >= 1920 | newTexture.height >= 1080)
                    {
                        // Got an eligible candidate - convert to material and return it.
                        return new Material(material)
                        {
                            mainTexture = newTexture,
                        };
                    }

                    // Logging.Message("image too small: ", imageFileName);
                }
                catch (Exception e)
                {
                    // Don't let a single exception stop us.
                    Logging.LogException(e, "exception reading texture file ", imageFileName ?? "null");
                }
            }

            // If we got here, we didn't get an image; return null.
            return null;
        }

        /// <summary>
        /// Attempts to replace the given material with one based on a imgur image download.
        /// </summary>
        /// <param name="material">Original material.</param>
        /// <param name="imageList">Image list to use.</param>
        /// <returns>New material based on original with new texture, or null if failed.</returns>
        private static Material GetImgurImage(Material material, List<string> imageList)
        {
            // Background texture base.
            Texture2D newTexture = new Texture2D(1, 1);

            // Randomise list order.
            System.Random random = new System.Random();
            foreach (string imageName in imageList.OrderBy(x => random.Next()))
            {
                try
                {
                    // Add certificate validation callback, because Mono has no certificates by default and https will automatically fail if we don't do this.
                    ServicePointManager.ServerCertificateValidationCallback += CertificateValidationFudge;

                    // Recreate direct image URL from image name.
                    string imageURL = "http://i.imgur.com/" + imageName + ".jpg";
                    Logging.Message("downloading image from ", imageURL);

                    // Download image and convert to texture.
                    byte[] imageData = new WebClient().DownloadData(imageURL);
                    newTexture.LoadImage(imageData);

                    // Need minimum image size of 1920x1080.
                    if (newTexture.width >= 1920 & newTexture.height >= 1080)
                    {
                        // Got an eligible candidate - convert to material and return it.
                        return new Material(material)
                        {
                            mainTexture = newTexture,
                        };
                    }

                    Logging.Message("image too small");
                }
                catch (Exception e)
                {
                    // Don't let a single exception stop us.
                    Logging.LogException(e, "exception creating new background texture from imgur download");
                }
                finally
                {
                    // Remove certificate validation callback (don't leave this hanging around).
                    ServicePointManager.ServerCertificateValidationCallback -= CertificateValidationFudge;
                }
            }

            // If we got here, we didn't get an image; return null.
            return null;
        }

        /// <summary>
        /// Populates the list of image URLs from /r/CitiesSkylines on imgur.
        /// </summary>
        private static void PopulateImgurRandomList()
        {
            // Don't do anything if the list is already populated.
            if (RandomImages.Count > 0)
            {
                return;
            }

            try
            {
                // Add certificate validation callback, because Mono has no certificates by default and https will automatically fail if we don't do this.
                ServicePointManager.ServerCertificateValidationCallback += CertificateValidationFudge;

                // Get the top images on /r/CitiesSkylines on imgur.
                string imgurFeed = new WebClient().DownloadString("http://imgur.com/r/CitiesSkylines/top/hit?scrolled");

                // This is what Regex was made for.
                MatchCollection imageCandidates = Regex.Matches(imgurFeed, @"<div id=\""(.*)\"" class=\""post\"">");
                foreach (Match match in imageCandidates)
                {
                    // Extract image name and add to list.
                    string matchValue = match.Value;
                    int index = matchValue.IndexOf("<div id=\"");
                    RandomImages.Add(matchValue.Substring(index + 9, 7));
                }

                Logging.Message("downloaded ", RandomImages.Count, " imgur URLs");
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception downloading top image list from imgur");
            }
            finally
            {
                // Remove certificate validation callback (don't leave this hanging around).
                ServicePointManager.ServerCertificateValidationCallback -= CertificateValidationFudge;
            }
        }
    }
}