using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KlijentBiblioteke
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int pozajmljeno = 0;
            try
            {
                // Pronalazi dostupnu IPv4 adresu
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                IPAddress selectedAddress = null;

                foreach (var address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address)) // Koristi IPv4 lokalnu adresu
                    {
                        selectedAddress = address;
                        break;
                    }
                }

                if (selectedAddress == null)
                {
                    Console.WriteLine("IPv4 adresa nije pronađena. Proverite mrežne postavke.");
                    return;
                }

                // Klijentski soket
                Socket InfoSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket PristupSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Kreira server adresu
                IPEndPoint serverEndPoint = new IPEndPoint(selectedAddress, 55555);
                Console.WriteLine($"Naziv racunara je: {hostName}");
                Console.WriteLine($"Klijent šalje poruke serveru na: {serverEndPoint}");

                while (true)
                {
                    Console.Write("Unesite naziv knjige za server (ili 'kraj' za izlaz): ");
                    string message = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(message)) continue;

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    InfoSocket.SendTo(messageBytes, serverEndPoint);

                    if (message.ToLower() == "kraj") break;

                    byte[] buffer = new byte[1024];
                    EndPoint serverResponseEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int receivedBytes = InfoSocket.ReceiveFrom(buffer, ref serverResponseEndPoint);

                    string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    Console.WriteLine($"Odgovor od servera: {response}");
                }

                InfoSocket.Close();
                PristupSocket.Close();
                Console.WriteLine("Klijent završio sa radom.");
                Console.ReadKey();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket greška: {ex.Message}");
            }
        }
    }
}
