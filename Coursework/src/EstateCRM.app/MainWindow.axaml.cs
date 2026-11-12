using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateCRM.App.Data;
using EstateCRM.App.Services;
using EstateCRM.App.Views;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App;

public partial class MainWindow : Window
{
    private readonly AuthService _authService = new();
    private readonly Stack<UserControl> _history = new();
    private readonly ContentControl _pageHost;
    private readonly TextBlock _userText;
    private readonly Button _backButton;
    private readonly Button _catalogButton;
    private readonly Button _logoutButton;
    private readonly Button _dealsButton;
    private readonly Button _viewingsButton;
    private readonly Button _reportButton;

    public MainWindow()
    {
        InitializeComponent();
        _pageHost = this.FindControl<ContentControl>("PageHost")!;
        _userText = this.FindControl<TextBlock>("UserText")!;
        _backButton = this.FindControl<Button>("BackButton")!;
        _catalogButton = this.FindControl<Button>("CatalogButton")!;
        _logoutButton = this.FindControl<Button>("LogoutButton")!;
        _dealsButton = this.FindControl<Button>("DealsButton")!;
        _viewingsButton = this.FindControl<Button>("ViewingsButton")!;
        _reportButton = this.FindControl<Button>("ReportButton")!;
        ShowLogin();
    }

    private void ShowLogin()
    {
        Session.Clear();
        _history.Clear();
        var page = new LoginView
        {
            SignInAsync = _authService.SignInAsync
        };
        page.LoginSucceeded += (_, _) =>
        {
            if (Session.IsAuthenticated)
                Navigate(new PropertiesView(), remember: false);
        };
        _pageHost.Content = page;
        UpdateHeader();
    }

    private void Navigate(UserControl page, bool remember = true)
    {
        if (!Session.IsAuthenticated)
        {
            ShowLogin();
            return;
        }

        if (remember && _pageHost.Content is UserControl current
            && current is not LoginView)
            _history.Push(current);

        _pageHost.Content = page;
        UpdateHeader();
    }

    private void UpdateHeader()
    {
        _userText.Text = Session.Caption;
        _catalogButton.IsEnabled = Session.IsAuthenticated;
        _dealsButton.IsEnabled = Session.IsAuthenticated;
        _viewingsButton.IsEnabled = Session.IsAuthenticated;
        _reportButton.IsVisible = Session.IsAuthenticated && (Session.Current?.RoleName is "Руководитель" or "Администратор");
        _logoutButton.IsVisible = Session.IsAuthenticated;
        _backButton.IsEnabled = Session.IsAuthenticated && _history.Count > 0;
    }

    private void CatalogClick(object? sender, RoutedEventArgs e)
    {
        if (!Session.IsAuthenticated || _pageHost.Content is PropertiesView)
            return;
        Navigate(new PropertiesView());
    }

    private void ViewingsClick(object? sender, RoutedEventArgs e)
    {
        if (!Session.IsAuthenticated || _pageHost.Content is ViewingsView) return;
        Navigate(new ViewingsView());
    }

    private void DealsClick(object? sender, RoutedEventArgs e)
    {
        if (!Session.IsAuthenticated || _pageHost.Content is DealsView) return;
        Navigate(new DealsView());
    }

    private void ReportClick(object? sender, RoutedEventArgs e)
    {
        if (!Session.IsAuthenticated || Session.Current?.RoleName is not ("Руководитель" or "Администратор")
            || _pageHost.Content is ReportView) return;
        Navigate(new ReportView());
    }

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        if (!Session.IsAuthenticated || _history.Count == 0)
            return;
        _pageHost.Content = _history.Pop();
        UpdateHeader();
    }

    private void LogoutClick(object? sender, RoutedEventArgs e) => ShowLogin();

    
}
