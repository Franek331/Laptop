#if ANDROID
using Android.Nfc;
using Android.Nfc.Tech;
using AndroidNdefMessage = Android.Nfc.NdefMessage;
#endif
using NdefLibrary.Ndef;
using NdefLibraryMessage = NdefLibrary.Ndef.NdefMessage;
using RozpoznawanieMatwarzy.Models;
using System.Text;

namespace RozpoznawanieMatwarzy.Services
{
    public class SerwisNFC
    {
        private static TaskCompletionSource<DaneNFC> _odczytTcs;
        private static TaskCompletionSource<bool> _zapisTcs;
        private static string _daneDoZapisu;

        // ✅ FLAGI DO ŚLEDZENIA STANU
        private static bool _czekaNaOdczyt = false;
        private static bool _czekaNaZapis = false;

        public SerwisNFC()
        {
        }

        // ✅ Metody do sprawdzenia stanu (publiczne statyczne dla MainActivity)
        public static bool CzekaNaOdczyt() => _czekaNaOdczyt;
        public static bool CzekaNaZapis() => _czekaNaZapis;

        public bool CzyNfcDostepne()
        {
#if ANDROID
            var adapter = NfcAdapter.GetDefaultAdapter(Android.App.Application.Context);
            return adapter != null;
#else
            return false;
#endif
        }

        public bool CzyNfcWlaczone()
        {
#if ANDROID
            var adapter = NfcAdapter.GetDefaultAdapter(Android.App.Application.Context);
            return adapter?.IsEnabled ?? false;
#else
            return false;
#endif
        }

        // 📝 ZAPIS NA KARTĘ NFC
        public async Task<bool> ZapiszNaKarteAsync(DaneNFC dane, Action<string> onStatus)
        {
            try
            {
                if (!CzyNfcDostepne())
                {
                    onStatus?.Invoke("❌ NFC nie jest dostępne na tym urządzeniu");
                    return false;
                }

                if (!CzyNfcWlaczone())
                {
                    onStatus?.Invoke("⚠️ Włącz NFC w ustawieniach");
                    return false;
                }

                _zapisTcs = new TaskCompletionSource<bool>();
                _daneDoZapisu = dane.DoJson();
                _czekaNaZapis = true; // ✅ FLAGA

                onStatus?.Invoke("📡 Przybliż kartę NFC do urządzenia...");

                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(_zapisTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    onStatus?.Invoke("⏱️ Timeout - nie wykryto karty");
                    ZatrzymajNasluchiwanie();
                    return false;
                }

                var wynik = await _zapisTcs.Task;

                if (wynik)
                {
                    onStatus?.Invoke($"✅ Zapisano: {dane.Imie}");
                }
                else
                {
                    onStatus?.Invoke("❌ Błąd zapisu na kartę");
                }

                ZatrzymajNasluchiwanie();
                return wynik;
            }
            catch (Exception ex)
            {
                onStatus?.Invoke($"❌ Błąd: {ex.Message}");
                ZatrzymajNasluchiwanie();
                return false;
            }
        }

        // 📖 ODCZYT Z KARTY NFC
        public async Task<DaneNFC> OdczytajZKartyAsync(Action<string> onStatus)
        {
            try
            {
                if (!CzyNfcDostepne())
                {
                    onStatus?.Invoke("❌ NFC nie jest dostępne");
                    return null;
                }

                if (!CzyNfcWlaczone())
                {
                    onStatus?.Invoke("⚠️ Włącz NFC w ustawieniach");
                    return null;
                }

                _odczytTcs = new TaskCompletionSource<DaneNFC>();
                _czekaNaOdczyt = true; // ✅ FLAGA

                onStatus?.Invoke("📡 Przybliż kartę NFC do urządzenia...");

                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(_odczytTcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    onStatus?.Invoke("⏱️ Timeout - nie wykryto karty");
                    ZatrzymajNasluchiwanie();
                    return null;
                }

                var dane = await _odczytTcs.Task;

                if (dane != null)
                {
                    onStatus?.Invoke($"✅ Odczytano: {dane.Imie}");
                }
                else
                {
                    onStatus?.Invoke("❌ Nie można odczytać danych z karty");
                }

                ZatrzymajNasluchiwanie();
                return dane;
            }
            catch (Exception ex)
            {
                onStatus?.Invoke($"❌ Błąd: {ex.Message}");
                ZatrzymajNasluchiwanie();
                return null;
            }
        }

#if ANDROID
        // 🔧 ZAPIS DO TAGU
        public static bool ZapiszDoTagu(Tag tag)
        {
            try
            {
                if (string.IsNullOrEmpty(_daneDoZapisu) || _zapisTcs == null)
                    return false;

                var ndef = Ndef.Get(tag);
                if (ndef == null)
                {
                    _zapisTcs?.TrySetResult(false);
                    return false;
                }

                ndef.Connect();

                if (!ndef.IsWritable)
                {
                    ndef.Close();
                    _zapisTcs?.TrySetResult(false);
                    return false;
                }

                var ndefMessage = new NdefLibraryMessage();
                var textRecord = new NdefTextRecord
                {
                    Text = _daneDoZapisu,
                    LanguageCode = "en"
                };
                ndefMessage.Add(textRecord);

                byte[] messageBytes = ndefMessage.ToByteArray();
                var androidMessage = new AndroidNdefMessage(messageBytes);

                ndef.WriteNdefMessage(androidMessage);
                ndef.Close();

                _zapisTcs?.TrySetResult(true);
                return true;
            }
            catch (Exception)
            {
                _zapisTcs?.TrySetResult(false);
                return false;
            }
        }

