using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RozpoznawanieMatwarzy.Services;
using RozpoznawanieMatwarzy.Models;
using RozpoznawanieMatwarzy.Views;
using System.Diagnostics;

namespace RozpoznawanieMatwarzy.ViewModels
{
    public class ModelRozpoznawania : INotifyPropertyChanged
    {
        private readonly SerwisApiTwarzy _serwisApi;
        private readonly SerwisNFC _serwisNfc;

        private ImageSource _wybraneZdjecie;
        private byte[] _zdjecieBytes;
        private bool _jestZajety;
        private string _wynik;
        private Color _kolorWyniku;
        private string _statusNfc;
        private bool _nfcAktywny;
        private string _statusBezpieczenstwa;
        private Color _kolorBezpieczenstwa;
        private bool _jestPoszukiwany;
        private bool _jestZastrzezony;
        private string _powidBezpieczenstwa;

        private OdpowiedzRozpoznania _ostatniaRozpoznana;
        private bool _czyMoznaOtworzycRaport;

        // ✅ DODAJ POLE DLA COMMAND'Y
        private ICommand _otworzRaportCommand;

        public ModelRozpoznawania()
        {
            _serwisApi = new SerwisApiTwarzy();
            _serwisNfc = new SerwisNFC();

            WybierzZGaleriiCommand = new Command(async () => await WybierzZGalerii());
            ZrobZdjecieCommand = new Command(async () => await ZrobZdjecie());
            RozpoznajCommand = new Command(async () => await Rozpoznaj(), () => CzyMoznaRozpoznac());
            OdczytajZNfcCommand = new Command(async () => await OdczytajZNfc());

            // ✅ NAPRAWIONO: Przypisz do właściwości
            OtworzRaportCommand = new Command(async () => await OtworzRaport(), () => CzyMoznaOtworzycRaport);

            KolorWyniku = Colors.Gray;
            KolorBezpieczenstwa = Colors.Green;
        }

        // ===========================
        // PROPERTIES
        // ===========================

        public ImageSource WybraneZdjecie
        {
            get => _wybraneZdjecie;
            set
            {
                _wybraneZdjecie = value;
                OnPropertyChanged();
                ((Command)RozpoznajCommand).ChangeCanExecute();
            }
        }

        public OdpowiedzRozpoznania OstatniaRozpoznana
        {
            get => _ostatniaRozpoznana;
            set
            {
                _ostatniaRozpoznana = value;
                OnPropertyChanged();
                CzyMoznaOtworzycRaport = value != null;
            }
        }

        public bool CzyMoznaOtworzycRaport
        {
            get => _czyMoznaOtworzycRaport;
            set
            {
                _czyMoznaOtworzycRaport = value;
                OnPropertyChanged();
                // Zaktualizuj CanExecute dla command'y
                if (OtworzRaportCommand is Command cmd)
                {
                    cmd.ChangeCanExecute();
                }
            }
        }

        public ICommand OtworzRaportCommand
        {
            get => _otworzRaportCommand;
            private set => _otworzRaportCommand = value;
        }

        public bool JestZajety
        {
            get => _jestZajety;
            set
            {
                _jestZajety = value;
                OnPropertyChanged();
                ((Command)RozpoznajCommand).ChangeCanExecute();
            }
        }

        public string Wynik
        {
            get => _wynik;
            set
            {
                _wynik = value;
                OnPropertyChanged();
            }
        }

