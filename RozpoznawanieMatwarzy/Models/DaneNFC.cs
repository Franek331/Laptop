using System.Text.Json;

namespace RozpoznawanieMatwarzy.Models
{
    public class DaneNFC
    {
        public string Pesel { get; set; }  // ID - PESEL
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public DateTime DataUrodzenia { get; set; }
        public string Plec { get; set; }  // "M" lub "K"
        public string SciezkaZdjecia { get; set; }
        public DateTime DataRejestracji { get; set; }

        public string DoJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static DaneNFC ZJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<DaneNFC>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}