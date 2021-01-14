using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Evolution_Backend.Controllers
{
    [ApiController]
    [Route("api")]
    public class GeneralController : ControllerBase
    {
        [HttpGet]
        [Route("info")]
        public ActionResult<object> Info()
        {
            var assemblyVersion = Assembly.GetEntryAssembly().GetName().Version;
            var version = new Version(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);

            var _info = new Dictionary<string, string>();
            _info.Add("Product", "Evolution Backend API");
            _info.Add("Current Version", "v" + version.ToString());
            _info.Add("Powered By", "Delfi Technologies A/S");

            var _logs = new List<VersionChanges>();

            var versionChanges = new VersionChanges("v1.0.0");
            versionChanges.Issues.Add("#commit sources");
            _logs.Add(versionChanges);

            return new { info = _info, logs = _logs };
        }

        class VersionChanges
        {
            public VersionChanges(string version)
            {
                Version = version;
                Issues = new List<string>();
            }
            public string Version { get; set; }
            public List<string> Issues { get; set; }
        }
    }
}
