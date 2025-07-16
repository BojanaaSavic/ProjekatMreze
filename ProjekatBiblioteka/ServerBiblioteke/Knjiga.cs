using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerBiblioteke
{
    [Serializable]
    public class Knjiga
    {
        public string Naziv = "";
        public string Autor = "";
        public int Kolicina = 0;

        public Knjiga(string naziv, string autor, int kolicina)
        {
            Naziv = naziv;
            Autor = autor;
            Kolicina = kolicina;
        }

        public override string ToString()
        {
            return Naziv + " " + Autor + " " + Kolicina;
        }
    }
}
