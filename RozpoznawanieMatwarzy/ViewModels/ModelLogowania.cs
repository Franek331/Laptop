using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using RozpoznawanieMatwarzy.Services;

namespace RozpoznawanieMatwarzy.ViewModels
{
    public class ModelLogowania : INotifyPropertyChanged
    {
        private readonly SerwisAutoryzacji _serwisAutoryzacji;

        private string _username;
        private string _password;
        private bool _jestZajety;
        private string _komunikat;
        private Color _kolorKomunikatu;

        public ModelLogowania()
        {
            _serwisAutoryzacji = new SerwisAutoryzacji();

            ZalogujCommand = new Command(async () => await Zaloguj(), () => CzyMoznaZalogowac());

            KolorKomunikatu = Colors.Gray;
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ((Command)ZalogujCommand).ChangeCanExecute();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ((Command)ZalogujCommand).ChangeCanExecute();
            }
        }

        public bool JestZajety
        {
            get => _jestZajety;
            set
            {
                _jestZajety = value;
                OnPropertyChanged();
                ((Command)ZalogujCommand).ChangeCanExecute();
            }
        }

        public string Komunikat
        {
            get => _komunikat;
            set
            {
                _komunikat = value;
                OnPropertyChanged();
            }
        }

        public Color KolorKomunikatu
        {
            get => _kolorKomunikatu;
            set
            {
                _kolorKomunikatu = value;
                OnPropertyChanged();
            }
        }

        public ICommand ZalogujCommand { get; }

        private async Task Zaloguj()
        {
            JestZajety = true;
            Komunikat = "Logowanie...";
            KolorKomunikatu = Colors.Orange;

            try
            {
                var result = await _serwisAutoryzacji.ZalogujAsync(Username, Password);

                if (result.Sukces)
                {
                    Komunikat = "✅ Zalogowano pomyślnie!";
                    KolorKomunikatu = Colors.Green;

                    // Przejdź do głównej strony aplikacji
                    await Task.Delay(1000); // Krótkie opóźnienie żeby użytkownik widział komunikat
                    await Shell.Current.GoToAsync("///RegisterPage");
                }
                else
                {
                    Komunikat = $"❌ {result.Wiadomosc}";
                    KolorKomunikatu = Colors.Red;
                    Password = string.Empty; // Wyczyść hasło
                }
            }
            catch (Exception ex)
            {
                Komunikat = $"❌ Błąd: {ex.Message}";
                KolorKomunikatu = Colors.Red;
            }
            finally
            {
                JestZajety = false;
            }
        }

        private bool CzyMoznaZalogowac()
        {
            return !JestZajety &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

