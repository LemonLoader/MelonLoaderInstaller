using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App.Views;

public partial class PatchingConsolePage : ContentPage
{
    public string Log
    {
        get => LogLabel.Text;
        set
        {
            LogLabel.Text = value;
            LogScrollView.ScrollToAsync(LogLabel, ScrollToPosition.End, false);
        }
    }

    public string TopTitle
    {
        get => TitleLabel.Text;
        set => TitleLabel.Text = value;
    }

    public bool BackButtonVisible { get => GoBackButton.IsVisible; set => GoBackButton.IsVisible = value; }

    private Command _goBackCommand;

    public PatchingConsolePage()
    {
        InitializeComponent();
        _goBackCommand = new(GoBack);

        GoBackButton.IsVisible = false;
        GoBackButton.GestureRecognizers.Add(new TapGestureRecognizer()
        {
            Command = _goBackCommand,
        });
    }

    private async void GoBack()
    {
        Shell.Current.GoToTabOnFirst(nameof(PatchAppPage));
        await Navigation.PopAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        return false;
    }
}