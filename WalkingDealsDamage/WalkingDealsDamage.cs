using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Networking;
using RoR2.Audio;
using RoR2.Orbs;
using Facepunch.Steamworks;
using static Facepunch.Steamworks.Inventory.Item;
using EntityStates.SurvivorPod;

namespace WalkingDealsDamage 
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class WalkingDealsDamage : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "TaranDev";
        public const string PluginName = "WalkingDealsDamage";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<float> healthDamagePercentage;

        public static ConfigEntry<float> sprintDamageMultiplier;

        public static ConfigEntry<bool> useBaseArmour;

        public static ConfigEntry<bool> useBear;

        public static ConfigEntry<bool> useRap;

        public static ConfigEntry<bool> useEgg;

        public static ConfigEntry<float> eggHealMultiplier;

        public static ConfigEntry<bool> useBrittleCrown;

        public static ConfigEntry<bool> usePennies;

        public static ConfigEntry<float> penniesPayMultiplier;

        public static ConfigEntry<bool> useThorns;

        public static ConfigEntry<bool> useMedkit;

        /*public static ConfigEntry<bool> walkingStopShieldRegen;*/

        /*public static ConfigEntry<bool> walkingAddCurse;*/

        public static ConfigEntry<bool> walkingCanKill;

        public void Awake()
        {
            Log.Init(Logger);
            configs();
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        int handledSecond = 0;
        bool exitPod = false;

        private void Update() {

            if(Run.instance && PlayerCharacterMasterController.instances.Count > 0) {
                CharacterBody player = PlayerCharacterMasterController.instances[0].master.GetBody();
                HealthComponent playerHealthComponent = player.healthComponent;
                SurvivorPodController pod = EntityStateManager.FindObjectOfType<SurvivorPodController>();
                if (player && (pod == null || !pod.vehicleSeat.enabled)) {
                    if(!player.characterMotor.atRest && (int)Run.TimeStamp.tNow > 3) {
                        //player.healthComponent.Networkhealth = player.healthComponent.Networkhealth - player.healthComponent.fullCombinedHealth * (healthDamagePercentage.Value / 100f * Time.deltaTime);

                        DamageInfo damageInfo = new DamageInfo();

                        float damage = player.healthComponent.fullCombinedHealth * (healthDamagePercentage.Value / 100f * Time.deltaTime);

                        if(player.isSprinting) {
                            damage = damage * sprintDamageMultiplier.Value;
                        }

                        damageInfo.damage = damage;
                        damageInfo.position = player.transform.position;
                        damageInfo.rejected = false;
                        damageInfo.damageType = DamageType.Generic;

                        if (!playerHealthComponent.alive || playerHealthComponent.godMode) {
                            return;
                        }
                        //CharacterMaster characterMaster = null;
                        //CharacterBody characterBody = null;
                        TeamIndex teamIndex = TeamIndex.None;
                        Vector3 vector = Vector3.zero;
                        float num = playerHealthComponent.combinedHealth;

                        // If bear config on
                        if(useBear.Value) {
                            if (playerHealthComponent.itemCounts.bear > 0 && Util.CheckRoll(Util.ConvertAmplificationPercentageIntoReductionPercentage(15f * (float)playerHealthComponent.itemCounts.bear) * Time.deltaTime)) {
                                EffectManager.SpawnEffect(effectData: new EffectData
                                {
                                    origin = damageInfo.position,
                                    rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
                                }, effectPrefab: HealthComponent.AssetReferences.bearEffectPrefab, transmit: true);
                                damageInfo.rejected = true;
                            }
                        }

                        /*// Some invicible buff?
                        if (player.HasBuff(RoR2Content.Buffs.HiddenInvincibility)) {
                            damageInfo.rejected = true;
                        }
                        if (player.HasBuff(RoR2Content.Buffs.Immune)) {
                            EffectManager.SpawnEffect(HealthComponent.AssetReferences.damageRejectedPrefab, new EffectData
                            {
                                origin = damageInfo.position
                            }, transmit: true);
                            damageInfo.rejected = true;
                        }*/

                        /*IOnIncomingDamageServerReceiver[] array = onIncomingDamageReceivers;
                        for (int i = 0; i < array.Length; i++) {
                            array[i].OnIncomingDamageServer(damageInfo);
                        }*/
                        if (damageInfo.rejected) {
                            return;
                        }
                        float num3 = damageInfo.damage;
                        if (num3 > 0f) {

                            // if armour config on
                            if(useBaseArmour.Value) {
                                float armor = player.armor;
                                armor += playerHealthComponent.adaptiveArmorValue;
                                float num5 = ((armor >= 0f) ? (1f - armor / (armor + 100f)) : (2f - 100f / (100f - armor)));
                                num3 = Mathf.Max(num3, num3 * num5);
                            }
                            
                            // if rap config on
                            if(useRap.Value) {
                                if (playerHealthComponent.itemCounts.armorPlate > 0) {
                                    num3 = num3 / 3 + num3 / ((float)playerHealthComponent.itemCounts.armorPlate + 1);
                                    //EntitySoundManager.EmitSoundServer(LegacyResourcesAPI.Load<NetworkSoundEventDef>("NetworkSoundEventDefs/nseArmorPlateBlock").index, base.gameObject);
                                }
                            }

                            // if egg config on
                            if ((int)Run.TimeStamp.tNow > handledSecond) {
                                if (useEgg.Value) {
                                    if (playerHealthComponent.itemCounts.parentEgg > 0) {
                                        playerHealthComponent.Heal((float)playerHealthComponent.itemCounts.parentEgg * 7.5f * eggHealMultiplier.Value, default(ProcChainMask));
                                        //playerHealthComponent.Heal((float)playerHealthComponent.itemCounts.parentEgg * 15f, default(ProcChainMask));
                                        //EntitySoundManager.EmitSoundServer(LegacyResourcesAPI.Load<NetworkSoundEventDef>("NetworkSoundEventDefs/nseParentEggHeal").index, base.gameObject);
                                    }
                                }
                            }
                            

                            /*if (player.hasOneShotProtection && (damageInfo.damageType & DamageType.BypassOneShotProtection) != DamageType.BypassOneShotProtection) {
                                float num6 = (fullCombinedHealth + barrier) * (1f - body.oneShotProtectionFraction);
                                float b = Mathf.Max(0f, num6 - serverDamageTakenThisUpdate);
                                float num7 = num3;
                                num3 = Mathf.Min(num3, b);
                                if (num3 != num7) {
                                    TriggerOneShotProtection();
                                }
                            }*/
                            /*if ((damageInfo.damageType & DamageType.BonusToLowHealth) != 0) {
                                float num8 = Mathf.Lerp(3f, 1f, combinedHealthFraction);
                                num3 *= num8;
                            }*/
                            /*if (body.HasBuff(RoR2Content.Buffs.LunarShell) && num3 > fullHealth * 0.1f) {
                                num3 = fullHealth * 0.1f;
                            }*/
                            /*if (itemCounts.minHealthPercentage > 0) {
                                float num9 = fullCombinedHealth * ((float)itemCounts.minHealthPercentage / 100f);
                                num3 = Mathf.Max(0f, Mathf.Min(num3, combinedHealth - num9));
                            }*/
                        }

                        CharacterMaster master = player.master;
                        if ((bool)master) {
                            // if brittle crown config on
                            if(useBrittleCrown.Value) {
                                if (playerHealthComponent.itemCounts.goldOnHit > 0) {
                                    uint num10 = (uint)((player.healthComponent.fullCombinedHealth * (healthDamagePercentage.Value / 100f)) / playerHealthComponent.fullCombinedHealth * (float)master.money * (float)playerHealthComponent.itemCounts.goldOnHit);
                                    uint money = master.money;
                                    master.money = (uint)Mathf.Max(0f, (float)master.money - (float)num10);
                                    if (money - master.money != 0) {
                                        GoldOrb goldOrb = new GoldOrb();
                                        goldOrb.origin = damageInfo.position;
                                        goldOrb.target = (player.mainHurtBox);
                                        goldOrb.goldAmount = 0u;
                                        OrbManager.instance.AddOrb(goldOrb);
                                        EffectManager.SimpleImpactEffect(HealthComponent.AssetReferences.loseCoinsImpactEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
                                    }
                                }
                            }

                            // if pennies config on
                            if ((int)Run.TimeStamp.tNow > handledSecond) {
                                if (usePennies.Value) {
                                    if (playerHealthComponent.itemCounts.goldOnHurt > 0) {
                                        int num11 = 3;
                                        GoldOrb goldOrb2 = new GoldOrb();
                                        goldOrb2.origin = damageInfo.position;
                                        goldOrb2.target = player.mainHurtBox;
                                        goldOrb2.goldAmount = (uint)((float)(playerHealthComponent.itemCounts.goldOnHurt * num11) * Run.instance.difficultyCoefficient * penniesPayMultiplier.Value);
                                        OrbManager.instance.AddOrb(goldOrb2);
                                        EffectManager.SimpleImpactEffect(HealthComponent.AssetReferences.gainCoinsImpactEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
                                    }
                                }
                            }
                            
                        }
                        if (playerHealthComponent.itemCounts.adaptiveArmor > 0) {
                            float num12 = num3 / playerHealthComponent.fullCombinedHealth * 100f * 30f * (float)playerHealthComponent.itemCounts.adaptiveArmor;
                            playerHealthComponent.adaptiveArmorValue = Mathf.Min(playerHealthComponent.adaptiveArmorValue + num12, 400f);
                        }
                        float num13 = num3;
                        // running stop shield regen config on
                        if (num13 > 0f) {
                            playerHealthComponent.isShieldRegenForced = false;
                            player.outOfDanger = false;
                            player.outOfDangerStopwatch = 0f;
                        }
/*                        // if curse config on
                        if(walkingAddCurse.Value) {
                            if (playerHealthComponent.body.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse8) {
                                float num14 = num13 / playerHealthComponent.fullCombinedHealth * 100f;
                                float num15 = 0.4f;
                                int num16 = Mathf.FloorToInt(num14 * num15);
                                for (int k = 0; k < num16; k++) {
                                    player.AddBuff(RoR2Content.Buffs.PermanentCurse);
                                }
                            }
                        }*/
                        if (num13 > 0f && playerHealthComponent.barrier > 0f) {
                            if (num13 <= playerHealthComponent.barrier) {
                                playerHealthComponent.Networkbarrier = playerHealthComponent.barrier - num13;
                                num13 = 0f;
                            } else {
                                num13 -= playerHealthComponent.barrier;
                                playerHealthComponent.Networkbarrier = 0f;
                            }
                        }
                        if (num13 > 0f && playerHealthComponent.shield > 0f) {
                            if (num13 <= playerHealthComponent.shield) {
                                playerHealthComponent.Networkshield = playerHealthComponent.shield - num13;
                                num13 = 0f;
                            } else {
                                num13 -= playerHealthComponent.shield;
                                playerHealthComponent.Networkshield = 0f;
                                float scale = 1f;
                                if ((bool)playerHealthComponent.body) {
                                    scale = playerHealthComponent.body.radius;
                                }
                                EffectManager.SpawnEffect(HealthComponent.AssetReferences.shieldBreakEffectPrefab, new EffectData
                                {
                                    origin = base.transform.position,
                                    scale = scale
                                }, transmit: true);
                            }
                        }
                        /*bool flag4 = (damageInfo.damageType & DamageType.VoidDeath) != 0 && (body.bodyFlags & CharacterBody.BodyFlags.ImmuneToVoidDeath) == 0;
                        float executionHealthLost = 0f;*/
                        GameObject gameObject = null;
                        if (num13 > 0f) {
                            float num17 = playerHealthComponent.health - num13;
                            // If cant die from walking setting
                            if(!walkingCanKill.Value) {
                                if (num17 < 1f && playerHealthComponent.health >= 1f) {
                                    num17 = 1f;
                                }
                            }
                            playerHealthComponent.Networkhealth = num17;
                        }
                        /*float num18 = float.NegativeInfinity;
                        bool flag5 = (body.bodyFlags & CharacterBody.BodyFlags.ImmuneToExecutes) != 0;
                        if (!flag4 && !flag5) {
                            if (isInFrozenState && num18 < 0.3f) {
                                num18 = 0.3f;
                                gameObject = FrozenState.executeEffectPrefab;
                            }
                            if ((bool)characterBody) {
                                if (body.isElite) {
                                    float executeEliteHealthFraction = characterBody.executeEliteHealthFraction;
                                    if (num18 < executeEliteHealthFraction) {
                                        num18 = executeEliteHealthFraction;
                                        gameObject = AssetReferences.executeEffectPrefab;
                                    }
                                }
                                if (!body.isBoss && (bool)characterBody.inventory && Util.CheckRoll((float)characterBody.inventory.GetItemCount(DLC1Content.Items.CritGlassesVoid) * 0.5f * damageInfo.procCoefficient, characterBody.master)) {
                                    flag4 = true;
                                    gameObject = AssetReferences.critGlassesVoidExecuteEffectPrefab;
                                    damageInfo.damageType |= DamageType.VoidDeath;
                                }
                            }
                        }
                        if (flag4 || (num18 > 0f && combinedHealthFraction <= num18)) {
                            flag4 = true;
                            executionHealthLost = Mathf.Max(combinedHealth, 0f);
                            if (health > 0f) {
                                Networkhealth = 0f;
                            }
                            if (shield > 0f) {
                                Networkshield = 0f;
                            }
                            if (barrier > 0f) {
                                Networkbarrier = 0f;
                            }
                        }
                        if (damageInfo.canRejectForce) {
                            TakeDamageForce(damageInfo);
                        }*/
                        /*DamageReport damageReport = new DamageReport(damageInfo, this, num3, num);
                        IOnTakeDamageServerReceiver[] array2 = onTakeDamageReceivers;
                        for (int i = 0; i < array2.Length; i++) {
                            array2[i].OnTakeDamageServer(damageReport);
                        }
                        if (num3 > 0f) {
                            SendDamageDealt(damageReport);
                        }
                        UpdateLastHitTime(damageReport.damageDealt, damageInfo.position, (damageInfo.damageType & DamageType.Silent) != 0, damageInfo.attacker);
                        if ((bool)damageInfo.attacker) {
                            List<IOnDamageDealtServerReceiver> gameObjectComponents = GetComponentsCache<IOnDamageDealtServerReceiver>.GetGameObjectComponents(damageInfo.attacker);
                            foreach (IOnDamageDealtServerReceiver item in gameObjectComponents) {
                                item.OnDamageDealtServer(damageReport);
                            }
                            GetComponentsCache<IOnDamageDealtServerReceiver>.ReturnBuffer(gameObjectComponents);
                        }
                        if ((bool)damageInfo.inflictor) {
                            List<IOnDamageInflictedServerReceiver> gameObjectComponents2 = GetComponentsCache<IOnDamageInflictedServerReceiver>.GetGameObjectComponents(damageInfo.inflictor);
                            foreach (IOnDamageInflictedServerReceiver item2 in gameObjectComponents2) {
                                item2.OnDamageInflictedServer(damageReport);
                            }
                            GetComponentsCache<IOnDamageInflictedServerReceiver>.ReturnBuffer(gameObjectComponents2);
                        }
                        GlobalEventManager.ServerDamageDealt(damageReport);*/
                        //playerHealthComponent.UpdateLastHitTime(damageReport.damageDealt, damageInfo.position, (damageInfo.damageType & DamageType.Silent) != 0, damageInfo.attacker);
                        if (!playerHealthComponent.alive) {
                            playerHealthComponent.killingDamageType = damageInfo.damageType;
                            /*if (flag4) {
                                GlobalEventManager.ServerCharacterExecuted(damageReport, executionHealthLost);
                                if ((object)gameObject != null) {
                                    EffectManager.SpawnEffect(gameObject, new EffectData
                                    {
                                        origin = body.corePosition,
                                        scale = (body ? body.radius : 1f)
                                    }, transmit: true);
                                }
                            }
                            IOnKilledServerReceiver[] components = GetComponents<IOnKilledServerReceiver>();
                            for (int i = 0; i < components.Length; i++) {
                                components[i].OnKilledServer(damageReport);
                            }
                            if ((bool)damageInfo.attacker) {
                                IOnKilledOtherServerReceiver[] components2 = damageInfo.attacker.GetComponents<IOnKilledOtherServerReceiver>();
                                for (int i = 0; i < components2.Length; i++) {
                                    components2[i].OnKilledOtherServer(damageReport);
                                }
                            }
                            if (Util.CheckRoll(globalDeathEventChanceCoefficient * 100f)) {
                                GlobalEventManager.instance.OnCharacterDeath(damageReport);
                            }*/
                        } else {
                            if(useMedkit.Value && num3 > 0) {
                                if (playerHealthComponent.itemCounts.medkit > 0) {
                                    player.AddTimedBuff(RoR2Content.Buffs.MedkitHeal, 2f);
                                }  
                            }
                            if (playerHealthComponent.itemCounts.healingPotion > 0 && playerHealthComponent.isHealthLow) {
                                player.inventory.RemoveItem(DLC1Content.Items.HealingPotion);
                                player.inventory.GiveItem(DLC1Content.Items.HealingPotionConsumed);
                                CharacterMasterNotificationQueue.SendTransformNotification(player.master, DLC1Content.Items.HealingPotion.itemIndex, DLC1Content.Items.HealingPotionConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                playerHealthComponent.HealFraction(0.75f, default(ProcChainMask));
                                EffectData effectData = new EffectData
                                {
                                    origin = base.transform.position
                                };
                                effectData.SetNetworkedObjectReference(base.gameObject);
                                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HealingPotionEffect"), effectData, transmit: true);
                            }
                            if (playerHealthComponent.itemCounts.fragileDamageBonus > 0 && playerHealthComponent.isHealthLow) {
                                player.inventory.GiveItem(DLC1Content.Items.FragileDamageBonusConsumed, playerHealthComponent.itemCounts.fragileDamageBonus);
                                player.inventory.RemoveItem(DLC1Content.Items.FragileDamageBonus, playerHealthComponent.itemCounts.fragileDamageBonus);
                                CharacterMasterNotificationQueue.SendTransformNotification(player.master, DLC1Content.Items.FragileDamageBonus.itemIndex, DLC1Content.Items.FragileDamageBonusConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                EffectData effectData2 = new EffectData
                                {
                                    origin = base.transform.position
                                };
                                effectData2.SetNetworkedObjectReference(base.gameObject);
                                EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData2, transmit: true);
                            }
                            // If thorns activate on walk
                            if ((int)Run.TimeStamp.tNow > handledSecond) {
                                if (useThorns.Value && num3 > 0) {
                                    int a = 5 + 2 * (playerHealthComponent.itemCounts.thorns - 1);
                                    if (playerHealthComponent.itemCounts.thorns > 0) {
                                        bool flag6 = playerHealthComponent.itemCounts.invadingDoppelganger > 0;
                                        float radius = 25f + 10f * (float)(playerHealthComponent.itemCounts.thorns - 1);
                                        bool isCrit = playerHealthComponent.body.RollCrit();
                                        float damageValue = 1.6f * playerHealthComponent.body.damage;
                                        TeamIndex teamIndex2 = playerHealthComponent.body.teamComponent.teamIndex;
                                        HurtBox[] hurtBoxes = new SphereSearch
                                        {
                                            origin = player.transform.position,
                                            radius = radius,
                                            mask = LayerIndex.entityPrecise.mask,
                                            queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
                                        }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamIndex2)).OrderCandidatesByDistance()
                                            .FilterCandidatesByDistinctHurtBoxEntities()
                                            .GetHurtBoxes();
                                        for (int l = 0; l < Mathf.Min(a, hurtBoxes.Length); l++) {
                                            LightningOrb lightningOrb = new LightningOrb();
                                            lightningOrb.attacker = base.gameObject;
                                            lightningOrb.bouncedObjects = null;
                                            lightningOrb.bouncesRemaining = 0;
                                            lightningOrb.damageCoefficientPerBounce = 1f;
                                            lightningOrb.damageColorIndex = DamageColorIndex.Item;
                                            lightningOrb.damageValue = damageValue;
                                            lightningOrb.isCrit = isCrit;
                                            lightningOrb.lightningType = LightningOrb.LightningType.RazorWire;
                                            lightningOrb.origin = player.transform.position;
                                            lightningOrb.procChainMask = default(ProcChainMask);
                                            lightningOrb.procChainMask.AddProc(ProcType.Thorns);
                                            lightningOrb.procCoefficient = (flag6 ? 0f : 0.5f);
                                            lightningOrb.range = 0f;
                                            lightningOrb.teamIndex = teamIndex2;
                                            lightningOrb.target = hurtBoxes[l];
                                            OrbManager.instance.AddOrb(lightningOrb);
                                        }
                                    }
                                }
                            }
                            
                        }
                    }

                    if ((int)Run.TimeStamp.tNow > handledSecond) {
                        handledSecond = (int)Run.TimeStamp.tNow;
                    }
                }
            } else {
                handledSecond = 0;
                exitPod = false;
            }
        }


        private void configs()
        {

            healthDamagePercentage = Config.Bind("General", "Walking Damage Percent", 1f, "Percent of full health done per second while walking. Full health includes base health and shield.\nDefault is 1 (1%).");
            ModSettingsManager.AddOption(new StepSliderOption(healthDamagePercentage,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 100f,
                    increment = 1f
                }));

            sprintDamageMultiplier = Config.Bind("General", "Sprinting Damage Multiplier", 2f, "How much to multiply walking damage by when sprinting.\nDefault is 2 (2X).");
            ModSettingsManager.AddOption(new StepSliderOption(sprintDamageMultiplier,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 10f,
                    increment = 1f
                }));

            useBaseArmour = Config.Bind("General", "Apply Player Base Armour", false, "If base player armour should be considered when calculating walking damage.\nDefault is false.");
            ModSettingsManager.AddOption(new CheckBoxOption(useBaseArmour));

            useBear = Config.Bind("General", "Trigger Tougher Times", true, "If walking damage can trigger and be blocked by Tougher Times.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(useBear));

            useRap = Config.Bind("General", "Trigger Repulsion Armour Plate", true, "If walking damage can trigger and be reduced by Repulsion Armour Plate.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(useRap));

            useEgg = Config.Bind("General", "Trigger Planula", true, "If walking damage can trigger and activate Planula.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(useEgg));

            eggHealMultiplier = Config.Bind("General", "Planula Heal Multiplier", 1f, "How much to multiply Planula heals by for balancing.\nDefault is 1 (1X).");
            ModSettingsManager.AddOption(new StepSliderOption(eggHealMultiplier,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 5f,
                    increment = 0.1f
                }));

            useBrittleCrown = Config.Bind("General", "Trigger Brittle Crown", false, "If walking damage can trigger and lose gold from Brittle Crown.\nDefault is false.");
            ModSettingsManager.AddOption(new CheckBoxOption(useBrittleCrown));

            usePennies = Config.Bind("General", "Trigger Roll of Pennies", true, "If walking damage can trigger and gain gold from Roll of Pennies.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(usePennies));

            penniesPayMultiplier = Config.Bind("General", "Roll of Pennies Pay Multiplier", 1f, "How much to multiply Rolls of Pennies payment by for balancing.\nDefault is 1 (1X).");
            ModSettingsManager.AddOption(new StepSliderOption(penniesPayMultiplier,
                new StepSliderConfig
                {
                    min = 0f,
                    max = 5f,
                    increment = 0.1f
                }));
            
            useThorns = Config.Bind("General", "Trigger Razerwire", true, "If walking damage can trigger Razerwire.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(useThorns));

            useMedkit = Config.Bind("General", "Trigger Medkit", true, "If walking damage can trigger Medkit heals.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(useMedkit));

            /*walkingStopShieldRegen = Config.Bind("General", "Stop Shield Regen", true, "If walking damage can prevent shield from regenning.\nDefault is true.");
            ModSettingsManager.AddOption(new CheckBoxOption(walkingStopShieldRegen));*/

            /*walkingAddCurse = Config.Bind("General", "Add Curse Stacks", false, "If walking damage can add permanent damage curse stacks in Eclipse 8. This will only happen if the walking damage percent is set high enough.\nDefault is false.");
            ModSettingsManager.AddOption(new CheckBoxOption(walkingAddCurse));*/

            walkingCanKill = Config.Bind("General", "Can Die From Walking", false, "If walking damage can take you below 1 health and kill you.\nDefault is false.");
            ModSettingsManager.AddOption(new CheckBoxOption(walkingCanKill));
        }
    }
}
