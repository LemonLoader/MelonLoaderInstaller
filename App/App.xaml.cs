using UraniumUI.Material.Resources;

namespace MelonLoader.Installer.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}
}
