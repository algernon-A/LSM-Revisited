﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text.RegularExpressions;
using UnityEngine;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Custom background image handling.
    /// </summary>
    internal static class BackgroundImage
    {
        // List of image URLs from Imgur.
        private static List<string> randomImages = new List<string>();


        private static List<string> curatedImages = new List<string>
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
            "eHxXttn"
        };

        /// <summary>
        /// Attempts to replace the given material with one based on a imgur image download.
        /// </summary>
        /// <param name="material">Original material</param>
        /// <returns>New material based on original with new texture, or null if failed</returns>
        internal static Material GetImgurImage(Material material)
        {
            // Determine which list to use.
            List<string> imageList = ModSettings.BackgroundImageMode == ModSettings.ImageMode.ImgurCuratedBackground ? curatedImages : randomImages; 

            // Try to populate the random list if we're using it and haven't already.
            if (imageList == randomImages && imageList.Count == 0)
            {
                PopulatImgurRandomList();
            }

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
                    byte[] imgurData = new WebClient().DownloadData(imageURL);
                    newTexture.LoadImage(imgurData);

                    // Need minimum image size of 1920x1080.
                    if (newTexture.width >= 1920 | newTexture.height >= 1080)
                    {
                        // Got an eligible candidate - convert to material and return it.
                        return new Material(material)
                        {
                            mainTexture = newTexture
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
        internal static void PopulatImgurRandomList()
        {
            // Don't do anything if the list is already populated.
            if (randomImages.Count > 0)
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
                    randomImages.Add(matchValue.Substring(index + 9, 7));
                }

                Logging.Message("downloaded ", randomImages.Count, " imgur URLs");
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


        /// <summary>
        /// Simple Mono https/ssl certificate validation override to ensure that all certificates are accepted.
        /// because Mono has no certificates by default and https will automatically fail if we don't do this.
        /// </summary>
        private static RemoteCertificateValidationCallback CertificateValidationFudge = (sender, cert, chain, sslPolicyErrors) => cert.Subject.Contains("CN=*.imgur.com");
    }
}