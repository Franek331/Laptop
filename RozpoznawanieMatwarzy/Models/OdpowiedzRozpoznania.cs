using System;

namespace RozpoznawanieMatwarzy.Models
{
    public class OdpowiedzRozpoznania
    {
        [System.Text.Json.Serialization.JsonPropertyName("Pesel")]
        public string Pesel { get; set; }  // ID - PESEL
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public DateTime DataUrodzenia { get; set; }
        public string Plec { get; set; }  // "M" lub "K"
        public double Pewnosc { get; set; }
        public bool Rozpoznano { get; set; }
        public string Wiadomosc { get; set; }

        public string PelneImie => $"{Imie} {Nazwisko}";
    }
}