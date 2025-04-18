﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Microsoft.Extensions.Options;
using Nitrox.Server.Subnautica.Models.Resources.Helper;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.Helper;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nitrox.Server.Subnautica.Models.Resources.Parsers;

internal sealed class RandomStartResource(SubnauticaAssetsManager assetsManager, IOptions<Configuration.ServerStartOptions> optionsProvider) : IGameResource
{
    private readonly SubnauticaAssetsManager assetsManager = assetsManager;
    private readonly IOptions<Configuration.ServerStartOptions> optionsProvider = optionsProvider;
    private Task<RandomStartGenerator> randomStartGenerator;
    public RandomStartGenerator RandomStartGenerator => GetRandomStartGeneratorAsync().GetAwaiter().GetResult();

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        randomStartGenerator = GetRandomStartGeneratorAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task<RandomStartGenerator> GetRandomStartGeneratorAsync(CancellationToken cancellationToken = default)
    {
        if (randomStartGenerator is { IsCompletedSuccessfully : true, Result: not null })
        {
            return await randomStartGenerator;
        }

        string bundlePath = Path.Combine(optionsProvider.Value.GetSubnauticaStandaloneResourcePath(), "essentials.unity_0ee8dd89ed55f05bc38a09cc77137d4e.bundle");
        BundleFileInstance bundleFileInst = assetsManager.LoadBundleFile(bundlePath);
        AssetsFileInstance assetFileInst = assetsManager.LoadAssetsFileFromBundle(bundleFileInst, 0, true);
        cancellationToken.ThrowIfCancellationRequested();

        AssetTypeValueField textureValueField = assetsManager.GetBaseField(assetFileInst, assetFileInst.file.GetAssetInfo(assetsManager, "RandomStart", AssetClassID.Texture2D));
        TextureFile textureFile = TextureFile.ReadTextureFile(textureValueField);
        byte[] texDat = textureFile.GetTextureData(assetFileInst);
        assetsManager.UnloadAll();
        cancellationToken.ThrowIfCancellationRequested();

        if (texDat is not { Length: > 0 })
        {
            return null;
        }

        Image<Bgra32> texture = Image.LoadPixelData<Bgra32>(texDat, textureFile.m_Width, textureFile.m_Height);
        texture.Mutate(x => x.Flip(FlipMode.Vertical));
        cancellationToken.ThrowIfCancellationRequested();
        return new RandomStartGenerator(new PixelProvider(texture));
    }

    private class PixelProvider : RandomStartGenerator.IPixelProvider
    {
        private readonly Image<Bgra32> texture;

        public PixelProvider(Image<Bgra32> texture)
        {
            Validate.NotNull(texture);
            this.texture = texture;
        }

        public byte GetRed(int x, int y) => texture[x, y].R;

        public byte GetGreen(int x, int y) => texture[x, y].G;

        public byte GetBlue(int x, int y) => texture[x, y].B;
    }
}
