using System;
using System.Linq;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace CKAN.NetKAN.Sources.Curse
{
    internal class CurseMod
    {
        public int id; // CID
        public string license;
        public string name;
        public string short_description;
        public string author;
        public CurseVersion[] versions;
        public Uri website;
        public Uri source_code;
        public int default_version_id;
        public Uri background;

        public CurseVersion Latest()
        {
            // The version we want is specified by `default_version_id`, it's not just
            // the latest. See GH #214. Thanks to @Starstrider42 for spotting this.

            var latest =
                from release in versions
                where release.id == default_version_id
                select release
            ;

            // There should only ever be one.
            return latest.First();
        }

        /// <summary>
        /// Returns the path to the mod's home on SpaceDock
        /// </summary>
        /// <returns>The home.</returns>
        public Uri GetPageUrl()
        {
            return CurseApi.ExpandPath(string.Format("/mod/{0}/{1}", id, name));
        }

        public override string ToString()
        {
            return string.Format("{0}", name);
        }

        public static CurseMod FromHTML(Uri latestUrl, string latestFile)
        {
            var mod = new CurseMod();
            mod.id = 0;
            mod.license = "nil";
            mod.name = "nil";
            mod.short_description = "nil";
            mod.author = "nil";
            mod.versions = new CurseVersion[1];
            mod.versions[0] = new CurseVersion();
            mod.versions[0].id = 0;
            mod.versions[0].KSP_version = new KSPVersion("any");
            mod.versions[0].changelog = "nil";
            mod.versions[0].download_path = new Uri(latestUrl.ToString() + "/download");
            mod.versions[0].friendly_version = new Version("0");
            mod.website = new Uri("http://foo.bar");
            mod.source_code = new Uri("http://foo.bar");
            mod.default_version_id = 0;
            mod.background = null;
            return mod;
        }
    }
}
