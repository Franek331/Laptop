using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using RozpoznawanieMatwarzy.Models;
using RozpoznawanieMatwarzy.Services;

namespace RozpoznawanieMatwarzy.ViewModels
{
    public class ModelRaportu : INotifyPropertyChanged
    {
        private readonly SerwisRaportu _serwisRaportu;
        private readonly SerwisAutoryzacji _serwisAutoryzacji;  // ✅ NOWE
        private static readonly Random _random = new Random();
        private static readonly object _lockObject = new object();
        private static HashSet<long> _wygenerowaneNumery = new HashSet<long>();

        // ✅ Właściwości operatora (wystawiającego)
        private string _operatorImie;
        public string OperatorImie
        {
            get => _operatorImie;
            set { _operatorImie = value; OnPropertyChanged(nameof(OperatorImie)); }
        }

        private string _operatorNazwisko;
        public string OperatorNazwisko
        {
            get => _operatorNazwisko;
            set { _operatorNazwisko = value; OnPropertyChanged(nameof(OperatorNazwisko)); }
        }

        private string _operatorPelneImie;
        public string OperatorPelneImie
        {
            get => _operatorPelneImie;
            set { _operatorPelneImie = value; OnPropertyChanged(nameof(OperatorPelneImie)); }
        }

        // ✅ Właściwości bindowane
        private string _pelneImie;
        public string PelneImie
        {
            get => _pelneImie;
            set { _pelneImie = value; OnPropertyChanged(nameof(PelneImie)); }
        }

        private string _pesel;
        public string Pesel
        {
            get => _pesel;
            set { _pesel = value; OnPropertyChanged(nameof(Pesel)); }
        }

        private string _dataUrodzenia;
        public string DataUrodzenia
        {
            get => _dataUrodzenia;
            set { _dataUrodzenia = value; OnPropertyChanged(nameof(DataUrodzenia)); }
        }

        private string _plec;
        public string Plec
        {
            get => _plec;
            set { _plec = value; OnPropertyChanged(nameof(Plec)); }
        }

        private double _pewnosc;
        public double Pewnosc
        {
            get => _pewnosc;
            set { _pewnosc = value; OnPropertyChanged(nameof(Pewnosc)); }
        }

        private string _notatka;
        public string Notatka
        {
            get => _notatka;
            set { _notatka = value; OnPropertyChanged(nameof(Notatka)); }
        }

        private string _przeprowadzoneDialania;
        public string PrzeprowadzoneDialania
        {
            get => _przeprowadzoneDialania;
            set { _przeprowadzoneDialania = value; OnPropertyChanged(nameof(PrzeprowadzoneDialania)); }
        }

        private bool _czyMandat;
        public bool CzyMandat
        {
            get => _czyMandat;
            set
            {
                _czyMandat = value;
                OnPropertyChanged(nameof(CzyMandat));

                // Automatycznie generuj numer mandatu gdy zaznaczone
                if (_czyMandat && string.IsNullOrEmpty(_numerMandata))
                {
                    NumerMandata = GenerujNumerMandata();
                }
                else if (!_czyMandat)
                {
                    NumerMandata = "";
                }
            }
        }

        private string _kwotaMandatu;
        public string KwotaMandatu
        {
            get => _kwotaMandatu;
            set { _kwotaMandatu = value; OnPropertyChanged(nameof(KwotaMandatu)); }
        }

        private string _numerMandata;
        public string NumerMandata
        {
            get => _numerMandata;
            set { _numerMandata = value; OnPropertyChanged(nameof(NumerMandata)); }
        }

        private string _typMandata;
        public string TypMandata
        {
            get => _typMandata;
            set { _typMandata = value; OnPropertyChanged(nameof(TypMandata)); }
        }

        private string _statusMandata;
        public string StatusMandata
        {
            get => _statusMandata;
            set { _statusMandata = value; OnPropertyChanged(nameof(StatusMandata)); }
        }

        private string _komunikat;
        public string Komunikat
        {
            get => _komunikat;
            set { _komunikat = value; OnPropertyChanged(nameof(Komunikat)); }
        }

        private Color _kolorStatusu;
        public Color KolorStatusu
        {
            get => _kolorStatusu;
            set { _kolorStatusu = value; OnPropertyChanged(nameof(KolorStatusu)); }
        }

        private bool _czyWidocznyStatus;
        public bool CzyWidocznyStatus
        {
            get => _czyWidocznyStatus;
            set { _czyWidocznyStatus = value; OnPropertyChanged(nameof(CzyWidocznyStatus)); }
        }

        private bool _jestZajety;
        public bool JestZajety
        {
            get => _jestZajety;
            set { _jestZajety = value; OnPropertyChanged(nameof(JestZajety)); }
        }

