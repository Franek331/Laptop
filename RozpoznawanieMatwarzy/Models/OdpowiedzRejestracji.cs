using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy.Models
{
    public class OdpowiedzRejestracji
    {
        public bool Sukces { get; set; }
        public string Wiadomosc { get; set; }
        public OsobaZarejestrowana Osoba { get; set; }

    }
}