        public Color KolorWyniku
        {
            get => _kolorWyniku;
            set
            {
                _kolorWyniku = value;
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

        public bool NfcAktywny
        {
            get => _nfcAktywny;
            set
            {
                _nfcAktywny = value;
                OnPropertyChanged();
            }
        }

        public string StatusBezpieczenstwa
        {
            get => _statusBezpieczenstwa;
            set
            {
                _statusBezpieczenstwa = value;
                OnPropertyChanged();
            }
        }

        public Color KolorBezpieczenstwa
        {
            get => _kolorBezpieczenstwa;
            set
            {
                _kolorBezpieczenstwa = value;
                OnPropertyChanged();
            }
        }

        public bool JestPoszukiwany
        {
            get => _jestPoszukiwany;
            set
            {
                _jestPoszukiwany = value;
                OnPropertyChanged();
            }
        }

        public bool JestZastrzezony
        {
            get => _jestZastrzezony;
            set
            {
                _jestZastrzezony = value;
                OnPropertyChanged();
            }
        }

        public string PowidBezpieczenstwa
        {
            get => _powidBezpieczenstwa;
            set
            {
                _powidBezpieczenstwa = value;
                OnPropertyChanged();
            }
        }

        public ICommand WybierzZGaleriiCommand { get; }
        public ICommand ZrobZdjecieCommand { get; }
        public ICommand RozpoznajCommand { get; }
        public ICommand OdczytajZNfcCommand { get; }

        // ===========================
        // METHODS
        // ===========================

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
                await Application.Current.MainPage.DisplayAlert("Błąd", ex.Message, "OK");
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
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Błąd", ex.Message, "OK");
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
            Wynik = "Zdjęcie wybrane! Kliknij Rozpoznaj.";
            KolorWyniku = Colors.Gray;
        }

        private async Task Rozpoznaj()
        {
            JestZajety = true;
            Wynik = "Rozpoznawanie...";
            KolorWyniku = Colors.Orange;

            try
            {
                var odpowiedz = await _serwisApi.RozpoznajTwarzAsync(_zdjecieBytes);

                // ✅ DODAJ DEBUGOWANIE
                DebugLog("📥 Odpowiedź z serwera RozpoznajTwarzAsync:");
                DebugLog($"   Rozpoznano: {odpowiedz.Rozpoznano}");
                DebugLog($"   Pesel: '{odpowiedz.Pesel}'");
                DebugLog($"   Imie: '{odpowiedz.Imie}'");
                DebugLog($"   Nazwisko: '{odpowiedz.Nazwisko}'");
                DebugLog($"   Pewnosc: {odpowiedz.Pewnosc}");
                DebugLog($"   Wiadomosc: '{odpowiedz.Wiadomosc}'");

                if (odpowiedz.Rozpoznano)
                {
                    var securityStatus = await _serwisApi.PobierzStatusBezpieczenstwa(odpowiedz.Pesel);
                    await WyswietlWynikRozpoznania(odpowiedz, securityStatus);
                }
                else
                {
                    Wynik = "❌ Nie rozpoznano";
                    KolorWyniku = Colors.Red;
                    StatusBezpieczenstwa = "";
                }
            }
            catch (Exception ex)
            {
                Wynik = $"❌ Błąd: {ex.Message}";
                KolorWyniku = Colors.Red;
                DebugLog($"💥 Błąd rozpoznawania: {ex}");
            }
            finally
            {
                JestZajety = false;
            }
        }

        private async Task WyswietlWynikRozpoznania(OdpowiedzRozpoznania odpowiedz, StatusBezpieczenstwa securityStatus)
        {
            // ✅ Zapisz ostatnią rozpoznaną osobę
            DebugLog("📋 Zapisuję ostatnią rozpoznaną osobę:");
            DebugLog($"   Pesel: '{odpowiedz.Pesel}'");
            DebugLog($"   Imie: '{odpowiedz.Imie}'");
            DebugLog($"   Nazwisko: '{odpowiedz.Nazwisko}'");

            OstatniaRozpoznana = odpowiedz;

            Wynik = $"✅ Rozpoznano: {odpowiedz.Imie} {odpowiedz.Nazwisko}\n" +
                    $"PESEL: {odpowiedz.Pesel}\n" +
                    $"";

            JestPoszukiwany = securityStatus.Poszukiwany;
            JestZastrzezony = securityStatus.Zastrzeżony;
            PowidBezpieczenstwa = securityStatus.Powód;

            if (securityStatus.Poszukiwany)
            {
                KolorWyniku = Colors.Red;
                KolorBezpieczenstwa = Colors.Red;
                StatusBezpieczenstwa = "🚨 POSZUKIWANY!🚨";
            }
            else if (securityStatus.Zastrzeżony)
            {
                KolorWyniku = Colors.Orange;
                KolorBezpieczenstwa = Colors.Orange;
                StatusBezpieczenstwa = "⚠️ ZASTRZEŻONY";
            }
            else
            {
                KolorWyniku = Colors.Green;
                KolorBezpieczenstwa = Colors.Green;
                StatusBezpieczenstwa = "✅ BRAK ZAGROŻENIA";
            }

            await Application.Current.MainPage.DisplayAlert("Rozpoznano!",
                $"Osoba: {odpowiedz.Imie} {odpowiedz.Nazwisko}\n" +
                $"Status: {StatusBezpieczenstwa}",
                "OK");
        }

        private async Task OdczytajZNfc()
        {
            JestZajety = true;
            StatusNfc = "Przygotowanie...";
            Wynik = "Oczekiwanie na NFC...";
            KolorWyniku = Colors.Orange;

            try
            {
                var dane = await _serwisNfc.OdczytajZKartyAsync(status =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusNfc = status;
                    });
                });

                if (dane != null)
                {
                    var nfcStatus = await _serwisApi.PobierzStatusNFC(dane.Pesel);

                    if (!nfcStatus.Aktywny)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "⚠️ NFC Nieaktywny",
                            $"NFC dla tej osoby jest wyłączone.",
                            "OK"
                        );
                        Wynik = "❌ NFC jest wyłączony";
                        KolorWyniku = Colors.Red;
                        return;
                    }

                    var securityStatus = await _serwisApi.PobierzStatusBezpieczenstwa(dane.Pesel);
                    ImageSource zdjecie = await PobierzZdjecieZSerwera(dane.Pesel);

