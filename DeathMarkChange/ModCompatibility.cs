using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using UnityEngine;
using ItemStats;
using ItemStats.Stat;
using ItemStats.ValueFormatters;

namespace DeathMarkChange
{
    public class ItemStatsCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dev.ontrigger.itemstats");
                }
                return (bool)_enabled;
            }
        }
        internal static void DeathMarkItemStats()
        {
            Dictionary<ItemIndex, ItemStatDef> itemDefs = ItemStatProvider.ItemDefs;
            ItemIndex key = RoR2Content.Items.DeathMark.itemIndex;
            ItemStatDef itemStatDef = new ItemStatDef();
            List<ItemStat> list = new List<ItemStat>();
            list.Add(new ItemStat((float itemCount, StatContext ctx) => DeathMarkChange.DamageIncreasePerDebuff.Value + DeathMarkChange.DamageIncreasePerDebuff.Value * DeathMarkChange.StackBonus.Value * (itemCount - 1), (float value, StatContext ctx) => string.Format("Damage Increase Per Debuff: {0}", value.FormatPercentage(1, 100f, float.MaxValue, false, "\"green\""))));
            itemStatDef.Stats = list;
            itemDefs[key] = itemStatDef;
        }
    }
}