using System.Net;

namespace TorProxy.Network
{
    public sealed class FilterBypass
    {

        public readonly IPAddress SourceAddress;

        public readonly IPAddress DestinationAddress;

        public FilterBypass(IPAddress sourceAddress, IPAddress destantionAddress)
        {
            SourceAddress = sourceAddress;
            DestinationAddress = destantionAddress;
        }

        public FilterBypass(string sourceAddress, string destnationAddress)
        {
            SourceAddress = IPAddress.Parse(sourceAddress);
            DestinationAddress = IPAddress.Parse(destnationAddress);
        }

        public bool IsBypass(IPAddress sourceAddr, IPAddress destinationAddr)
        {
            return SourceAddress.Equals(sourceAddr) && DestinationAddress.Equals(destinationAddr);
        }

        public bool IsBypass(string sourceAddress, string destinationAddress)
        {
            return SourceAddress.Equals(IPAddress.Parse(sourceAddress)) && DestinationAddress.Equals(IPAddress.Parse(destinationAddress));
        }
    }
}
