﻿using Microsoft.Extensions.Logging;
using Server.Base.Core.Extensions;
using Server.Reawakened.BundleHost.Configs;
using Server.Reawakened.BundleHost.Models;
using Server.Reawakened.BundleHost.Services;
using Server.Reawakened.Core.Configs;
using static Server.Reawakened.BundleHost.Extensions.GetMainAsset;

namespace Server.Reawakened.BundleHost.Extensions;

public static class AssetDictionaryExtensions
{
    public static void AddModifiedAssets(this Dictionary<string, InternalAssetInfo> assets,
        AssetBundleRConfig config, ServerRConfig serverConfig)
    {
        var assetsToAdd = new Dictionary<string, InternalAssetInfo>();

        foreach (var modifier in config.AssetModifiers)
            foreach (var oldAsset in assets.Keys.Where(a => a.EndsWith(modifier)))
            {
                var assetName = oldAsset[..^modifier.Length];
                assetsToAdd.AddChangedNameToDict(assetName, assets[oldAsset]);
            }

        foreach (var version in config.AssetRenames)
        {
            if (version.Key > serverConfig.GameVersion)
                continue;

            foreach (var replacement in version.Value)
            {
                foreach (var oldAsset in assets.Where(a => a.Key == replacement.Key))
                    assetsToAdd.AddChangedNameToDict(replacement.Value, oldAsset.Value);
            }
        }

        foreach (var asset in assetsToAdd
                     .Where(asset => !assets.ContainsKey(asset.Key)))
            assets.Add(asset.Key, asset.Value);
    }

    public static void AddChangedNameToDict(this Dictionary<string, InternalAssetInfo> assets, string assetName,
        InternalAssetInfo oldAsset)
    {
        if (assets.ContainsKey(assetName))
            return;

        var asset = oldAsset.DeepCopy();
        asset.Name = assetName;

        assets.Add(assetName, asset);
    }

    public static void AddLocalXmlFiles(this Dictionary<string, InternalAssetInfo> assets,
        ILogger<BuildAssetList> logger, AssetBundleRConfig config)
    {
        logger.LogInformation("Loading local XML files from '{LocalAssetsDirectory}'", config.LocalAssetsDirectory);

        foreach (var asset in Directory
                     .GetFiles(config.LocalAssetsDirectory, "*.xml")
                     .Select(file => new InternalAssetInfo
                     {
                         BundleSize = Convert.ToInt32(new FileInfo(file).Length / 1024),
                         Locale = RFC1766Locales.LanguageCodes.en_us,
                         Name = Path.GetFileName(file).Split('.')[0],
                         Type = AssetInfo.TypeAsset.XML,
                         Path = file,
                         Version = 0
                     })
                    .Where(a =>
                    {
                        if (assets.ContainsKey(a.Name))
                        {
                            if (!config.ForceLocalAsset.Contains(a.Name))
                                return false;

                            assets.Remove(a.Name);
                        }

                        logger.LogTrace("Adding asset {Name} from local assets.", a.Name);
                        return true;
                    }))
            assets.Add(asset.Name, asset);
    }
}
