using MREZENAJNOVIJIMOGUCI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Klijent
{
    public class KlijentB
    {
        static void Main(string[] args)
        {
            KlijentB k = new KlijentB();
            k.Pokreni();
        }
        private int mojID;
        private TcpClient tcpClient;
        private UdpClient udpClient;
        private List<Iznajmljivanje> iznajmljene = new List<Iznajmljivanje>();

        private string serverIP = "127.0.0.1";
        private int tcpPort = 5000;
        private int udpPort = 5001;

        public void Pokreni()
        {
            // TCP Povezivanje
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIP, tcpPort);
            NetworkStream stream = tcpClient.GetStream();

            byte[] buffer = new byte[256];
            int br = stream.Read(buffer, 0, buffer.Length);
            string poruka = Encoding.UTF8.GetString(buffer, 0, br);
            Console.WriteLine(poruka);

            // Ekstrahuj ID
            if (poruka.Contains("ID"))
            {
                string[] delovi = poruka.Split(':');
                mojID = int.Parse(delovi[1].Trim());
            }

            // UDP priprema
            udpClient = new UdpClient();

            // Glavni meni
            while (true)
            {
                Console.WriteLine("\nMeni:");
                Console.WriteLine("1. Proveri knjigu (UDP)");
                Console.WriteLine("2. Iznajmi knjigu (TCP)");
                Console.WriteLine("3. Vrati iznajmljenu knjige");
                Console.WriteLine("4. Vidi iznajmljene knjige");
                Console.WriteLine("0. Izlaz");
                Console.Write("Izbor: ");
                string izbor = Console.ReadLine();

                switch (izbor)
                {
                    case "1":
                        ProveriKnjigu();
                        break;
                    case "2":
                        IznajmiKnjigu();
                        break;

                    case "3":
                        VratiKnjigu();
                        break;

                    case "4":
                        PregledKnjiga();
                        break;

                    case "0":
                        return;
                    default:
                        Console.WriteLine("Nepoznat izbor.");
                        break;
                }
            }
        }

        private void ProveriKnjigu()
        {
            Console.Write("Naslov: ");
            string naslov = Console.ReadLine();
            Console.Write("Autor: ");
            string autor = Console.ReadLine();

            string zahtev = $"{naslov};{autor}";
            byte[] zahtevBytes = Encoding.UTF8.GetBytes(zahtev);
            udpClient.Send(zahtevBytes, zahtevBytes.Length, serverIP, udpPort);

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] odgovor = udpClient.Receive(ref ep);
            Console.WriteLine("Server: " + Encoding.UTF8.GetString(odgovor));
        }

        private void IznajmiKnjigu()
        {
            Console.Write("Naslov: ");
            string naslov = Console.ReadLine();
            Console.Write("Autor: ");
            string autor = Console.ReadLine();
            Console.Write("Broj primeraka: ");
            int brPrimeraka = int.Parse(Console.ReadLine());

            string zahtev = $"IZNAJMI;{mojID};{naslov};{autor};{brPrimeraka}";
            byte[] podaci = Encoding.UTF8.GetBytes(zahtev);
            NetworkStream stream = tcpClient.GetStream();
            stream.Write(podaci, 0, podaci.Length);

            byte[] buffer = new byte[256];
            int br = stream.Read(buffer, 0, buffer.Length);
            string odgovor = Encoding.UTF8.GetString(buffer, 0, br);
            Console.WriteLine("Server: " + odgovor);
        }


        private void VratiKnjigu()
        {
            Console.Write("Naslov: ");
            string naslov = Console.ReadLine();
            Console.Write("Autor: ");
            string autor = Console.ReadLine();
            Console.Write("Broj primeraka za vracanje: ");
            int br = int.Parse(Console.ReadLine());

            string zahtev = $"VRATI;{mojID};{naslov};{autor};{br}";
            NetworkStream stream = tcpClient.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(zahtev);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[256];
            int brOdgovor = stream.Read(buffer, 0, buffer.Length);
            string odgovor = Encoding.UTF8.GetString(buffer, 0, brOdgovor);
            Console.WriteLine("Server: " + odgovor);

            foreach (Iznajmljivanje i in iznajmljene)
            {
                if (i.KnjigaI.Naslov == naslov && i.KnjigaI.Autor == autor)
                    iznajmljene.Remove(i);
            }
        }

        private void PregledKnjiga()
        {

        }
    }
}
