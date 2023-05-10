using System.Collections.Generic;
using System.Net;

namespace Networker{
    public class UdpDebugInstance : UdpInstance
    {
        public int NumOutgoingPackets = 0;
        public int NumIncomingPackets = 0;

        public List<byte[]> RecentIncomingPackets = new List<byte[]>(10);
        public List<byte[]> RecentOutgoingPackets = new List<byte[]>(10);
        public UdpDebugInstance(int incPort, int outPort) : base(incPort, outPort)
        {
        }

        public override void OnDataReceive(byte[] pack, IPEndPoint src)
        {
            RecentIncomingPackets.Add(pack);
        }
        internal override void AfterPacketSend(byte[] data)
        {
            RecentOutgoingPackets.Add(data);
        }
    }
}