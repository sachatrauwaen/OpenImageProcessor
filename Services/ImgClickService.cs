// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalFileImageService.cs" company="James Jackson-South">
//   Copyright (c) James Jackson-South.
//   Licensed under the Apache License, Version 2.0.
// </copyright>
// <summary>
//   The local file image service for retrieving images from the file system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using ImageProcessor.Web.Helpers;
using ImageProcessor.Web.Services;

namespace Satrabel.OpenImageProcessor.Services
{
    /// <summary>
    /// The local file image service for retrieving images from the file system.
    /// </summary>
    public class ImgClickService : IImageService
    {
        /// <summary>
        /// Gets or sets the prefix for the given implementation.
        /// <remarks>
        /// This value is used as a prefix for any image requests that should use this service.
        /// </remarks>
        /// </summary>
        public string Prefix { get; set; } = string.Empty; //"imgclick.axd";

        /// <summary>
        /// Gets a value indicating whether the image service requests files from
        /// the locally based file system.
        /// </summary>
        public bool IsFileLocalService => true;

        /// <summary>
        /// Gets or sets any additional settings required by the service.
        /// </summary>
        public Dictionary<string, string> Settings { get; set; }

        /// <summary>
        /// Gets or sets the white list of <see cref="System.Uri"/>.
        /// </summary>
        public Uri[] WhiteList { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current request passes sanitizing rules.
        /// </summary>
        /// <param name="path">
        /// The image path.
        /// </param>
        /// <returns>
        /// <c>True</c> if the request is valid; otherwise, <c>False</c>.
        /// </returns>
        public bool IsValidRequest(string path)
        {
            if (ImageHelpers.IsValidImageExtension(path)) return true;
            if (path.ToLowerInvariant().Contains(ImgClickHelper.Key()))
                return ImgClickHelper.IsValidImgClickRequest(HttpContext.Current, path);
            return false;
        }

        /// <summary>
        /// Gets the image using the given identifier.
        /// </summary>
        /// <param name="id">
        /// The value identifying the image to fetch.
        /// </param>
        /// <returns>
        /// The <see cref="System.Byte"/> array containing the image data.
        /// </returns>
        public async Task<byte[]> GetImage(object id)
        {
            string realpath;
            string path = id.ToString();
            var isLinkClick = path.ToLowerInvariant().IndexOf(ImgClickHelper.Key());

            if (isLinkClick >= 0)
            {
                var filekey = ImgClickHelper.ParseId(path.Substring(isLinkClick + ImgClickHelper.Key().Length + 1));
                realpath = ImgClickHelper.GetFileNameFromPath(HttpContext.Current, filekey);
            }
            else
            {
                realpath = path;
            }

            // Check to see if the file exists.
            if (!File.Exists(realpath))
            {
                throw new HttpException((int)HttpStatusCode.NotFound, $"No image exists at {realpath}");
            }

            byte[] buffer;
            using (FileStream file = new FileStream(realpath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                buffer = new byte[file.Length];
                await file.ReadAsync(buffer, 0, (int)file.Length);
            }

            return buffer;
        }
    }
}
