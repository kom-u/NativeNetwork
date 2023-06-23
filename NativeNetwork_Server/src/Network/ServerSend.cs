using System;
using System.Collections.Generic;
using System.Text;



namespace Server {
    class ServerSend {
        private static void SendTCPData(int _toClient, Packet _packet) {
            _packet.WriteLength();
            Server.clientDictionary[_toClient].tcp?.SendData(_packet);
        }



        private static void SendTCPDataToAll(Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++) {
                Server.clientDictionary[i].tcp?.SendData(_packet);
            }
        }



        private static void SendTCPDataToAll(int _exceptClient, Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++) {
                if (i == _exceptClient)
                    continue;
                Server.clientDictionary[i].tcp?.SendData(_packet);
            }
        }



        private static void SendUDPData(int _toClient, Packet _packet) {
            _packet.WriteLength();
            Server.clientDictionary[_toClient].udp?.SendData(_packet);
        }



        private static void SendUDPDataToAll(Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++) {
                Server.clientDictionary[i].udp?.SendData(_packet);
            }
        }



        private static void SendUDPDataToAll(int _exceptClient, Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i < Server.MaxPlayers; i++) {
                if (i == _exceptClient)
                    continue;
                Server.clientDictionary[i].udp?.SendData(_packet);
            }
        }



        public static void Welcome(int _toClient, string _message) {
            using (Packet _packet = new Packet((int)ServerPackets.welcome)) {
                _packet.Write(_message);
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }



        public static void UDPTest(int _toClient) {
            using (Packet _packet = new Packet((int)ServerPackets.udpTest)) {
                _packet.Write("A test packet for UDP.");

                SendUDPData(_toClient, _packet);
            }
        }
    }
}