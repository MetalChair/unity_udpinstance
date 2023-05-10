using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Networker
{
    public abstract class UdpInstance
    {
        //The current deltaTime which will be sent in packets to sync time
        public int CurrentTick = 0;

        internal bool _running = true;


        /// <summary>
        /// The underlying UdpClient Instance used to send data
        /// </summary>
        private Socket _outgoingSocket =
            new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        /// <summary>
        /// The underlying UdpClient Instance used to send data
        /// </summary>
        private Socket _incomingSocket =
            new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        private IPEndPoint _outgoingSocketIpEndpoint =
            new IPEndPoint(IPAddress.None, 3000);
        private IPEndPoint _incomingSocketIpEndpoint =
            new IPEndPoint(IPAddress.Any, 3001);

        internal bool _isServer = false;

        internal BlockingCollection<Tuple<byte[], IPEndPoint>> _outgoingPacketBuffer =
            new BlockingCollection<Tuple<byte[], IPEndPoint>>();
        internal BlockingCollection<Tuple<byte[], IPEndPoint>> _incomingPacketBuffer =
            new BlockingCollection<Tuple<byte[], IPEndPoint>>();

        internal Task _mainRunnerTask;
        internal Task _outgoingPacketTask;
        internal Task _incomingPacketTask;


        public UdpInstance(int incPort, int outPort)
        {
            SetupIPEndpoints(incPort, outPort);
            BindSockets();
            SetupTasks();
        }

        public void Start(){
            Debug.Log("Starting server");
            _running = true;
            _outgoingPacketTask.Start();
            _incomingPacketTask.Start();
            _mainRunnerTask.Start();

        }

        /// <summary>
        /// Sets up the tasks this instance uses to receive/send data.
        /// </summary>
        private void SetupTasks(){
            _outgoingPacketTask = new Task(() => PacketSender());
            _incomingPacketTask = new Task(() => RecievePackets());
            _mainRunnerTask = new Task(() => MainRunnerThread());
        }

        /// <summary>
        /// Send a uid
        /// </summary>
        /// <param name="dest"></param>
        // public abstract void SendReliable(IPEndPoint dest);

        private void BindSockets()
        {
            _incomingSocket.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, true);
            _incomingSocket.Bind(_incomingSocketIpEndpoint);

            _outgoingSocket.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, true);
            _outgoingSocket.Bind(_outgoingSocketIpEndpoint);
        }

        /// <summary>
        /// Function that is called on the receipt of packets by the UdpInsance.
        /// Returns a byte array representing the raw packet data and the
        /// corresponding endpoint it was received from.
        /// </summary>
        public abstract void OnDataReceive(Byte[] pack, IPEndPoint src);
        internal void MainRunnerThread()
        {
            while(_running){
                Debug.Log("Starting main handler thread");
                foreach (var packet in _incomingPacketBuffer.GetConsumingEnumerable())
                {
                    OnDataReceive(packet.Item1, packet.Item2);
                }
            }
            Debug.Log("Exiting main thread");
        }


        private void SetupIPEndpoints(int incPort, int outPort)
        {
            _incomingSocketIpEndpoint = new IPEndPoint(IPAddress.Any, incPort);
            _outgoingSocketIpEndpoint = new IPEndPoint(IPAddress.Any, outPort);
        }

        private async Task PacketSender()
        {
            if (_outgoingPacketBuffer == null || _outgoingSocket == null)
            {
                Debug.LogError("Tried to start a packet sender with null queue/socket");
                throw new Exception();
            }
            while (_running)
            {
                foreach (var epPackTuple in _outgoingPacketBuffer.GetConsumingEnumerable())
                {
                    await _outgoingSocket.SendToAsync(
                        epPackTuple.Item1,
                        SocketFlags.None,
                        epPackTuple.Item2
                    );
                    AfterPacketSend(epPackTuple.Item1);
                }
            }

            Debug.Log("Exiting packet sender loop");

        }

        internal virtual void AfterPacketSend(byte[] data){
            
        }

        private void RecievePackets()
        {
            if (_incomingSocket == null || _incomingPacketBuffer == null)
            {
                Debug.LogError("Tried to start a packet reciever with null queue/socket");
                throw new Exception();
            }
            byte[] buffer = new byte[2048];
            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            _incomingSocket.BeginReceiveFrom(
                buffer,
                0,
                buffer.Length,
                SocketFlags.None,
                ref remoteEp,
                RecieveCallback,
                buffer
            );
        }

        private void RecieveCallback(IAsyncResult ar)
        {
            if (_incomingSocket == null || _incomingPacketBuffer == null)
            {
                Debug.Log("Tried to receive packets with a null UDP client");
                return;
            }

            try
            {
                // End the asynchronous receive operation and get the number of bytes received
                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                int bytesRead = _incomingSocket.EndReceiveFrom(ar, ref remoteEp);

                // Create a buffer to hold the received data
                byte[] buffer = (byte[])ar.AsyncState;

                if (bytesRead > 0 && bytesRead <= buffer.Length)
                {
                    var asIp = (IPEndPoint)remoteEp;
                    //Enqueue received packet
                    _incomingPacketBuffer.Add(new Tuple<byte[], IPEndPoint>(
                        buffer.Take(bytesRead).ToArray(),
                        new IPEndPoint(asIp.Address, asIp.Port)
                    ));
                }
                if(_running){
                    // Begin receiving the next packet asynchronously
                    _incomingSocket.BeginReceiveFrom(
                        buffer,
                        0,
                        buffer.Length,
                        SocketFlags.None,
                        ref remoteEp,
                        RecieveCallback,
                        buffer
                    );
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket has been closed, ignore
            }
            catch (SocketException ex)
            {
                Debug.Log("Socket exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Send a packet using the udp instance
        /// </summary>
        public void Send(byte[] data, IPEndPoint target)
        {
            Debug.Log("Sending packet to " + target.Port + " " + target.Address);
            _outgoingPacketBuffer.Add(new Tuple<byte[], IPEndPoint>(data, target));
        }
        
        public void Tick(){
            CurrentTick += 1;
        }

        public void Stop()
        {
            Debug.Log("Stopping UdpInstance");
            _running = false;
            _incomingPacketBuffer.Dispose();
            _outgoingPacketBuffer.Dispose();
            _outgoingSocket.Dispose();
            _incomingSocket.Dispose();
        }

    }
}