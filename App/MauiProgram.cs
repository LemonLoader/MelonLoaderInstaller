using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using MelonLoader.Installer.App.Views;
using MauiIcons.SegoeFluent;
using MauiIcons.Fluent;

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
			.UseSegoeFluentMauiIcons()
            .UseFluentMauiIcons()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Inter-Regular.ttf", "InterRegular");
				fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
            });

        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(PermissionSetupPage), typeof(PermissionSetupPage));
        Routing.RegisterRoute(nameof(SelectADBDevicePage), typeof(SelectADBDevicePage));
        Routing.RegisterRoute(nameof(PatchAppPage), typeof(PatchAppPage));
        Routing.RegisterRoute(nameof(PatchingConsolePage), typeof(PatchingConsolePage));

        return builder.Build();
	}
}
