﻿using Microsoft.Extensions.Logging;
using Server.Base.Accounts.Extensions;
using Server.Base.Accounts.Models;
using Server.Base.Core.Extensions;
using Server.Base.Network;
using Server.Reawakened.Configs;
using Server.Reawakened.Entities;
using Server.Reawakened.Levels.Enums;
using Server.Reawakened.Levels.Extensions;
using Server.Reawakened.Levels.Models.Entities;
using Server.Reawakened.Levels.Models.Planes;
using Server.Reawakened.Levels.Services;
using Server.Reawakened.Network.Extensions;
using Server.Reawakened.Network.Helpers;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.XMLs.Bundles;
using WorldGraphDefines;
using static NavMesh;

namespace Server.Reawakened.Levels.Models;

public class Level
{
    private readonly ILogger<LevelHandler> _logger;
    private readonly ServerConfig _serverConfig;

    private readonly HashSet<int> _clientIds;
    public readonly Dictionary<int, NetState> Clients;

    private readonly LevelHandler _levelHandler;
    private readonly WorldGraph _worldGraph;

    public LevelInfo LevelInfo { get; set; }
    public LevelPlanes LevelPlaneHandler { get; set; }
    public LevelEntities LevelEntityHandler { get; set; }

    public long TimeOffset { get; set; }
    
    public long Time => Convert.ToInt64(Math.Floor((GetTime.GetCurrentUnixMilliseconds() - TimeOffset) / 1000.0));

    public Level(LevelInfo levelInfo, LevelPlanes levelPlaneHandler, ServerConfig serverConfig,
        LevelHandler levelHandler, WorldGraph worldGraph, ReflectionUtils reflection, IServiceProvider services, ILogger<LevelHandler> logger)
    {
        _serverConfig = serverConfig;
        _levelHandler = levelHandler;
        _worldGraph = worldGraph;
        _logger = logger;
        Clients = new Dictionary<int, NetState>();
        _clientIds = new HashSet<int>();

        LevelInfo = levelInfo;
        LevelPlaneHandler = levelPlaneHandler;
        TimeOffset = GetTime.GetCurrentUnixMilliseconds();

        LevelEntityHandler = new LevelEntities(this, _levelHandler, reflection, services, _logger);
    }

    public void AddClient(NetState newClient, out JoinReason reason)
    {
        var playerId = -1;

        if (_clientIds.Count > _serverConfig.PlayerCap)
        {
            reason = JoinReason.Full;
        }
        else
        {
            playerId = 1;

            while (_clientIds.Contains(playerId))
                playerId++;

            Clients.Add(playerId, newClient);
            _clientIds.Add(playerId);
            reason = JoinReason.Accepted;
        }

        newClient.Get<Player>().PlayerId = playerId;

        if (reason == JoinReason.Accepted)
        {
            var newPlayer = newClient.Get<Player>();

            if (LevelInfo.LevelId == -1)
                return;

            // JOIN CONDITION
            newClient.SendXml("joinOK", $"<pid id='{newPlayer.PlayerId}' /><uLs />");

            if (LevelInfo.LevelId == 0)
                return;

            // USER ENTER
            var newAccount = newClient.Get<Account>();

            foreach (var currentClient in Clients.Values)
            {
                var currentPlayer = currentClient.Get<Player>();
                var currentAccount = currentClient.Get<Account>();

                var areDifferentClients = currentPlayer.UserInfo.UserId != newPlayer.UserInfo.UserId;

                SendUserEnterData(newClient, currentPlayer, currentAccount);

                if (areDifferentClients)
                    SendUserEnterData(currentClient, newPlayer, newAccount);
            }
        }
        else
        {
            newClient.SendXml("joinKO", $"<error>{reason.GetJoinReasonError()}</error>");
        }
    }

