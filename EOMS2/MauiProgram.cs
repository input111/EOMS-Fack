using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace EOMS2;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		
        builder
            .UseMauiApp<App>()
            .UseLocalNotification()    // 添加这行
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
  
        
#if DEBUG
		builder.Logging.AddDebug();
#endif

        return builder.Build();
	}
}
