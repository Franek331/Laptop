using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RozpoznawanieMatwarzy.Models;

namespace RozpoznawanieMatwarzy.Services
{
    public class SerwisApiTwarzy
    {
        private readonly HttpClient _httpClient;
        private readonly SerwisAutoryzacji _serwisAutoryzacji;

        public SerwisApiTwarzy()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Stale.URL_BAZY),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            _serwisAutoryzacji = new SerwisAutoryzacji();
        }

        // Dodaj token do requestu
        private async Task DodajTokenDoRequestu()
        {
            var token = await _serwisAutoryzacji.PobierzTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        // Rejestracja twarzy z tokenem autoryzacji
        public async Task<OdpowiedzRejestracji> ZarejestrujTwarzAsync(
            string imie, string nazwisko, string pesel, DateTime dataUrodzenia, string plec, byte[] zdjecieBytes)
        {
            try
            {
                await DodajTokenDoRequestu();

                var content = new MultipartFormDataContent();

                content.Add(new StringContent(imie), "firstName");
                content.Add(new StringContent(nazwisko), "lastName");
                content.Add(new StringContent(pesel), "pesel");
                content.Add(new StringContent(dataUrodzenia.ToString("yyyy-MM-dd")), "dateOfBirth");
                content.Add(new StringContent(plec), "gender");

                var imageContent = new ByteArrayContent(zdjecieBytes);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "photo", "photo.jpg");

                var response = await _httpClient.PostAsync(Stale.ENDPOINT_REJESTRACJA, content);

                if (response.IsSuccessStatusCode)
                {
                    var wynik = await response.Content.ReadFromJsonAsync<OdpowiedzRejestracji>();
                    return wynik ?? new OdpowiedzRejestracji
                    {
                        Sukces = false,
                        Wiadomosc = "Błąd deserializacji odpowiedzi"
                    };
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new OdpowiedzRejestracji
                    {
                        Sukces = false,
                        Wiadomosc = "Sesja wygasła. Zaloguj się ponownie."
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new OdpowiedzRejestracji
                    {
                        Sukces = false,
                        Wiadomosc = $"Błąd serwera: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OdpowiedzRejestracji
                {
                    Sukces = false,
                    Wiadomosc = $"Błąd: {ex.Message}"
                };
            }
        }

        // Rozpoznawanie twarzy (bez tokenu - publiczny endpoint)
        public async Task<OdpowiedzRozpoznania> RozpoznajTwarzAsync(byte[] zdjecieBytes)
        {
            try
            {
                var content = new MultipartFormDataContent();

                var imageContent = new ByteArrayContent(zdjecieBytes);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "photo", "photo.jpg");

                var response = await _httpClient.PostAsync(Stale.ENDPOINT_ROZPOZNANIE, content);

                if (response.IsSuccessStatusCode)
                {
                    var wynik = await response.Content.ReadFromJsonAsync<OdpowiedzRozpoznania>();
                    return wynik ?? new OdpowiedzRozpoznania
                    {
                        Rozpoznano = false,
                        Wiadomosc = "Błąd deserializacji"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new OdpowiedzRozpoznania
                    {
                        Rozpoznano = false,
                        Wiadomosc = $"Błąd: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new OdpowiedzRozpoznania
                {
                    Rozpoznano = false,
                    Wiadomosc = $"Błąd: {ex.Message}"
                };
            }
        }

        // Pobierz listę osób
        public async Task<List<OsobaZarejestrowana>> PobierzListeTwarzyAsync()
        {
            try
            {
                await DodajTokenDoRequestu();
                var response = await _httpClient.GetAsync(Stale.ENDPOINT_LISTA_TWARZY);

                if (response.IsSuccessStatusCode)
                {
                    var lista = await response.Content.ReadFromJsonAsync<List<OsobaZarejestrowana>>();
                    return lista ?? new List<OsobaZarejestrowana>();
                }

                return new List<OsobaZarejestrowana>();
            }
            catch
            {
                return new List<OsobaZarejestrowana>();
            }
        }

        // Pobierz szczegóły osoby
        public async Task<OsobaZarejestrowana> PobierzOsobeAsync(string pesel)
        {
            try
            {
                await DodajTokenDoRequestu();
                var response = await _httpClient.GetAsync($"{Stale.URL_BAZY}/api/faces/{pesel}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var document = System.Text.Json.JsonDocument.Parse(json);
                    var osoba = document.RootElement.GetProperty("Osoba");

                    return new OsobaZarejestrowana
                    {
                        Pesel = osoba.GetProperty("Pesel").GetString(),
                        Imie = osoba.GetProperty("Imie").GetString(),
                        Nazwisko = osoba.GetProperty("Nazwisko").GetString(),
                        DataUrodzenia = DateTime.Parse(osoba.GetProperty("DataUrodzenia").GetString()),
                        Plec = osoba.GetProperty("Plec").GetString(),
                        SciezkaZdjecia = osoba.GetProperty("SciezkaZdjecia").GetString(),
                        DataZarejestrowania = DateTime.Parse(osoba.GetProperty("DataRejestracji").GetString())
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<StatusBezpieczenstwa> PobierzStatusBezpieczenstwa(string pesel)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/security/status/{pesel}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        var status = doc.RootElement.GetProperty("Status");
                        return new StatusBezpieczenstwa
                        {
                            Poszukiwany = status.GetProperty("Poszukiwany").GetBoolean(),
                            Zastrzeżony = status.GetProperty("Zastrzeżony").GetBoolean(),
                            Powód = status.TryGetProperty("Powód", out var reason)
                                ? reason.GetString()
                                : "",
                            KolorAlertu = status.GetProperty("KolorAlertu").GetString()
                        };
                    }
                }
                return new StatusBezpieczenstwa { KolorAlertu = "green" };
            }
            catch
            {
                return new StatusBezpieczenstwa { KolorAlertu = "green" };
            }
        }

        public async Task<StatusNFC> PobierzStatusNFC(string pesel)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/nfc/status/{pesel}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        var nfc = doc.RootElement.GetProperty("NFC");
                        return new StatusNFC
                        {
                            Zarejestrowany = nfc.GetProperty("Zarejestrowany").GetBoolean(),
                            Aktywny = nfc.GetProperty("Aktywny").GetBoolean(),
                            NfcUid = nfc.TryGetProperty("NfcUid", out var uid)
                                ? uid.GetString()
                                : null
                        };
                    }
                }
                return new StatusNFC { Zarejestrowany = false, Aktywny = false };
            }
            catch
            {
                return new StatusNFC { Zarejestrowany = false, Aktywny = false };
            }
        }

        // Usuń osobę
        public async Task<bool> UsunOsobeAsync(string pesel)
        {
            try
            {
                await DodajTokenDoRequestu();
                var response = await _httpClient.DeleteAsync($"{Stale.URL_BAZY}/api/faces/{pesel}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}