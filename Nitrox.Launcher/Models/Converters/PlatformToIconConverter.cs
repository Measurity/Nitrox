using System;
using System.Globalization;
using NitroxModel.Discovery.Models;

namespace Nitrox.Launcher.Models.Converters;

internal class PlatformToIconConverter : Converter<PlatformToIconConverter>
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return BitmapAssetValueConverter.GetBitmapFromPath(GetIconPathForPlatform(value as Platform?));
    }

    private string GetIconPathForPlatform(Platform? platform)
    {
        return platform switch
        {
            Platform.EPIC => "/Assets/Images/store-icons/epic.png",
            Platform.STEAM => "/Assets/Images/store-icons/steam.png",
            Platform.MICROSOFT => "/Assets/Images/store-icons/xbox.png",
            Platform.DISCORD => "/Assets/Images/store-icons/discord.png",
            _ => "/Assets/Images/store-icons/missing.png",
        };
    }
}
