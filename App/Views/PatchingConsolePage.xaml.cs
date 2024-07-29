namespace MelonLoader.Installer.App.Views;

public partial class PatchingConsolePage : ContentPage
{
	public string Log { get => LogLabel.Text; set => LogLabel.Text = value; }

	public PatchingConsolePage()
	{
		InitializeComponent();
	}

    protected override bool OnBackButtonPressed()
    {
		return false;
    }
}