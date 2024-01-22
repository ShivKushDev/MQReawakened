﻿using Server.Reawakened.Entities.Components;
using Server.Reawakened.Players.Models.Arenas;
using Server.Reawakened.Players.Models.Groups;
using Server.Reawakened.Players.Models.Trade;
using Server.Reawakened.Rooms.Models.Planes;

namespace Server.Reawakened.Players.Models;

public class TemporaryDataModel
{
    public int GameObjectId { get; set; } = 0;
    public int Direction { get; set; } = 0;

    public bool Invincible { get; set; } = false;
    public bool OnGround { get; set; } = false;

    public Vector3Model Position { get; set; } = new Vector3Model();
    public Vector3Model Velocity { get; set; } = new Vector3Model();

    public CheckpointControllerComp NextRespawnPosition { get; set; }

    public ArenaModel ArenaModel { get; set; }
    public TradeModel TradeModel { get; set; }
    public GroupModel Group { get; set; }

    public Dictionary<int, List<string>> CurrentAchievements { get; set; } = [];
}
