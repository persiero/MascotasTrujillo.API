using Microsoft.Extensions.Logging;
using MascotasTrujillo.App.Services;
using MascotasTrujillo.App.Views;

namespace MascotasTrujillo.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps() // <--- Mantenemos tu configuración de mapas
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // 1. Registramos nuestro servicio para usarlo en cualquier pantalla (solo una vez)
            builder.Services.AddSingleton<ApiService>();

            // 2. Registramos el contenedor principal de pestañas
            builder.Services.AddSingleton<AppShell>();

            // 3. Registramos TODAS las pantallas para la Inyección de Dependencias
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RadarPage>();
            builder.Services.AddTransient<MisReportesPage>();
            builder.Services.AddTransient<MascotaDetailPage>();
            builder.Services.AddTransient<RegistroPage>();
            builder.Services.AddTransient<RegistrarMascotaPage>();
            builder.Services.AddTransient<MisMascotasPage>();

            return builder.Build();
        }
    }
}