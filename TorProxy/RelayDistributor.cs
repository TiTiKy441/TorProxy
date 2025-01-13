using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace TorProxy
{
    public class RelayDistributor
    {

        public static RelayDistributor Instance { get; private set; }

        public RelayList RelayInfo { get; private set; }

        public List<Relay> ExitRelays { get; private set; } = new();
        public List<Relay> GuardRelays { get; private set; } = new();

        public readonly string FilePath;

        public RelayDistributor(string filePath)
        {
            FilePath = filePath;
            Instance = this;
            Read();
        }

        /**
         * Reading file in the constructor instead of getting passed a lot of information is better imo
         **/
        public void Read()
        {
            using (FileStream stream = File.OpenRead(FilePath)) RelayInfo = JsonDocument.Parse(stream).Deserialize<RelayList>();

            ExitRelays = GetRelaysWithFlags(new string[1] { "Exit", }).ToList();
            GuardRelays = GetRelaysWithFlags(new string[1] { "Guard", }).ToList();
        }

        public Relay[] GetRelaysWithFlags(string[] flags)
        {
            return RelayInfo.Relays.Where((x, i) => flags.All(y => x.Flags.Contains(y))).ToArray();
        }

        public Relay[] GetRelaysWithFlag(string flag)
        {
            return RelayInfo.Relays.Where((x, i) => x.Flags.Contains(flag)).ToArray();
        }

        public Relay[] GetRelaysFromCountries(string[] countries)
        {
            return RelayInfo.Relays.Where((x, i) => countries.All(y => x.Country.Contains(y))).ToArray();
        }

        public Relay[] GetRelaysFromCountry(string country)
        {
            return RelayInfo.Relays.Where((x, i) => x.Country.Contains(country)).ToArray();
        }

        public Relay? FindRelayByIp(string ip)
        {
            return RelayInfo.Relays.Where((x, i) => x.Addresses.Contains(ip)).FirstOrDefault();
        }
    }

    [Serializable]
    public struct RelayList
    {
        //[JsonIgnore]
        [JsonPropertyName("version")]
        public string Version { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("build_revision")]
        public string BuildRevision { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("relays_published")]
        public string RelaysPublishDate { get; set; }

        [JsonPropertyName("bridges_published")]
        public string BridgesPublishDate { get; set; }

        [JsonPropertyName("relays")]
        public Relay[] Relays { get; set; }

        [JsonPropertyName("bridges")]
        public Bridge[] Bridges { get; set; }
    }

    [Serializable]
    public struct Relay
    {
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonPropertyName("or_addresses")]
        public string[] Addresses { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("last_seen")]
        public string LastSeen { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("last_changed_address_or_port")]
        public string LastChangedAddressOrPort { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("first_seen")]
        public string FirstSeen { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("last_restarted")]
        public string LastRestarted { get; set; }

        [JsonPropertyName("running")]
        public bool Running { get; set; }

        [JsonPropertyName("flags")]
        public string[] Flags { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("country_name")]
        public string CountryName { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("as")]
        public string AS { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("as_name")]
        public string ASName { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("consensus_weight")]
        public ulong ConsesusWeight { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("bandwidth_rate")]
        public ulong BandwidthRate { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("bandwidth_burst")]
        public ulong BandwidthBurst { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("observed_bandwidth")]
        public ulong ObservedBandwidth { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("advertised_bandwidth")]
        public ulong AdvertisedBandwidth { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("exit_policy")]
        public string[] ExitPolicy { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("exit_policy_summary")]
        public RelayExitPolicySummary ExitPolicySummary { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("contact")]
        public string Contact { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("version")]
        public string Version { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("version_status")]
        public string VersionStatus { get; set; }

        [JsonIgnore]
        [JsonPropertyName("effective_family")]
        public string[] EffectiveFamily { get; set; }

        [JsonIgnore]
        [JsonPropertyName("alleged_family")]
        public string[] AllegedFamily { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("consensus_weight_fraction")]
        public double ConsesusWeightFraction { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("guard_probability")]
        public double GuardProbability { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("middle_probability")]
        public double MiddleProbability { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("exit_probability")]
        public double ExitProbability { get; set; }

        [JsonPropertyName("recommended_version")]
        public bool IsRecommendedVersion { get; set; }

        [JsonPropertyName("measured")]
        public bool Measured { get; set; }

    }

    [Serializable]
    public struct Bridge
    {
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("hashed_fingerprint")]
        public string HashedFingerprint { get; set; }

        [JsonPropertyName("or_addresses")]
        public string[] Addresses { get; set; }

        [JsonPropertyName("last_seen")]
        public string LastSeen { get; set; }

        [JsonPropertyName("first_seen")]
        public string FirstSeen { get; set; }

        [JsonPropertyName("last_restarted")]
        public string LastRestarted { get; set; }

        [JsonPropertyName("running")]
        public bool Running { get; set; }

        [JsonPropertyName("flags")]
        public string[] Flags { get; set; }

        [JsonPropertyName("advertised_bandwidth")]
        public ulong AdvertisedBandwidth { get; set; }

        [JsonPropertyName("contact")]
        public string Contact { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("version_status")]
        public string VersionStatus { get; set; }

        [JsonPropertyName("recommended_version")]
        public bool IsRecommendedVersion { get; set; }

        [JsonPropertyName("transports")]
        public string[] Transports { get; set; }

        [JsonPropertyName("bridgedb_distributor")]
        public string BridgedbDistributor { get; set; }

        [JsonPropertyName("blocklist")]
        public string[] Blocklist { get; set; }
    }

    [Serializable]
    public struct RelayExitPolicySummary
    {

        [JsonPropertyName("accept")]
        public string[] Accept { get; set; }


        [JsonPropertyName("reject")]
        public string[] Reject { get; set; }

    }
}
