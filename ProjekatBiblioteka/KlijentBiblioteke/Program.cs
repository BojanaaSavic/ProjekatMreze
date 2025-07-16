using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ServerBiblioteke;

namespace KlijentBiblioteke
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();


            Socket Pristupna = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket Info = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[1024];

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
            IPEndPoint serverEP = new IPEndPoint(selectedAddress, 50001);
            Console.WriteLine($"Naziv racunara je: {hostName}");
            Console.WriteLine($"Klijent šalje poruke serveru na: {serverEP}");

            Console.WriteLine("Klijent je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            Pristupna.Connect(serverEP);
            Info.Connect(serverEP);
            Console.WriteLine("Klijent je uspesno povezan sa serverom!");

            while (true)
            {
                SwitchMetoda(Info, Pristupna, serverEP, buffer, binaryFormatter);
               // Console.WriteLine("Da li zelite kraj programa? DA/NE");
                if (Console.ReadLine().ToLower() == "DA")
                    break;
            }

            Info.Close();
            Pristupna.Close();
            Console.WriteLine("Klijent završio sa radom.");
            Console.ReadKey();
        }

        static void SwitchMetoda(Socket Info, Socket Pristupna, IPEndPoint serverEP, byte[] buffer, BinaryFormatter binaryFormatter)
        {
            Console.WriteLine("Izaberite zeljenu opciju.\n 1.Provera stanja knjige\n 2.Podizanje knjige\n ");
            int h = int.Parse(Console.ReadLine() ?? "");

            switch (h)
            {
                case 1:
                    while (true)
                    {
                        Console.Write("Unesite naziv knjige za server (ili 'kraj' za izlaz): ");
                        string message = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(message)) continue;

                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        Info.SendTo(messageBytes, serverEP);

                        if (message.ToLower() == "kraj") break;

                        EndPoint serverResponseEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        int receivedBytes = Info.ReceiveFrom(buffer, ref serverResponseEndPoint);

                        string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                        Console.WriteLine($"{response}");

                    }
                    Console.WriteLine("\n Pritisnite 'enter' za meni.");
                    break;
                case 2:
                    while (true)
                    {
                        Console.WriteLine("Unesite naziv: ");
                        string naziv = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(naziv)) continue;

                        Console.WriteLine("Unesite autora: ");
                        string autor = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(autor)) continue;

                        Console.WriteLine("Unesite kolicinu: ");
                        int kolicina = Convert.ToInt32(Console.ReadLine());


                        Knjiga knjiga = new Knjiga(naziv, autor, kolicina);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            binaryFormatter.Serialize(ms, knjiga);
                            byte[] data = ms.ToArray();
                            Pristupna.Send(data);
                        }

                        Console.WriteLine("Unesite 'kraj' za izlaz");
                        string message = Console.ReadLine();
                        if (message.ToLower() == "kraj") break;


                        EndPoint serverResponseEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        int receivedBytes = Pristupna.ReceiveFrom(buffer, ref serverResponseEndPoint);

                        string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                        Console.WriteLine($"Odgovor od servera: {response}");
                    }
                    Console.WriteLine("\n Pritisnite 'enter' za meni.");
                    break;

                default:
                    Console.WriteLine("Pogresno unesen broj");
                    break;

            }
        }
    }
}

