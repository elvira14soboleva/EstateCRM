using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EstateCRM.App.Services;

namespace EstateCRM.App.Views;

public partial class ViewingsView : UserControl
{
    private readonly ViewingService _service = new();
    private bool _busy, _started;
    private int? _matchedRequest;
    private T C<T>(string name) where T : Control => this.FindControl<T>(name)!;
    public ViewingsView()
    {
        AvaloniaXamlLoader.Load(this);
        C<TextBox>("DateTimeBox").Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy HH:mm");
        Loaded += async (_, _) => { if (!_started) { _started = true; await ReloadAsync(); } };
    }
    private void SetBusy(bool value)
    {
        _busy = value; C<ProgressBar>("LoadingBar").IsVisible = value;
        C<Button>("RefreshButton").IsEnabled = !value;
        C<StackPanel>("MatchPanel").IsEnabled = !value; C<StackPanel>("StatusPanel").IsEnabled = !value;
        bool edit = Session.Current?.RoleName is "Администратор" or "Риелтор";
        C<Button>("CreateButton").IsEnabled = !value &&
     (Session.Current?.RoleName is "Администратор" or "Руководитель" or "Риелтор");
        C<Button>("CompleteButton").IsEnabled = !value &&
    (Session.Current?.RoleName is "Администратор" or "Руководитель" or "Риелтор");
        C<Button>("CancelButton").IsEnabled = !value &&
    (Session.Current?.RoleName is "Администратор" or "Руководитель" or "Риелтор");
    }
    private void Message(string message, bool error)
    {
        var text = C<TextBlock>("MessageText"); text.Text = message; text.Classes.Set("error", error); text.IsVisible = true;
    }
    private async Task ReloadAsync(string? success = null)
    {
        if (_busy) return; SetBusy(true); C<TextBlock>("MessageText").IsVisible = false;
        try
        {
            var requests = await _service.RequestsAsync(); var items = await _service.ListAsync();
            C<ComboBox>("RequestBox").ItemsSource = requests; C<ComboBox>("RequestBox").SelectedIndex = -1;
            C<ComboBox>("ScheduledBox").ItemsSource = items.Where(v => v.Status == "Запланирован" && !v.HasDeal).ToList();
            C<ItemsControl>("ViewingList").ItemsSource = items;
            C<TextBlock>("CountText").Text = $"Просмотров: {items.Count} · активных заявок: {requests.Count}";
            if (success != null) Message(success, false);
        }
        catch (DealValidationException ex) { Message(ex.Message, true); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Message((success == null ? "Не удалось загрузить данные." : success + " Не удалось обновить список.") + " Проверьте подключение.", true); }
        finally { SetBusy(false); }
    }
    private async void RefreshClick(object? s, RoutedEventArgs e) => await ReloadAsync();
    private void RequestChanged(object? s, SelectionChangedEventArgs e)
    {
        var hint = this.FindControl<TextBlock>("RequestHint"); var box = this.FindControl<ComboBox>("PropertyBox");
        if (hint == null || box == null) return;
        var request = C<ComboBox>("RequestBox").SelectedItem as RequestOption;
        hint.Text = request?.Conditions ?? "Условия заявки появятся после выбора.";
        box.ItemsSource = null; _matchedRequest = null;
    }
    private async void MatchClick(object? s, RoutedEventArgs e)
    {
        if (_busy) return;
        if (C<ComboBox>("RequestBox").SelectedItem is not RequestOption req) { Message("Выберите заявку.", true); return; }
        SetBusy(true); C<TextBlock>("MessageText").IsVisible = false;
        try
        {
            var matches = await _service.MatchesAsync(req.Id); C<ComboBox>("PropertyBox").ItemsSource = matches;
            _matchedRequest = req.Id; C<TextBlock>("MatchHint").Text = $"Подходящих активных объектов: {matches.Count}.";
        }
        catch (DealValidationException ex) { Message(ex.Message, true); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Message("Не удалось выполнить подбор. Проверьте подключение.", true); }
        finally { SetBusy(false); }
    }
    private async void CreateClick(object? s, RoutedEventArgs e)
    {
        if (_busy) return;
        if (C<ComboBox>("RequestBox").SelectedItem is not RequestOption req || C<ComboBox>("PropertyBox").SelectedItem is not MatchedProperty p || _matchedRequest != req.Id)
        { Message("Выберите заявку и подходящий объект после поиска.", true); return; }
        if (!DateTime.TryParseExact(C<TextBox>("DateTimeBox").Text?.Trim(), "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        { Message("Введите дату и время в формате дд.мм.гггг чч:мм.", true); return; }
        await ExecuteAsync(() => _service.CreateAsync(req.Id, p.Id, time), "Просмотр назначен.");
    }
    private async void CompleteClick(object? s, RoutedEventArgs e) => await ChangeStatus("Проведён");
    private async void CancelClick(object? s, RoutedEventArgs e) => await ChangeStatus("Отменён");
    private async Task ChangeStatus(string status)
    {
        if (_busy) return;
        if (C<ComboBox>("ScheduledBox").SelectedItem is not ViewingRow row) { Message("Выберите запланированный просмотр.", true); return; }
        var comment = C<TextBox>("CommentBox").Text ?? "";
        await ExecuteAsync(() => _service.SetStatusAsync(row.Id, status, comment), $"Статус просмотра изменён: {status}.");
    }
    private async Task ExecuteAsync(Func<Task> action, string success)
    {
        SetBusy(true); C<TextBlock>("MessageText").IsVisible = false; bool saved = false;
        try { await action(); saved = true; }
        catch (DealValidationException ex) { Message(ex.Message, true); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Message("Ошибка сохранения. Проверьте подключение и обновите список перед повтором.", true); }
        finally { SetBusy(false); }
        if (saved) { C<TextBox>("CommentBox").Text = ""; await ReloadAsync(success); }
    }
}
