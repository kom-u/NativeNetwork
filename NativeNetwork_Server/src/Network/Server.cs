using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;



namespace Server {
    public class Server {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        private static TcpListener? tcpListener;
        private static UdpClient? udpListener;

        public static Dictionary<int, Client> clientDictionary = new Dictionary<int, Client>();

        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler>? packetHandlerDictionary;



        private static void InitializeServerData() {
            for (int i = 1; i <= MaxPlayers; i++) {
                clientDictionary.Add(i, new Client(i));
            }

            packetHandlerDictionary = new Dictionary<int, PacketHandler>() {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.udpTestReceived, ServerHandle.UDPTestReceived },
            };

            Console.WriteLine("Initialized packets.");
        }



        public static void Start(int _maxPlayer, int _port) {
            MaxPlayers = _maxPlayer;
            Port = _port;

            Console.WriteLine("Starting server...");
            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on {Port}.");
        }



        private static void TCPConnectCallback(IAsyncResult _result) {
            if (tcpListener == null)
                throw new NullReferenceException();

            TcpClient client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayers; i++) {
                if (clientDictionary[i]?.tcp?.socket == null) {
                    clientDictionary[i]?.tcp?.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server Full");
        }



        private static void UDPReceiveCallback(IAsyncResult _result) {
            try {
                if (udpListener == null) 
                    throw new NullReferenceException();

                IPEndPoint? clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(_result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (data.Length < 4) {
                    return;
                }

                using (Packet packet = new Packet(data)) {
                    int clientId = packet.ReadInt();

                    if (clientId == 0 || clientEndPoint == null)
                        return;

                    IPEndPoint? clientEndPointCheck = clientDictionary[clientId]?.udp?.endPoint;

                    if (clientEndPointCheck == null) {
                        clientDictionary[clientId]?.udp?.Connect(clientEndPoint);
                        return;
                    }

                    if (clientEndPointCheck.ToString() == clientEndPoint.ToString()) {
                        clientDictionary[clientId]?.udp?.HandleData(packet);
                    }
                }
            } catch (Exception _ex) {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }



        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet) {
            try {
                if (udpListener == null)
                    throw new NullReferenceException();

                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            } catch (Exception _ex) {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }
    }
}