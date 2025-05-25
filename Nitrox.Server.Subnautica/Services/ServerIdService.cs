using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nitrox.Server.Subnautica.Models.Configuration;
using NitroxModel.Networking.Packets;
using NitroxModel.Security;

namespace Nitrox.Server.Subnautica.Services;

/// <summary>
///     Provides an identity to this server so that clients can securely connect and be sure it's authentic.
/// </summary>
internal sealed class ServerIdService(IOptions<ServerStartOptions> optionsProvider, ILogger<ServerIdService> logger) : IHostedService, IDisposable
{
    private readonly ILogger<ServerIdService> logger = logger;
    private readonly IOptions<ServerStartOptions> optionsProvider = optionsProvider;
    private AsymCrypto encryptor;

    /// <inheritdoc cref="SessionPolicy.PublicKey"/>
    public byte[] PublicKey => encryptor.PublicKey;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using FileStream fs = new(Path.Combine(optionsProvider.Value.GetServerSavePath(), "server.pem"), FileMode.OpenOrCreate);
        encryptor = await AsymCrypto.CreateOrLoad(fs);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    ///     Decrypts data using the private key. This only works with data that was encrypted with the public key.
    /// </summary>
    public byte[] Decrypt(byte[] data) => encryptor.Decrypt(data);

    public void Dispose() => encryptor?.Dispose();
}
