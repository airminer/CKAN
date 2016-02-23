using System;
using System.Text.RegularExpressions;
using CKAN.NetKAN.Services;
using log4net;
using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Net;

namespace CKAN.NetKAN.Sources.Curse
{
    internal sealed class CurseApi : ICurseApi
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CurseApi));

        private static readonly Uri CurseBase = new Uri("http://kerbal.curseforge.com/projects/");
        private static readonly string FilesBase = "/files/";

        private readonly IHttpService _http;

        public CurseApi(IHttpService http)
        {
            _http = http;
        }

        public CurseMod GetMod(string modId)
        {

            var response = Call(modId);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);
            var latestHref = doc.DocumentNode.SelectNodes("//*[@id=\"content\"]/section/div/div/div[2]/table/tbody/tr[1]/td[2]/div/div[2]/a")[0].Attributes[1].Value;
            var latestUrl = new Uri(CurseBase, latestHref);
            var latestFile = _http.DownloadText(latestUrl);

            //// Check if the mod has been removed from SD.
            //var error = JsonConvert.DeserializeObject<CurseError>(response);
            //
            //if (error.error)
            //{
            //    var errorMessage = string.Format("Could not get the mod from SD, reason: {0}.", error.reason);
            //    throw new Kraken(errorMessage);
            //}

            return CurseMod.FromHTML(latestUrl, latestFile);
        }

        // TODO: DBB: Make this private
        /// <summary>
        ///     Returns the route with the SpaceDock URI (not the API URI) pre-pended.
        /// </summary>
        public static Uri ExpandPath(string route)
        {
            Log.DebugFormat("Expanding {0} to full SD URL", route);

            // Alas, this isn't as simple as it may sound. For some reason
            // some—but not all—SD mods don't work the same way if the path provided
            // is escaped or un-escaped. Since our curl implementation preserves the
            // "original" string used to download a mod, we need to jump through some
            // hoops to make sure this is escaped.

            // Update: The Uri class under mono doesn't un-escape everything when
            // .ToString() is called, even though the .NET documentation says that it
            // should. Rather than using it and going through escaping hell, we'll simply
            // concat our strings together and preserve escaping that way. If SD ever
            // start returning fully qualified URLs then we should see everyting break
            // pretty quickly, and we can rejoice because we won't need any of this code
            // again. -- PJF, KSP-CKAN/CKAN#816.

            // Step 1: Escape any spaces present. SD seems to escape everything else fine.
            route = Regex.Replace(route, " ", "%20");

            // Step 2: Trim leading slashes and prepend the SD host
            var urlFixed = new Uri(CurseBase + route.TrimStart('/'));

            // Step 3: Profit!
            Log.DebugFormat("Expanded URL is {0}", urlFixed.OriginalString);
            return urlFixed;
        }

        public static Uri ResolveRedirect(string url)
        {
            Uri redirUrl = new Uri(url);
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(redirUrl);
            request.AllowAutoRedirect = false;
            request.UserAgent = Net.UserAgentString;
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();
            response.Close();
            while (response.Headers["Location"] != null)
            {
                redirUrl = new Uri(redirUrl, response.Headers["Location"]);
                request = (HttpWebRequest)WebRequest.Create(redirUrl);
                request.AllowAutoRedirect = false;
                request.UserAgent = Net.UserAgentString;
                response = (HttpWebResponse)request.GetResponse();
                response.Close();
            }
            return redirUrl;
        }

        private string Call(string path)
        {
            // TODO: There's got to be a better way than using regexps.
            // new Uri (spacedock_api, path) doesn't work, it only uses the *base* of the first arg,
            // and hence drops the /api path.

            // Remove leading and trailing slashes.
            path = Regex.Replace(path, "^/+|/+$", "");

            var url = Regex.Replace(ResolveRedirect(CurseBase + path).ToString(), "\\?.*$", "") + FilesBase;

            Log.DebugFormat("Calling {0}", url);

            return _http.DownloadText(new Uri(url));
        }
    }
}