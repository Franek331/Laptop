using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy.Models
{
    public class OdpowiedzRaportu
    {
        public bool Sukces { get; set; }
        public string Wiadomosc { get; set; }
        public string RaportId { get; set; }
        public Raport Raport { get; set; }
    }
}