        // 🔧 ODCZYT Z TAGU
        public static bool OdczytajZTagu(Tag tag)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[NFC] OdczytajZTagu started");
                
                if (_odczytTcs == null)
                {
                    System.Diagnostics.Debug.WriteLine("[NFC] _odczytTcs is null!");
                    return false;
                }

                var ndef = Ndef.Get(tag);
                if (ndef == null)
                {
                    System.Diagnostics.Debug.WriteLine("[NFC] Ndef.Get(tag) returned null!");
                    _odczytTcs?.TrySetResult(null);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("[NFC] Ndef object obtained, connecting...");
                ndef.Connect();
                
                var androidMessage = ndef.NdefMessage;
                System.Diagnostics.Debug.WriteLine($"[NFC] NdefMessage: {androidMessage}");
                
                ndef.Close();

                if (androidMessage?.GetRecords() == null || androidMessage.GetRecords().Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[NFC] No records found in message!");
                    _odczytTcs?.TrySetResult(null);
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[NFC] Found {androidMessage.GetRecords().Length} records");
                
                byte[] messageBytes = androidMessage.ToByteArray();
                System.Diagnostics.Debug.WriteLine($"[NFC] Message bytes: {messageBytes.Length} bytes");
                
                var ndefMessage = NdefLibraryMessage.FromByteArray(messageBytes);
                System.Diagnostics.Debug.WriteLine($"[NFC] NdefLibraryMessage: {ndefMessage}, Count: {ndefMessage?.Count}");

                if (ndefMessage != null && ndefMessage.Count > 0)
                {
                    var record = ndefMessage[0];
                    System.Diagnostics.Debug.WriteLine($"[NFC] Record type: {record.GetType().Name}");

                    if (record is NdefTextRecord textRecord)
                    {
                        string jsonData = textRecord.Text;
                        System.Diagnostics.Debug.WriteLine($"[NFC] Text data (NdefTextRecord): {jsonData}");
                        
                        var dane = DaneNFC.ZJson(jsonData);
                        System.Diagnostics.Debug.WriteLine($"[NFC] Parsed DaneNFC: {dane}");
                        
                        _odczytTcs?.TrySetResult(dane);
                        return dane != null;
                    }
                    else if (record is NdefLibrary.Ndef.NdefRecord genericRecord)
                    {
                        // ✅ Jeśli jest generycznym NdefRecord, spróbuj wyciągnąć payload
                        System.Diagnostics.Debug.WriteLine($"[NFC] Record is generic NdefRecord (NdefLibrary)");
                        System.Diagnostics.Debug.WriteLine($"[NFC] TNF: {genericRecord.TypeNameFormat}");
                        System.Diagnostics.Debug.WriteLine($"[NFC] Type: {System.Text.Encoding.UTF8.GetString(genericRecord.Type ?? new byte[0])}");
                        
                        try
                        {
                            // Payload zawiera dane tekstu
                            if (genericRecord.Payload != null && genericRecord.Payload.Length > 0)
                            {
                                // Format NDEF Text Record: [lang_code_length][lang_code][text]
                                // Dla prostoty, weź wszystko po pierwszych bajtach
                                byte[] payload = genericRecord.Payload;
                                
                                // Pomiń pierwszy bajt (language code length)
                                int langCodeLength = payload[0] & 0x3F;
                                int textStart = 1 + langCodeLength;
                                
                                if (textStart < payload.Length)
                                {
                                    string jsonData = System.Text.Encoding.UTF8.GetString(payload, textStart, payload.Length - textStart);
                                    System.Diagnostics.Debug.WriteLine($"[NFC] Extracted text data: {jsonData}");
                                    
                                    var dane = DaneNFC.ZJson(jsonData);
                                    System.Diagnostics.Debug.WriteLine($"[NFC] Parsed DaneNFC from generic record: {dane}");
                                    
                                    _odczytTcs?.TrySetResult(dane);
                                    return dane != null;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[NFC] Error parsing generic record: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[NFC] Record is unknown type: {record.GetType().Name}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[NFC] ndefMessage is null or empty");
                }

                _odczytTcs?.TrySetResult(null);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NFC] Exception in OdczytajZTagu: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NFC] StackTrace: {ex.StackTrace}");
                _odczytTcs?.TrySetResult(null);
                return false;
            }
        }
#endif

        public void ZatrzymajNasluchiwanie()
        {
            _odczytTcs?.TrySetResult(null);
            _zapisTcs?.TrySetResult(false);
            _odczytTcs = null;
            _zapisTcs = null;
            _daneDoZapisu = null;
            _czekaNaOdczyt = false; // ✅ RESET FLAGI
            _czekaNaZapis = false;  // ✅ RESET FLAGI
        }
    }
}