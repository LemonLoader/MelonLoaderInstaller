﻿using MelonLoader.Installer.App.Utils;
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
            LoadingLabel.IsVisible = false;
            HeaderGrid.RowDefinitions = [new(new(2, GridUnitType.Star)), new(GridLength.Star)]; // "2*, *"
        };

        viewModel.OnAppAddingReset += () =>
        {
            LoadingLabel.IsVisible = true;
            var twoStar = new RowDefinition(new(2, GridUnitType.Star));
            HeaderGrid.RowDefinitions = [twoStar, twoStar, twoStar, new(GridLength.Star)]; // "2*, 2*, 2*, *"
        };

        if (!AndroidPermissionHandler.HaveRequired())
        {
            Shell.Current.GoToTabOnFirst(nameof(PermissionSetupPage));
            return;
        }
    }
}