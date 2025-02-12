using NdisApi;

namespace TorProxy.Network
{
    public abstract class NdisApiUser
    {

        protected readonly static NdisApiDotNet _ndisapi = new(null);


        public NdisApiUser()
        {
            if (!_ndisapi.IsDriverLoaded()) throw new InvalidOperationException("ndisapi driver is not loaded");
            if (!_ndisapi.GetTcpipBoundAdaptersInfo().Item1 || _ndisapi.GetTcpipBoundAdaptersInfo().Item2.Count == 0) throw new Exception("ndisapi was unable to query network devices");
        }


        // TODO: REDO
        // I am not sure how would this perform in real world
        public static NetworkAdapter GetActiveAdapter()
        {
            foreach (NetworkAdapter i in _ndisapi.GetTcpipBoundAdaptersInfo().Item2)
            {
                if (Utils.GetMainInterface()?.GetPhysicalAddress().ToString() == i.CurrentAddress.ToString())
                {
                    return i;
                }
            }
            return _ndisapi.GetTcpipBoundAdaptersInfo().Item2.First();
        }
    }
}