        private ObservableCollection<string> _listaTypowMandatow;
        public ObservableCollection<string> ListaTypowMandatow
        {
            get => _listaTypowMandatow;
            set { _listaTypowMandatow = value; OnPropertyChanged(nameof(ListaTypowMandatow)); }
        }

        private ObservableCollection<string> _listaStatusowMandatu;
        public ObservableCollection<string> ListaStatusowMandatu
        {
            get => _listaStatusowMandatu;
            set { _listaStatusowMandatu = value; OnPropertyChanged(nameof(ListaStatusowMandatu)); }
        }

        // Commands
        public ICommand ZapiszRaportCommand { get; }
        public ICommand WyslijRaportCommand { get; }
        public ICommand WyczyscCommand { get; }
        public ICommand GenerujNowyNumerCommand { get; }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public ModelRaportu()
        {
            _serwisRaportu = new SerwisRaportu();
            _serwisAutoryzacji = new SerwisAutoryzacji();  // ✅ NOWE

            // Inicjalizuj listy
            ListaTypowMandatow = new ObservableCollection<string>
            {
                "Parkowanie w niedozwolonym miejscu",
                "Brak biletu parkingowego",
                "Przekroczenie prędkości",
                "Przejechanie na czerwonym świetle",
                "Brak pasów bezpieczeństwa",
                "Jazda pod wpływem alkoholu",
                "Inne"
            };

            ListaStatusowMandatu = new ObservableCollection<string>
            {
                "Do wysłania",
                "Wysłany",
                "Opłacony",
                "Niepłacony",
                "Zakwestionowany"
            };

            // Inicjalizuj Commands
            ZapiszRaportCommand = new Command(async () => await ZapiszRaport());
            WyslijRaportCommand = new Command(async () => await WyslijRaport());
            WyczyscCommand = new Command(Wyczysc);
            GenerujNowyNumerCommand = new Command(() => GenerujNowyNumerMandata());

            // Domyślne wartości
            CzyMandat = false;
            StatusMandata = "Do wysłania";
            KolorStatusu = Colors.Black;
            CzyWidocznyStatus = false;
            JestZajety = false;

            // ✅ ZAŁADUJ DANE OPERATORA - ZMIENIONE
            ZaladujDaneOperatora();
        }

        /// <summary>
        /// Załaduj dane zalogowanego operatora z SerwisAutoryzacji
        /// </summary>
        private async void ZaladujDaneOperatora()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("👤 ZaladujDaneOperatora - START");

                // ✅ Pobierz dane z SerwisAutoryzacji
                var imie = await _serwisAutoryzacji.PobierzOperatoraImieAsync();
                var nazwisko = await _serwisAutoryzacji.PobierzOperatoraNazwiskoAsync();

                System.Diagnostics.Debug.WriteLine($"👤 Pobrane dane: {imie} {nazwisko}");

                if (!string.IsNullOrEmpty(imie) && !string.IsNullOrEmpty(nazwisko))
                {
                    OperatorImie = imie;
                    OperatorNazwisko = nazwisko;
                    OperatorPelneImie = $"{imie} {nazwisko}";
                    System.Diagnostics.Debug.WriteLine($"✅ Dane operatora załadowane: {OperatorPelneImie}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Brak danych operatora - fallback do API");
                    // Fallback - spróbuj pobrać z API verify-token
                    await PobierzDaneOperatoraZAPI();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd ładowania danych operatora: {ex.Message}");
            }
        }

        /// <summary>
        /// Pobierz dane operatora z API verify-token (fallback)
        /// </summary>
        private async Task PobierzDaneOperatoraZAPI()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔐 PobierzDaneOperatoraZAPI - START");

