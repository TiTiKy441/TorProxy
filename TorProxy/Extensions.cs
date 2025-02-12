using TorProxy.Relays;

namespace TorProxy
{
    public static class Extensions
    {
        public static void Shuffle<T>(this Random rng, List<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }

        public static Relay? FirstOrNull(this IEnumerable<Relay> relays)
        {
            if (relays.Count() == 0) return null;
            return relays.First();
        }

        public static Relay[] GetRelaysWithFlags(this Relay[] relays, string[] flags)
        {
            return relays.Where((x, i) => flags.All(y => x.Flags.Contains(y))).ToArray();
        }

        public static Relay[] GetRelaysWithFlag(this Relay[] relays, string flag)
        {
            return relays.Where((x, i) => x.Flags.Contains(flag)).ToArray();
        }

        public static Relay[] GetRelaysFromCountry(this Relay[] relays, string country)
        {
            return relays.Where((x, i) => x.Country.Contains(country)).ToArray();
        }

        public static Relay? FindRelayByIp(this Relay[] relays, string ip)
        {
            return relays.Where((x, i) => x.Addresses.Contains(ip)).FirstOrNull();
        }

        public static Relay? FindRelayByFingerprint(this Relay[] relays, string fingerprint)
        {
            return relays.Where((x, i) => x.Fingerprint == fingerprint).FirstOrNull();
        }

        public static Relay[] GetRelaysWithoutFlags(this Relay[] relays, string[] flags)
        {
            return relays.Where((x, i) => !flags.Any(y => x.Flags.Contains(y))).ToArray();
        }

        public static Relay[] GetRelaysWithoutFlag(this Relay[] relays, string flag)
        {
            return relays.Where((x, i) => !x.Flags.Contains(flag)).ToArray();
        }
    }
}
