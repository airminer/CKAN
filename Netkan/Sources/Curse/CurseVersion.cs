using System;
using System.Text.RegularExpressions;
using log4net;
using Newtonsoft.Json;

namespace CKAN.NetKAN.Sources.Curse
{
    public class CurseVersion
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (CurseVersion));

        public KSPVersion KSP_version;
        public string changelog;

        public Uri download_path;

        public Version friendly_version;
        public int id;
    }
}