    public void SendCharacterInfo(Player newPlayer, NetState newClient)
    {
        // WHERE TO SPAWN
        var character = newPlayer.GetCurrentCharacter();

        BaseSyncedEntity spawnLocation = null;

        var spawnPoints = LevelEntityHandler.GetEntities<SpawnPointEntity>();
        var portals = LevelEntityHandler.GetEntities<PortalControllerEntity>();

        var realSpawn = character.SpawnPoint != 0 || character.PortalId != 0;

        if (realSpawn)
        {
            if (portals.TryGetValue(character.PortalId, out var portal))
            {
                spawnLocation = portal;
            }
            else
            {
                if (spawnPoints.TryGetValue(character.SpawnPoint, out var spawnPoint))
                    spawnLocation = spawnPoint;
                else
                    _logger.LogError("Could not find portal '{PortalId}' or spawn '{SpawnId}'.",
                        character.PortalId, character.SpawnPoint);
            }
        }

        var defaultSpawn = spawnPoints.Values.MinBy(p => p.Index);

        if (defaultSpawn != null)
            spawnLocation ??= defaultSpawn;
        else
            throw new InvalidDataException($"Could not find default spawn point in {LevelInfo.LevelId}, as there are none initialized!");

        character.Data.SpawnPositionX = spawnLocation.Position.X + spawnLocation.Scale.X / 2;
        character.Data.SpawnPositionY = spawnLocation.Position.Y + spawnLocation.Scale.Y / 2;
        character.Data.SpawnOnBackPlane = spawnLocation.Position.Z > 1;

        _logger.LogDebug("Spawning {CharacterName} at object portal '{NodePortalId} spawn '{SpawnPoint}' to '{NewLevel}'.",
            character.Data.CharacterName,
            character.PortalId != 0 ? character.PortalId : "DEFAULT",
            character.SpawnPoint != 0 ? character.SpawnPoint : "DEFAULT",
            character.Level
        );

        _logger.LogDebug("Position of spawn: {Position}", spawnLocation);

        // CHARACTER DATA

        foreach (var currentClient in Clients.Values)
        {
            var currentPlayer = currentClient.Get<Player>();

            var areDifferentClients = currentPlayer.UserInfo.UserId != newPlayer.UserInfo.UserId;

            SendCharacterInfoData(newClient, currentPlayer,
                areDifferentClients ? CharacterInfoType.Lite : CharacterInfoType.Portals);

            if (areDifferentClients)
                SendCharacterInfoData(currentClient, newPlayer, CharacterInfoType.Lite);
        }
    }

    private static void SendUserEnterData(NetState state, Player player, Account account) =>
        state.SendXml("uER",
            $"<u i='{player.UserInfo.UserId}' m='{account.IsModerator()}' s='{account.IsSpectator()}' p='{player.PlayerId}'><n>{account.Username}</n></u>");

    public void SendCharacterInfoData(NetState state, Player player, CharacterInfoType type)
    {
        var character = player.GetCurrentCharacter();

        var info = type switch
        {
            CharacterInfoType.Lite => character.Data.GetLightCharacterData(),
            CharacterInfoType.Portals => character.Data.BuildPortalData(),
            CharacterInfoType.Detailed => character.Data.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        state.SendXt("ci", player.UserInfo.UserId.ToString(), info, character.GetCharacterObjectId(),
            LevelInfo.Name);
    }

    public void DumpPlayersToLobby()
    {
        foreach (var playerId in Clients.Keys)
            DumpPlayerToLobby(playerId);
    }

    public void DumpPlayerToLobby(int playerId)
    {
        var client = Clients[playerId];
        client.Get<Player>().JoinLevel(client, _levelHandler.GetLevelFromId(-1), out _);
        RemoveClient(playerId);
    }

    public void RemoveClient(int playerId)
    {
        Clients.Remove(playerId);
        _clientIds.Remove(playerId);

        if (Clients.Count == 0 && LevelInfo.LevelId > 0)
            _levelHandler.RemoveLevel(LevelInfo.LevelId);
    }

    public void SendSyncEvent(SyncEvent syncEvent, Player sentPlayer = null)
    {
        var syncEventMsg = syncEvent.EncodeData();

        foreach (
            var client in
            from client in Clients.Values
            let receivedPlayer = client.Get<Player>()
            where sentPlayer == null || receivedPlayer.UserInfo.UserId != sentPlayer.UserInfo.UserId
            select client
        )
            client.SendXt("ss", syncEventMsg);
    }

    public void SendSyncEventToPlayer(SyncEvent syncEvent, NetState state)
    {
        var syncEventMsg = syncEvent.EncodeData();

        state.SendXt("ss", syncEventMsg);
    }
}
