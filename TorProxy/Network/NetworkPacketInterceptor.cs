using NdisApi;
using PacketDotNet;

namespace TorProxy.Network
{
    public class NetworkPacketInterceptor : NdisApiUser, IDisposable, INetworkStatistics
    {

        protected bool _disposed = false;

        public readonly MSTCP_FLAGS Mode;

        protected bool _capturing = false;

        protected ManualResetEvent _packetEvent = new(false);

        protected readonly NetworkAdapter _adapter;

        protected readonly NdisBufferResource _buffer;

        protected Thread _packetHandlerThread;

        public ulong PacketsReceived { get; protected set; }

        public ulong BytesReceived { get; protected set; }

        public ulong PacketsSent { get; protected set; }

        public ulong BytesSent { get; protected set; }

        public const int BufferSize = 64;

        public NetworkPacketInterceptor(MSTCP_FLAGS mode) : base()
        {
            Mode = mode;
            _adapter = GetActiveAdapter();
            _buffer = new NdisBufferResource(BufferSize);
            _packetEvent = new ManualResetEvent(false);
            _packetHandlerThread = new Thread(FilterWork);
        }

        protected virtual void FilterWork()
        {
            Tuple<bool, List<RawPacket>> packetList;
            do
            {
                _packetEvent.WaitOne();
                packetList = _ndisapi.ReadPackets(_adapter.Handle, _buffer);

                if (!packetList.Item1 || packetList.Item2.Count == 0) continue;

                foreach (RawPacket rawPacket in packetList.Item2)
                {
                    try
                    {
                        HandlePacket(rawPacket);
                    }
                    catch (Exception)
                    {
                        //TODO: Unable to handle packet
                    }
                }
                //if (Mode == MSTCP_FLAGS.MSTCP_FLAG_FILTER_DIRECT || Mode == MSTCP_FLAGS.MSTCP_FLAG_LOOPBACK_FILTER)
                //{
                    //_ndisapi.FlushAdapterPacketQueue(_adapter.Handle);
                //}

                _packetEvent.Reset(); // I forgot this line one time
            } while (_capturing);
        }

        protected virtual void HandlePacket(RawPacket rawPacket)
        {
            if (rawPacket.DeviceFlags == PACKET_FLAG.PACKET_FLAG_ON_RECEIVE)
            {
                if (Mode == MSTCP_FLAGS.MSTCP_FLAG_TUNNEL || Mode == MSTCP_FLAGS.MSTCP_FLAG_RECV_TUNNEL)
                    _ndisapi.SendPacketToMstcp(_adapter.Handle, rawPacket);

                PacketsReceived += 1;
                BytesReceived += (ulong)rawPacket.Data.Length;
            }
            else
            {
                if (Mode == MSTCP_FLAGS.MSTCP_FLAG_TUNNEL || Mode == MSTCP_FLAGS.MSTCP_FLAG_SENT_TUNNEL)
                    _ndisapi.SendPacketToAdapter(_adapter.Handle, rawPacket);

                PacketsSent += 1;
                BytesSent += (ulong)rawPacket.Data.Length;
            }
        }

        public virtual void Start()
        {
            if (_capturing) return;
            _capturing = true;
            _ndisapi.SetPacketEvent(_adapter.Handle, _packetEvent);
            _ndisapi.SetAdapterMode(_adapter.Handle, Mode);
            CreateNewThread();
            _packetHandlerThread.Start();
        }

        public virtual void Stop()
        {
            if (!_capturing) return;
            _capturing = false;
            _packetEvent.Set();
            _packetHandlerThread.Join();
            _ndisapi.SetPacketEvent(_adapter.Handle, null);
            _ndisapi.SetAdapterMode(_adapter.Handle, 0);
        }

        protected virtual void CreateNewThread()
        {
            if (_packetHandlerThread != null && _packetHandlerThread.IsAlive)
            {
                throw new ThreadStateException("Packet handler thread is already running");
            }

            _packetHandlerThread = new Thread(FilterWork);
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            Stop();
            _buffer.Dispose();
            _packetEvent.Dispose();
            _ndisapi.SetPacketEvent(_adapter.Handle, null);
            _ndisapi.SetAdapterMode(_adapter.Handle, 0);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
