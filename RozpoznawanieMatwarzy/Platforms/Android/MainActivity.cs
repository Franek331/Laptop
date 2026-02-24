using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Microsoft.Maui;
using RozpoznawanieMatwarzy.Services;

namespace RozpoznawanieMatwarzy;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTop)] // ✅ WAŻNE dla NFC
public class MainActivity : MauiAppCompatActivity
{
    private NfcAdapter _nfcAdapter;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
    }

    protected override void OnResume()
    {
        base.OnResume();

        System.Diagnostics.Debug.WriteLine("=== OnResume called ===");
        System.Diagnostics.Debug.WriteLine($"NFC Adapter: {_nfcAdapter}");
        System.Diagnostics.Debug.WriteLine($"NFC Enabled: {_nfcAdapter?.IsEnabled}");

        // ✅ Włącz nasłuch NFC TYLKO gdy aplikacja jest aktywna (ForegroundDispatch)
        if (_nfcAdapter != null && _nfcAdapter.IsEnabled)
        {
            var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);

            // ✅ Najpierw spróbuj bez IntentFilters - to powinno złapać wszystkie tagi
            try
            {
                System.Diagnostics.Debug.WriteLine("Enabling NFC ForegroundDispatch...");
                _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, null, null);
                System.Diagnostics.Debug.WriteLine("NFC ForegroundDispatch enabled successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NFC EnableForegroundDispatch error: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("NFC not available or disabled");
        }
    }

    protected override void OnPause()
    {
        base.OnPause();

        // ✅ Wyłącz nasłuch gdy aplikacja nie jest aktywna
        if (_nfcAdapter != null)
        {
            try
            {
                _nfcAdapter.DisableForegroundDispatch(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NFC DisableForegroundDispatch error: {ex.Message}");
            }
        }
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        HandleNfcTag(intent);
    }

    private void HandleNfcTag(Intent intent)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== HandleNfcTag called ===");
            System.Diagnostics.Debug.WriteLine($"Intent: {intent}");
            System.Diagnostics.Debug.WriteLine($"Intent Action: {intent?.Action}");

            if (intent == null || intent.Action == null)
            {
                System.Diagnostics.Debug.WriteLine("Intent is null or has no action");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Received intent action: {intent.Action}");

            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;

            System.Diagnostics.Debug.WriteLine($"Tag: {tag}");

            if (tag == null)
            {
                System.Diagnostics.Debug.WriteLine("Tag is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"NFC Tag detected!");
            System.Diagnostics.Debug.WriteLine($"CzekaNaOdczyt: {SerwisNFC.CzekaNaOdczyt()}");
            System.Diagnostics.Debug.WriteLine($"CzekaNaZapis: {SerwisNFC.CzekaNaZapis()}");

            // ✅ Sprawdź czy czekamy na odczyt czy zapis
            if (SerwisNFC.CzekaNaOdczyt())
            {
                System.Diagnostics.Debug.WriteLine("Performing NFC READ");
                var result = SerwisNFC.OdczytajZTagu(tag);
                System.Diagnostics.Debug.WriteLine($"Read result: {result}");
            }
            else if (SerwisNFC.CzekaNaZapis())
            {
                System.Diagnostics.Debug.WriteLine("Performing NFC WRITE");
                var result = SerwisNFC.ZapiszDoTagu(tag);
                System.Diagnostics.Debug.WriteLine($"Write result: {result}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No NFC operation pending - nic się nie będzie działo!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HandleNfcTag error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}