using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using RozpoznawanieMatwarzy.Models;

namespace RozpoznawanieMatwarzy.Services
{
    public class SerwisAutoryzacji
    {
        private readonly HttpClient _httpClient;
        private const string TOKEN_KEY = "1745618756195nfcsjnjbnv";
        private const string USERNAME_KEY = "username";
        // ✅ NOWE KLUCZE DO PRZECHOWYWANIA DANYCH OPERATORA
        private const string OPERATOR_IMIE_KEY = "operator_imie";
        private const string OPERATOR_NAZWISKO_KEY = "operator_nazwisko";

        public SerwisAutoryzacji()
        {
            // ✅ DEBUGUJ URL
            var baseUrl = Stale.URL_BAZY;
            System.Diagnostics.Debug.WriteLine($"🔍 URL_BAZY: {baseUrl}");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // ✅ DODAJ DOMYŚLNE HEADERY
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RozpoznawanieMatwarzy/1.0");
        }

        public async Task<LoginResponse> ZalogujAsync(string username, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📝 ZalogujAsync - username: {username}");

                var loginData = new
                {
                    username = username,
                    password = password
                };

                System.Diagnostics.Debug.WriteLine($"📤 Wysyłam POST do: {_httpClient.BaseAddress}api/login");
                System.Diagnostics.Debug.WriteLine($"📤 Dane: {System.Text.Json.JsonSerializer.Serialize(loginData)}");

                var response = await _httpClient.PostAsJsonAsync("api/login", loginData);

                System.Diagnostics.Debug.WriteLine($"📥 Status code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Odpowiedź pomyślna (200)");

                    try
                    {
                        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

                        System.Diagnostics.Debug.WriteLine($"✅ Rozpakowana odpowiedź:");
                        System.Diagnostics.Debug.WriteLine($"   Sukces: {result?.Sukces}");
                        System.Diagnostics.Debug.WriteLine($"   Wiadomosc: {result?.Wiadomosc}");
                        System.Diagnostics.Debug.WriteLine($"   Token: {result?.Token?.Substring(0, Math.Min(50, result?.Token?.Length ?? 0)) ?? "null"}...");

                        if (result != null && result.Sukces)
                        {
                            // ✅ NOWE: Zapisz token i dane użytkownika
                            System.Diagnostics.Debug.WriteLine("💾 Zapisuję dane do SecureStorage...");

                            await SecureStorage.SetAsync(TOKEN_KEY, result.Token);
                            await SecureStorage.SetAsync(USERNAME_KEY, username);

                            // ✅ KRYTYCZNE: Zapisz imię i nazwisko operatora
                            if (result.Uzytkownik != null)
                            {
                                var imie = result.Uzytkownik.FirstName ?? "";
                                var nazwisko = result.Uzytkownik.LastName ?? "";

                                System.Diagnostics.Debug.WriteLine($"👤 Zapisuję operatora:");
                                System.Diagnostics.Debug.WriteLine($"   Imię: {imie}");
                                System.Diagnostics.Debug.WriteLine($"   Nazwisko: {nazwisko}");

                                await SecureStorage.SetAsync(OPERATOR_IMIE_KEY, imie);
                                await SecureStorage.SetAsync(OPERATOR_NAZWISKO_KEY, nazwisko);

                                System.Diagnostics.Debug.WriteLine("✅ Dane operatora zapisane");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("⚠️ Brak obiektu Uzytkownik w odpowiedzi");
                            }

                            System.Diagnostics.Debug.WriteLine("✅ Token zapisany");
                        }

                        return result ?? new LoginResponse
                        {
                            Sukces = false,
                            Wiadomosc = "Pusta odpowiedź z serwera"
                        };
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Błąd parsowania JSON: {parseEx.Message}");

                        var rawContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"📄 Raw content: {rawContent}");

                        return new LoginResponse
                        {
                            Sukces = false,
                            Wiadomosc = $"Błąd parsowania odpowiedzi: {parseEx.Message}"
                        };
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Błąd HTTP {response.StatusCode}");

                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📄 Error content: {errorContent}");

                    return new LoginResponse
                    {
                        Sukces = false,
                        Wiadomosc = $"Błąd: {response.StatusCode} - {errorContent}"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HttpRequestException: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   InnerException: {ex.InnerException?.Message}");

                return new LoginResponse
                {
                    Sukces = false,
                    Wiadomosc = $"Błąd połączenia: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Wyjątek ogólny: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");

                return new LoginResponse
                {
                    Sukces = false,
                    Wiadomosc = $"Błąd: {ex.Message}"
                };
            }
        }

        public async Task<string> PobierzTokenAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync(TOKEN_KEY);
                System.Diagnostics.Debug.WriteLine($"🔑 PobierzTokenAsync: {(string.IsNullOrEmpty(token) ? "BRAK" : "OK")}");
                return token;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd pobierania tokenu: {ex.Message}");
                return null;
            }
        }

        public async Task<string> PobierzUsernameAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(USERNAME_KEY);
            }
            catch
            {
                return null;
            }
        }

        // ✅ NOWE: Metody do pobierania danych operatora
        public async Task<string> PobierzOperatoraImieAsync()
        {
            try
            {
                var imie = await SecureStorage.GetAsync(OPERATOR_IMIE_KEY);
                System.Diagnostics.Debug.WriteLine($"👤 PobierzOperatoraImieAsync: {imie ?? "BRAK"}");
                return imie;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd pobierania imienia operatora: {ex.Message}");
                return null;
            }
        }

        public async Task<string> PobierzOperatoraNazwiskoAsync()
        {
            try
            {
                var nazwisko = await SecureStorage.GetAsync(OPERATOR_NAZWISKO_KEY);
                System.Diagnostics.Debug.WriteLine($"👤 PobierzOperatoraNazwiskoAsync: {nazwisko ?? "BRAK"}");
                return nazwisko;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd pobierania nazwiska operatora: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CzyZalogowanyAsync()
        {
            var token = await PobierzTokenAsync();
            bool zalogowany = !string.IsNullOrEmpty(token);
            System.Diagnostics.Debug.WriteLine($"👤 CzyZalogowanyAsync: {zalogowany}");
            return zalogowany;
        }

        public async Task WylogujAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚪 WylogujAsync...");
                SecureStorage.Remove(TOKEN_KEY);
                SecureStorage.Remove(USERNAME_KEY);
                SecureStorage.Remove(OPERATOR_IMIE_KEY);        // ✅ Usuń dane operatora
                SecureStorage.Remove(OPERATOR_NAZWISKO_KEY);    // ✅ Usuń dane operatora
                System.Diagnostics.Debug.WriteLine("✅ Wylogowano");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd wylogowania: {ex.Message}");
            }
        }

        public async Task<bool> WeryfikujTokenAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔐 WeryfikujTokenAsync...");

                var token = await PobierzTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("❌ Brak tokenu");
                    return false;
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "RozpoznawanieMatwarzy/1.0");

                System.Diagnostics.Debug.WriteLine("📤 Wysyłam GET /api/verify-token");

                var response = await _httpClient.GetAsync("api/verify-token");

                System.Diagnostics.Debug.WriteLine($"📥 Status: {response.StatusCode}");

                bool isValid = response.IsSuccessStatusCode;
                System.Diagnostics.Debug.WriteLine($"✅ Token ważny: {isValid}");

                return isValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Błąd weryfikacji: {ex.Message}");
                return false;
            }
        }
    }
}