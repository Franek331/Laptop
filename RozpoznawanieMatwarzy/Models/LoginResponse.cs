using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RozpoznawanieMatwarzy.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("Sukces")]
        public bool Sukces { get; set; }

        [JsonPropertyName("Token")]
        public string Token { get; set; }

        [JsonPropertyName("Uzytkownik")]
        public UzytkownikInfo Uzytkownik { get; set; }

        [JsonPropertyName("Wiadomosc")]
        public string Wiadomosc { get; set; }
    }
}