                    // ✅ NAPRAWIONO: Utwórz OdpowiedzRozpoznania z danych NFC
                    var odpowiedz = new OdpowiedzRozpoznania
                    {
                        Pesel = dane.Pesel,
                        Imie = dane.Imie,
                        Nazwisko = dane.Nazwisko,
                        DataUrodzenia = dane.DataUrodzenia,
                        Plec = dane.Plec,
                        Pewnosc = 1.0,  // 100% pewność z NFC
                        Rozpoznano = true,
                        Wiadomosc = "Odczytano z NFC"
                    };

                    OstatniaRozpoznana = odpowiedz;

                    Wynik = $"✅ Odczytano:\n{dane.Imie} {dane.Nazwisko}\n" +
                            $"PESEL: {dane.Pesel}";

                    JestPoszukiwany = securityStatus.Poszukiwany;
                    JestZastrzezony = securityStatus.Zastrzeżony;

                    if (securityStatus.Poszukiwany)
                    {
                        KolorWyniku = Colors.Red;
                        KolorBezpieczenstwa = Colors.Red;
                        StatusBezpieczenstwa = "🚨 POSZUKIWANY!";
                    }
                    else if (securityStatus.Zastrzeżony)
                    {
                        KolorWyniku = Colors.Orange;
                        KolorBezpieczenstwa = Colors.Orange;
                        StatusBezpieczenstwa = "⚠️ ZASTRZEŻONY";
                    }
                    else
                    {
                        KolorWyniku = Colors.Green;
                        KolorBezpieczenstwa = Colors.Green;
                        StatusBezpieczenstwa = "✅ OK";
                    }

                    if (zdjecie != null)
                    {
                        WybraneZdjecie = zdjecie;
                    }

                    await Application.Current.MainPage.DisplayAlert("Odczytano!",
                        $"Osoba: {dane.Imie} {dane.Nazwisko}\n" +
                        $"Status: {StatusBezpieczenstwa}",
                        "OK");

                    StatusNfc = "";
                }
                else
                {
                    Wynik = "❌ Nie odczytano";
                    KolorWyniku = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                StatusNfc = $"❌ Błąd: {ex.Message}";
                Wynik = $"❌ Błąd NFC";
                KolorWyniku = Colors.Red;
                await Application.Current.MainPage.DisplayAlert("Błąd", ex.Message, "OK");
            }
            finally
            {
                JestZajety = false;
                _serwisNfc.ZatrzymajNasluchiwanie();
            }
        }

        private async Task<ImageSource> PobierzZdjecieZSerwera(string pesel)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var baseUrl = "http://192.168.88.253:5000";
                    var response = await httpClient.GetAsync($"{baseUrl}/api/faces/{pesel}");

                    if (!response.IsSuccessStatusCode)
                        return null;

                    var json = await response.Content.ReadAsStringAsync();
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        var osoba = doc.RootElement.GetProperty("Osoba");
                        if (osoba.TryGetProperty("SciezkaZdjecia", out var pathElement))
                        {
                            var photoPath = pathElement.GetString();
                            if (!string.IsNullOrEmpty(photoPath))
                            {
                                var photoUrl = $"{baseUrl}{photoPath}";
                                var photoBytes = await httpClient.GetByteArrayAsync(photoUrl);
                                return ImageSource.FromStream(() => new MemoryStream(photoBytes));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd: {ex.Message}");
            }
            return null;
        }

        // ✅ DODAJ TĘ METODĘ
        private async Task OtworzRaport()
        {
            if (OstatniaRozpoznana == null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "❌ Brak danych",
                    "Najpierw rozpoznaj osobę",
                    "OK"
                );
                return;
            }

            try
            {
                DebugLog("🔄 Otwieranie raportu...");
                DebugLog($"   OstatniaRozpoznana.Pesel: '{OstatniaRozpoznana.Pesel}'");
                DebugLog($"   OstatniaRozpoznana.Imie: '{OstatniaRozpoznana.Imie}'");

                RaportHelper.OstatniaRozpoznana = OstatniaRozpoznana;
                RaportHelper.WybraneZdjecie = WybraneZdjecie;

                DebugLog("✅ RaportHelper ustawiony, nawiguję do RaportPage");

                await Shell.Current.GoToAsync("///RaportPage");
            }
            catch (Exception ex)
            {
                DebugLog($"💥 Błąd otwierania raportu: {ex}");
                await Application.Current.MainPage.DisplayAlert("❌ Błąd", ex.Message, "OK");
            }
        }

        private bool CzyMoznaRozpoznac()
        {
            return !JestZajety && _zdjecieBytes != null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ✅ DEBUGOWANIE
        private void DebugLog(string message)
        {
            Debug.WriteLine($"[ModelRozpoznawania] {message}");
        }
    }
}