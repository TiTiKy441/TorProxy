using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TorProxy.Network.ProxiFyre
{
    [Serializable]
    public struct ProxiFyreConfig
    {

        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; }

        [JsonPropertyName("proxies")]
        public ProxiFyreProxy[] Proxies { get; set; }

    }
}
