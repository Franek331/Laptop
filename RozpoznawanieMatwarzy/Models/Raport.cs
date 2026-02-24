using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RozpoznawanieMatwarzy.Models
{
    public class Raport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Pesel { get; set; }

        public string Imie { get; set; }

        public string Nazwisko { get; set; }

        public DateTime DataUrodzenia { get; set; }

        public string Plec { get; set; }

        public double Pewnosc { get; set; }

        // Dane raportu
        public DateTime DataRaportu { get; set; } = DateTime.Now;

        public string Notatka { get; set; }

        public string PrzeprowadzoneDialania { get; set; }

        public bool CzyMandat { get; set; }

        public string KwotaMandatu { get; set; }

        public string NumerMandata { get; set; }

        public string StatusMandata { get; set; } = "Do wysłania";

        public string TypMandata { get; set; }

        public string Operator { get; set; }

        public DateTime DataWyslania { get; set; }

        public string Status { get; set; } = "Nowy";

        public string PelneImie => $"{Imie} {Nazwisko}";

        public string DanePodstawowe => $"{PelneImie}\nPESEL: {Pesel}\nData ur.: {DataUrodzenia:dd.MM.yyyy}";
        public string DaneMandata => CzyMandat ? $"Kwota: {KwotaMandatu}\nNumer: {NumerMandata}" : "Bez mandatu";

        public string OperatorImie { get; set; }
        public string OperatorNazwisko { get; set; }
    }
}