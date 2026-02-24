using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy.Models
{
    public class HistoriaRaportu
    {
        public string RaportId { get; set; }
        public string Pesel { get; set; }
        public string PelneImie { get; set; }
        public DateTime DataRaportu { get; set; }
        public string Status { get; set; }
        public bool CzyMandat { get; set; }
        public string Operator { get; set; }
    }
}
