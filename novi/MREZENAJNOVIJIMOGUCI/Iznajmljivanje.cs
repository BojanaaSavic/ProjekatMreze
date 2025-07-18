using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MREZENAJNOVIJIMOGUCI
{
    public class Iznajmljivanje
    {
        public Knjiga KnjigaI { get; set; } // "Naslov - Autor"
        public int ClanID { get; set; }
        public DateTime DatumVracanja { get; set; }
        public int BrojPrimeraka { get; set; }

        public Iznajmljivanje() { }

        public Iznajmljivanje(Knjiga knjiga, int clanId, int brojPrimeraka)
        {
            KnjigaI = knjiga;
            ClanID = clanId;
            BrojPrimeraka = brojPrimeraka;
            DatumVracanja = DateTime.Now.AddDays(14); // dve nedelje
        }
    }
}
