using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SongPrompter.Services;
using SongPrompter.ViewModels;

namespace SongPrompter
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Workaround for issue: https://github.com/dotnet/maui/issues/11662
            builder.ConfigureMauiHandlers(_ =>
                {
                    LabelHandler.Mapper.AppendToMapping(nameof(View.BackgroundColor), (handler, View) => handler.UpdateValue(nameof(IView.Background)));
                    ButtonHandler.Mapper.AppendToMapping(nameof(View.BackgroundColor), (handler, View) => handler.UpdateValue(nameof(IView.Background)));
                });

            builder.Services
                .AddSingleton<IDataService, DataService>()
                .AddSingleton<MainPage>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<SongPage>()
                .AddSingleton<SongViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}