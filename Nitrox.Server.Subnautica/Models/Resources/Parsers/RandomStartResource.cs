using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Nitrox.Model.Subnautica.DataStructures.GameLogic;
using Nitrox.Server.Subnautica.Models.Resources.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nitrox.Server.Subnautica.Models.Resources.Parsers;

internal sealed class RandomStartResource(SubnauticaAssetsManager assetsManager, IOptions<ServerStartOptions> options) : IGameResource
{
    private readonly SubnauticaAssetsManager assetsManager = assetsManager;
    private readonly IOptions<ServerStartOptions> options = options;
    private readonly TaskCompletionSource<RandomStartGenerator> randomStartGeneratorTcs = new();
    public RandomStartGenerator RandomStartGenerator => randomStartGeneratorTcs.Task.GetAwaiter().GetResult();

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        randomStartGeneratorTcs.TrySetResult(await LoadAndGetRandomStartGeneratorAsync(cancellationToken));
    }

    private Task<RandomStartGenerator> LoadAndGetRandomStartGeneratorAsync(CancellationToken cancellationToken = default)
    {
        string bundlePath = Path.Combine(options.Value.GetSubnauticaStandaloneResourcePath(), "essentials.unity_0ee8dd89ed55f05bc38a09cc77137d4e.bundle");
        BundleFileInstance bundleFileInst = assetsManager.LoadBundleFile(bundlePath);
        AssetsFileInstance assetFileInst = assetsManager.LoadAssetsFileFromBundle(bundleFileInst, 0, true);
        cancellationToken.ThrowIfCancellationRequested();

        AssetTypeValueField textureValueField = assetsManager.GetBaseField(assetFileInst, assetFileInst.file.GetAssetInfo(assetsManager, "RandomStart", AssetClassID.Texture2D));
        TextureFile textureFile = TextureFile.ReadTextureFile(textureValueField);
        byte[] texDat = textureFile.GetTextureData(assetFileInst);
        cancellationToken.ThrowIfCancellationRequested();

        if (texDat is not { Length: > 0 })
        {
            return Task.FromResult<RandomStartGenerator>(null);
        }

        Image<Bgra32> texture = Image.LoadPixelData<Bgra32>(texDat, textureFile.m_Width, textureFile.m_Height);
        texture.Mutate(x => x.Flip(FlipMode.Vertical));
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new RandomStartGenerator(new PixelProvider(texture)));
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
