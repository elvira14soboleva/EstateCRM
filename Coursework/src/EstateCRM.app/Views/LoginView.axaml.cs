using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateCRM.App.Services;

namespace EstateCRM.App.Views;

public partial class LoginView : UserControl
{
    private readonly TextBox _loginBox;
    private readonly TextBox _passwordBox;
    private readonly TextBlock _errorText;
    private readonly ProgressBar _loginProgress;
    private readonly Button _signInButton;
    private bool _isSigningIn;

    public Func<string, string, Task<bool>>? SignInAsync { get; set; }
    public event EventHandler? LoginSucceeded;

    public LoginView()
    {
        InitializeComponent();
        _loginBox = this.FindControl<TextBox>("LoginBox")!;
        _passwordBox = this.FindControl<TextBox>("PasswordBox")!;
        _errorText = this.FindControl<TextBlock>("ErrorText")!;
        _loginProgress = this.FindControl<ProgressBar>("LoginProgress")!;
        _signInButton = this.FindControl<Button>("SignInButton")!;
        Loaded += (_, _) => _loginBox.Focus();
    }

    private async void SignInClick(object? sender, RoutedEventArgs e)
    {
        if (_isSigningIn) return;
        _errorText.IsVisible = false;
        string login = _loginBox.Text?.Trim() ?? "";
        string password = _passwordBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(login))
        {
            ShowError("Введите логин.");
            _loginBox.Focus();
            return;
        }
        if (password.Length == 0)
        {
            ShowError("Введите пароль.");
            _passwordBox.Focus();
            return;
        }
        var authenticate = SignInAsync;
        if (authenticate is null)
        {
            ShowError("Проверка входа ещё не подключена.");
            return;
        }
        _isSigningIn = true;
        _signInButton.IsEnabled = false;
        _loginBox.IsEnabled = false;
        _passwordBox.IsEnabled = false;
        _loginProgress.IsVisible = true;
        bool success = false;
        try
        {
            success = await authenticate(login, password);
            if (!success)
                ShowError("Неверный логин или пароль, либо учётная запись неактивна.");
        }
        catch (LoginBlockedException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ShowError("Не удалось выполнить вход. Проверьте подключение к базе данных и повторите попытку.");
        }
        finally
        {
            _isSigningIn = false;
            _signInButton.IsEnabled = true;
            _loginBox.IsEnabled = true;
            _passwordBox.IsEnabled = true;
            _loginProgress.IsVisible = false;
        }
        if (success)
        {
            _passwordBox.Text = "";
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShowError(string message)
    {
        _errorText.Text = message;
        _errorText.IsVisible = true;
    }
}
