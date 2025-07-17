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
        private static int brojPokusaja;

        static void Main(string[] args)
        {

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            List<Knjiga> knjige = new List<Knjiga>();
            List<Guid> idovi = new List<Guid>();
            Knjiga k = new Knjiga("", "", 0);
            int pozajmljeno = 0;
            byte[] buffer = new byte[1024];
            Socket PristupSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket InfoSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 5000);
            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);

            PristupSocket.Bind(new IPEndPoint(IPAddress.Any, 5000));
            InfoSocket.Bind(new IPEndPoint(IPAddress.Any, 5000));
            InfoSocket.Blocking = false;


            PristupSocket.Listen(4000);
            //InfoSocket.Listen(10);
            //InfoSocket.Blocking = false;


            Console.WriteLine($"Server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEP}");

            Socket pristupAccepted = PristupSocket.Accept();
            Console.WriteLine($"Povezao se klijent! Adresa: {pristupAccepted.RemoteEndPoint}");
            pristupAccepted.Blocking = false; 


            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            //IPAddress selectedAddress = null;
            EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
           

            /*foreach (var address in addresses)
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
            }*/

            Console.WriteLine($"Naziv racunara je: {hostName}\n");
            Console.WriteLine($"Server pokrenut na {PristupSocket.LocalEndPoint} i na {InfoSocket.LocalEndPoint}\n ");

            Console.WriteLine($"IP Adresa TCP: {serverEP.Address.ToString()}");
            Console.WriteLine($"Port je: {serverEP.Port}");


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

            Console.WriteLine("Izaberite zeljenu opciju.\n 1.Unos nove knjige\n 2.Provera stanja knjige\n 3.Podizanje knjige\n ");

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
                       

                        if (InfoSocket.Poll(1000 * 1000, SelectMode.SelectRead))
                        {

                            int receivedBytes = InfoSocket.ReceiveFrom(buffer, ref clientEndPoint);
                            string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                            Console.WriteLine($"Poruka od {clientEndPoint}: {message}");

                            Guid id = Guid.NewGuid();
                            idovi.Add(id);


                            if (message.ToLower() == "kraj") break;
                            string odgovor;
                            byte[] odgB;

                            foreach (Knjiga n in knjige)
                            {
                                if ((n.Naziv == message) && (n.Kolicina > 0))
                                {
                                    odgovor = $"Trazena knjiga: {n.ToString()}";
                                    odgB = Encoding.UTF8.GetBytes(odgovor);
                                    InfoSocket.SendTo(odgB, clientEndPoint);
                                }
                                else
                                {
                                    odgovor = "Ne postoji trazena knjiga";
                                    odgB = Encoding.UTF8.GetBytes(odgovor);
                                    InfoSocket.SendTo(odgB, clientEndPoint);
                                }
                            }
                            brojPokusaja = 0;
                        }
                        else
                        {

                            Console.WriteLine($"Pokusaj {++brojPokusaja}: nije primljena poruka...\n");
                            if (brojPokusaja == 30)
                            {
                                brojPokusaja = 0;
                                break;
                            }
                        }

                        /*string response = $"Server odgovor: {odgB}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        InfoSocket.SendTo(responseBytes, clientEndPoint);*/
                    }
                    Console.WriteLine("\n Pritisnite 'enter' za meni.");
                    break;
                case 3:

                    while (true)
                    {
                        /*if (PristupSocket.IsBound)
                            Console.WriteLine("TCP povezan");
                        
                        //int receiveBytes = PristupSocket.Receive(buffer);
                        int receivedBytes = PristupSocket.Receive(buffer);
                        string message = Encoding.UTF8.GetString(buffer);


                        Console.WriteLine($"Poruka od {clientEndPoint}: {message}");*/
                        Console.WriteLine("Cekam klijenta");

                        if (InfoSocket.Poll(1000 * 1000, SelectMode.SelectRead))
                        {
                            int receivedBytes = InfoSocket.ReceiveFrom(buffer, ref clientEndPoint);
                            string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                            //Console.WriteLine($"Poruka od {clientEndPoint}: {message}");

                            if (message.ToLower() == "kraj") break;

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
                                        //PristupSocket.Send(odgB);
                                        InfoSocket.SendTo(odgB, clientEndPoint);
                                    }
                                    else
                                    {
                                        string odgovor = "Ne postoji toliko primeraka trazene knjige";
                                        byte[] odgB = Encoding.UTF8.GetBytes(odgovor);
                                        //PristupSocket.Send(odgB);
                                        InfoSocket.SendTo(odgB, clientEndPoint);
                                    }
                                }

                            }
                        }
                        else
                        {

                            Console.WriteLine($"Pokusaj {++brojPokusaja}: nije primljena poruka...\n");
                            if (brojPokusaja == 30)
                            {
                                brojPokusaja = 0;
                                break;
                            }
                        }

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