                var token = await _serwisAutoryzacji.PobierzTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Brak tokenu");
                    return;
                }

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(Stale.URL_BAZY),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await httpClient.GetAsync("/api/verify-token");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        var element = doc.RootElement;
                        if (element.TryGetProperty("Uzytkownik", out var uzytkownik))
                        {
                            if (uzytkownik.TryGetProperty("FirstName", out var firstName) &&
                                uzytkownik.TryGetProperty("LastName", out var lastName))
                            {
                                OperatorImie = firstName.GetString() ?? "";
                                OperatorNazwisko = lastName.GetString() ?? "";
                                OperatorPelneImie = $"{OperatorImie} {OperatorNazwisko}".Trim();

                                System.Diagnostics.Debug.WriteLine($"✅ Dane z API załadowane: {OperatorPelneImie}");
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd pobierania z API: {ex.Message}");
            }
        }

        /// <summary>
        /// Generuje unikalny numer mandatu w zakresie 1-10,000,000,000
        /// </summary>
        private string GenerujNumerMandata()
        {
            lock (_lockObject)
            {
                long numer;
                int prob = 0;
                const int maxProb = 1000;

                do
                {
                    numer = _random.NextInt64(1, 10000000001L);
                    prob++;

                    if (prob >= maxProb)
                    {
                        numer = DateTime.Now.Ticks % 10000000000L + 1;
                        break;
                    }
                }
                while (_wygenerowaneNumery.Contains(numer));

                _wygenerowaneNumery.Add(numer);
                return numer.ToString("D11");
            }
        }

        /// <summary>
        /// Generuje nowy numer mandatu na żądanie użytkownika
        /// </summary>
        private void GenerujNowyNumerMandata()
        {
            if (CzyMandat)
            {
                NumerMandata = GenerujNumerMandata();
                Komunikat = "🔄 Wygenerowano nowy numer mandatu";
                KolorStatusu = Colors.Blue;
                CzyWidocznyStatus = true;

                Task.Delay(2000).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CzyWidocznyStatus = false;
                    });
                });
            }
        }

        /// <summary>
        /// Wczytaj dane rozpoznanej osoby
        /// </summary>
        public void WczytajDaneOsoby(object osoba, ImageSource zdjecie)
        {
            try
            {
                if (osoba == null)
                {
                    Komunikat = "❌ Błąd: Brak danych osoby";
                    KolorStatusu = Colors.Red;
                    CzyWidocznyStatus = true;
                    return;
                }

                var type = osoba.GetType();
                var properties = type.GetProperties();

                object peselObj = null;
                object imieObj = null;
                object nazwiskoObj = null;
                object dataObj = null;
                object plecObj = null;
                object pewnoscObj = null;

                foreach (var prop in properties)
                {
                    switch (prop.Name)
                    {
                        case "Pesel":
                            peselObj = prop.GetValue(osoba);
                            break;
                        case "Imie":
                            imieObj = prop.GetValue(osoba);
                            break;
                        case "Nazwisko":
                            nazwiskoObj = prop.GetValue(osoba);
                            break;
                        case "DataUrodzenia":
                            dataObj = prop.GetValue(osoba);
                            break;
                        case "Plec":
                            plecObj = prop.GetValue(osoba);
                            break;
                        case "Pewnosc":
                            pewnoscObj = prop.GetValue(osoba);
                            break;
                    }
                }

                Pesel = peselObj?.ToString() ?? "";
                string imie = imieObj?.ToString() ?? "";
                string nazwisko = nazwiskoObj?.ToString() ?? "";
                PelneImie = $"{imie} {nazwisko}".Trim();
                DataUrodzenia = dataObj?.ToString() ?? "";
                Plec = plecObj?.ToString() ?? "";

                if (pewnoscObj != null)
                {
                    if (double.TryParse(pewnoscObj.ToString(), out double pewnosc))
                    {
                        Pewnosc = pewnosc;
                    }
                }

                if (string.IsNullOrWhiteSpace(Pesel))
                {
                    Komunikat = "❌ KRYTYCZNY: PESEL nie został wczytany!";
                    KolorStatusu = Colors.Red;
                    CzyWidocznyStatus = true;
                    return;
                }

                CzyWidocznyStatus = false;
                Komunikat = "";
            }
            catch (Exception ex)
            {
                Komunikat = $"❌ Błąd: {ex.Message}";
                KolorStatusu = Colors.Red;
                CzyWidocznyStatus = true;
            }
        }

        /// <summary>
        /// Wygeneruj obiekt Raport
        /// </summary>
        private Raport GenerujRaport()
        {
            var czasiImieNazwisko = PelneImie?.Split(' ');
            string imie = czasiImieNazwisko?.Length > 0 ? czasiImieNazwisko[0] : "BRAK";
            string nazwisko = czasiImieNazwisko?.Length > 1
                ? string.Join(" ", czasiImieNazwisko.Skip(1))
                : "BRAK";

            DateTime dataUrodzenia = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(this.DataUrodzenia))
            {
                if (DateTime.TryParse(this.DataUrodzenia, out var parsedDate))
                {
                    dataUrodzenia = parsedDate;
                }
            }

            var raport = new Raport
            {
                Id = Guid.NewGuid().ToString(),
                Pesel = this.Pesel,
                Imie = imie,
                Nazwisko = nazwisko,
                DataUrodzenia = dataUrodzenia,
                Plec = this.Plec,
                Pewnosc = this.Pewnosc,
                Notatka = this.Notatka ?? "",
                PrzeprowadzoneDialania = this.PrzeprowadzoneDialania ?? "",
                CzyMandat = this.CzyMandat,
                KwotaMandatu = this.CzyMandat && !string.IsNullOrEmpty(this.KwotaMandatu)
                    ? this.KwotaMandatu
                    : null,
                NumerMandata = this.CzyMandat && !string.IsNullOrEmpty(this.NumerMandata)
                    ? this.NumerMandata
                    : null,
                TypMandata = this.TypMandata ?? "",
                StatusMandata = this.StatusMandata ?? "Do wysłania",
                // ✅ Dodaj dane operatora
                OperatorImie = this.OperatorImie ?? "",
                OperatorNazwisko = this.OperatorNazwisko ?? "",
                Operator = this.OperatorPelneImie ?? ""
            };

            return raport;
        }

        /// <summary>
        /// Zapisz raport lokalnie
        /// </summary>
        private async Task ZapiszRaport()
        {
            if (JestZajety) return;

            try
            {
                JestZajety = true;
                CzyWidocznyStatus = false;

                if (string.IsNullOrWhiteSpace(Pesel))
                {
                    Komunikat = "❌ Błąd: Brakuje PESELu - data nie została załadowana prawidłowo";
                    KolorStatusu = Colors.Red;
                    CzyWidocznyStatus = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(OperatorImie) || string.IsNullOrWhiteSpace(OperatorNazwisko))
                {
                    Komunikat = "❌ Błąd: Brakuje danych operatora - Zaloguj się ponownie";
                    KolorStatusu = Colors.Red;
                    CzyWidocznyStatus = true;
                    return;
                }

                if (CzyMandat && string.IsNullOrEmpty(NumerMandata))
                {
                    NumerMandata = GenerujNumerMandata();
                }

                var raport = GenerujRaport();

                var wynik = await _serwisRaportu.ZapiszRaportAsync(raport);

                if (wynik.Sukces)
                {
                    Komunikat = "✅ Raport zapisany pomyślnie!";
                    KolorStatusu = Colors.Green;
                }
                else
                {
                    Komunikat = $"❌ Błąd: {wynik.Wiadomosc}";
                    KolorStatusu = Colors.Red;
                }

                CzyWidocznyStatus = true;
            }
            catch (Exception ex)
            {
                Komunikat = $"❌ Wyjątek: {ex.Message}";
                KolorStatusu = Colors.Red;
                CzyWidocznyStatus = true;
            }
            finally
            {
                JestZajety = false;
            }
        }

        /// <summary>
        /// Wyślij raport do systemu
        /// </summary>
        private async Task WyslijRaport()
        {
            if (JestZajety) return;

            try
            {
                JestZajety = true;
                CzyWidocznyStatus = false;

                if (string.IsNullOrWhiteSpace(Pesel))
                {
                    Komunikat = "❌ Błąd: Brakuje PESELu\n\nOsoby nie została załadowana prawidłowo.\nWróć do rozpoznawania twarzy.";
                    KolorStatusu = Colors.Red;
                    CzyWidocznyStatus = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(OperatorImie) || string.IsNullOrWhiteSpace(OperatorNazwisko))
                {
                    Komunikat = "❌ Błąd: Brakuje danych operatora\n\nZaloguj się ponownie do aplikacji.";
                    KolorStatusu = Colors.Red;
                    CzyWidocznyStatus = true;
                    return;
                }

                if (CzyMandat && string.IsNullOrEmpty(NumerMandata))
                {
                    NumerMandata = GenerujNumerMandata();
                }

                var raport = GenerujRaport();

                var wynik = await _serwisRaportu.WyslijRaportAsync(raport);

                if (wynik.Sukces)
                {
                    Komunikat = "✅ Raport wysłany do systemu!\n\nMandat został zarejestrowany.";
                    KolorStatusu = Colors.Green;

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(1500);
                        Wyczysc();
                    });
                }
                else
                {
                    Komunikat = wynik.Wiadomosc;
                    KolorStatusu = Colors.Red;
                }

                CzyWidocznyStatus = true;
            }
            catch (Exception ex)
            {
                Komunikat = $"❌ Błąd: {ex.Message}";
                KolorStatusu = Colors.Red;
                CzyWidocznyStatus = true;
            }
            finally
            {
                JestZajety = false;
            }
        }

        /// <summary>
        /// Wyczyść formularz
        /// </summary>
        private void Wyczysc()
        {
            PelneImie = "";
            Pesel = "";
            DataUrodzenia = "";
            Plec = "";
            Pewnosc = 0;
            Notatka = "";
            PrzeprowadzoneDialania = "";
            CzyMandat = false;
            KwotaMandatu = "";
            NumerMandata = "";
            TypMandata = ListaTypowMandatow.FirstOrDefault() ?? "";
            StatusMandata = "Do wysłania";

            Komunikat = "";
            CzyWidocznyStatus = false;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}