#region Copyright
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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Services.Localization;
using PermissionsNotMetException = DotNetNuke.Services.FileSystem.PermissionsNotMetException;

#endregion

namespace Satrabel.OpenImageProcessor
{
    public class ImgClickHandler : IHttpHandler
    {
        #region IHttpHandler Members

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// This handler handles requests for LinkClick.aspx, but only those specifc
        /// to file serving
        /// </summary>
        /// <param name="context">System.Web.HttpContext)</param>
        /// <remarks>
        /// </remarks>
        /// -----------------------------------------------------------------------------
        public void ProcessRequest(HttpContext context)
        {
            const string key = "LinkClick";

            var tabId = -1;
            var moduleId = -1;
            try
            {
                //get TabId
                if (context.Request.QueryString["tabid"] != null)
                {
                    int.TryParse(context.Request.QueryString["tabid"], out tabId);
                }

                //get ModuleId
                if (context.Request.QueryString["mid"] != null)
                {
                    int.TryParse(context.Request.QueryString["mid"], out moduleId);
                }
            }
            catch (Exception)
            {
                //The TabId or ModuleId are incorrectly formatted (potential DOS)
                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(context.Request);
            }

            var portalSettings = PortalController.Instance.GetCurrentPortalSettings();

            //get Language
            string language = portalSettings.DefaultLanguage;
            if (context.Request.QueryString["language"] != null)
            {
                language = context.Request.QueryString["language"];
            }
            else
            {
                if (context.Request.Cookies["language"] != null)
                {
                    language = context.Request.Cookies["language"].Value;
                }
            }
            if (LocaleController.Instance.IsEnabled(ref language, portalSettings.PortalId))
            {
                DotNetNuke.Services.Localization.Localization.SetThreadCultures(new CultureInfo(language), portalSettings);
                DotNetNuke.Services.Localization.Localization.SetLanguage(language);
            }

            //get the URL
            string url = "";
            bool blnForceDownload = false;
            if (context.Request.QueryString["fileticket"] != null)
            {
                url = "FileID=" + FileLinkClickController.Instance.GetFileIdFromLinkClick(context.Request.QueryString);
            }
            if (!string.IsNullOrEmpty(url))
            {
                url = url.Replace(@"\", @"/");

                //update clicks, this must be done first, because the url tracker works with unmodified urls, like tabid, fileid etc
                var objUrls = new UrlController();
                objUrls.UpdateUrlTracking(portalSettings.PortalId, url, moduleId, -1);
                TabType urlType = Globals.GetURLType(url);
                if (urlType == TabType.Tab)
                {
                    //verify whether the tab is exist, otherwise throw out 404.
                    if (TabController.Instance.GetTab(int.Parse(url), portalSettings.PortalId, false) == null)
                    {
                        DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException();
                    }
                }
                if (urlType != TabType.File)
                {
                    url = Globals.LinkClick(url, tabId, moduleId, false);
                }

                if (urlType == TabType.File && url.ToLowerInvariant().StartsWith("fileid=") == false)
                {
                    //to handle legacy scenarios before the introduction of the FileServerHandler
                    var fileName = Path.GetFileName(url);

                    var folderPath = url.Substring(0, url.LastIndexOf(fileName, StringComparison.InvariantCulture));
                    var folder = FolderManager.Instance.GetFolder(portalSettings.PortalId, folderPath);

                    var file = FileManager.Instance.GetFile(folder, fileName);

                    url = "FileID=" + file.FileId;
                }

                //get optional parameters
                if (context.Request.QueryString["clientcache"] != null)
                {
                }
                if ((context.Request.QueryString["forcedownload"] != null) || (context.Request.QueryString["contenttype"] != null))
                {
                    blnForceDownload = bool.Parse(context.Request.QueryString["forcedownload"]);
                }
                var contentDisposition = blnForceDownload ? ContentDisposition.Attachment : ContentDisposition.Inline;

                //clear the current response
                context.Response.Clear();
                var fileManager = FileManager.Instance;
                try
                {
                    switch (urlType)
                    {
                        case TabType.File:
                            var download = false;
                            var file = fileManager.GetFile(int.Parse(UrlUtils.GetParameterValue(url)));
                            if (file != null)
                            {
                                if (!file.IsEnabled || !HasAPublishedVersion(file))
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

                                try
                                {
                                    var folderMapping = FolderMappingController.Instance.GetFolderMapping(file.PortalId, file.FolderMappingID);
                                    var directUrl = fileManager.GetUrl(file);
                                    EventManager.Instance.OnFileDownloaded(new FileDownloadedEventArgs()
                                    {
                                        FileInfo = file,
                                        UserId = UserController.Instance.GetCurrentUserInfo().UserID
                                    });

                                    if (directUrl.Contains(key) || (blnForceDownload && folderMapping.FolderProviderType == "StandardFolderProvider"))
                                    {
                                        fileManager.WriteFileToResponse(file, contentDisposition);
                                        download = true;
                                    }
                                    else
                                    {
                                        context.Response.Redirect(directUrl, /*endResponse*/ true);
                                    }
                                }
                                catch (PermissionsNotMetException)
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
                                catch (ThreadAbortException) //if call fileManager.WriteFileToResponse ThreadAbortException will shown, should catch it and do nothing.
                                {

                                }
                                catch (Exception ex)
                                {
                                    //Logger.Error(ex);
                                }
                            }

                            if (!download)
                            {
                                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(url);
                            }
                            break;
                        case TabType.Url:
                            //prevent phishing by verifying that URL exists in URLs table for Portal
                            if (objUrls.GetUrl(portalSettings.PortalId, url) != null)
                            {
                                context.Response.Redirect(url, true);
                            }
                            break;
                        default:
                            //redirect to URL
                            context.Response.Redirect(url, true);
                            break;
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (Exception)
                {
                    DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(url);
                }
            }
            else
            {
                DotNetNuke.Services.Exceptions.Exceptions.ProcessHttpException(url);
            }
        }

        private bool HasAPublishedVersion(IFileInfo file)
        {
            if (file.HasBeenPublished)
            {
                return true;
            }
            //We should allow creator to see the file that is pending to be approved
            var user = UserController.Instance.GetCurrentUserInfo();
            return user != null && user.UserID == file.CreatedByUserID;
        }

        public bool IsReusable => true;

        #endregion
    }
}