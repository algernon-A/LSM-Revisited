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
    using System.Threading;
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
            "xKv1IXE",
            "bkrnxbs",
            "N75RlZw",
            "L04Xquk",
            "3dLIYdW",
            "fLooD1S",
            "56wgof3",
            "hJkVHOV",
            "wHikRDm",
            "auUxPsy",
            "2dcmsLl",
            "mTNA6of",
            "MtJiG0x",
            "w6hHEsE",
            "6vs9wYs",
            "FkMBrDR",
            "jeqpvkb",
            "falRocy",
            "QYA95jw",
            "mW2av7v",
            "65FTls3",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "bDd7f1z",
            "SI9l1Im",
            "hLakgLG",
            "t1tMMzB",
            "AaZAYIL",
            "Kx0XE1A",
            "5j86fMi",
            "zrnNUxO",
            "rdtwLnF",
            "0Wdbcwg",
            "IKSjOqg",
            "oho7hy8",
            "zFo2F95",
            "J57csCQ",
            "1fuidqs",
            "kUWSn7b",
            "UovZaHz",
            "XennCqw",
            "r0FLa3N",
            "WJfNPms",
            "BuLmBmv",
            "257qPwi",
            "cfEuNQ8",
            "ZQW8mUS",
            "zQHsYoX",
            "gZfOaoW",
            "SAV82cF",
            "T7Yh0hU",
            "knmDtAR",
            "r7co2La",
            "X4l8whG",
            "5qBm3J1",
            "Byff99p",
            "9d7BLCu",
            "NzWiWfy",
            "dBQozfx",
            "1v87xzW",
            "kovEmSI",
            "W6vMrk5",
            "1MN9jf3",
            "03w1ODF",
            "Rf6LE5c",
            "ypTHCHR",
            "fbcMbbv",
            "wvOlj1J",
            "ZmWw64y",
            "TVQekcM",
            "zXc0i4h",
            "P3vji3g",
            "1DUCn0j",
            "pXjZGxN",
            "QbsgfO3",
            "6PnjWQk",
            "2PG64TR",
            "M4hZxn9",
            "EOddjuN",
            "4u5ezDo",
            "gyBnR7F",
            "L4VudOX",
            "HNxg48S",
            "ODBgGgd",
            "Otgb4KZ",
            "0mKDIF6",
            "8oIxSQD",
            "BSVM4LF",
            "4nUz5sI",
            "IWa9VAD",
            "kTiOis3",
            "hO0E2OT",
            "HR59XnG",
            "aFwjRYF",
            "t84M6ga",
            "7ongppt",
            "P4lQ9Pj",
            "DyWMESH",
            "O6pE2XL",
            "V4NuJ2E",
            "DtOsWUl",
            "yGCyAzM",
            "RcFB4gi",
            "354uOxf",
            "7rvIrQZ",
            "fNNnQCB",
            "Ps8Rdwr",
            "6pEF2w4",
            "ZdNPakx",
            "CuGdgiC",
            "5Ele2Vs",
            "St5Gfml",
            "q2DlmNU",
            "Ub46eOc",
            "H08LQs8",
            "aeTDKSZ",
            "nRB2qnM",
            "4TRKAjH",
            "zE1oBgH",
            "nu3knFe",
            "WQ3Zjzq",
            "ZODnQnx",
            "CpA8w7F",
            "pnJMLQ1",
            "FD3jtdY",
            "DVuhXqZ",
            "1vM5iLa",
            "xMV9Ev3",
            "JRDZtdT",
            "8Ypcn1w",
            "kQNYDek",
            "TpyRBvy",
            "hBokbnW",
            "zaORMmo",
            "l8O6Hra",
            "TvYADQz",
            "G9S8Y9V",
            "CXb9kEf",
            "o9eQQ2I",
            "wrcdpc8",
            "GbrTiKd",
            "IlqsfNj",
            "dM87CYl",
            "jKgOARp",
            "H2Q0Fec",
            "CdKdvXS",
            "OddJSnW",
            "MxUrrt6",
            "GTKXV2E",
            "SVfVJ4P",
            "W8Tevms",
            "Ao9FQos",
            "Zsr0LMt",
            "TmFbL9q",
            "1HRfJ3i",
            "P9wJeC4",
            "xVFxswV",
            "iPoiCia",
            "aZazvLI",
            "vA5fT9g",
            "4XUhX7D",
            "K9WGs66",
            "apPHD61",
            "M20G7eU",
            "zzmxM8J",
            "PYC5hyD",
            "9TuVLqb",
            "2C02Z5Q",
            "XaZ2YDe",
            "Xdpl4g6",
            "QFoGR2i",
            "IdEE6lI",
            "MDccrCS",
            "qEySV6N",
            "0sVkAU9",
            "StjdJ87",
            "AXmwGmN",
            "U1CGOgB",
            "EMzpfz2",
            "xA8MikP",
            "3TJ2yxD",
            "tJ4xXiH",
            "hJtN6VQ",
            "HowVFdD",
            "bPz0Kkt",
            "BBzV11x",
            "dPnooV3",
            "bTjWKK3",
            "dBNmHI5",
            "fOqcYjP",
            "CuZdRqL",
            "TAfrcim",
            "kuSRM0U",
            "f4f9CmT",
            "knnhOyy",
            "pi7ZbtT",
            "oapjR0U",
            "b3GhlQo",
            "jcN0hFH",
            "K5JBkP9",
            "lfozNyX",
            "7fhztZw",
            "zM9YaF2",
            "k8Su6ma",
            "JZwpJiG",
            "Foh2Oku",
            "Ycgyr1y",
            "jW7GkgM",
            "GCYfNbM",
            "uNlps0f",
            "8tW4HNr",
            "DtjH3c9",
            "rvKTqN6",
            "GorjnyB",
            "WPUlaQc",
            "RoDnU03",
            "azuhpyy",
            "RVFbO7k",
            "XvHY1eX",
            "X2OYRAv",
            "LhzxTwR",
            "6biTwI1",
            "wmvIQyi",
            "Mf3gfZQ",
            "nPDlt2C",
            "5Vryiyb",
            "Ik0ptqA",
            "ZmjtQ7m",
            "RMHLIR4",
            "tAMhjOu",
            "XX4TqNh",
            "OCeviTx",
            "FE1b4xS",
            "YESvpRY",
            "7dufs6E",
            "KE0H51b",
            "DikIlyi",
            "o4s094o",
            "ZkYGpIi",
            "ySkSk89",
            "B6S8OR5",
            "B6S8OR5",
            "37r2KEu",
            "9R9BCSP",
            "aXnlihL",
            "ctaJ0tn",
            "2qeiwDL",
            "hdSW7d5",
            "HskqYNl",
            "KaQqEX8",
            "iGDe8kB",
            "l4v6vJU",
            "6qWg8WG",
            "Y72mSiL",
            "icrm5dk",
            "13m22CE",
            "mos0qSK",
            "thnuocv",
            "eGLD1NN",
            "qCAVndW",
            "b8rOlx1",
            "QBJfH1o",
            "3O30lp4",
            "L5TQlQQ",
            "APzxig1",
            "PwdUrbg",
            "VFas8wl",
            "xg2aU5a",
            "AWD0Xcn",
            "kOiyuMy",
            "00uHAy7",
            "Lareb3L",
            "B8HJUbu",
            "fkRDVk9",
            "tQXNVWN",
            "20sp6dV",
            "Kip0oSm",
            "XzHVTlS",
            "yFMvmPX",
            "rdHt4QX",
            "HIULNnb",
            "6B4tVyP",
            "BYNpq0K",
            "fG9WMwM",
            "VcTIBOP",
            "zAQqAIi",
            "0mCvYIj",
            "puUjIqb",
            "IuzQQf1",
            "Ew8T6Va",
            "rXxFTbT",
            "Pnnsj2o",
            "tvMViEV",
            "TpwRgQp",
            "AZQZw0Q",
            "NKLnCAK",
            "rHLp0QW",
            "FOFJNxh",
            "YVTsJ6I",
            "qMKGjH7",
            "0jdBGcl",
            "whYDFIA",
            "HfOEND5",
            "KzCaw1d",
            "KEFkc4p",
            "3tVt8O9",
            "BNRl63J",
            "iQLbI6A",
            "3SAjv68",
            "qBd8LdD",
            "N3Zfp59",
            "P0qocaj",
            "9U15vh8",
            "smGrryS",
            "EhvI1b4",
            "AMl9TcA",
            "PYsk086",
            "4Q34rfy",
            "BDaaoZs",
            "sF8Wc4e",
            "crDrHCa",
            "6x8hDCr",
            "rC4zInW",
            "xUG6IiP",
            "QLbViW2",
            "VkVoOXK",
            "nYtomDf",
            "mAbipzW",
            "9jJPLtZ",
            "PWDgqkq",
            "im9ZEW5",
            "696opYw",
            "kzTuN7c",
            "FmKez9P",
            "y5qNrHQ",
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

                    // Download asynchronously.
                    ImageDownloader.StartImgurImageDownload(RandomImages);
                }

                // Curated images - download asynchronously.
                if (value == ImageMode.ImgurCurated)
                {
                    ImageDownloader.StartImgurImageDownload(CuratedImages);
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
        /// <param name="originalMaterial">Original material.</param>
        /// <returns>New material based on original with new texture, or null if failed.</returns>
        internal static Material GetImage(Material originalMaterial)
        {
            switch (s_imageMode)
            {
                case ImageMode.LocalRandom:
                    return GetLocalImage(originalMaterial);

                case ImageMode.ImgurCurated:
                    return GetDownloadedMaterial(originalMaterial, CuratedImages);

                case ImageMode.ImgurRandom:
                    // Try to populate the random list if we haven't already.
                    if (RandomImages.Count == 0)
                    {
                        PopulateImgurRandomList();
                    }

                    return GetDownloadedMaterial(originalMaterial, RandomImages);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Retrieves any downloaded image data and applies it to the given material.
        /// Performs a minimum size check of 1920x1080.
        /// </summary>
        /// <param name="originalMaterial">Original material to update.</param>
        /// <param name="imageList">Image list to attempt next download from if this attempt was unsuccessful.</param>
        /// <returns>Updated material, or null if unusccessful (no download, downloaded image too small, or other failure).</returns>
        private static Material GetDownloadedMaterial(Material originalMaterial, List<string> imageList)
        {
            if (Monitor.TryEnter(ImageDownloader.Lock) && ImageDownloader.ImageData is byte[] imageData)
            {
                try
                {
                    // Clear the image ready flag to show we've done this one..
                    ImageDownloader.DownloadReady = false;

                    Texture2D newTexture = new Texture2D(1, 1);
                    if (newTexture.LoadImage(imageData))
                    {
                        // Need minimum image size of 1920x1080.
                        if (newTexture.width >= 1920 & newTexture.height >= 1080)
                        {
                            // Convert to material and return it.
                            return new Material(originalMaterial)
                            {
                                mainTexture = newTexture,
                            };
                        }

                        Logging.Message("downloaded image was too small; skipping");
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "exception creating downloaded image material");
                }
                finally
                {
                    Monitor.Exit(ImageDownloader.Lock);
                }
            }

            // If we got here, we didn't get an image; return null (for now) after triggering a new download attempt.
            ImageDownloader.StartImgurImageDownload(imageList);
            return null;
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

                    // Got an eligible candidate - convert to material and return it.
                    return new Material(material)
                    {
                        mainTexture = newTexture,
                    };
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