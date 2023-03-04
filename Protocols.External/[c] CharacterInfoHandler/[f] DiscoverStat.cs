﻿using Server.Base.Logging;
using Server.Reawakened.Network.Protocols;

namespace Protocols.External._c__CharacterInfoHandler;

public class DiscoverStat : ExternalProtocol
{
    public override string ProtocolName => "cf";

    public FileLogger FileLogger { get; set; }

    public override void Run(string[] message)
    {
        var character = Player.Character;

        var stat = int.Parse(message[5]);

        if (!character.Data.DiscoveredStats.Contains(stat))
            character.Data.DiscoveredStats.Add(stat);

        if (stat is >= 0 and < 12 and not 6)
            return;

        FileLogger.WriteGenericLog<DiscoverStat>("discovered-stats", "Discovered Character Stat",
            stat.ToString(), LoggerType.Trace);
    }
}
