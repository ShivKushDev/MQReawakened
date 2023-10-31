﻿using Server.Reawakened.Network.Extensions;
using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players.Helpers;

namespace Protocols.External._p__GroupHandler;

public class PromoteMember : ExternalProtocol
{
    public override string ProtocolName => "pp";

    public PlayerHandler PlayerHandler { get; set; }

    public override void Run(string[] message)
    {
        var newLeader = message[5];

        Player.Group.SetLeaderName(newLeader);

        foreach (var player in Player.Group.GetMembers())
            player.SendXt("pp", newLeader);
    }
}
