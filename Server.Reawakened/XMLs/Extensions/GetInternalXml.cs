﻿using Achievement.StaticData;
using Microsoft.Extensions.Logging;
using Server.Base.Logging;
using Server.Reawakened.Players;
using Server.Reawakened.Players.Extensions;
using Server.Reawakened.Players.Models.Character;
using Server.Reawakened.XMLs.Enums;
using System.Xml;

namespace Server.Reawakened.XMLs.Extensions;

public static class GetInternalXml
{
    public static List<ItemModel> GetXmlItems(this XmlNode node)
    {
        var itemList = new List<ItemModel>();

        foreach (XmlNode item in node.ChildNodes)
        {
            if (item.Name != "Item")
                continue;

            var itemId = -1;
            var count = -1;
            var bindingCount = -1;
            var delayUseExpiry = DateTime.Now;
            var weight = 1;

            foreach (XmlAttribute itemAttribute in item.Attributes)
            {
                switch (itemAttribute.Name)
                {
                    case "itemId":
                        itemId = int.Parse(itemAttribute.Value);
                        break;
                    case "count":
                        count = int.Parse(itemAttribute.Value);
                        break;
                    case "bindingCount":
                        bindingCount = int.Parse(itemAttribute.Value);
                        break;
                    case "delayUseExpiry":
                        delayUseExpiry = DateTime.Parse(itemAttribute.Value);
                        break;
                    case "weight":
                        weight = int.Parse(itemAttribute.Value);
                        break;
                }
            }

            itemList.Add(new ItemModel()
            {
                ItemId = itemId,
                Count = count,
                BindingCount = bindingCount,
                DelayUseExpiry = delayUseExpiry,
                Weight = weight
            });
        }

        return itemList;
    }

    public static List<AchievementDefinitionRewards> GetXmlRewards(this XmlNode node,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var rewardList = new List<AchievementDefinitionRewards>();

        foreach (XmlNode reward in node.ChildNodes)
        {
            if (reward.Name != "Reward")
                continue;

            var type = RewardType.Unknown;
            object value = null;
            var quantity = -1;

            foreach (XmlAttribute rewardAttribute in reward.Attributes)
            {
                switch (rewardAttribute.Name)
                {
                    case "type":
                        type = type.GetEnumValue(rewardAttribute.Value, logger);
                        continue;
                    case "value":
                        value = int.TryParse(rewardAttribute.Value, out var valInt) ? valInt : rewardAttribute.Value;
                        continue;
                    case "quantity":
                        quantity = int.Parse(rewardAttribute.Value);
                        continue;
                }
            }

            rewardList.Add(new AchievementDefinitionRewards()
            {
                id = 0,
                achievementId = 0,
                typeId = (int)type,
                value = value,
                quantity = quantity
            });
        }

        return rewardList;
    }

    public static List<AchievementDefinitionConditions> GetXmlConditions(this XmlNode node, int achievementId,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var conditionList = new List<AchievementDefinitionConditions>();

        foreach (XmlNode condition in node.ChildNodes)
        {
            if (condition.Name != "Condition")
                continue;

            var id = -1;
            var title = string.Empty;
            var goal = -1;
            var visible = false;

            foreach (XmlAttribute conditionAttribute in condition.Attributes)
            {
                switch (conditionAttribute.Name)
                {
                    case "id":
                        id = int.Parse(conditionAttribute.Value);
                        continue;
                    case "title":
                        title = conditionAttribute.Value;
                        continue;
                    case "goal":
                        goal = int.Parse(conditionAttribute.Value);
                        continue;
                    case "visible":
                        visible = visible.GetBoolValue(conditionAttribute.Value, logger);
                        continue;
                }
            }

            conditionList.Add(new AchievementDefinitionConditions()
            {
                id = int.Parse(achievementId.ToString() + id),
                achievementId = achievementId,
                title = title,
                goal = goal,
                visible = visible
            });
        }

        return conditionList;
    }

    public static void RewardPlayer(this List<AchievementDefinitionRewards> rewards, Player player,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        var hasUpdatedItems = false;

        foreach (var reward in rewards)
            switch ((RewardType)reward.typeId)
            {
                case RewardType.NickCash:
                    var nickCash = int.Parse(reward.value.ToString());
                    player.AddNCash(nickCash);
                    break;
                case RewardType.Bananas:
                    var bananaCount = int.Parse(reward.value.ToString());
                    player.AddBananas(bananaCount);
                    break;
                case RewardType.Item:
                    var itemId = int.Parse(reward.value.ToString());
                    var quantity = reward.quantity;
                    
                    var item = player.DatabaseContainer.ItemCatalog.GetItemFromId(itemId);

                    player.AddItem(item, quantity);
                    hasUpdatedItems = true;
                    break;
                case RewardType.Xp:
                    var xp = int.Parse(reward.value.ToString());
                    player.AddReputation(xp);
                    break;
                case RewardType.Title:
                    break;
                default:
                    logger.LogError("Unknown reward type {Type}", reward.typeId);
                    break;
            }

        if (hasUpdatedItems)
            player.SendUpdatedInventory(false);
    }
}
