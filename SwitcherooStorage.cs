using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Wox.Infrastructure.Storage;

namespace Wox.Plugin.Switcheroo
{
    public class SwitcherooStorage : JsonStrorage<SwitcherooStorage>
    {
        [JsonProperty]
        public bool OverrideAltTab { get; set; }
        
        protected override string ConfigFolder
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        protected override string ConfigName { get; } = "SwitcherooSettings";

        protected override SwitcherooStorage LoadDefault()
        {
            OverrideAltTab = false;
            return this;
        }
    }
}
