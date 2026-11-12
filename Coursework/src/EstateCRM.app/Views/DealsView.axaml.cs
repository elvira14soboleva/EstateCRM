using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EstateCRM.App.Services;
using Microsoft.EntityFrameworkCore;

namespace EstateCRM.App.Views;

public partial class DealsView : UserControl
{
    private readonly DealService _service = new();
    private bool _busy;
    private bool _started;
    private T Control<T>(string name) where T : Control => this.FindControl<T>(name)!;

    public DealsView()
    {
        AvaloniaXamlLoader.Load(this);
        Control<TextBox>("DateBox").Text = DateTime.Today.ToString("dd.MM.yyyy");
        Loaded += async (_, _) => { if (!_started) { _started = true; await RefreshAsync(); } };
    }

    private void Busy(bool busy)
    {
        _busy = busy;
        Control<StackPanel>("FormPanel").IsEnabled = !busy;
        Control<Button>("RefreshButton").IsEnabled = !busy;
        Control<StackPanel>("AdminPanel").IsEnabled = !busy && Session.Current?.RoleName == "Администратор";
        Control<ProgressBar>("LoadingBar").IsVisible = busy;
    }

    private void Message(string text, bool error)
    {
        var block = Control<TextBlock>("MessageText");
        block.Text = text;
        block.Classes.Set("error", error);
        block.IsVisible = true;
    }

    private async System.Threading.Tasks.Task RefreshAsync(string? afterSave = null)
    {
        if (_busy) return;
        Busy(true);
        Control<TextBlock>("MessageText").IsVisible = false;
        try
        {
            var deals = await _service.GetDealsAsync();
            var views = await _service.GetAvailableViewingsAsync();
            Control<ItemsControl>("DealsList").ItemsSource = deals;
            Control<ComboBox>("AdminDealBox").ItemsSource = deals;
            var box = Control<ComboBox>("ViewingBox");
            box.ItemsSource = views;
            box.SelectedIndex = -1;
            Control<TextBlock>("CountText").Text = $"Сделок: {deals.Count}. Доступных просмотров: {views.Count}.";
            if (afterSave != null) Message(afterSave, false);
            else if (views.Count == 0) Message("Нет проведённых просмотров без сделки. Обновите список после добавления или изменения просмотра.", false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Message(afterSave == null ? "Не удалось загрузить сделки. Проверьте подключение к базе данных."
                : afterSave + " Не удалось обновить список; нажмите «Обновить».", true);
            Control<TextBlock>("CountText").Text = "Список не обновлён.";
        }
        finally { Busy(false); }
    }

    private async void RefreshClick(object? sender, RoutedEventArgs e) => await RefreshAsync();

    private void ViewingChanged(object? sender, SelectionChangedEventArgs e)
    {
        var hint = this.FindControl<TextBlock>("ViewingHint");
        if (hint == null) return;
        var item = Control<ComboBox>("ViewingBox").SelectedItem as AvailableViewing;
        hint.Text = item is null
            ? "Выберите просмотр. Процент берётся из вида сделки объекта."
            : $"Вид сделки: {item.Type}. Комиссия: {item.Percent:0.##}% от суммы. При сохранении ставка будет проверена ещё раз.";
    }

    private async void ChangeDateClick(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (Control<ComboBox>("AdminDealBox").SelectedItem is not DealRow deal)
        { Message("Выберите сделку.", true); return; }
        if (!DateTime.TryParseExact(Control<TextBox>("AdminDateBox").Text?.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        { Message("Введите дату в формате дд.мм.гггг.", true); return; }
        Busy(true); bool saved = false;
        try { await _service.ChangeDateAsync(deal.Id, DateOnly.FromDateTime(date)); saved = true; }
        catch (DealValidationException ex) { Message(ex.Message, true); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Message("Ошибка изменения даты. Обновите список перед повтором.", true); }
        finally { Busy(false); }
        if (saved) await RefreshAsync("Дата сделки изменена.");
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        Control<TextBlock>("MessageText").IsVisible = false;
        if (Control<ComboBox>("ViewingBox").SelectedItem is not AvailableViewing view)
        { Message("Выберите проведённый просмотр.", true); return; }
        if (!DateTime.TryParseExact(Control<TextBox>("DateBox").Text?.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        { Message("Введите дату в формате дд.мм.гггг, например 15.11.2026.", true); return; }
        string number = Control<TextBox>("NumberBox").Text?.Trim() ?? "";
        string file = Control<TextBox>("FileBox").Text?.Trim() ?? "";
        string raw = (Control<TextBox>("AmountBox").Text ?? "").Trim().Replace("\u00a0", "").Replace("\u202f", "").Replace(" ", "").Replace(',', '.');
        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount))
        { Message("Введите сумму числом, например 1000000,00.", true); return; }
        Busy(true);
        bool saved = false;
        try
        {
            await _service.CreateAsync(view.Id, DateOnly.FromDateTime(date), number, file, amount);
            saved = true;
        }
        catch (DealValidationException ex) { Message(ex.Message, true); }
        catch (DbUpdateException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Message("Не удалось сохранить сделку. Возможно, номер договора или просмотр уже использован. Обновите список и проверьте данные.", true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Message("Ошибка сохранения. Проверьте подключение. Перед повтором обновите список: возможно, сделка уже записана.", true);
        }
        finally { Busy(false); }
        if (!saved) return;
        Control<TextBox>("NumberBox").Text = "";
        Control<TextBox>("FileBox").Text = "";
        Control<TextBox>("AmountBox").Text = "";
        await RefreshAsync("Сделка сохранена.");
    }
}
