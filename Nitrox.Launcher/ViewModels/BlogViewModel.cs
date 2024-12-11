using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nitrox.Launcher.Models.Converters;
using Nitrox.Launcher.Models.Design;
using Nitrox.Launcher.Models.Utils;
using Nitrox.Launcher.ViewModels.Abstract;
using NitroxModel.Logger;
using ReactiveUI;

namespace Nitrox.Launcher.ViewModels;

public partial class BlogViewModel : RoutableViewModelBase
{
    public static Bitmap FallbackImage { get; } = BitmapAssetValueConverter.GetBitmapFromPath("/Assets/Images/blog/vines.png");

    [ObservableProperty]
    private AvaloniaList<NitroxBlog> nitroxBlogs = [];

    public BlogViewModel()
    {
    }

    internal override async Task ViewContentLoadAsync()
    {
        if (Design.IsDesignMode)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                NitroxBlogs.Clear();
                NitroxBlogs.AddRange(await Downloader.GetBlogsAsync());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while trying to display nitrox blogs");
            }
        });
    }

    [RelayCommand]
    private void BlogEntryClick(string blogUrl)
    {
        UriBuilder blogUriBuilder = new(blogUrl)
        {
            Scheme = Uri.UriSchemeHttps,
            Port = -1
        };

        Process.Start(
            new ProcessStartInfo(blogUriBuilder.Uri.ToString())
            {
                UseShellExecute = true,
                Verb = "open"
            }
        )?.Dispose();
    }
}
