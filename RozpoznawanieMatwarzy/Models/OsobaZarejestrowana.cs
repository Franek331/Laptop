using System;

namespace RozpoznawanieMatwarzy.Models
{
    public class OsobaZarejestrowana
    {
        public string Pesel { get; set; }  // ID - PESEL
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public DateTime DataUrodzenia { get; set; }
        public string Plec { get; set; }  // "M" lub "K"
        public string SciezkaZdjecia { get; set; }
        public DateTime DataZarejestrowania { get; set; }

        public string PelneImie => $"{Imie} {Nazwisko}";
    }
}