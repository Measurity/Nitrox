﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Nitrox.Launcher.Models.Converters;

public class BitmapAssetValueConverter : Converter<BitmapAssetValueConverter>, IValueConverter
{
    private static readonly string assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new Exception("Unable to get Assembly name");
    private static readonly Dictionary<string, Bitmap> assetCache = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }
        if (value is not string rawUri || !targetType.IsAssignableFrom(typeof(Bitmap)))
        {
            throw new NotSupportedException();
        }
        if (assetCache.TryGetValue(rawUri, out Bitmap bitmap))
        {
            return bitmap;
        }

        Uri uri;
        // Allow for assembly overrides
        if (rawUri.StartsWith("avares://"))
        {
            uri = new Uri(rawUri);
        }
        else
        {
            uri = new Uri($"avares://{assemblyName}{rawUri}");
        }

        bitmap = new Bitmap(AssetLoader.Open(uri));
        assetCache.Add(rawUri, bitmap);
        return bitmap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
