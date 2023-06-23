using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;



public class Client : SingletonMonobehaviour<Client> {
    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlerDictionary;



    private void Start() {
        tcp = new TCP();
        udp = new UDP();
    }


    public void ConnectToServer() {
        InitializeClientData();
        tcp.Connect();
    }



    private void InitializeClientData() {
        packetHandlerDictionary = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.udpTest, ClientHandle.UDPTest}
        };
        Debug.Log("Initialized packets.");
    }



    public class TCP {
        public TcpClient socket;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBufferArray;



        public void Connect() {
            socket = new TcpClient {
                ReceiveBufferSize = DataBuffer.bufferSize,
                SendBufferSize = DataBuffer.bufferSize
            };

            receiveBufferArray = new byte[DataBuffer.bufferSize];
            socket.BeginConnect(Instance.ip, Instance.port, ConnectCallback, socket);
        }



        private void ConnectCallback(IAsyncResult _result) {
            socket.EndConnect(_result);

            if (!socket.Connected)
                return;

            stream = socket.GetStream();
            receivedData = new Packet();
            stream.BeginRead(receiveBufferArray, 0, DataBuffer.bufferSize, ReceiveCallback, null);
        }



        private void ReceiveCallback(IAsyncResult _result) {
            try {
                if (stream == null || receiveBufferArray == null)
                    throw new NullReferenceException();

                int byteLength = stream.EndRead(_result);
                if (byteLength <= 0) {
                    // TODO: disconnect
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBufferArray, data, byteLength);

                receivedData.Reset(HandleData(data));
                stream.BeginRead(receiveBufferArray, 0, DataBuffer.bufferSize, ReceiveCallback, null);
            } catch {
                // TODO: disconnect
            }
        }



        public void SendData(Packet _packet) {
            try {
                if (socket == null)
                    throw new NullReferenceException();

                stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
            } catch (Exception _ex) {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }



        private bool HandleData(byte[] _data) {
            int packetLength = 0;

            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4) {
                packetLength = receivedData.ReadInt();
                if (packetLength <= 0)
                    return true;
            }

            while (packetLength > 0
            && packetLength <= receivedData.UnreadLength()) {
                byte[] packetByteArray = receivedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() => {
                    using (Packet packet = new Packet(packetByteArray)) {
                        int packetId = packet.ReadInt();
                        packetHandlerDictionary[packetId](packet);
                    }
                });

                packetLength = 0;

                if (receivedData.UnreadLength() >= 4) {
                    packetLength = receivedData.ReadInt();

                    if (packetLength <= 0)
                        return true;
                }
            }


            if (packetLength <= 1)
                return true;

            return false;
        }
    }



    public class UDP {
        public UdpClient socket;
        public IPEndPoint endPoint;



        public UDP() {
            endPoint = new IPEndPoint(IPAddress.Parse(Instance.ip), Instance.port);
        }



        public void Connect(int _localPort) {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet packet = new Packet()) {
                SendData(packet);
            }
        }



        public void ReceiveCallback(IAsyncResult _result) {
            try {
                byte[] data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (data.Length < 4) {
                    // TODO: disconnect
                    return;
                }

                HandleData(data);
            } catch {
                // TODO: disconnect
            }
        }



        public void SendData(Packet _packet) {
            try {
                _packet.InsertInt(Instance.myId);
                if (socket != null) {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            } catch (Exception _ex) {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }



        private void HandleData(byte[] _data) {
            using (Packet packet = new Packet(_data)) {
                int packetLength = packet.ReadInt();
                _data = packet.ReadBytes(packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() => {
                using (Packet packet = new Packet(_data)) {
                    int packetId = packet.ReadInt();
                    packetHandlerDictionary[packetId](packet);
                }
            });
        }
    }
}
