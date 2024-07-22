using MelonLoader.Installer.App.Utils;
using MelonLoader.Installer.App.ViewModels;

namespace MelonLoader.Installer.App.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        MainPageViewModel viewModel = new();
        BindingContext = viewModel;

        viewModel.OnAppAddingComplete += () =>
        {
            HeaderGrid.Remove(LoadingLabel);
            HeaderGrid.RowDefinitions = [new(new(2, GridUnitType.Star)), new(new(1, GridUnitType.Star))]; // "2*, *"
        };
    }
}