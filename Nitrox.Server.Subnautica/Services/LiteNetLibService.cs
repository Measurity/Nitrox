using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nitrox.Server.Subnautica.Database.Models;
using Nitrox.Server.Subnautica.Models.Administration;
using Nitrox.Server.Subnautica.Models.Configuration;
using Nitrox.Server.Subnautica.Models.Events;
using Nitrox.Server.Subnautica.Models.Events.Core;
using Nitrox.Server.Subnautica.Models.Helper;
using Nitrox.Server.Subnautica.Models.Packets.Core;
using Nitrox.Server.Subnautica.Models.Packets.Processors.Core;
using Nitrox.Server.Subnautica.Models.Respositories;
using NitroxModel.Networking.Packets;
using NitroxModel.Networking.Packets.Core;

namespace Nitrox.Server.Subnautica.Services;

/// <summary>
///     Opens the LiteNetLib channel and starts sending incoming messages to <see cref="packetRegistryService" /> for
///     processing.
/// </summary>
internal class LiteNetLibService : BackgroundService, IServerPacketSender, ISeeSessionDisconnected, IKickPlayer
{
    private readonly NetDataWriter dataWriter = new();
    private readonly IHostEnvironment hostEnvironment;
    private readonly ServerIdService serverIdService;
    private readonly EventBasedNetListener listener;
    private readonly ILogger<LiteNetLibService> logger;
    private readonly IOptions<SubnauticaServerOptions> optionsProvider;
    private readonly PacketRegistryService packetRegistryService;
    private readonly PacketSerializationService packetSerializationService;
    private readonly ConcurrentDictionary<SessionId, PeerContext> contextBySessionId = [];
    private readonly NetManager server;
    private readonly ITrigger<ISeeSessionCreated, Session> sessionCreatedTrigger;
    private readonly SessionRepository sessionRepository;
    private readonly Channel<Task> taskChannel = Channel.CreateUnbounded<Task>();
    public int EventPriority => -100;

    public LiteNetLibService(PacketRegistryService packetRegistryService, PacketSerializationService packetSerializationService, SessionRepository sessionRepository, ITrigger<ISeeSessionCreated, Session> sessionCreatedTrigger,
                             IHostEnvironment hostEnvironment, ServerIdService serverIdService, IOptions<SubnauticaServerOptions> optionsProvider, ILogger<LiteNetLibService> logger)
    {
        this.packetRegistryService = packetRegistryService;
        this.packetSerializationService = packetSerializationService;
        this.optionsProvider = optionsProvider;
        this.hostEnvironment = hostEnvironment;
        this.serverIdService = serverIdService;
        this.sessionRepository = sessionRepository;
        this.sessionCreatedTrigger = sessionCreatedTrigger;
        this.logger = logger;
        listener = new EventBasedNetListener();
        server = new NetManager(listener);
    }

    public async Task<bool> KickPlayer(SessionId sessionId, string reason = "")
    {
        if (!contextBySessionId.TryGetValue(sessionId, out PeerContext context))
        {
            return false;
        }
        await SendPacket(new PlayerKicked(reason), sessionId);
        server.DisconnectPeer(context.Peer); // This will trigger client disconnect, which will clear the session data.
        return true;
    }

    public ValueTask SendPacket<T>(T packet, SessionId sessionId) where T : Packet
    {
        if (!contextBySessionId.TryGetValue(sessionId, out PeerContext context))
        {
            return ValueTask.CompletedTask;
        }
        if (context.Peer.ConnectionState != ConnectionState.Connected)
        {
            logger.ZLogWarning($"Cannot send packet {packet?.GetType().Name:@TypeName} to closed connection {(context.Peer as IPEndPoint).ToSensitive():@EndPoint} with session id {sessionId}");
            return ValueTask.CompletedTask;
        }
        SendPacket(packet, context.Peer);
        return ValueTask.CompletedTask;
    }

