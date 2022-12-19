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
            "xKv1IXE.jpg",
            "bkrnxbs.jpg",
            "N75RlZw.jpg",
            "L04Xquk.jpg",
            "3dLIYdW.jpg",
            "fLooD1S.jpg",
            "56wgof3.jpg",
            "hJkVHOV.jpg",
            "wHikRDm.jpg",
            "auUxPsy.jpg",
            "2dcmsLl.jpg",
            "mTNA6of.jpg",
            "MtJiG0x.jpg",
            "w6hHEsE.jpg",
            "6vs9wYs.jpg",
            "FkMBrDR.jpg",
            "jeqpvkb.jpg",
            "falRocy.jpg",
            "QYA95jw.jpg",
            "mW2av7v.jpg",
            "65FTls3.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "bDd7f1z.jpg",
            "SI9l1Im.jpg",
            "hLakgLG.jpg",
            "t1tMMzB.jpg",
            "AaZAYIL.jpg",
            "Kx0XE1A.jpg",
            "5j86fMi.jpg",
            "zrnNUxO.jpg",
            "rdtwLnF.jpg",
            "0Wdbcwg.jpg",
            "IKSjOqg.jpg",
            "oho7hy8.jpg",
            "zFo2F95.jpg",
            "J57csCQ.jpg",
            "1fuidqs.jpg",
            "kUWSn7b.jpg",
            "UovZaHz.jpg",
            "XennCqw.jpg",
            "r0FLa3N.jpg",
            "WJfNPms.jpg",
            "BuLmBmv.jpg",
            "257qPwi.jpg",
            "cfEuNQ8.jpg",
            "ZQW8mUS.jpg",
            "zQHsYoX.jpg",
            "gZfOaoW.jpg",
            "SAV82cF.jpg",
            "T7Yh0hU.jpg",
            "knmDtAR.jpg",
            "r7co2La.jpg",
            "X4l8whG.jpg",
            "5qBm3J1.jpg",
            "Byff99p.jpg",
            "9d7BLCu.jpg",
            "NzWiWfy.jpg",
            "dBQozfx.jpg",
            "1v87xzW.jpg",
            "kovEmSI.jpg",
            "W6vMrk5.jpg",
            "1MN9jf3.jpg",
            "03w1ODF.jpg",
            "Rf6LE5c.jpg",
            "ypTHCHR.jpg",
            "fbcMbbv.jpg",
            "wvOlj1J.jpg",
            "ZmWw64y.jpg",
            "TVQekcM.jpg",
            "zXc0i4h.jpg",
            "P3vji3g.jpg",
            "1DUCn0j.jpg",
            "pXjZGxN.jpg",
            "QbsgfO3.jpg",
            "6PnjWQk.jpg",
            "2PG64TR.jpg",
            "M4hZxn9.jpg",
            "EOddjuN.jpg",
            "4u5ezDo.jpg",
            "gyBnR7F.jpg",
            "L4VudOX.jpg",
            "HNxg48S.jpg",
            "ODBgGgd.jpg",
            "Otgb4KZ.jpg",
            "0mKDIF6.jpg",
            "8oIxSQD.jpg",
            "BSVM4LF.jpg",
            "4nUz5sI.jpg",
            "IWa9VAD.jpg",
            "kTiOis3.jpg",
            "hO0E2OT.jpg",
            "HR59XnG.jpg",
            "aFwjRYF.jpg",
            "t84M6ga.jpg",
            "7ongppt.jpg",
            "P4lQ9Pj.jpg",
            "DyWMESH.jpg",
            "O6pE2XL.jpg",
            "V4NuJ2E.jpg",
            "DtOsWUl.jpg",
            "yGCyAzM.jpg",
            "RcFB4gi.jpg",
            "354uOxf.jpg",
            "7rvIrQZ.jpg",
            "fNNnQCB.jpg",
            "Ps8Rdwr.jpg",
            "6pEF2w4.jpg",
            "ZdNPakx.jpg",
            "CuGdgiC.jpg",
            "5Ele2Vs.jpg",
            "St5Gfml.jpg",
            "q2DlmNU.jpg",
            "Ub46eOc.jpg",
            "H08LQs8.jpg",
            "aeTDKSZ.jpg",
            "nRB2qnM.jpg",
            "4TRKAjH.jpg",
            "zE1oBgH.jpg",
            "nu3knFe.jpg",
            "WQ3Zjzq.jpg",
            "ZODnQnx.jpg",
            "CpA8w7F.jpg",
            "pnJMLQ1.jpg",
            "FD3jtdY.jpg",
            "DVuhXqZ.jpg",
            "1vM5iLa.jpg",
            "xMV9Ev3.jpg",
            "JRDZtdT.jpg",
            "8Ypcn1w.jpg",
            "kQNYDek.jpg",
            "TpyRBvy.jpg",
            "hBokbnW.jpg",
            "zaORMmo.jpg",
            "l8O6Hra.jpg",
            "TvYADQz.jpg",
            "G9S8Y9V.jpg",
            "CXb9kEf.jpg",
            "o9eQQ2I.jpg",
            "wrcdpc8.jpg",
            "GbrTiKd.jpg",
            "IlqsfNj.jpg",
            "dM87CYl.jpg",
            "jKgOARp.jpg",
            "H2Q0Fec.jpg",
            "CdKdvXS.jpg",
            "OddJSnW.jpg",
            "MxUrrt6.jpg",
            "GTKXV2E.jpg",
            "SVfVJ4P.jpg",
            "W8Tevms.jpg",
            "Ao9FQos.jpg",
            "Zsr0LMt.jpg",
            "TmFbL9q.jpg",
            "1HRfJ3i.jpg",
            "P9wJeC4.jpg",
            "xVFxswV.jpg",
            "iPoiCia.jpg",
            "aZazvLI.jpg",
            "vA5fT9g.jpg",
            "4XUhX7D.jpg",
            "K9WGs66.jpg",
            "apPHD61.jpg",
            "M20G7eU.jpg",
            "zzmxM8J.jpg",
            "PYC5hyD.jpg",
            "9TuVLqb.jpg",
            "2C02Z5Q.jpg",
            "XaZ2YDe.jpg",
            "Xdpl4g6.jpg",
            "QFoGR2i.jpg",
            "IdEE6lI.jpg",
            "MDccrCS.jpg",
            "qEySV6N.jpg",
            "0sVkAU9.jpg",
            "StjdJ87.jpg",
            "AXmwGmN.jpg",
            "U1CGOgB.jpg",
            "EMzpfz2.jpg",
            "xA8MikP.jpg",
            "3TJ2yxD.jpg",
            "tJ4xXiH.jpg",
            "hJtN6VQ.jpg",
            "HowVFdD.jpg",
            "bPz0Kkt.jpg",
            "BBzV11x.jpg",
            "dPnooV3.jpg",
            "bTjWKK3.jpg",
            "dBNmHI5.jpg",
            "fOqcYjP.jpg",
            "CuZdRqL.jpg",
            "TAfrcim.jpg",
            "kuSRM0U.jpg",
            "f4f9CmT.jpg",
            "knnhOyy.jpg",
            "pi7ZbtT.jpg",
            "oapjR0U.jpg",
            "b3GhlQo.jpg",
            "jcN0hFH.jpg",
            "K5JBkP9.jpg",
            "lfozNyX.jpg",
            "7fhztZw.jpg",
            "zM9YaF2.jpg",
            "k8Su6ma.jpg",
            "JZwpJiG.jpg",
            "Foh2Oku.jpg",
            "Ycgyr1y.jpg",
            "jW7GkgM.jpg",
            "GCYfNbM.jpg",
            "uNlps0f.jpg",
            "8tW4HNr.jpg",
            "DtjH3c9.jpg",
            "rvKTqN6.jpg",
            "GorjnyB.jpg",
            "WPUlaQc.jpg",
            "RoDnU03.jpg",
            "azuhpyy.jpg",
            "RVFbO7k.jpg",
            "XvHY1eX.jpg",
            "X2OYRAv.jpg",
            "LhzxTwR.jpg",
            "6biTwI1.jpg",
            "wmvIQyi.jpg",
            "Mf3gfZQ.jpg",
            "nPDlt2C.jpg",
            "5Vryiyb.jpg",
            "Ik0ptqA.jpg",
            "ZmjtQ7m.jpg",
            "RMHLIR4.jpg",
            "tAMhjOu.jpg",
            "XX4TqNh.jpg",
            "OCeviTx.jpg",
            "FE1b4xS.jpg",
            "YESvpRY.jpg",
            "7dufs6E.jpg",
            "KE0H51b.jpg",
            "DikIlyi.jpg",
            "o4s094o.jpg",
            "ZkYGpIi.jpg",
            "ySkSk89.jpg",
            "B6S8OR5.jpg",
            "B6S8OR5.jpg",
            "37r2KEu.jpg",
            "9R9BCSP.jpg",
            "aXnlihL.jpg",
            "ctaJ0tn.jpg",
            "2qeiwDL.jpg",
            "hdSW7d5.jpg",
            "HskqYNl.jpg",
            "KaQqEX8.jpg",
            "iGDe8kB.jpg",
            "l4v6vJU.jpg",
            "6qWg8WG.jpg",
            "Y72mSiL.jpg",
            "icrm5dk.jpg",
            "13m22CE.jpg",
            "mos0qSK.jpg",
            "thnuocv.jpg",
            "eGLD1NN.jpg",
            "qCAVndW.jpg",
            "b8rOlx1.jpg",
            "QBJfH1o.jpg",
            "3O30lp4.jpg",
            "L5TQlQQ.jpg",
            "APzxig1.jpg",
            "PwdUrbg.jpg",
            "VFas8wl.jpg",
            "xg2aU5a.jpg",
            "AWD0Xcn.jpg",
            "kOiyuMy.jpg",
            "00uHAy7.jpg",
            "Lareb3L.jpg",
            "B8HJUbu.jpg",
            "fkRDVk9.jpg",
            "tQXNVWN.jpg",
            "20sp6dV.jpg",
            "Kip0oSm.jpg",
            "XzHVTlS.jpg",
            "yFMvmPX.jpg",
            "rdHt4QX.jpg",
            "HIULNnb.jpg",
            "6B4tVyP.jpg",
            "BYNpq0K.jpg",
            "fG9WMwM.jpg",
            "VcTIBOP.jpg",
            "0mCvYIj.jpg",
            "puUjIqb.jpg",
            "IuzQQf1.jpg",
            "Ew8T6Va.jpg",
            "rXxFTbT.jpg",
            "Pnnsj2o.jpg",
            "tvMViEV.jpg",
            "TpwRgQp.jpg",
            "AZQZw0Q.jpg",
            "NKLnCAK.jpg",
            "rHLp0QW.jpg",
            "FOFJNxh.jpg",
            "YVTsJ6I.jpg",
            "qMKGjH7.jpg",
            "0jdBGcl.jpg",
            "whYDFIA.jpg",
            "HfOEND5.jpg",
            "KzCaw1d.jpg",
            "KEFkc4p.jpg",
            "3tVt8O9.jpg",
            "BNRl63J.jpg",
            "iQLbI6A.jpg",
            "3SAjv68.jpg",
            "qBd8LdD.jpg",
            "N3Zfp59.jpg",
            "P0qocaj.jpg",
            "9U15vh8.jpg",
            "smGrryS.jpg",
            "EhvI1b4.jpg",
            "AMl9TcA.jpg",
            "PYsk086.jpg",
            "4Q34rfy.jpg",
            "BDaaoZs.jpg",
            "sF8Wc4e.jpg",
            "crDrHCa.jpg",
            "6x8hDCr.jpg",
            "rC4zInW.jpg",
            "xUG6IiP.jpg",
            "QLbViW2.jpg",
            "VkVoOXK.jpg",
            "nYtomDf.jpg",
            "mAbipzW.jpg",
            "9jJPLtZ.jpg",
            "PWDgqkq.jpg",
            "im9ZEW5.jpg",
            "696opYw.jpg",
            "kzTuN7c.jpg",
            "FmKez9P.jpg",
            "y5qNrHQ.jpg",
            "xQTNcYt.jpg",
            "iW3lrEL.jpg",
            "8iRpRA6.jpg",
            "Qg64omI.jpg",
            "7dp2I91.jpg",
            "FXv5BIw.jpg",
            "lbnOQ5X.jpg",
            "1I6RxZC.jpg",
            "HM1clW6.jpg",
            "wYG9sRD.jpg",
            "qS7qyrN.jpg",
            "qS7qyrN.jpg",
            "vAZFsRI.jpg",
            "XQTvM4H.jpg",
            "aBinPtb.jpg",
            "V6a1j3a.jpg",
            "Pl4X2ns.jpg",
            "8GQwfSI.jpg",
            "zGmzJFp.jpg",
            "IE71SaF.jpg",
            "m95s1E3.jpg",
            "EkuSh1j.jpg",
            "oG0lK0o.jpg",
            "tCm54Qz.jpg",
            "W2bU2zb.jpg",
            "oDwElAQ.jpg",
            "qpv3vtx.jpg",
            "wiluTX7.jpg",
            "8UMWrRf.jpg",
            "G5gpfbe.jpg",
            "gMe5YT6.jpg",
            "AtygE4u.jpg",
            "aVmI8gB.jpg",
            "4RCO3E0.jpg",
            "MJ6MUGn.jpg",
            "XlTVbFU.jpg",
            "GsdfTTE.jpg",
            "e7C3Kte.jpg",
            "dh8AH4Z.jpg",
            "nwe99Tn.jpg",
            "tZJDMhx.jpg",
            "pUec4KM.jpg",
            "Kyior51.jpg",
            "QxRLlQK.jpg",
            "XVbwTSs.jpg",
            "Zx7KR7B.jpg",
            "XRU1Lxj.jpg",
            "bjgjl7g.jpg",
            "vo0ubcY.jpg",
            "mJ82hwD.jpg",
            "YqUbCek.jpg",
            "bnWUt6N.jpg",
            "sXuTjL6.jpg",
            "0QZzh65.jpg",
            "7adFPyP.jpg",
            "cwKt38a.jpg",
            "wmRZfPa.jpeg",
            "rrWCuNH.jpeg",
            "a7MmRRz.jpeg",
            "W6yuVkT.jpeg",
            "A4MOw11.jpeg",
            "GAm3JXJ.jpeg",
            "07B9mxf.jpeg",
            "PRodftE.jpeg",
            "bIXDsHX.jpeg",
            "fTkIJyC.jpeg",
            "tynIAlF.jpeg",
            "44jRW3P.jpeg",
            "77DKsab.jpeg",
            "SqSWzJ0.jpeg",
            "3Wo8tJJ.jpeg",
            "6DQ4P4A.jpeg",
            "94xvnCD.jpeg",
            "kdgUno4.jpeg",
            "HlG8ipL.jpeg",
            "HLKlzdv.jpeg",
            "0UMwb5A.jpeg",
            "GnG24Uj.jpeg",
            "sN6TW0B.jpeg",
            "prsJCPj.jpeg",
            "q85HTeU.jpeg",
            "nFi7w93.jpeg",
            "BJVMNLB.png",
            "0VdKsjt.png",
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

                        Logging.KeyMessage("downloaded image was too small; skipping");
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
            // Abort due to imgur changes.
            return;

            // Don't do anything if the list is already populated.
            if (RandomImages.Count > 0)
            {
                return;
            }

            Logging.Message("populating random image list");

            try
            {
                // Add certificate validation callback, because Mono has no certificates by default and https will automatically fail if we don't do this.
                ServicePointManager.ServerCertificateValidationCallback += CertificateValidationFudge;

                // Get the top images on /r/CitiesSkylines on imgur.
                WebClient webClient = new WebClient();
                string imgurFeed = webClient.DownloadString("http://imgur.com/t/CitiesSkylines/hit?scrolled");

                Logging.Message("downloaded ", imgurFeed.Length);

                // This is what Regex was made for.
                MatchCollection albumCandidates = Regex.Matches(imgurFeed, @"<a class=\""image-list-link\"" href=\""(.*)\"" data-page=\""0\"">");

                // Look at each matched album.
                foreach (Match album in albumCandidates)
                {
                    int index = album.Value.IndexOf("href=\"");
                    string albumLink = "http://imgur.com" + album.Value.Substring(index + 6, 25);
                    Logging.Message("downloading album ", albumLink);
                    string albumFeed = webClient.DownloadString(albumLink);
                    Logging.Message("downloaded ", albumFeed.Length);

                    MatchCollection imageCandidates = Regex.Matches(albumFeed, @"<meta property=\""og:image\"" data-react-helmet=\""true\"" content=\""https://i.imgur.com/.*\.jpeg");
                    foreach (Match match in imageCandidates)
                    {
                        // Extract image name and add to list.
                        string matchValue = match.Value;
                        index = matchValue.IndexOf("https://i.imgur.com/");
                        Logging.Message("found image ", matchValue.Substring(index + 20, 7));
                        RandomImages.Add(matchValue.Substring(index + 20, 7));
                    }
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