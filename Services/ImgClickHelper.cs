﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2016
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections.Specialized;
using System.Threading;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.FileSystem;
using PermissionsNotMetException = DotNetNuke.Services.FileSystem.PermissionsNotMetException;

#endregion

namespace Satrabel.OpenImageProcessor.Services
{

    public static class ImgClickHelper
    {
        public static string Key() => "imgclick";

        public static bool IsValidRequest(HttpContext context, string path)
        {
            var id = path.Substring(Key().Length + 1);
            var filename = GetFileNameFromPath(context, id.Replace("/", "\\"));
            return !string.IsNullOrEmpty(filename);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// This handler handles requests for LinkClick.aspx, but only those specifc
        /// to file serving
        /// </summary>
        /// <param name="context">System.Web.HttpContext)</param>
        /// <param name="id"></param>
        /// <remarks>
        /// </remarks>
        /// -----------------------------------------------------------------------------
        public static string GetFileNameFromPath(HttpContext context, string id)
        {
            string retval = string.Empty;

            var fileId = GetFileIdFromFilename(id);
            if (fileId > 0)
            {
                retval = GetFileNameIfAllowed(context, fileId);
            }
            else
            {
                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException($"fileid not found for Id {id}");
            }
            return retval;
        }

        private static int GetFileIdFromFilename(string id)
        {
            var piece = id.Split('\\');

            if (piece.Length != 2) throw new ArgumentOutOfRangeException(nameof(id));
            if (!piece[1].EndsWith(".axd")) throw new ArgumentOutOfRangeException(nameof(id));

            int portalid;
            int.TryParse(piece[0], out portalid);
            var hash = piece[1].Substring(0, piece[1].Length - 4);

            var coll = new NameValueCollection { { "fileticket", hash }, { "portalid", portalid.ToString() } };
            return FileLinkClickController.Instance.GetFileIdFromLinkClick(coll);
        }

        private static string GetFileNameIfAllowed(HttpContext context, int fileId)
        {
            string retval = string.Empty;
            try
            {
                var url = "FileID=" + fileId;
                var file = FileManager.Instance.GetFile(int.Parse(UrlUtils.GetParameterValue(url)));
                if (file != null && file.IsImageFile())
                {
                    if (!file.IsEnabled /*|| !HasAPublishedVersion(file)*/)
                    {
                        if (context.Request.IsAuthenticated)
                        {
                            context.Response.Redirect(Globals.AccessDeniedURL(DotNetNuke.Services.Localization.Localization.GetString("FileAccess.Error")), true);
                        }
                        else
                        {
                            context.Response.Redirect(Globals.AccessDeniedURL(), true);
                        }
                    }
                    else
                    {
                        var folder = FolderManager.Instance.GetFolder(file.FolderId);
                        if (FolderPermissionController.Instance.CanViewFolder(folder))
                        {
                            retval = file.PhysicalPath + ".resources";
                        }
                        else
                        {
                            //UserInfo objUserInfo = UserController.Instance.GetCurrentUserInfo();
                            //PortalSettings settings = PortalController.Instance.GetCurrentPortalSettings();

                            //todo uncomment this line when you are able to get current user and current portalsettings
                            //throw new PermissionsNotMetException("You do not have permission to view this file.");
                            //todo or better, let retval = the path to a thumbnail file (not allowed) or a file specified by the settings
                            retval = file.PhysicalPath + ".resources";
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException($"File not found for fileid {fileId}");
            }
            return retval;
        }

        public static bool IsImageFile(this IFileInfo file)
        {
            return (Globals.glbImageFileTypes + ",").IndexOf(file.Extension.ToLower().Replace(".", "") + ",") > -1;
        }
    }
}