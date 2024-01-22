﻿using Microsoft.Extensions.Logging;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Network.Protocols;

namespace Protocols.External._h__HotbarHandler;

public class SetSlot : ExternalProtocol
{
    public override string ProtocolName => "hs";

    public ILogger<SetSlot> Logger { get; set; }

    public override void Run(string[] message)
    {
        var character = Player.Character;

        var hotbarSlotId = int.Parse(message[5]);
        var itemId = int.Parse(message[6]);

        if (!character.TryGetItem(itemId, out var item))
        {
            Logger.LogError("Could not find item with ID {itemId} in inventory.", itemId);
            return;
        }

        character.Data.Hotbar.HotbarButtons[hotbarSlotId] = item;

        SendXt("hs", character.Data.Hotbar);
    }
}
