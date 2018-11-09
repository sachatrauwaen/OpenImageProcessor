using System;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;
using ImageProcessor.Web.Services;

namespace Satrabel.OpenImageProcessor.Services
{
    public class LocalFileImageServiceBlacklist : LocalFileImageService
    {
        public override bool IsValidRequest(string path)
        {

            if (this.Settings["BlacklistRegex"] != null)
            {
                Regex rgx = new Regex(this.Settings["BlacklistRegex"]);

                if (rgx.IsMatch(path))
                {
                    return false;
                }
            }

            return base.IsValidRequest(path);
        }
    }
}