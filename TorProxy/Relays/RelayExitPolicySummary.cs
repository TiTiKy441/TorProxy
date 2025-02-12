using System.Text.Json.Serialization;

namespace TorProxy.Relays
{
    [Serializable]
    public struct RelayExitPolicySummary
    {

        [JsonPropertyName("accept")]
        public string[] Accept { get; set; }


        [JsonPropertyName("reject")]
        public string[] Reject { get; set; }

    }
}
