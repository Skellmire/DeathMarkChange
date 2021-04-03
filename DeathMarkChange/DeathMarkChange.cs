using System;
using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using R2API;
using R2API.Utils;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DeathMarkChange
{
    [BepInDependency("com.bepis.r2api")]
    [R2APISubmoduleDependency("LanguageAPI")]
    [BepInDependency("dev.ontrigger.itemstats", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Skell.DeathMarkChange", "DeathMarkChange", "1.1.2")]
    public class DeathMarkChange : BaseUnityPlugin
    {
        public void Awake()
        {
            ConfigInit();
            LanguageTokenInit();
            Hook();
            if (ItemStatsCompatibility.enabled)
            {
                ItemStatsCompatibility.DeathMarkItemStats();
            }
        }

        private void ConfigInit()
        {
            MinimumDebuffsRequired = Config.Bind<int>(
            "DeathMarkChange",
            "Minimum Debuffs Required",
            2,
            "The minimum amount of debuffs required to trigger Death Mark."
            );
            DamageIncreasePerDebuff = Config.Bind<float>(
            "DeathMarkChange",
            "Damage Increase Per Debuff",
            .05f,
            "The percent by which your damage will be increased per debuff on the enemy, as a decimal. I recommend changing this to 0.1 if you aren't playing with mods that add a lot of debuffs that the player can make use of."
            );
            StackBonus = Config.Bind<float>(
            "DeathMarkChange",
            "Damage Bonus Per Stack",
            .5f,
            "The percent of the damage increase per debuff by which your damage will be increased per additional stack of Death Mark."
            );
            AIBlacklist = Config.Bind<bool>(
            "DeathMarkChange",
            "Blacklist from AI Use",
            true,
            "Whether or not Death Mark is blacklisted from enemy item pools. This is true by default because Death Mark now stacks across the entire team, meaning Void Fields would become almost unwinnable if the enemies got Death Mark."
            );
        }

        private void LanguageTokenInit()
        {
            LanguageAPI.Add("ITEM_DEATHMARK_DESC", "Enemies with <style=cIsDamage>" + MinimumDebuffsRequired.Value.ToString() + "</style> or more debuffs are <style=cIsDamage>marked for death</style>, increasing damage taken by <style=cIsDamage>" + (DamageIncreasePerDebuff.Value * 100f).ToString() + "%</style> <style=cStack>(+" + (DamageIncreasePerDebuff.Value * 100f * StackBonus.Value).ToString() + "% per stack)</style> per debuff from all sources for <style=cIsUtility>7</style> <style=cStack>(+7 per stack)</style> seconds.");
        }

        private void Hook()
        {
            IL.RoR2.GlobalEventManager.OnHitEnemy += OnHitEnemyHook;
            IL.RoR2.HealthComponent.TakeDamage += TakeDamageHook;
            if (AIBlacklist.Value)
            {
                On.RoR2.ItemCatalog.DefineItems += DefineItemsHook;
            }
        }

        private void DefineItemsHook(On.RoR2.ItemCatalog.orig_DefineItems orig)
        {
            orig();
            ItemCatalog.GetItemDef(RoR2Content.Items.DeathMark.itemIndex).tags = new ItemTag[]
            {
                ItemTag.Damage,
                ItemTag.AIBlacklist
            };
        }

        private void TakeDamageHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchBrfalse(out ILLabel IL_05C2),
                x => x.MatchLdloc(6),
                x => x.MatchLdcR4(1.5f),
                x => x.MatchMul(),
                x => x.MatchStloc(6),
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(7),
                x => x.MatchStfld<DamageInfo>("damageColorIndex")
                );
            c.Index += 3;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_1);
            c.EmitDelegate<Func<HealthComponent, CharacterBody, float>>((self, attacker) =>
            {
                if (attacker.master.inventory)
                {
                    int DeathMarkCount = Util.GetItemCountForTeam(attacker.master.teamIndex, RoR2Content.Items.DeathMark.itemIndex, false, true);
                    int debuffCount = 0;
                    foreach (BuffIndex buffType in BuffCatalog.debuffBuffIndices)
                    {
                        if (self.body.HasBuff(buffType))
                        {
                            debuffCount++;
                        }
                    }
                    DotController dotController = DotController.FindDotController(self.gameObject);
                    if (dotController)
                    {
                        for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                        {
                            if (dotController.HasDotActive(dotIndex))
                            {
                                debuffCount++;
                            }
                        }
                    }
                    float damageBonus = debuffCount * DamageIncreasePerDebuff.Value;
                    if (DeathMarkCount > 0)
                    {
                        return 1f + damageBonus + (StackBonus.Value * damageBonus * ((float)DeathMarkCount - 1f));
                    }
                    return 1f + damageBonus;
                }
                return 1.5f;
            });
        }

        private void OnHitEnemyHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdloc(16),
                x => x.MatchLdcI4(4),
                x => x.MatchBlt(out ILLabel IL_0BD7)
                );
            c.Index += 2;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, MinimumDebuffsRequired.Value);
        }

        public static ConfigEntry<int> MinimumDebuffsRequired { get; set; }
        public static ConfigEntry<float> DamageIncreasePerDebuff { get; set; }
        public static ConfigEntry<float> StackBonus { get; set; }
        public static ConfigEntry<bool> AIBlacklist { get; set; }
    }
}
