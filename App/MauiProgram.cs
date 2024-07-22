using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using MelonLoader.Installer.App.Views;

namespace MelonLoader.Installer.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitMarkup()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Inter-Regular.ttf", "InterRegular");
				fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
            });

        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(PermissionSetupPage), typeof(PermissionSetupPage));

        return builder.Build();
	}
}
