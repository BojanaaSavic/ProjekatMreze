using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerBiblioteke
{
    public class Program
    {
        static void Main(string[] args)
        {
            List<Knjiga> knjige = new List<Knjiga>();
            Knjiga k  = new Knjiga("", "", 0);

            Console.WriteLine("Prvi test");
            try
            {
                // Pronalazi dostupnu IPv4 adresu
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                IPAddress selectedAddress = null;
                Console.WriteLine("Drugi test");
                foreach (var address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork) // Koristi IPv4
                    {
                        selectedAddress = address;
                        break;
                    }
                }
                Console.WriteLine("Treci test");
                if (selectedAddress == null)
                {
                    Console.WriteLine("IPv4 adresa nije pronađena. Proverite mrežne postavke.\n");
                    return;
                }
                Console.WriteLine("Cetvrti test");
                // Kreira IPEndPoint za server
                IPEndPoint serverEndPoint = new IPEndPoint(selectedAddress, 55555);
                Socket INFO = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Socket PRISTUPNA = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine("Peti test");
                // Povezivanje i slušanje
                PRISTUPNA.Bind(serverEndPoint);
                Console.WriteLine($"Naziv racunara je: {hostName}\n");
                Console.WriteLine($"Server pokrenut na {PRISTUPNA.LocalEndPoint}\n");
                //treba ispisati IP adrese i portove za UDP i TCP


                Console.WriteLine("Ako zelite da unesete novu knjigu napisite 'unos'.\n");
                string request = Console.ReadLine();
                if (request.ToLower() == "unos")
                {
                    bool help = true;
                    while (help)
                    {
                        Console.WriteLine("Unesite naziv knjige.\n");
                        k.Naziv = Console.ReadLine();
                        Console.WriteLine("Unesite autora knjige.\n");
                        k.Autor = Console.ReadLine();
                        Console.WriteLine("Unesite kolicinu knjiga.\n");
                        k.Kolicina = int.Parse(Console.ReadLine());

                        knjige.Add(k);
                        Console.WriteLine("Da li zelite da unesete jos knjiga?\n DA/NE\n");
                        string procitaj = Console.ReadLine();
                        if (procitaj.ToLower() == "ne") help = false;
                    }
                }
                    
                

                // Prima i šalje poruke
                EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int receivedBytes = PRISTUPNA.ReceiveFrom(buffer, ref clientEndPoint);
                    string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    Console.WriteLine($"Poruka od {clientEndPoint}: {message}");

                    if (message.ToLower() == "kraj") break;

                    foreach(Knjiga n in knjige)
                    {
                        if ((n.Naziv == message) && (n.Kolicina > 0))
                        {
                            string odgovor = $"Trazena knjiga: {n.ToString()}";
                            byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                            PRISTUPNA.SendTo(odgB, clientEndPoint);
                        }
                        else
                        {
                            string odgovor = "Ne postoji trazena knjiga";
                            byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                            PRISTUPNA.SendTo(odgB, clientEndPoint);
                        }
                    }

                    string response = $"Server odgovor: {message}";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    PRISTUPNA.SendTo(responseBytes, clientEndPoint);
                }

                INFO.Close();
                PRISTUPNA.Close();
                Console.WriteLine("Server završio sa radom.");
                Console.ReadKey();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket greška: {ex.Message}");
            }
        }
    }
}
