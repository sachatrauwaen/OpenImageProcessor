using DotNetNuke.Web.Api;
using ImageProcessor.Web.HttpModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Satrabel.OpenImageProcessor
{
    public class ValidatingRequest : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            ImageProcessingModule.ValidatingRequest += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.QueryString))
                {
                    var queryCollection = HttpUtility.ParseQueryString(args.QueryString);
                    // ignore cachebuster
                    if (queryCollection.AllKeys.Contains("ver"))
                    {
                        queryCollection.Remove("ver");
                        args.QueryString = queryCollection.ToString();
                    }
                }
            }; 
        }
    }
}