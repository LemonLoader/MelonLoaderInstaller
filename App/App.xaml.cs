namespace MelonLoader.Installer.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

        Current!.UserAppTheme = AppTheme.Dark;
		UserAppTheme = AppTheme.Dark;

        MainPage = new AppShell();
	}
}
