using MREZENAJNOVIJIMOGUCI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class ServerB
    {
        static void Main(string[] args)
        {
            ServerB s = new ServerB();
            s.Pokreni();
        }

        private TcpListener tcpListener;
        private UdpClient udpClient;
        private List<Knjiga> knjige = new List<Knjiga>();
        private List<Iznajmljivanje> iznajmljivanja = new List<Iznajmljivanje>();
        private Dictionary<int, TcpClient> aktivniKlijenti = new Dictionary<int, TcpClient>();
        private int idGenerator = 10000;

        public void Pokreni()
        {
            int tcpPort = 5000;
            int udpPort = 5001;
            tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            udpClient = new UdpClient(udpPort);

            tcpListener.Start();
            Console.WriteLine($"[PRISTUPNA TCP]: {((IPEndPoint)tcpListener.LocalEndpoint).Address}:{tcpPort}");
            Console.WriteLine($"[INFO UDP]: {((IPEndPoint)udpClient.Client.LocalEndPoint).Address}:{udpPort}");

            // Inicijalne knjige
            knjige.Add(new Knjiga("Na Drini ćuprija", "Ivo Andrić", 5));
            knjige.Add(new Knjiga("Prokleta avlija", "Ivo Andrić", 3));

            bool dodaj = false;

            Console.WriteLine("Da li zelite da unesete novu knjigu: DA/NE ?");
            if (Console.ReadLine() == "DA")
            {
                dodaj = true;
                while(dodaj == true)
                {
                    Knjiga k = new Knjiga();
                    Console.WriteLine("Unesite naziv knjige.\n");
                    k.Naslov = Console.ReadLine();
                    Console.WriteLine("Unesite autora knjige.\n");
                    k.Autor = Console.ReadLine();
                    Console.WriteLine("Unesite kolicinu knjiga.\n");
                    k.Kolicina = int.Parse(Console.ReadLine());

                    knjige.Add(k);
                    
                }
                
            }

            Thread tcpThread = new Thread(ObradiTCP);
            tcpThread.Start();

            Thread udpThread = new Thread(ObradiUDP);
            udpThread.Start();

            Console.WriteLine("\n[Nevracene knjige sa prethodnog rada:]");
            foreach (var iznaj in iznajmljivanja)
            {
                if (DateTime.Now > iznaj.DatumVracanja)
                {
                    Console.WriteLine($"Clan {iznaj.ClanID} kasni sa: {iznaj.KnjigaI} ({iznaj.BrojPrimeraka}) od {iznaj.DatumVracanja.ToShortDateString()}");
                }
            }

        }

        private void ObradiTCP()
        {
            while (true)
            {
                TcpClient klijent = tcpListener.AcceptTcpClient();
                int id = idGenerator++;
                aktivniKlijenti[id] = klijent;

                Thread klijentNit = new Thread(() => ObradiKlijentaTCP(klijent, id));
                klijentNit.Start();
            }
        }

        private void ObradiKlijentaTCP(TcpClient klijent, int id)
        {
            NetworkStream stream = klijent.GetStream();
            byte[] buffer = Encoding.UTF8.GetBytes($"Prijavljen. Tvoj ID je: {id}");
            stream.Write(buffer, 0, buffer.Length);

            // TODO: dalje obrada iznajmljivanja, vraćanja itd.
            while (true)
            {
                buffer = new byte[256];
                int br = stream.Read(buffer, 0, buffer.Length);
                string poruka = Encoding.UTF8.GetString(buffer, 0, br);

                if (poruka.StartsWith("IZNAJMI"))
                {
                    // ... (postojeća iznajmljivanje logika)
                }
                else if (poruka.StartsWith("VRATI"))
                {
                    string[] delovi = poruka.Split(';');
                    id = int.Parse(delovi[1]);
                    string naslov = delovi[2];
                    string autor = delovi[3];
                    int broj = int.Parse(delovi[4]);

                    string kljuc = $"{naslov} - {autor}";

                    bool nadjeno = false;

                    // Povećaj količinu u knjigama
                    foreach (var knjiga in knjige)
                    {
                        if (knjiga.Naslov == naslov && knjiga.Autor == autor)
                        {
                            knjiga.Kolicina += broj;
                            nadjeno = true;
                            break;
                        }
                    }

                    if (!nadjeno)
                    {
                        stream.Write(Encoding.UTF8.GetBytes("Greska: Knjiga ne postoji."), 0, "Greska: Knjiga ne postoji.".Length);
                        continue;
                    }


                    // Ukloni iz iznajmljivanja
                    iznajmljivanja.RemoveAll(i => i.ClanID == id && (i.KnjigaI.Naslov + i.KnjigaI.Autor) == kljuc);

                    string potvrda = "Uspesno vraceno.";
                    stream.Write(Encoding.UTF8.GetBytes(potvrda), 0, potvrda.Length);
                }
            }

        }

        private void ObradiUDP()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                byte[] data = udpClient.Receive(ref ep);
                string poruka = Encoding.UTF8.GetString(data);
                string odgovor = ProveriKnjigu(poruka.Trim());
                byte[] odgData = Encoding.UTF8.GetBytes(odgovor);
                udpClient.Send(odgData, odgData.Length, ep);
            }
        }

        private string ProveriKnjigu(string upit)
        {
            foreach (var k in knjige)
            {
                if (upit.Contains(k.Naslov) && upit.Contains(k.Autor))
                {
                    if (k.Kolicina > 0)
                        return $"DA - Dostupno: {k.Kolicina}";
                    else
                        return "NE - Nema dostupnih primeraka";
                }
            }
            return "NE - Knjiga nije pronađena";
        }
    }
}
