using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using RozpoznawanieMatwarzy.Models;

namespace RozpoznawanieMatwarzy.Services
{
    public class SerwisRaportu
    {
        private readonly HttpClient _httpClient;
        private string _jwtToken = "";  
        public SerwisRaportu()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Stale.URL_BAZY),
                Timeout = TimeSpan.FromSeconds(120)  // ✅ Zwiększony timeout dla Python service
            };
        }

        /// <summary>
        /// Zapisz raport lokalnie
        /// </summary>
        /// 
        public void UstawToken(string token)
    {
        _jwtToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
        public async Task<OdpowiedzRaportu> ZapiszRaportAsync(Raport raport)
        {
            try
            {
                // ✅ Walidacja
                if (string.IsNullOrWhiteSpace(raport?.Pesel))
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "❌ Błąd: Brakuje numeru PESEL"
                    };
                }

                DebugLog($"💾 Zapisuję raport dla: {raport.Pesel} - {raport.Imie} {raport.Nazwisko}");

                // ✅ Wyślij z options aby serializować na camelCase
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(raport, options);
                DebugLog($"📋 JSON do wysłania (camelCase):\n{json}");

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/reports/save", content);

                if (response.IsSuccessStatusCode)
                {
                    var wynik = await response.Content.ReadFromJsonAsync<OdpowiedzRaportu>();
                    DebugLog($"✅ Raport zapisany: {raport.Id}");
                    
                    return wynik ?? new OdpowiedzRaportu
                    {
                        Sukces = true,
                        Wiadomosc = "✅ Raport zapisany",
                        RaportId = raport.Id,
                        Raport = raport
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    DebugLog($"❌ Błąd serwera ({response.StatusCode}): {errorContent}");
                    
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = $"❌ Błąd serwera: {response.StatusCode}\n{errorContent}"
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                DebugLog($"🌐 Błąd połączenia: {httpEx.Message}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"🌐 Błąd połączenia:\n{httpEx.Message}\n\nSprawdź czy serwer działa na:\n{Stale.URL_BAZY}"
                };
            }
            catch (Exception ex)
            {
                DebugLog($"💥 Wyjątek: {ex.Message}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"💥 Błąd: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Wyślij raport do systemu (zapisz w bazie + wyślij do notyfikacji)
        /// </summary>
        public async Task<OdpowiedzRaportu> WyslijRaportAsync(Raport raport)
        {
            try
            {
                // ✅ Walidacja
                if (raport == null)
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "❌ Błąd: Raport jest pusty"
                    };
                }

                if (string.IsNullOrWhiteSpace(raport.Pesel))
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "❌ Błąd: Brakuje numeru PESEL\n\nDane nie została załadowana prawidłowo z rozpoznawania twarzy."
                    };
                }

                if (string.IsNullOrWhiteSpace(raport.Imie) || string.IsNullOrWhiteSpace(raport.Nazwisko))
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = $"❌ Błąd: Brakuje imienia lub nazwiska\n\nImie: {raport.Imie ?? "BRAK"}\nNazwisko: {raport.Nazwisko ?? "BRAK"}"
                    };
                }

                DebugLog($"📤 Wysyłam raport:");
                DebugLog($"   PESEL: {raport.Pesel}");
                DebugLog($"   Imie: {raport.Imie}");
                DebugLog($"   Nazwisko: {raport.Nazwisko}");
                DebugLog($"   CzyMandat: {raport.CzyMandat}");
                DebugLog($"   URL: {Stale.URL_BAZY}/api/reports/submit");

                // ✅ Wyślij z options aby serializować na camelCase
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(raport, options);
                DebugLog($"📋 JSON do wysłania (camelCase):\n{json}");

                // Zamiast PostAsJsonAsync, użyj ręcznego wysyłania z custom options
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/reports/submit", content);

                if (response.IsSuccessStatusCode)
                {
                    var wynik = await response.Content.ReadFromJsonAsync<OdpowiedzRaportu>();
                    DebugLog($"✅ Raport wysłany: {raport.Id}");
                    
                    return wynik ?? new OdpowiedzRaportu
                    {
                        Sukces = true,
                        Wiadomosc = "✅ Raport wysłany do systemu pomyślnie!",
                        RaportId = raport.Id,
                        Raport = raport
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    DebugLog($"❌ Błąd serwera ({response.StatusCode}):");
                    DebugLog($"   {errorContent}");
                    
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = $"❌ Błąd serwera {(int)response.StatusCode}\n\nOdpowiedź serwera:\n{errorContent}"
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                DebugLog($"🌐 Błąd połączenia: {httpEx.Message}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"🌐 Błąd połączenia:\n{httpEx.Message}\n\n⚠️ Serwer jest niedostępny na:\n{Stale.URL_BAZY}\n\nSprawdź czy:\n1. Node.js server jest uruchomiony\n2. Adres serwera jest prawidłowy\n3. Urządzenie ma dostęp do sieci"
                };
            }
            catch (TaskCanceledException tcEx)
            {
                DebugLog($"⏱️ Timeout: {tcEx.Message}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"⏱️ Upłynął limit czasu oczekiwania na serwer (120s)\n\nSerwer odpowiada zbyt wolno. Spróbuj ponownie."
                };
            }
            catch (Exception ex)
            {
                DebugLog($"💥 Wyjątek: {ex}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"💥 Błąd: {ex.Message}\n\nTyp: {ex.GetType().Name}"
                };
            }
        }

        /// <summary>
        /// Pobierz historię raportów dla osoby
        /// </summary>
        public async Task<OdpowiedzRaportu> PobierzRaportyAsync(string pesel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "❌ Błąd: Brakuje numeru PESEL"
                    };
                }

                DebugLog($"📥 Pobieram raporty dla PESEL: {pesel}");

                var response = await _httpClient.GetAsync($"/api/reports/person/{pesel}");

                if (response.IsSuccessStatusCode)
                {
                    var wynik = await response.Content.ReadFromJsonAsync<OdpowiedzRaportu>();
                    return wynik ?? new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "Brak raportów"
                    };
                }

                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = "Nie znaleziono raportów"
                };
            }
            catch (Exception ex)
            {
                DebugLog($"❌ Błąd pobierania raportów: {ex.Message}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"Błąd: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Pobierz raport po ID
        /// </summary>
        public async Task<OdpowiedzRaportu> PobierzRaportAsync(string raportId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(raportId))
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "❌ Błąd: Brakuje ID raportu"
                    };
                }

                var response = await _httpClient.GetAsync($"/api/reports/{raportId}");

                if (response.IsSuccessStatusCode)
                {
                    var wynik = await response.Content.ReadFromJsonAsync<OdpowiedzRaportu>();
                    return wynik ?? new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "Raport nie znaleziony"
                    };
                }

                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = "Raport nie istnieje"
                };
            }
            catch (Exception ex)
            {
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"Błąd: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Usuń raport
        /// </summary>
        public async Task<OdpowiedzRaportu> UsunRaportAsync(string raportId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(raportId))
                {
                    return new OdpowiedzRaportu
                    {
                        Sukces = false,
                        Wiadomosc = "❌ Błąd: Brakuje ID raportu"
                    };
                }

                DebugLog($"🗑️ Usuwam raport: {raportId}");

                var response = await _httpClient.DeleteAsync($"/api/reports/{raportId}");

                if (response.IsSuccessStatusCode)
                {
                    DebugLog($"✅ Raport usunięty");
                    return new OdpowiedzRaportu
                    {
                        Sukces = true,
                        Wiadomosc = "✅ Raport usunięty"
                    };
                }

                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = "❌ Nie można usunąć raportu"
                };
            }
            catch (Exception ex)
            {
                DebugLog($"❌ Błąd usuwania: {ex.Message}");
                return new OdpowiedzRaportu
                {
                    Sukces = false,
                    Wiadomosc = $"Błąd: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Debugowanie - wyświetl w Output
        /// </summary>
        private void DebugLog(string message)
        {
            Debug.WriteLine($"[SerwisRaportu] {message}");
#if DEBUG
            System.Diagnostics.Debugger.Log(0, "RaportyDebug", $"{message}\n");
#endif
        }
    }
}