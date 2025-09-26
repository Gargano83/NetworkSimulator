using Microsoft.Extensions.Logging;
using NetworkSimulator.Desktop.Services;

namespace NetworkSimulator.Desktop
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
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<AnalysisStateService>();
            builder.Services.AddSingleton<SimulationService>();
            builder.Services.AddSingleton<MlNetRoutingAgent>();

            return builder.Build();
        }
    }
}
