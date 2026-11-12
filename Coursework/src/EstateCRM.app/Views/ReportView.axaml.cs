using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EstateCRM.App.Services;

namespace EstateCRM.App.Views;

public partial class ReportView : UserControl
{
    private readonly ReportService _service = new();
    private bool _busy;
    private T Control<T>(string name) where T : Control => this.FindControl<T>(name)!;

    public ReportView()
    {
        AvaloniaXamlLoader.Load(this);
        var today = DateTime.Today;
        Control<TextBox>("FromBox").Text = new DateTime(today.Year, today.Month, 1).ToString("dd.MM.yyyy");
        Control<TextBox>("ToBox").Text = today.ToString("dd.MM.yyyy");
    }

    private void Message(string value, bool error)
    {
        var message = Control<TextBlock>("MessageText");
        message.Text = value;
        message.Classes.Set("error", error);
        message.IsVisible = true;
    }

    private async void BuildClick(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        Control<Border>("ResultPanel").IsVisible = false;
        Control<TextBlock>("MessageText").IsVisible = false;
        var fromText = Control<TextBox>("FromBox").Text?.Trim();
        var toText = Control<TextBox>("ToBox").Text?.Trim();
        if (!DateTime.TryParseExact(fromText, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            || !DateTime.TryParseExact(toText, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        { Message("Введите обе даты в формате дд.мм.гггг.", true); return; }
        var from = DateOnly.FromDateTime(start);
        var to = DateOnly.FromDateTime(end);
        try { ReportService.ValidatePeriod(from, to); }
        catch (DealValidationException ex) { Message(ex.Message, true); return; }
        _busy = true;
        Control<Button>("BuildButton").IsEnabled = false;
        Control<TextBox>("FromBox").IsEnabled = false;
        Control<TextBox>("ToBox").IsEnabled = false;
        Control<ProgressBar>("LoadingBar").IsVisible = true;
        try
        {
            var result = await _service.GetSummaryAsync(from, to);
            Control<TextBlock>("PeriodText").Text = $"Период: {from:dd.MM.yyyy} — {to:dd.MM.yyyy}";
            Control<TextBlock>("CountText").Text = $"Количество сделок: {result.Count}";
            Control<TextBlock>("AmountText").Text = $"Общая сумма сделок: {result.TotalAmount:N2} ₽";
            Control<TextBlock>("CommissionText").Text = $"Общая комиссия: {result.TotalCommission:N2} ₽";
            Control<Border>("ResultPanel").IsVisible = true;
            if (result.Count == 0) Message("За выбранный период сделок нет.", false);
        }
        catch (DealValidationException ex) { Message(ex.Message, true); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Message("Не удалось получить сводку. Проверьте подключение к базе данных и повторите попытку.", true);
        }
        finally
        {
            _busy = false;
            Control<Button>("BuildButton").IsEnabled = true;
            Control<TextBox>("FromBox").IsEnabled = true;
            Control<TextBox>("ToBox").IsEnabled = true;
            Control<ProgressBar>("LoadingBar").IsVisible = false;
        }
    }
}
