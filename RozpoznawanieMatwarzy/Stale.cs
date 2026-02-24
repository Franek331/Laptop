using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy
{
    public class Stale
    {
        public const string URL_BAZY = "http://192.168.88.253:5000";

        public const string ENDPOINT_REJESTRACJA = "/api/register";
        public const string ENDPOINT_ROZPOZNANIE = "/api/recognize";
        public const string ENDPOINT_LISTA_TWARZY = "/api/faces";

    }
}
