using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy.Models
{
    public class StatusNFC
    {
        public bool Zarejestrowany { get; set; }
        public bool Aktywny { get; set; }
        public string NfcUid { get; set; }
    }
}
