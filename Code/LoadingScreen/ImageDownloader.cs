// <copyright file="ImageDownloader.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Security;
    using AlgernonCommons;

    /// <summary>
    /// Asynchronous downloading of imgur images.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal cross-thread field")]
    internal static class ImageDownloader
    {
        /// <summary>
        /// Image download lock.
        /// </summary>
        internal static readonly object Lock = new object();

        /// <summary>
        /// Indicates whether a download is ready.
        /// </summary>
        internal static bool DownloadReady = false;

        /// <summary>
        /// Simple Mono https/ssl certificate validation override to ensure that all certificates are accepted,
        /// because Mono has no certificates by default and https will automatically fail if we don't do this.
        /// We don't do anything more sophisticated because we really don't care about site validation for this.
        /// You could argue that even checking the common name is overkill here, but we'll do it anyway.
        /// </summary>
        private static readonly RemoteCertificateValidationCallback CertificateValidationFudge = (sender, cert, chain, sslPolicyErrors) => cert.Subject.Contains("CN=*.imgur.com");

        // Downloaded image data.
        private static byte[] s_imageData;

        /// <summary>
        /// Download task delegate.
        /// </summary>
        /// <param name="imageList">Image list to use.</param>
        private delegate void AsyncImageDownload(List<string> imageList);

        /// <summary>
        /// Gets the downloaded raw image data byte array.
        /// </summary>
        internal static byte[] ImageData => s_imageData;

        /// <summary>
        /// Attempts to asynchronously download an image selecteed randomly from the provided list of imgur images.
        /// </summary>
        /// <param name="imageList">Image list to use.</param>
        internal static void StartImgurImageDownload(List<string> imageList)
        {
            Logging.Message("starting asynchronous image download");
            AsyncImageDownload imageDownload = new AsyncImageDownload(DownloadImgurImage);
            imageDownload.BeginInvoke(imageList, new AsyncCallback(DownloadComplete), imageDownload);
        }

        /// <summary>
        /// Called on completion of the download.
        /// </summary>
        /// <param name="result">Asynchronous result.</param>
        private static void DownloadComplete(IAsyncResult result)
        {
        }

        /// <summary>
        /// Attempts to replace the given material with one based on a imgur image download selected randomly from the provided list.
        /// </summary>
        /// <param name="imageList">Image list to use.</param>
        private static void DownloadImgurImage(List<string> imageList)
        {
            lock (Lock)
            {
                // Randomise list order.
                Random random = new Random();

                // Timeout counter.
                int attemptCount = 0;

                try
                {
                    // Add certificate validation callback, because Mono has no certificates by default and https will automatically fail if we don't do this.
                    ServicePointManager.ServerCertificateValidationCallback += CertificateValidationFudge;
                    WebClient webClient = new WebClient();

                    while (true)
                    {
                        // Don't keep going beyond one attempt for each item on list.
                        if (++attemptCount >= imageList.Count)
                        {
                            Logging.Message(attemptCount, " download attempts exceeds list count of ", imageList.Count, "; aborting");
                            return;
                        }

                        // Recreate direct image URL from image name.
                        string imageURL = "http://i.imgur.com/" + imageList[random.Next(imageList.Count)];
                        try
                        {
                            // Download image data.
                            s_imageData = webClient.DownloadData(imageURL);
                            Logging.KeyMessage("downloaded background image ", imageURL);
                            DownloadReady = true;
                            return;
                        }
                        catch (Exception e)
                        {
                            // Don't let a single exception stop us.
                            Logging.LogException(e, "exception downloading image ", imageURL ?? "null");
                        }
                    }
                }
                finally
                {
                    // Remove certificate validation callback (don't leave this hanging around).
                    ServicePointManager.ServerCertificateValidationCallback -= CertificateValidationFudge;
                }
            }
        }
    }
}