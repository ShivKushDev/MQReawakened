﻿using Server.Reawakened.Network.Protocols;
using Server.Reawakened.Players.Services;

namespace Protocols.External._f__FriendsHandler;
public class DeleteResponse : ExternalProtocol
{
    public override string ProtocolName => "fd";

    public CharacterHandler CharacterHandler { get; set; }

    public override void Run(string[] message)
    {
        var characterName = message[5];
        var friend = CharacterHandler.GetCharacterFromName(characterName);

        if (friend != null)
        {
            Player.Character.Data.Friends.Remove(friend.Id);
            friend.Data.Friends.Remove(Player.CharacterId);

            SendXt("fd", characterName, "1");
        }
        else
            SendXt("fd", characterName, "0");
    }
}
