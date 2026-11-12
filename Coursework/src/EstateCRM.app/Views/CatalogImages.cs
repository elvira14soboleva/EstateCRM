using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace EstateCRM.App.Views;

public sealed class CatalogPhotoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => CatalogImages.Load(value as string);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

internal static class CatalogImages
{
    private static readonly Dictionary<string, Bitmap> Cache = new();
    private const string Root = "avares://EstateCRM.App/Assets/";
    public static Bitmap? Load(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                string normalized = path.Trim().Replace('\\', '/');
                if (Path.IsPathRooted(path))
                    return ReadFile(path) ?? Placeholder();
                if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                {
                    if (uri.IsFile) return ReadFile(uri.LocalPath) ?? Placeholder();
                    if (normalized.StartsWith(Root, StringComparison.Ordinal))
                        return ReadAsset(normalized) ?? Placeholder();
                    return Placeholder(); // Сеть не используется.
                }
                var external = ReadFile(Path.Combine(AppContext.BaseDirectory, normalized));
                if (external is not null) return external;
                string name = Path.GetFileName(normalized);
                return ReadAsset(Root + "Images/" + name) ?? Placeholder();
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        return Placeholder();
    }

    private static Bitmap? Placeholder() => ReadAsset(Root + "picture.png");

    private static Bitmap? ReadAsset(string name)
    {
        if (Cache.TryGetValue(name, out var cached)) return cached;
        try
        {
            var uri = new Uri(name);
            if (!AssetLoader.Exists(uri)) return null;
            using var stream = AssetLoader.Open(uri);
            var image = new Bitmap(stream);
            Cache[name] = image;
            return image;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); return null; }
    }

    private static Bitmap? ReadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var info = new FileInfo(path);
            string key = info.FullName + "|" + info.LastWriteTimeUtc.Ticks + "|" + info.Length;
            if (Cache.TryGetValue(key, out var cached)) return cached;
            using var stream = File.OpenRead(path);
            var image = new Bitmap(stream);
            Cache[key] = image;
            return image;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); return null; }
    }

    public static void Clear()
    {
        foreach (var image in Cache.Values) image.Dispose();
        Cache.Clear();
    }
}
