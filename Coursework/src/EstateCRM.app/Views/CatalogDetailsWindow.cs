using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EstateCRM.App.Models;

namespace EstateCRM.App.Views;

public sealed class CatalogDetailsWindow : Window
{
    public CatalogDetailsWindow(PropertyCatalogItem item)
    {
        Title = $"EstateCRM · объект № {item.PropertyId}";
        Width = 680; Height = 720;
        MinWidth = 500; MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var body = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        body.Children.Add(Label(item.NumberText, 12));
        body.Children.Add(Label(item.Address, 24, true));
        body.Children.Add(Label(item.CategoryText, 15));
        var image = CatalogImages.Load(item.PhotoPath);
        if (image is not null)
            body.Children.Add(new Image { Source = image, Height = 220, Stretch = Stretch.Uniform });
        else
            body.Children.Add(Label("Фотография не добавлена", 14));
        body.Children.Add(Label(item.PriceText, 25, true));
        body.Children.Add(Label(item.FactsText, 15));
        body.Children.Add(Label("Район: " + item.DistrictName, 14));
        body.Children.Add(Label("Статус: " + item.StatusName, 14));
        body.Children.Add(Label("Этаж: " + item.FloorText, 14));
        body.Children.Add(Label("Добавлен: " + item.DateText, 14));
        body.Children.Add(Label("Описание", 18, true));
        body.Children.Add(Label(item.DescriptionText, 14));
        body.Children.Add(Label("Режим просмотра. Данные объекта не изменяются.", 12));
        var close = new Button
        {
            Content = "Закрыть", HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16), Padding = new Thickness(18, 10)
        };
        close.Click += (_, _) => Close();
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        layout.Children.Add(new ScrollViewer { Content = body });
        Grid.SetRow(close, 1);
        layout.Children.Add(close);
        Content = layout;
    }

    private static TextBlock Label(string text, double size, bool bold = false) => new()
    {
        Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap,
        FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal
    };
}
