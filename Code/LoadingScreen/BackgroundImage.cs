using System;
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
        private static List<string> imgurURLs = new List<string>();


        /// <summary>
        /// Attempts to replace the given material with one based on a imgur image download.
        /// </summary>
        /// <param name="material">Original material</param>
        /// <returns>New material based on original with new texture, or null if failed</returns>
        internal static Material GetImgurImage(Material material)
        {
            // Try to populate the imgur list if we haven't already.
            if (imgurURLs.Count == 0)
            {
                PopulateImgurList();
            }

            // Randomise list order.
            System.Random random = new System.Random();
            foreach (string url in imgurURLs.OrderBy(x => random.Next()))
            {
                try
                {
                    // Add certificate validation callback, because Mono has no certificates by default and https will automatically fail if we don't do this.
                    ServicePointManager.ServerCertificateValidationCallback += CertificateValidationFudge;
                    
                    // Download image and convert to texture.
                    Logging.Message("downloading image from ", url);
                    byte[] imgurData = new WebClient().DownloadData(url);
                    Texture2D newTexture = new Texture2D(1, 1);
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
        internal static void PopulateImgurList()
        {
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
                    // Extract image name.
                    string matchValue = match.Value;
                    int index = matchValue.IndexOf("<div id=\"");
                    string str = matchValue.Substring(index + 9, 7);
                    
                    // Recreate direct image URL from image name.
                    imgurURLs.Add("http://i.imgur.com/" + str + ".jpg");
                }

                Logging.Message("downloaded ", imgurURLs.Count, " imgur URLs");
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception downloading image list from imgur");
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
        private static RemoteCertificateValidationCallback CertificateValidationFudge = (sender, cert, chain, sslPolicyErrors) => true;
    }
}