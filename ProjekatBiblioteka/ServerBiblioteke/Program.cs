using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ServerBiblioteke
{
    public class Program
    {
        static void Main(string[] args)
        {

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            List<Knjiga> knjige = new List<Knjiga>();
            List<Guid> idovi = new List<Guid>();
            Knjiga k = new Knjiga("", "", 0);
            int pozajmljeno = 0;
            Socket PristupSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket InfoSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 50001);

            PristupSocket.Bind(serverEP);
            InfoSocket.Bind(serverEP);

            PristupSocket.Listen(5);
            //InfoSocket.Listen(5);


            Console.WriteLine($"Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEP}");

            Socket pristupAccepted = PristupSocket.Accept();
            Console.WriteLine($"Povezao se klijent! Adresa: {pristupAccepted.RemoteEndPoint}");

            //Socket InfoAccepted = InfoSocket.Accept();
            //Console.WriteLine($"Povezao se klijent! Adresa: {InfoAccepted.RemoteEndPoint}");

            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            IPAddress selectedAddress = null;
            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[1024];

            foreach (var address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork) // Koristi IPv4
                {
                    selectedAddress = address;
                    break;
                }
            }

            if (selectedAddress == null)
            {
                Console.WriteLine("IPv4 adresa nije pronađena. Proverite mrežne postavke.\n");
                return;
            }


            Console.WriteLine($"Naziv racunara je: {hostName}\n");
            Console.WriteLine($"Server pokrenut na {PristupSocket.LocalEndPoint} i na {InfoSocket.LocalEndPoint}\n ");

            Console.WriteLine($"IP Adresa TCP: {serverEP.Address.ToString()}");
            Console.WriteLine($"Port je: {serverEP.Port}");


            Console.WriteLine("Izaberite zeljenu opciju.\n 1.Unos nove knjige\n 2.Provera stanja knjige\n 3.Podizanje knjige\n ");

            //int h = int.Parse(Console.ReadLine() ?? "");

            //Napravi klasu za switch
            while (true)
            {
                SwitchMetoda(k, knjige, idovi, InfoSocket, PristupSocket, buffer, clientEndPoint, binaryFormatter, pozajmljeno);
                //Console.WriteLine("Da li zelite kraj programa? DA/NE");
                if (Console.ReadLine().ToLower() == "DA")
                    break;
            }

            InfoSocket.Close();
            PristupSocket.Close();
            Console.WriteLine("Server završio sa radom.");
            Console.ReadKey();
        }

        static void SwitchMetoda(Knjiga k, List<Knjiga> knjige, List<Guid> idovi, Socket InfoSocket, Socket PristupSocket, byte[] buffer, EndPoint clientEndPoint, BinaryFormatter binaryFormatter, int p)
        {
           int h = int.Parse(Console.ReadLine() ?? "");
            switch (h)
            {
                case 1:
                    Console.WriteLine("Unesite naziv knjige.\n");
                    k.Naziv = Console.ReadLine();
                    Console.WriteLine("Unesite autora knjige.\n");
                    k.Autor = Console.ReadLine();
                    Console.WriteLine("Unesite kolicinu knjiga.\n");
                    k.Kolicina = int.Parse(Console.ReadLine());

                    knjige.Add(k);
                    Console.WriteLine("\n Pritisnite 'enter' za meni.");
                    break;
                case 2:
                    while (true)
                    {
                        int receivedBytes = InfoSocket.ReceiveFrom(buffer, ref clientEndPoint);
                        string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                        Console.WriteLine($"Poruka od {clientEndPoint}: {message}");

                        Guid id = Guid.NewGuid();
                        idovi.Add(id);


                        if (receivedBytes == 0) break;

                        foreach (Knjiga n in knjige)
                        {
                            if ((n.Naziv == message) && (n.Kolicina > 0))
                            {
                                string odgovor = $"Trazena knjiga: {n.ToString()}";
                                byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                                InfoSocket.SendTo(odgB, clientEndPoint);
                            }
                            else
                            {
                                string odgovor = "Ne postoji trazena knjiga";
                                byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                                InfoSocket.SendTo(odgB, clientEndPoint);
                            }
                        }

                        string response = $"Server odgovor: {message}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        InfoSocket.SendTo(responseBytes, clientEndPoint);
                    }
                    Console.WriteLine("\n Pritisnite 'enter' za meni.");
                    break;
                case 3:

                    while (true)
                    {
                        int receivedBytes = PristupSocket.ReceiveFrom(buffer, ref clientEndPoint);
                        string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);


                        Console.WriteLine($"Poruka od {clientEndPoint}: {message}");



                        if (receivedBytes == 0) break;

                        using (MemoryStream ms = new MemoryStream(buffer, 0, receivedBytes))
                        {
                            Knjiga kk = (Knjiga)binaryFormatter.Deserialize(ms);
                            foreach (Knjiga n in knjige)
                            {
                                if ((kk.Naziv == n.Naziv) && (n.Kolicina > kk.Kolicina))
                                {
                                    string odgovor = $"Knjiga je uspesno pozajmljena";
                                    p++;
                                    n.Kolicina -= kk.Kolicina;
                                    byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                                    PristupSocket.SendTo(odgB, clientEndPoint);
                                }
                                else
                                {
                                    string odgovor = "Ne postoji toliko primeraka trazene knjige";
                                    byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                                    PristupSocket.SendTo(odgB, clientEndPoint);
                                }
                            }

                        }
                        string response = $"Server odgovor: {message}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        PristupSocket.SendTo(responseBytes, clientEndPoint);
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
