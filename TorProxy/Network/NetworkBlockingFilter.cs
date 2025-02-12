using NdisApi;
using System.Net.Sockets;

namespace TorProxy.Network
{
    public class NetworkBlockingFilter : NetworkPacketInterceptor
    {

        public readonly bool BidirectionalBypass;

        public readonly FilterBypass[] Bypass;

        private readonly List<StaticFilter> _staticFilters;

        // I wonder why does it need MSTCP_FLAG_TUNNEL to work(?)
        public static readonly new MSTCP_FLAGS Mode = MSTCP_FLAGS.MSTCP_FLAG_TUNNEL;

        // Blocks all connections and allows only from:
        //
        // bypass.SourceAddress -> bypass.DestinationAddress
        // bypass.SourceAddress <- bypass.DestinationAddress    <when bidirectionalBypass = true
        //
        // ================================================
        //
        // bypassAction - action for bypassed packets, if REDIRECT, would be forwarded to HandlePacket, is PASS, would be passed withouth handling
        // globalAction - action for non-bypassed packets, if REDIRECT, would be forwarded to HandlePacket, if DROP, would be dropped
        public NetworkBlockingFilter(FilterBypass[] bypass,
            bool bidirectionalBypass = true,
            StaticFilter.FILTER_PACKET_ACTION bypassAction = StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_REDIRECT,
            StaticFilter.FILTER_PACKET_ACTION globalAction = StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_DROP) : base(Mode)
        {
            BidirectionalBypass = bidirectionalBypass;
            Bypass = bypass;

            // If we have a bidirectional bypass, amount of static filters would be doubled
            // +1 for the last blocking filter
            List<StaticFilter> filters = new((bidirectionalBypass ? bypass.Length * 2 : bypass.Length) + 1);

            IpAddressFilter ipfilter;
            StaticFilter staticFilter;
            IpNetRange sourceNetRange;
            IpNetRange destinationNetRange;
            foreach (FilterBypass bps in bypass)
            {
                sourceNetRange = new IpNetRange(IpNetRange.ADDRESS_TYPE.IP_RANGE_TYPE, bps.SourceAddress, bps.SourceAddress);
                destinationNetRange = new IpNetRange(IpNetRange.ADDRESS_TYPE.IP_RANGE_TYPE, bps.DestinationAddress, bps.DestinationAddress);

                ipfilter = new IpAddressFilter(
                    AddressFamily.InterNetwork,
                    IpAddressFilter.IP_FILTER_FIELDS.IP_FILTER_SRC_ADDRESS | IpAddressFilter.IP_FILTER_FIELDS.IP_FILTER_DEST_ADDRESS,
                    sourceNetRange,
                    destinationNetRange,
                    0
                    );

                staticFilter = new StaticFilter(
                    _adapter.Handle,
                    PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                    bypassAction,
                    StaticFilter.STATIC_FILTER_FIELDS.NETWORK_LAYER_VALID | StaticFilter.STATIC_FILTER_FIELDS.TRANSPORT_LAYER_VALID,
                    null,
                    ipfilter,
                    null
                    );

                filters.Add(staticFilter);
                if (!bidirectionalBypass) continue;

                ipfilter = new IpAddressFilter(
                    AddressFamily.InterNetwork,
                    IpAddressFilter.IP_FILTER_FIELDS.IP_FILTER_SRC_ADDRESS | IpAddressFilter.IP_FILTER_FIELDS.IP_FILTER_DEST_ADDRESS,
                    destinationNetRange,
                    sourceNetRange,
                    0
                    );


                staticFilter = new StaticFilter(
                    _adapter.Handle,
                    PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                    bypassAction,
                    StaticFilter.STATIC_FILTER_FIELDS.NETWORK_LAYER_VALID | StaticFilter.STATIC_FILTER_FIELDS.TRANSPORT_LAYER_VALID,
                    null,
                    ipfilter,
                    null
                    );
                filters.Add(staticFilter);
            }
            filters.Add(new StaticFilter(
                _adapter.Handle,
                PACKET_FLAG.PACKET_FLAG_ON_SEND_RECEIVE,
                globalAction,
                0,
                null,
                null,
                null
                ));

            _staticFilters = filters;
        }

        public override void Start()
        {
            base.Start();
            if (!_ndisapi.ResetPacketFilterTable()) throw new AggregateException("Unable to reset packet filter table");
            if (!_ndisapi.SetPacketFilterTable(_staticFilters)) throw new AggregateException("Unable to set packet filter table");
        }

        public override void Stop()
        {
            base.Stop();
            //if (!_ndisapi.ResetPacketFilterTable()) throw new AggregateException("Unable to reset packet filter table");
            //if (!_ndisapi.SetPacketFilterTable(_staticFilters)) throw new AggregateException("Unable to set packet filter table");
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            _ndisapi.ResetPacketFilterTable();
            base.Dispose();
        }
    }
}
