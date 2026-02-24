using RozpoznawanieMatwarzy.Services;
using RozpoznawanieMatwarzy.Views;

namespace RozpoznawanieMatwarzy
{
    public partial class AppShell : Shell
    {
        private readonly SerwisAutoryzacji _serwisAutoryzacji;

        public AppShell()
        {
            InitializeComponent();

            _serwisAutoryzacji = new SerwisAutoryzacji();

            // Rejestracja tras
            Routing.RegisterRoute("LoginPage", typeof(StronaLogowania));
            Routing.RegisterRoute("MainPage", typeof(StronaGlowna));
            Routing.RegisterRoute("RegisterPage", typeof(StronaRejestracji));
            Routing.RegisterRoute("RecognizePage", typeof(StronaRozpoznawania));
            Routing.RegisterRoute("RaportPage", typeof(StronaRaportu)); 

            // Sprawdź czy użytkownik jest zalogowany
            SprawdzStatusLogowania();
        }

        private async void SprawdzStatusLogowania()
        {
            var czyZalogowany = await _serwisAutoryzacji.CzyZalogowanyAsync();
            
            if (czyZalogowany)
            {
                // Jeśli zalogowany, idź do strony głównej
                var username = await _serwisAutoryzacji.PobierzUsernameAsync();
                
                // Zaktualizuj label w headerze (jeśli istnieje)
                if (this.FindByName<Label>("UsernameLabel") is Label label)
                {
                    label.Text = $"Zalogowano jako: {username}";
                }

                await GoToAsync("///RegisterPage");
            }
            else
            {
                // Jeśli nie zalogowany, idź do strony logowania
                await GoToAsync("///LoginPage");
            }
        }

        private async void OnWylogujClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Wylogowanie",
                "Czy na pewno chcesz się wylogować?",
                "Tak",
                "Nie"
            );

            if (confirm)
            {
                await _serwisAutoryzacji.WylogujAsync();
                await GoToAsync("///LoginPage");
            }
        }
    }
}