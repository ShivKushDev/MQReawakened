﻿using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Base.Core.Extensions;
using Server.Base.Core.Helpers;
using Server.Base.Core.Models;
using Server.Base.Core.Services;
using Web.AssetBundles.Models;
using Web.Launcher.Services;

namespace Web.AssetBundles.Services;

public class ReplaceCaches : IService
{
    private readonly BuildAssetList _buildAssetList;
    private readonly AssetBundleConfig _config;
    private readonly ServerConsole _console;
    private readonly StartGame _game;
    private readonly ILogger<ReplaceCaches> _logger;
    private readonly EventSink _sink;

    public ReplaceCaches(ServerConsole console, EventSink sink, BuildAssetList buildAssetList, AssetBundleConfig config,
        ILogger<ReplaceCaches> logger, StartGame game)
    {
        _console = console;
        _sink = sink;
        _buildAssetList = buildAssetList;
        _config = config;
        _logger = logger;
        _game = game;
    }

    public void Initialize() => _sink.WorldLoad += Load;

    public void Load() =>
        _console.AddCommand(new ConsoleCommand("replaceCaches",
            "Replaces all generated Web Player cache files with their real counterparts.",
            _ => ReplaceWebPlayerCache()));

    private void ReplaceWebPlayerCache()
    {
        if (_config.FlushCacheOnStart)
            if (_logger.Ask("Flushing the cache on start is enabled, would you like to disable this?"))
                _config.FlushCacheOnStart = false;

        var assetDictionary = _buildAssetList.InternalAssets
            .ToDictionary(a => Path.GetFileName(a.Value.Path), a => a.Value);

        var cachedFiles = Directory.GetFiles(Path.GetDirectoryName(_config.WebPlayerInfoFile)!, "*.*",
                SearchOption.AllDirectories).Select(c => new KeyValuePair<string, string>(Path.GetFileName(c), c))
            .Where(c => c.Key != "__info").ToArray();

        var filteredCaches = cachedFiles.Where(c => assetDictionary.ContainsKey(c.Key!)).ToArray();

        _logger.LogInformation("Loaded {NumAssetDict} Assets With {Caches} Caches ({TotalFiles} Total Files).",
            assetDictionary.Count, filteredCaches.Length, cachedFiles.Length);

        using (var bar = new DefaultProgressBar(filteredCaches.Length, "Replacing Caches", _logger))
        {
            foreach (var cache in filteredCaches)
            {
                if (!assetDictionary.ContainsKey(cache.Key))
                    continue;

                var asset = assetDictionary[cache.Key];

                bar.SetMessage($"Overwriting {cache.Key} ({asset.Name})");

                File.Copy(asset.Path, cache.Value, true);

                bar.SetMessage($"{asset.Path} -> {cache.Value}");

                bar.TickBar();
            }
        }

        if (!_config.StartLauncherOnReplace)
            if (_logger.Ask("The launcher is not set to restart on replacement, would you like to enable this?"))
                _config.StartLauncherOnReplace = true;

        if (_config.StartLauncherOnReplace)
            _game.LaunchGame();
    }
}
