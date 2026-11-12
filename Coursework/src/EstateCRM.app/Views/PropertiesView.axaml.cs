using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using EstateCRM.App.Models;
using EstateCRM.App.Services;

namespace EstateCRM.App.Views;

public partial class PropertiesView : UserControl
{
    private readonly PropertyService _service = new();
    private bool _busy;
    private bool _started;
    private bool _filtersLoaded;
    private T Control<T>(string name) where T : Control => this.FindControl<T>(name)!;

    public PropertiesView()
    {
        // Не зависит от генерации метода InitializeComponent редактором Avalonia.
        AvaloniaXamlLoader.Load(this);
        Loaded += async (_, _) =>
        {
            if (_started) return;
            _started = true;
            await ReloadAsync();
        };
        // Кэш не очищается при обычном переходе, пока картинки ещё могут отображаться.
        // Все ресурсы освобождаются при закрытии окна приложения.
        AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                owner.Closed -= OwnerClosed;
                owner.Closed += OwnerClosed;
            }
        };
    }

    private void OwnerClosed(object? sender, EventArgs e) => CatalogImages.Clear();

    private async void ApplyClick(object? sender, RoutedEventArgs e) => await ReloadAsync();

    private async void ResetClick(object? sender, RoutedEventArgs e)
    {
        if (_busy) return;
        Control<TextBox>("SearchBox").Text = "";
        Control<TextBox>("MinPriceBox").Text = "";
        Control<TextBox>("MaxPriceBox").Text = "";
        Control<ComboBox>("TypeBox").SelectedIndex = 0;
        Control<ComboBox>("DistrictBox").SelectedIndex = 0;
        Control<ComboBox>("DealBox").SelectedIndex = 0;
        Control<ComboBox>("StatusBox").SelectedIndex = 0;
        Control<ComboBox>("SortBox").SelectedIndex = 0;
        await ReloadAsync();
    }

    private static decimal? ParsePrice(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string text = value.Trim().Replace(" ", "").Replace("\u00a0", "")
            .Replace("\u202f", "").Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out decimal price)
            || price < 0 || price > 999999999999.99m)
            throw new ArgumentException($"Поле «{field}»: введите неотрицательную цену, например 6500000.");
        return price;
    }

    private int? SelectedId(string name) =>
        Control<ComboBox>(name).SelectedItem is PropertyCatalogOption { Id: > 0 } item
            ? item.Id : null;

    private async Task ReloadAsync()
    {
        if (_busy) return;
        decimal? min, max;
        try
        {
            min = ParsePrice(Control<TextBox>("MinPriceBox").Text, "Цена от");
            max = ParsePrice(Control<TextBox>("MaxPriceBox").Text, "Цена до");
            if (min.HasValue && max.HasValue && min > max)
                throw new ArgumentException("Минимальная цена не должна превышать максимальную.");
        }
        catch (ArgumentException ex)
        {
            ShowMessage(ex.Message, true);
            return;
        }

        _busy = true;
        Control<StackPanel>("FilterPanel").IsEnabled = false;
        Control<Button>("RefreshButton").IsEnabled = false;
        Control<ProgressBar>("LoadingBar").IsVisible = true;
        Control<Border>("MessagePanel").IsVisible = false;
        Control<TextBlock>("CountText").Text = "Загрузка объектов…";
        Control<ItemsControl>("CardsList").ItemsSource = null;
        try
        {
            if (!_filtersLoaded)
            {
                var filters = await _service.GetFiltersAsync();
                Control<ComboBox>("TypeBox").ItemsSource = filters.Types;
                Control<ComboBox>("DistrictBox").ItemsSource = filters.Districts;
                Control<ComboBox>("DealBox").ItemsSource = filters.DealTypes;
                Control<ComboBox>("StatusBox").ItemsSource = filters.Statuses;
                Control<ComboBox>("TypeBox").SelectedIndex = 0;
                Control<ComboBox>("DistrictBox").SelectedIndex = 0;
                Control<ComboBox>("DealBox").SelectedIndex = 0;
                Control<ComboBox>("StatusBox").SelectedIndex = 0;
                _filtersLoaded = true;
            }
            var filter = new PropertyCatalogQuery
            {
                Address = Control<TextBox>("SearchBox").Text?.Trim() ?? "",
                TypeId = SelectedId("TypeBox"), DistrictId = SelectedId("DistrictBox"),
                DealTypeId = SelectedId("DealBox"), StatusId = SelectedId("StatusBox"), PriceMin = min, PriceMax = max,
                Sort = Control<ComboBox>("SortBox").SelectedIndex
            };
            var items = await _service.GetCatalogAsync(filter);
            Control<ItemsControl>("CardsList").ItemsSource = items;
            Control<TextBlock>("CountText").Text = $"Найдено объектов: {items.Count}";
            if (items.Count == 0)
                ShowMessage("Объекты не найдены. Измените условия или нажмите «Сбросить». Если список остаётся пустым, проверьте наличие объектов в БД.", false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Control<TextBlock>("CountText").Text = "Каталог не загружен";
            ShowMessage("Не удалось загрузить каталог. Проверьте подключение к EstateCRM и соответствие модели БД. После исправления нажмите «Обновить».", true);
        }
        finally
        {
            Control<ProgressBar>("LoadingBar").IsVisible = false;
            Control<StackPanel>("FilterPanel").IsEnabled = true;
            Control<Button>("RefreshButton").IsEnabled = true;
            _busy = false;
        }
    }

    private void ShowMessage(string text, bool error)
    {
        var message = Control<TextBlock>("MessageText");
        message.Text = text;
        message.Foreground = error ? Brushes.Firebrick : Brushes.SlateGray;
        Control<Border>("MessagePanel").IsVisible = true;
    }

    private async void DetailsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PropertyCatalogItem item }
            && TopLevel.GetTopLevel(this) is Window owner)
            await new CatalogDetailsWindow(item).ShowDialog(owner);
    }
}
