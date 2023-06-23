using System;
using System.Collections.Generic;
using System.Text;



namespace Server {
    class ServerHandle {
        public static void WelcomeReceived(int _fromClient, Packet _packet) {
            int clientID = _packet.ReadInt();
            string username = _packet.ReadString();

            Console.WriteLine($"{Server.clientDictionary[_fromClient].tcp?.socket?.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");

            if (_fromClient != clientID) {
                Console.WriteLine($"Player \"{username}\" (ID: {_fromClient}) has assumed the wrong client ID ({clientID})!");
            }

            // TODO: send player into game
        }



        public static void UDPTestReceived(int _fromClient, Packet _packet) {
            string message = _packet.ReadString();

            Console.WriteLine($"Received packet via UDP. Contains message: {message}");
        }
    }
}