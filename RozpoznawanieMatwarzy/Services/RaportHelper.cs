using RozpoznawanieMatwarzy.Models;

namespace RozpoznawanieMatwarzy.Services
{
    /// <summary>
    /// Helper do przekazywania danych między ViewModelami
    /// </summary>
    public static class RaportHelper
    {
        public static OdpowiedzRozpoznania OstatniaRozpoznana { get; set; }
        public static ImageSource WybraneZdjecie { get; set; }
    }
}