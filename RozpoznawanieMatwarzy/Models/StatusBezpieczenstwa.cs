using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy.Models
{
    public class StatusBezpieczenstwa
    {
        public bool Poszukiwany { get; set; }
        public bool Zastrzeżony { get; set; }
        public string Powód { get; set; }
        public string KolorAlertu { get; set; }
    }
}
