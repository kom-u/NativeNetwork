using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;



namespace Server {
    public class Client {
        public int id;
        public TCP? tcp;
        public UDP? udp;



        public Client(int _clientId) {
            id = _clientId;
            tcp = new TCP(id);
            udp = new UDP(id);
        }



        public class TCP {
            private readonly int id;
            public TcpClient? socket;
            private NetworkStream? stream;
            private Packet? receivedData;
            private byte[]? receiveBufferArray;

            public TCP(int _id) {
                id = _id;
            }



            public void Connect(TcpClient _socket) {
                socket = _socket;
                socket.ReceiveBufferSize = DataBuffer.bufferSize;
                socket.SendBufferSize = DataBuffer.bufferSize;

                stream = socket.GetStream();
                receivedData = new Packet();
                receiveBufferArray = new byte[DataBuffer.bufferSize];
                stream.BeginRead(receiveBufferArray, 0, DataBuffer.bufferSize, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");
            }



            private void ReceiveCallback(IAsyncResult _result) {
                try {
                    if (stream == null || receiveBufferArray == null) throw new NullReferenceException();

                    int byteLength = stream.EndRead(_result);
                    if (byteLength <= 0) {
                        // TODO: disconnect
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBufferArray, data, byteLength);

                    receivedData?.Reset(HandleData(data));
                    stream.BeginRead(receiveBufferArray, 0, DataBuffer.bufferSize, ReceiveCallback, null);
                } catch (Exception _ex) {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    // TODO: disconnect
                }
            }



            public void SendData(Packet _packet) {
                try {
                    if (socket == null)
                        throw new NullReferenceException();

                    stream?.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                } catch (Exception _ex) {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }


            private bool HandleData(byte[] _data) {
                int packetLength = 0;

                receivedData?.SetBytes(_data);

                if (receivedData?.UnreadLength() >= 4) {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                        return true;
                }

                while (packetLength > 0
                && packetLength <= receivedData?.UnreadLength()) {
                    byte[] packetByteArray = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() => {
                        using (Packet packet = new Packet(packetByteArray)) {
                            int packetId = packet.ReadInt();
                            Server.packetHandlerDictionary?[packetId](id, packet);
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
            public IPEndPoint? endPoint;
            private int id;



            public UDP(int _id) {
                id = _id;
            }



            public void Connect(IPEndPoint _endPoint) {
                endPoint = _endPoint;

                ServerSend.UDPTest(id); 
            }



            public void SendData(Packet _packet) {
                if (endPoint == null)
                    throw new NullReferenceException();
                
                Server.SendUDPData(endPoint, _packet);
            }



            public void HandleData(Packet _packet) {
                int packetLength = _packet.ReadInt();
                byte[] packetByteArray = _packet.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() => {
                    using (Packet packet = new Packet(packetByteArray)) {
                        int packetId = packet.ReadInt();
                        Server.packetHandlerDictionary?[packetId](id, packet);
                    }
                });
            }
        }
    }
}