    public ValueTask SendPacketToAll<T>(T packet) where T : Packet
    {
        foreach (KeyValuePair<SessionId, PeerContext> pair in contextBySessionId)
        {
            SendPacket(packet, pair.Value.Peer);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask SendPacketToOthers<T>(T packet, SessionId excludedSessionId) where T : Packet
    {
        foreach (KeyValuePair<SessionId, PeerContext> pair in contextBySessionId)
        {
            if (pair.Key == excludedSessionId)
            {
                continue;
            }
            SendPacket(packet, pair.Value.Peer);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleSessionDisconnect(Session disconnectedSession)
    {
        if (!contextBySessionId.TryRemove(disconnectedSession.Id, out PeerContext context))
        {
            return ValueTask.CompletedTask;
        }
        if (disconnectedSession is { Player.Id: var playerId, Player.Name: { } playerName })
        {
            logger.ZLogInformation($"Player {playerName:@PlayerName} #{playerId:@PlayerId} on {(context.Peer as EndPoint).ToSensitive():@EndPoint} disconnected");
        }
        else
        {
            logger.ZLogInformation($"Session #{disconnectedSession.Id:@SessionId} on {(context.Peer as EndPoint).ToSensitive():@EndPoint} disconnected");
        }
        Disconnect disconnectPacket = new(disconnectedSession.Id);
        foreach (KeyValuePair<SessionId, PeerContext> pair in contextBySessionId)
        {
            if (pair.Key == disconnectedSession.Id)
            {
                continue;
            }
            SendPacket(disconnectPacket, pair.Value.Peer);
        }
        return ValueTask.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        listener.PeerConnectedEvent += PeerConnected;
        listener.PeerDisconnectedEvent += (peer, _) => ClientDisconnected(peer);
        listener.NetworkReceiveEvent += NetworkDataReceived;
        listener.ConnectionRequestEvent += OnConnectionRequest;

        server.ChannelsCount = (byte)typeof(Packet.UdpChannelId).GetEnumValues().Length;
        server.BroadcastReceiveEnabled = true;
        server.UnconnectedMessagesEnabled = true;
        server.UnsyncedEvents = true;
        server.UpdateTime = 15;
        if (hostEnvironment.IsDevelopment() && Debugger.IsAttached)
        {
            server.DisconnectTimeout = 300000; //Disables Timeout (for 5 min) for debug purpose (like if you jump though the server code)
        }
        if (!server.Start(optionsProvider.Value.ServerPort))
        {
            throw new Exception("Failed to start LiteNetLib service");
        }
        logger.ZLogDebug($"Now listening for connections");

        try
        {
            await foreach (Task task in taskChannel.Reader.ReadAllAsync(stoppingToken))
            {
                await task;
            }
        }
        catch (OperationCanceledException)
        {
            ServerStopped serverStopped = new();
            foreach (PeerContext context in contextBySessionId.Values)
            {
                SendPacket(serverStopped, context.Peer);
            }
            await Task.Delay(500, CancellationToken.None); // TODO: Need async function to wait for all packets to be sent away.
            logger.LogDebug("Waiting for LiteNetLib to stop...");
            await Task.Run(() => server.Stop(), CancellationToken.None);
            logger.ZLogDebug($"stopped");
            listener.ClearPeerConnectedEvent();
            listener.ClearPeerDisconnectedEvent();
            listener.ClearNetworkReceiveEvent();
            listener.ClearConnectionRequestEvent();
            throw;
        }
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        if (request.Data.GetString() != "nitrox")
        {
            request.Reject();
            return;
        }

        if (!taskChannel.Writer.TryWrite(ProcessConnectionRequestAsync(sessionRepository, request, contextBySessionId, sessionCreatedTrigger)))
        {
            logger.ZLogWarning($"Failed to queue client connect request task for {request.RemoteEndPoint.ToSensitive():@EndPoint}");
        }

        static async Task ProcessConnectionRequestAsync(SessionRepository sessionRepository, ConnectionRequest request, ConcurrentDictionary<SessionId, PeerContext> contextsBySessionId, ITrigger<ISeeSessionCreated, Session> sessionCreatedTrigger)
        {
            Session session = await sessionRepository.GetOrCreateSessionAsync(request.RemoteEndPoint.Address.ToString(), (ushort)request.RemoteEndPoint.Port);
            if (session == null)
            {
                // TODO: Tell user that all session slots are taken.
                request.Reject();
                return;
            }
            contextsBySessionId.TryAdd(session.Id, new PeerContext(request.Accept(), ""));
            await sessionCreatedTrigger.Trigger(session);
        }
    }

    private void ClientDisconnected(NetPeer peer)
    {
        SessionId? sessionId = null;
        foreach (KeyValuePair<SessionId, PeerContext> pair in contextBySessionId)
        {
            if (pair.Value.Peer.Id == peer.Id)
            {
                sessionId = pair.Key;
            }
        }

        if (!sessionId.HasValue)
        {
            logger.ZLogWarning($"Disconnected peer id {peer.Id} did not have an associated session id!");
            return;
        }
        if (!taskChannel.Writer.TryWrite(sessionRepository.DeleteSessionAsync(sessionId.Value)))
        {
            logger.ZLogWarning($"Failed to queue client disconnect task for {(peer as EndPoint).ToSensitive():@EndPoint}");
        }
    }

    private void PeerConnected(NetPeer peer) => logger.ZLogInformation($"Connection made by {peer.Address.ToSensitive():@Address}:{peer.Port}");

    private void NetworkDataReceived(NetPeer peer, NetDataReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        int packetDataLength = reader.GetInt();
        byte[] packetData = ArrayPool<byte>.Shared.Rent(packetDataLength);
        try
        {
            reader.GetBytes(packetData, packetDataLength);
            Packet packet = Packet.Deserialize(packetData);
            if (!taskChannel.Writer.TryWrite(ProcessPacket(peer, packet)))
            {
                logger.ZLogError($"Failed to queue packet processor task for packet type {packet.GetType().Name:@TypeName} from {peer.Address:@Address}:{peer.Port:@Port}");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(packetData, true);
        }
    }

    private async Task ProcessPacket(NetPeer peer, Packet packet)
    {
        Session session = await sessionRepository.GetOrCreateSessionAsync(peer.Address.ToString(), (ushort)peer.Port);
        Type packetType = packet.GetType();
        logger.ZLogTrace($"Incoming packet {packetType.Name:@TypeName} by session #{session.Id:@SessionId}");
        PacketProcessorsInvoker.Entry processor = packetRegistryService.GetProcessor(packetType);

        try
        {
            switch (GetProcessorTarget(processor, session))
            {
                case ProcessorTarget.ANONYMOUS:
                    using (EasyPool<AnonProcessorContext>.Lease lease = EasyPool<AnonProcessorContext>.Rent())
                    {
                        ref AnonProcessorContext context = ref lease.GetRef();
                        if (context == null)
                        {
                            context = new AnonProcessorContext(session.Id, this);
                        }
                        else
                        {
                            context.Sender = session.Id;
                        }
                        await processor.Execute(context, packet);
                    }
                    break;
                case ProcessorTarget.AUTHENTICATED:
                    using (EasyPool<AuthProcessorContext>.Lease lease = EasyPool<AuthProcessorContext>.Rent())
                    {
                        ref AuthProcessorContext context = ref lease.GetRef();
                        if (context == null)
                        {
                            context = new AuthProcessorContext((session.Player.Id, session.Id), this);
                        }
                        else
                        {
                            context.Sender = (session.Player.Id, session.Id);
                        }
                        await processor.Execute(context, packet);
                    }
                    break;
                default:
                    logger.ZLogWarning($"Received invalid, unauthenticated packet: {packetType.Name:@TypeName}");
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.ZLogError(ex, $"Error in packet processor {processor.GetType().Name:@TypeName}");
        }

        static ProcessorTarget GetProcessorTarget(PacketProcessorsInvoker.Entry processor, Session session)
        {
            if (processor == null)
            {
                return ProcessorTarget.INVALID;
            }
            if (typeof(IAnonPacketProcessor).IsAssignableFrom(processor.InterfaceType))
            {
                return ProcessorTarget.ANONYMOUS;
            }
            if (typeof(IAuthPacketProcessor).IsAssignableFrom(processor.InterfaceType) && session.Player is { Id.IsPlayer: true })
            {
                return ProcessorTarget.AUTHENTICATED;
            }
            return ProcessorTarget.INVALID;
        }
    }

    private void SendPacket(Packet packet, NetPeer peer)
    {
        using EasyPool<MemoryStream>.Lease lease = EasyPool<MemoryStream>.Rent();
        ref MemoryStream stream = ref lease.GetRef();
        stream ??= new MemoryStream(ushort.MaxValue);

        int startPos = (int)stream.Position;
        packetSerializationService.SerializeInto(packet, stream);
        int bytesWritten = (int)(stream.Position - startPos);
        Span<byte> packetData = stream.GetBuffer().AsSpan().Slice(startPos, bytesWritten);

        lock (dataWriter)
        {
            dataWriter.Reset();
            dataWriter.Put(packetData.Length);
            dataWriter.ResizeIfNeed(packetData.Length + 4);
            packetData.CopyTo(dataWriter.Data.AsSpan().Slice(4));
            dataWriter.SetPosition(packetData.Length + 4);
            peer.Send(dataWriter, (byte)packet.UdpChannel, NitroxDeliveryMethod.ToLiteNetLib(packet.DeliveryMethod));
        }

        // Cleanup pooled data.
        stream.Position = 0;
    }

    private enum ProcessorTarget
    {
        INVALID,
        ANONYMOUS,
        AUTHENTICATED
    }

    private record PeerContext(NetPeer Peer, string EncryptionKey); // TODO: DO SOMETHING USEFUL WITH ENCRYPTION HERE!
}
