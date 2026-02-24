using Microsoft.Extensions.Logging;
using RozpoznawanieMatwarzy.Services;
using RozpoznawanieMatwarzy.Views;

namespace RozpoznawanieMatwarzy
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<SerwisApiTwarzy>();
            builder.Services.AddSingleton<SerwisNFC>();
            builder.Services.AddSingleton<SerwisAutoryzacji>();

            builder.Services.AddTransient<StronaRejestracji>();
            builder.Services.AddTransient<StronaRozpoznawania>();
            builder.Services.AddTransient<StronaLogowania>();
            builder.Services.AddTransient<StronaRaportu>();




#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
