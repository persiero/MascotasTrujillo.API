using Microsoft.Extensions.Logging;

namespace MascotasTrujillo.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps() // <--- ¡Añade esta línea!
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Registramos nuestro servicio para usarlo en cualquier pantalla
            builder.Services.AddSingleton<MascotasTrujillo.App.Services.ApiService>();

            builder.Services.AddSingleton<MascotasTrujillo.App.Services.ApiService>();
            builder.Services.AddTransient<MascotasTrujillo.App.Views.LoginPage>();

            return builder.Build();
        }
    }
}
