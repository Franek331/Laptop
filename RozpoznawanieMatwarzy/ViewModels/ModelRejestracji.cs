using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RozpoznawanieMatwarzy.Services;
using RozpoznawanieMatwarzy.Models;

namespace RozpoznawanieMatwarzy.ViewModels
{
    public class ModelRejestracji : INotifyPropertyChanged
    {
        private readonly SerwisApiTwarzy _serwisApi;
        private readonly SerwisNFC _serwisNfc;

        private string _imie;
        private string _nazwisko;
        private string _pesel;
        private DateTime _dataUrodzenia = DateTime.Now;
        private string _plec = "M";
        private ImageSource _wybraneZdjecie;
        private byte[] _zdjecieBytes;
        private bool _jestZajety;
        private string _komunikat;
        private string _statusNfc;

        public ModelRejestracji()
        {
            _serwisApi = new SerwisApiTwarzy();
            _serwisNfc = new SerwisNFC();

            WybierzZGaleriiCommand = new Command(async () => await WybierzZGalerii());
            ZrobZdjecieCommand = new Command(async () => await ZrobZdjecie());
            ZarejestrujCommand = new Command(async () => await Zarejestruj(), () => CzyMoznaZarejestrowac());
            ZapiszNaNfcCommand = new Command(async () => await ZapiszNaNfc(), () => CzyMoznaZapiszNaNfc());
        }

        public string Imie
        {
            get => _imie;
            set
            {
                _imie = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
            }
        }

        public string Nazwisko
        {
            get => _nazwisko;
            set
            {
                _nazwisko = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
            }
        }

        public string Pesel
        {
            get => _pesel;
            set
            {
                _pesel = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
                ((Command)ZapiszNaNfcCommand).ChangeCanExecute();
            }
        }

        public DateTime DataUrodzenia
        {
            get => _dataUrodzenia;
            set
            {
                _dataUrodzenia = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
            }
        }

        public string Plec
        {
            get => _plec;
            set
            {
                _plec = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
            }
        }

        public ImageSource WybraneZdjecie
        {
            get => _wybraneZdjecie;
            set
            {
                _wybraneZdjecie = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
            }
        }

        public bool JestZajety
        {
            get => _jestZajety;
            set
            {
                _jestZajety = value;
                OnPropertyChanged();
                ((Command)ZarejestrujCommand).ChangeCanExecute();
                ((Command)ZapiszNaNfcCommand).ChangeCanExecute();
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

        public string StatusNfc
        {
            get => _statusNfc;
            set
            {
                _statusNfc = value;
                OnPropertyChanged();
            }
        }

        public ICommand WybierzZGaleriiCommand { get; }
        public ICommand ZrobZdjecieCommand { get; }
        public ICommand ZarejestrujCommand { get; }
        public ICommand ZapiszNaNfcCommand { get; }

        private async Task WybierzZGalerii()
        {
            try
            {
                var wynik = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Wybierz zdjęcie"
                });

                if (wynik != null)
                {
                    await PrzetworzWybraneZdjecie(wynik);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd",
                    $"Nie można wybrać zdjęcia: {ex.Message}", "OK");
            }
        }

        private async Task ZrobZdjecie()
        {
            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    var zdjecie = await MediaPicker.Default.CapturePhotoAsync();

                    if (zdjecie != null)
                    {
                        await PrzetworzWybraneZdjecie(zdjecie);
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Błąd",
                        "Aparat nie jest dostępny", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd",
                    $"Nie można zrobić zdjęcia: {ex.Message}", "OK");
            }
        }

        private async Task PrzetworzWybraneZdjecie(FileResult zdjecie)
        {
            var stream = await zdjecie.OpenReadAsync();

            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                _zdjecieBytes = memoryStream.ToArray();
            }

            WybraneZdjecie = ImageSource.FromStream(() => new MemoryStream(_zdjecieBytes));
            Komunikat = "Zdjęcie wybrane! Wprowadź dane i kliknij Zarejestruj.";
        }

        private async Task Zarejestruj()
        {
            JestZajety = true;
            Komunikat = "Rejestrowanie...";

            try
            {
                var odpowiedz = await _serwisApi.ZarejestrujTwarzAsync(
                    Imie, Nazwisko, Pesel, DataUrodzenia, Plec, _zdjecieBytes);

                if (odpowiedz.Sukces)
                {
                    bool zapiszNaNfc = await Application.Current.MainPage.DisplayAlert(
                        "Sukces!",
                        $"Zarejestrowano: {Imie} {Nazwisko}\n\nCzy zapisać na NFC?",
                        "Tak",
                        "Nie"
                    );

                    if (zapiszNaNfc)
                    {
                        await ZapiszNaNfc();
                    }
                    else
                    {
                        CzyscFormularz();
                        Komunikat = "Gotowe!";
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Błąd",
                        odpowiedz.Wiadomosc, "OK");
                    Komunikat = "Błąd rejestracji.";
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd",
                    $"Błąd: {ex.Message}", "OK");
                Komunikat = "Błąd połączenia.";
            }
            finally
            {
                JestZajety = false;
            }
        }

        private async Task ZapiszNaNfc()
        {
            if (string.IsNullOrEmpty(Pesel))
            {
                await Application.Current.MainPage.DisplayAlert("Błąd",
                    "Najpierw zarejestruj osobę", "OK");
                return;
            }

            JestZajety = true;
            StatusNfc = "Przygotowanie...";

            try
            {
                var dane = new DaneNFC
                {
                    Pesel = Pesel,
                    Imie = Imie,
                    Nazwisko = Nazwisko,
                    DataUrodzenia = DataUrodzenia,
                    Plec = Plec,
                    SciezkaZdjecia = "",
                    DataRejestracji = DateTime.Now
                };

                bool sukces = await _serwisNfc.ZapiszNaKarteAsync(dane, status =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusNfc = status;
                    });
                });

                if (sukces)
                {
                    await Application.Current.MainPage.DisplayAlert("Sukces!",
                        "Dane zapisane na NFC", "OK");

                    CzyscFormularz();
                    Komunikat = "Gotowe!";
                    StatusNfc = "";
                    ((Command)ZapiszNaNfcCommand).ChangeCanExecute();
                }
            }
            catch (Exception ex)
            {
                StatusNfc = $"❌ Błąd: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert("Błąd",
                    $"Nie można zapisać: {ex.Message}", "OK");
            }
            finally
            {
                JestZajety = false;
                _serwisNfc.ZatrzymajNasluchiwanie();
            }
        }

        private void CzyscFormularz()
        {
            Imie = string.Empty;
            Nazwisko = string.Empty;
            Pesel = string.Empty;
            DataUrodzenia = DateTime.Now;
            Plec = "M";
            WybraneZdjecie = null;
            _zdjecieBytes = null;
        }

        private bool CzyMoznaZarejestrowac()
        {
            return !JestZajety &&
                   !string.IsNullOrWhiteSpace(Imie) &&
                   !string.IsNullOrWhiteSpace(Nazwisko) &&
                   !string.IsNullOrWhiteSpace(Pesel) &&
                   Pesel.Length == 11 &&
                   _zdjecieBytes != null;
        }

        private bool CzyMoznaZapiszNaNfc()
        {
            return !string.IsNullOrEmpty(Pesel) && !JestZajety;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}