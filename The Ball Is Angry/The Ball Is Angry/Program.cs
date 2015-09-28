﻿using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace The_Ball_Is_Angry
{
    class Program
    {
        static string Author = "iCreative";
        static string AddonName = "The ball is angry";
        static float RefreshTime = 0.4f;
        static Dictionary<int, DamageInfo> PredictedDamage = new Dictionary<int, DamageInfo>() { };
        static AIHeroClient myHero { get { return ObjectManager.Player; } }
        static Vector3 mousePos { get { return Game.CursorPos; } }
        static Menu menu;
        static Dictionary<string, Menu> SubMenu = new Dictionary<string, Menu>() { };
        static Spell.Skillshot Q, W, E, R;
        static Spell.Targeted Ignite;
        static GameObject E_Target = null;
        static GameObject BallObject;
        static float LastGapclose = 0f;
        static Vector3 Ball
        {
            get
            {
                if (BallObject != null && BallObject.IsValid)
                {
                    return BallObject.Position;
                }
                return myHero.Position;
            }
        }
        static float Overkill
        {
            get
            {
                return (float)((100 + SubMenu["Misc"]["Overkill"].Cast<Slider>().CurrentValue) / 100);
            }
        }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoad;
        }
        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Orianna) { return; }
            Chat.Print(AddonName + " made by " + Author + " loaded, have fun!.");
            Q = new Spell.Skillshot(SpellSlot.Q, 815, SkillShotType.Circular, 0, 1200, 130);
            Q.AllowedCollisionCount = int.MaxValue;
            W = new Spell.Skillshot(SpellSlot.W, 255, SkillShotType.Circular, 250, int.MaxValue, 30);
            W.AllowedCollisionCount = int.MaxValue;
            E = new Spell.Skillshot(SpellSlot.E, 1095, SkillShotType.Circular, 0, 1800, 85);
            E.AllowedCollisionCount = int.MaxValue;
            R = new Spell.Skillshot(SpellSlot.R, 400, SkillShotType.Circular, 500, int.MaxValue, 30);
            R.AllowedCollisionCount = int.MaxValue;
            var slot = myHero.GetSpellSlotFromName("summonerdot");
            if (slot != SpellSlot.Unknown)
            {
                Ignite = new Spell.Targeted(slot, 600);
            }
            menu = MainMenu.AddMenu(AddonName, AddonName + " by " + Author + " v1.3");
            menu.AddLabel(AddonName + " made by " + Author);

            SubMenu["Combo"] = menu.AddSubMenu("Combo", "Combo");
            SubMenu["Combo"].Add("TF", new Slider("Use TeamFight Logic if enemies near >=", 3, 1, 5));
            SubMenu["Combo"].AddGroupLabel("1 vs 1 Logic");
            SubMenu["Combo"].Add("Q", new CheckBox("Use Q On Target", true));
            SubMenu["Combo"].Add("W", new CheckBox("Use W On Target", true));
            SubMenu["Combo"].Add("R", new CheckBox("Use R On Target If Killable", true));
            SubMenu["Combo"].AddGroupLabel("TeamFight Logic");
            SubMenu["Combo"].Add("Q2", new Slider("Use Q If Hit", 2, 1, 5));
            SubMenu["Combo"].Add("W2", new Slider("Use W If Hit", 2, 1, 5));
            SubMenu["Combo"].Add("E", new Slider("Use E If Hit", 1, 1, 5));
            SubMenu["Combo"].Add("E2", new Slider("Use E If HealthPercent <=", 30, 0, 100));
            SubMenu["Combo"].Add("R2", new Slider("Use R if Hit", 3, 1, 5));

            SubMenu["Harass"] = menu.AddSubMenu("Harass", "Harass");
            SubMenu["Harass"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Harass"].Add("W", new CheckBox("Use W", true));
            SubMenu["Harass"].Add("E", new Slider("Use E If Hit", 1, 1, 5));
            SubMenu["Harass"].Add("E2", new Slider("Use E If HealthPercent <=", 30, 0, 100));
            SubMenu["Harass"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["LaneClear"] = menu.AddSubMenu("LaneClear", "LaneClear");
            SubMenu["LaneClear"].AddGroupLabel("LaneClear Minions");
            SubMenu["LaneClear"].Add("Q", new Slider("Use Q If Hit", 4, 0, 10));
            SubMenu["LaneClear"].Add("W", new Slider("Use W If Hit", 3, 0, 10));
            SubMenu["LaneClear"].Add("E", new Slider("Use E If Hit", 6, 0, 10));
            SubMenu["LaneClear"].AddGroupLabel("Unkillable minions");
            SubMenu["LaneClear"].Add("Q2", new CheckBox("Use Q", true));
            SubMenu["LaneClear"].Add("Mana", new Slider("Min. Mana Percent:", 50, 0, 100));

            SubMenu["LastHit"] = menu.AddSubMenu("LastHit", "LastHit");
            SubMenu["LastHit"].AddGroupLabel("Unkillable minions");
            SubMenu["LastHit"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["LastHit"].Add("Mana", new Slider("Min. Mana Percent:", 50, 0, 100));

            SubMenu["JungleClear"] = menu.AddSubMenu("JungleClear", "JungleClear");
            SubMenu["JungleClear"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["JungleClear"].Add("W", new CheckBox("Use W", true));
            SubMenu["JungleClear"].Add("E", new CheckBox("Use E", true));
            SubMenu["JungleClear"].Add("Mana", new Slider("Min. Mana Percent:", 20, 0, 100));

            SubMenu["KillSteal"] = menu.AddSubMenu("KillSteal", "KillSteal");
            SubMenu["KillSteal"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["KillSteal"].Add("W", new CheckBox("Use W", true));
            SubMenu["KillSteal"].Add("E", new CheckBox("Use E", true));
            SubMenu["KillSteal"].Add("R", new CheckBox("Use R", false));
            SubMenu["KillSteal"].Add("Ignite", new CheckBox("Use Ignite", true));

            SubMenu["Flee"] = menu.AddSubMenu("Flee", "Flee");
            SubMenu["Flee"].Add("Q", new CheckBox("Use Q", true));
            SubMenu["Flee"].Add("W", new CheckBox("Use W", true));
            SubMenu["Flee"].Add("E", new CheckBox("Use E", true));

            SubMenu["Draw"] = menu.AddSubMenu("Drawing", "Drawing");
            SubMenu["Draw"].Add("Ball", new CheckBox("Draw ball position", true));
            SubMenu["Draw"].Add("W", new CheckBox("Draw W Range", true));
            SubMenu["Draw"].Add("R", new CheckBox("Draw R Range", true));

            SubMenu["Misc"] = menu.AddSubMenu("Misc", "Misc");
            SubMenu["Misc"].Add("Overkill", new Slider("Overkill % for damage prediction", 10, 0, 100));
            SubMenu["Misc"].Add("BlockR", new CheckBox("Block R if will not hit", true));
            SubMenu["Misc"].Add("R", new CheckBox("Use R to Interrupt Channeling", true));
            SubMenu["Misc"].Add("E", new CheckBox("Use E to Initiate", true));
            SubMenu["Misc"].Add("W2", new Slider("Use W if Hit", 3, 1, 5));
            SubMenu["Misc"].Add("R2", new Slider("Use R if Hit", 4, 1, 5));

            Game.OnTick += OnTick;
            GameObject.OnCreate += OnCreateObj;
            GameObject.OnDelete += OnDelete;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            Gapcloser.OnGapcloser += OnGapcloser;
            Spellbook.OnCastSpell += OnCastSpell;
            BallObject = ObjectManager.Get<GameObject>().FirstOrDefault(obj => obj.Name != null && obj.IsValid && obj.Name.ToLower().Contains("doomball"));
            Orbwalker.OnUnkillableMinion += OnUnkillableMinion;
        }

        private static void OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            /**
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {

                if (myHero.ManaPercent >= SubMenu["LastHit"]["Mana"].Cast<Slider>().CurrentValue)
                {
                    if (target.IsValidTarget(Q.Range) && SubMenu["LastHit"]["Q"].Cast<CheckBox>().CurrentValue)
                    {
                        int time = (int)(1000 * Extensions.Distance(Q.SourcePosition.Value, target) / Q.Speed + Q.CastDelay);
                        var health = Prediction.Health.GetPrediction(target, time);
                        if (health >= 0 && health <= Damage(target, Q.Slot))
                        {
                            CastQ(target);
                        }
                    }
                }
            }
            **/
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                if (args.Slot == R.Slot && SubMenu["Misc"]["BlockR"].Cast<CheckBox>().CurrentValue && HitR() == 0)
                {
                    args.Process = false;
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && !Orbwalker.CanMove && Orbwalker.LastTarget.Type != myHero.Type)
                {
                    args.Process = false;
                }
            }
        }

        private static void OnTick(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            Q.SourcePosition = Ball;
            E.SourcePosition = Ball;
            W.SourcePosition = Ball;
            R.SourcePosition = Ball;
            if (R.IsReady() && SubMenu["Misc"]["R2"].Cast<Slider>().CurrentValue <= HitR())
            {
                myHero.Spellbook.CastSpell(SpellSlot.R);
            }
            if (W.IsReady() && SubMenu["Misc"]["W2"].Cast<Slider>().CurrentValue <= HitW(HeroManager.Enemies.ToList<Obj_AI_Base>()))
            {
                myHero.Spellbook.CastSpell(SpellSlot.W);
            }
            KillSteal();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                {
                    JungleClear();
                }
                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                {
                    LaneClear();
                }
            }
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHit();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
        }

        private static void KillSteal()
        {
            foreach (AIHeroClient enemy in HeroManager.Enemies)
            {
                if (enemy.IsValidTarget(E.Range) && enemy.HealthPercent <= 40)
                {
                    var damageI = GetBestCombo(enemy);
                    if (damageI.Damage >= enemy.Health)
                    {
                        if (SubMenu["KillSteal"]["Q"].Cast<CheckBox>().CurrentValue && (Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q)) { CastQ(enemy); }
                        if (SubMenu["KillSteal"]["W"].Cast<CheckBox>().CurrentValue && (Damage(enemy, W.Slot) >= enemy.Health || damageI.W)) { CastW(enemy); }
                        if (SubMenu["KillSteal"]["E"].Cast<CheckBox>().CurrentValue && (Damage(enemy, E.Slot) >= enemy.Health || damageI.E)) { CastE(enemy); }
                        if (SubMenu["KillSteal"]["R"].Cast<CheckBox>().CurrentValue && (Damage(enemy, R.Slot) >= enemy.Health || damageI.R)) { CastR(enemy); }
                        if ((SubMenu["KillSteal"]["E"].Cast<CheckBox>().CurrentValue || SubMenu["KillSteal"]["Q"].Cast<CheckBox>().CurrentValue) && ((Damage(enemy, Q.Slot) >= enemy.Health || damageI.Q) || (Damage(enemy, W.Slot) >= enemy.Health || damageI.W) || (Damage(enemy, R.Slot) >= enemy.Health || damageI.R))) { ThrowBall(enemy); }
                    }
                    if (Ignite != null && SubMenu["KillSteal"]["Ignite"].Cast<CheckBox>().CurrentValue && Ignite.IsReady() && myHero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) >= enemy.Health)
                    {
                        Ignite.Cast(enemy);
                    }
                }
            }
        }

        private static void Combo()
        {
            AIHeroClient target = TargetSelector.GetTarget(Q.Range + Q.Width, DamageType.Magical);
            if (target.IsValidTarget())
            {
                var damageI = GetBestCombo(target);
                if (Q.IsReady() && SubMenu["Combo"]["Q2"].Cast<Slider>().CurrentValue > 0)
                {
                    List<Obj_AI_Base> list = HeroManager.Enemies.Where<Obj_AI_Base>(o => o.IsValidTarget(Q.Range + Q.Width)).ToList();
                    if (list.Count >= SubMenu["Combo"]["Q2"].Cast<Slider>().CurrentValue)
                    {
                        var info = BestHitQ(list);
                        if (info.Item1 != Vector3.Zero && info.Item2 >= SubMenu["Combo"]["Q2"].Cast<Slider>().CurrentValue)
                        {
                            Q.Cast(info.Item1);
                        }
                    }

                }
                if (SubMenu["Combo"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                if (W.IsReady() && SubMenu["Combo"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
                if (W.IsReady() && SubMenu["Combo"]["W2"].Cast<Slider>().CurrentValue > 0)
                {
                    if (HitW(HeroManager.Enemies.ToList<Obj_AI_Base>()) >= SubMenu["Combo"]["W2"].Cast<Slider>().CurrentValue)
                    {
                        myHero.Spellbook.CastSpell(W.Slot);
                    }

                }
                if (E.IsReady() && SubMenu["Combo"]["E"].Cast<Slider>().CurrentValue > 0)
                {
                    List<Obj_AI_Base> list = HeroManager.Enemies.Where<Obj_AI_Base>(o => o.IsValidTarget(E.Range)).ToList();
                    if (list.Count >= SubMenu["Combo"]["E"].Cast<Slider>().CurrentValue)
                    {
                        var info = BestHitE(list);
                        if (info.Item1 != null && info.Item2 != null)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            int bestHit = info.Item2;
                            if (bestHit > SubMenu["Combo"]["E"].Cast<Slider>().CurrentValue && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
                if (E.IsReady() && SubMenu["Combo"]["E2"].Cast<Slider>().CurrentValue > myHero.HealthPercent && myHero.HealthPercent < target.HealthPercent)
                {
                    if (target.GetAutoAttackRange(myHero) < Extensions.Distance(myHero, target))
                    {
                        CastE(myHero);
                    }
                }
                if (Ball.CountEnemiesInRange(Q.Range) <= SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)
                {
                    if (SubMenu["Combo"]["R"].Cast<CheckBox>().CurrentValue && damageI.R && damageI.Damage >= target.Health) { CastR(target); }
                }
                if (Ball.CountEnemiesInRange(Q.Range) >= SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)
                {
                    if (HitR() >= SubMenu["Combo"]["R2"].Cast<Slider>().CurrentValue)
                    {
                        myHero.Spellbook.CastSpell(R.Slot);
                    }
                }
            }
        }

        private static void Harass()
        {
            AIHeroClient target = TargetSelector.GetTarget(Q.Range + Q.Width, DamageType.Magical);
            if (target.IsValidTarget() && myHero.ManaPercent >= SubMenu["Harass"]["Mana"].Cast<Slider>().CurrentValue)
            {
                var damageI = GetBestCombo(target);
                if (SubMenu["Harass"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(target); }
                if (W.IsReady() && SubMenu["Harass"]["W"].Cast<CheckBox>().CurrentValue) { CastW(target); }
                if (E.IsReady() && SubMenu["Harass"]["E"].Cast<Slider>().CurrentValue > 0)
                {
                    List<Obj_AI_Base> list = HeroManager.Enemies.Where<Obj_AI_Base>(o => o.IsValidTarget(E.Range)).ToList();
                    if (list.Count >= SubMenu["Harass"]["E"].Cast<Slider>().CurrentValue)
                    {
                        var info = BestHitE(list);
                        if (info.Item1 != null && info.Item2 != null)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            int bestHit = info.Item2;
                            if (bestHit > SubMenu["Harass"]["E"].Cast<Slider>().CurrentValue && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
                if (E.IsReady() && SubMenu["Harass"]["E2"].Cast<Slider>().CurrentValue > myHero.HealthPercent && myHero.HealthPercent < target.HealthPercent)
                {
                    if (target.GetAutoAttackRange(myHero) < Extensions.Distance(myHero, target))
                    {
                        CastE(myHero);
                    }
                }
            }
        }
        private static void LaneClear()
        {
            if (myHero.ManaPercent >= SubMenu["LaneClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (E.IsReady() && SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue > 0)
                {
                    var minions = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), E.Range, true);
                    if (minions.Count >= SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue)
                    {
                        var info = BestHitE(minions);
                        if (info.Item1 != null && info.Item2 != null)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            int bestHit = info.Item2;
                            if (SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue > 0 && bestHit >= SubMenu["LaneClear"]["E"].Cast<Slider>().CurrentValue && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
                if (Q.IsReady() && SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue > 0)
                {
                    var minions = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), Q.Range, true);
                    if (minions.Count >= SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue)
                    {
                        var info2 = BestHitQ(minions);
                        if (info2.Item1 != Vector3.Zero && info2.Item2 != null)
                        {
                            Vector3 bestPos = info2.Item1;
                            int bestHit = info2.Item2;
                            if (SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue > 0 && bestHit >= SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue)
                            {
                                Q.Cast(bestPos);
                            }
                        }
                    }
                }
                if (W.IsReady() && SubMenu["LaneClear"]["W"].Cast<Slider>().CurrentValue > 0 && HitW(EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, Ball.To2D(), W.Range, true)) >= SubMenu["LaneClear"]["Q"].Cast<Slider>().CurrentValue)
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
                if (SubMenu["LaneClear"]["Q2"].Cast<CheckBox>().CurrentValue)
                {
                    QLastHit();
                }
            }
        }
        private static void LastHit()
        {
            if (myHero.ManaPercent >= SubMenu["LastHit"]["Mana"].Cast<Slider>().CurrentValue)
            {
                if (SubMenu["LastHit"]["Q"].Cast<CheckBox>().CurrentValue)
                {
                    QLastHit();
                }
            }
        }
        private static void QLastHit()
        {
            if (Q.IsReady())
            {
                foreach (Obj_AI_Base minion in EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), Q.Range, true).Where(o => o.Health <= 2.0f * Damage(o, Q.Slot)))
                {
                    bool CanCalculate = false;
                    if (minion.IsValidTarget())
                    {
                        if (!Orbwalker.CanAutoAttack)
                        {
                            if (Orbwalker.CanMove && Orbwalker.LastTarget != null && Orbwalker.LastTarget.NetworkId != minion.NetworkId)
                            {
                                CanCalculate = true;
                            }
                        }
                        else
                        {
                            if (myHero.GetAutoAttackRange(minion) < Extensions.Distance(myHero, minion))
                            {
                                CanCalculate = true;
                            }
                            else
                            {
                                var speed = myHero.BasicAttack.MissileSpeed;
                                var time = (int)(1000 * Extensions.Distance(myHero, minion) / speed + myHero.AttackCastDelay * 1000 + Game.Ping - 100);
                                var predHealth = Prediction.Health.GetPrediction(minion, time);
                                if (predHealth <= 0)
                                {
                                    CanCalculate = true;
                                }
                                /**
                                if (!Orbwalker.CanBeLastHitted(minion))
                                {
                                    CanCalculate = true;
                                }**/
                            }
                        }
                    }
                    if (CanCalculate)
                    {
                        var dmg = Damage(minion, Q.Slot);
                        var time = (int)(1000 * Extensions.Distance(Q.SourcePosition.Value, minion) / Q.Speed + Q.CastDelay - 70);
                        var predHealth = Prediction.Health.GetPrediction(minion, time);
                        if (time > 0 && predHealth == minion.Health) { return; }
                        if (dmg > predHealth && predHealth > 0)
                        {
                            CastQ(minion);
                        }
                    }
                }
            }

        }
        private static void JungleClear()
        {
            if (myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
            {
                foreach (Obj_AI_Base minion in EntityManager.GetJungleMonsters(myHero.Position.To2D(), 1000f))
                {
                    if (minion.IsValidTarget() && myHero.ManaPercent >= SubMenu["JungleClear"]["Mana"].Cast<Slider>().CurrentValue)
                    {
                        if (SubMenu["JungleClear"]["E"].Cast<CheckBox>().CurrentValue) { CastE(minion); }
                        if (SubMenu["JungleClear"]["Q"].Cast<CheckBox>().CurrentValue) { CastQ(minion); }
                        if (SubMenu["JungleClear"]["W"].Cast<CheckBox>().CurrentValue) { CastW(minion); }
                    }
                }
            }
        }

        private static void Flee()
        {
            if (SubMenu["Flee"]["Q"].Cast<CheckBox>().CurrentValue)
            {
                if (Q.IsReady() && Extensions.Distance(myHero, Ball) > W.Range && !E.IsReady() && BallObject != null && !BallObject.Name.ToLower().Contains("missile"))
                {
                    myHero.Spellbook.CastSpell(Q.Slot, myHero.ServerPosition);
                }
            }
            if (SubMenu["Flee"]["W"].Cast<CheckBox>().CurrentValue)
            {
                if (W.IsReady() && Extensions.Distance(myHero, Ball) < W.Range)
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
            }
            if (SubMenu["Flee"]["E"].Cast<CheckBox>().CurrentValue)
            {
                if (E.IsReady() && Extensions.Distance(myHero, Ball) > W.Range)
                {
                    CastE(myHero);
                }
            }
        }
        private static void CastQ(Obj_AI_Base target, int minhits = 1)
        {
            if (Q.IsReady() && target.IsValidTarget(Q.Range + Q.Width))
            {
                if (E.IsReady() && target.Type == myHero.Type && Extensions.Distance(Ball, target) > Q.Range * 1.2f && Extensions.Distance(myHero, target) < Extensions.Distance(Ball, target))
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.HitChancePercent <= 5)
                    {
                        CastE(myHero);
                    }
                }
                if (Game.Time - LastGapclose < 1.0f) { return; }
                List<Obj_AI_Base> list = new List<Obj_AI_Base>();
                if (target.Type == GameObjectType.AIHeroClient)
                {
                    list = HeroManager.Enemies.ToList<Obj_AI_Base>();
                }
                else
                {
                    if (EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), E.Range).Count > 0)
                    {
                        list = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), E.Range);
                    }
                    else if (EntityManager.GetJungleMonsters(myHero.Position.To2D(), E.Range).Count > 0)
                    {
                        list = EntityManager.GetJungleMonsters(myHero.Position.To2D(), E.Range).ToList<Obj_AI_Base>();
                    }
                }
                if (list.Count < minhits) { return; }
                var t = BestHitQ(list, target);
                if (t.Item1 != Vector3.Zero && t.Item2 >= minhits)
                {
                    Q.Cast(t.Item1);
                }
                /**
                var pred = Q.GetPrediction(target);
                if (pred.HitChancePercent >= 70)
                {
                    Q.Cast(pred.CastPosition);
                }**/
            }
        }

        private static void CastW(Obj_AI_Base target, float percent = 70)
        {
            if (W.IsReady())
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChancePercent >= percent && Extensions.Distance(Ball, pred.CastPosition) <= W.Range)
                {
                    myHero.Spellbook.CastSpell(W.Slot);
                }
            }
        }
        private static void CastE(Obj_AI_Base target)
        {
            if (E.IsReady() && target != null && target.IsValid && Extensions.Distance(myHero, target) < E.Range)
            {
                if (target.Team == myHero.Team)
                {
                    myHero.Spellbook.CastSpell(E.Slot, target);
                }
                else
                {
                    List<Obj_AI_Base> list = new List<Obj_AI_Base>();
                    if (target.Type == GameObjectType.AIHeroClient)
                    {
                        list = HeroManager.Enemies.ToList<Obj_AI_Base>();
                    }
                    else
                    {
                        if (EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), E.Range).Count > 0)
                        {
                            list = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.Position.To2D(), E.Range);
                        }
                        else if (EntityManager.GetJungleMonsters(myHero.Position.To2D(), E.Range).Count > 0)
                        {
                            list = EntityManager.GetJungleMonsters(myHero.Position.To2D(), E.Range).ToList<Obj_AI_Base>();
                        }
                    }
                    if (list.Count > 0)
                    {
                        var info = BestHitE(list);
                        if (info.Item1 != null && info.Item2 != null)
                        {
                            Obj_AI_Base bestAlly = info.Item1;
                            int bestHit = info.Item2;
                            if (bestHit > 0 && bestAlly.IsValid)
                            {
                                CastE(bestAlly);
                            }
                        }
                    }
                }
            }
        }
        private static void ThrowBall(Obj_AI_Base target)
        {
            Obj_AI_Base eAlly = null;
            Vector3 predictedPos = Vector3.Zero;
            if (E.IsReady() && target.IsValidTarget() && Extensions.Distance(Ball, target) > R.Width)
            {
                var pred = E.GetPrediction(target);
                foreach (AIHeroClient ally in HeroManager.Allies.Where(o => o.IsValid && Extensions.Distance(myHero, o) < E.Range && Extensions.Distance(Ball, o) > 0))
                {
                    var pred2 = E.GetPrediction(ally);
                    if (Extensions.Distance(pred.CastPosition, pred2.CastPosition) < R.Width)
                    {
                        if (eAlly == null)
                        {
                            eAlly = ally;
                            predictedPos = pred2.CastPosition;
                        }
                        else if (Extensions.Distance(pred.CastPosition, predictedPos) > Extensions.Distance(pred.CastPosition, pred2.CastPosition))
                        {
                            eAlly = ally;
                            predictedPos = pred2.CastPosition;
                        }
                    }
                }
            }
            if (eAlly != null)
            {
                CastE(eAlly);
            }
            else
            {
                CastQ(target);
            }
        }
        private static int HitW(List<Obj_AI_Base> list)
        {
            int count = 0;
            if (W.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(obj => Extensions.Distance(obj.ServerPosition, Ball) <= W.Range * 1.5f))
                {
                    var pred = W.GetPrediction(obj);
                    if (pred.HitChancePercent >= 50 && Extensions.Distance(Ball, pred.CastPosition) <= W.Range)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        private static int HitR()
        {
            int count = 0;
            if (R.IsReady())
            {
                foreach (AIHeroClient obj in HeroManager.Enemies.Where(o => o.IsValidTarget() && Extensions.Distance(Ball, o) <= R.Range * 1.6f))
                {
                    var pred = R.GetPrediction(obj);
                    if (pred.HitChancePercent >= 50 && Extensions.Distance(Ball, pred.CastPosition) <= R.Range)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        private static Tuple<int, Dictionary<int, bool>> CountHitQ(Vector3 StartPos, Vector3 EndPos, List<Obj_AI_Base> list)
        {
            int count = 0;
            Dictionary<int, bool> counted = new Dictionary<int, bool>();
            if (Q.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(o => o.IsValidTarget(Q.Range + Q.Width)))
                {
                    var info = obj.ServerPosition.To2D().ProjectOn(StartPos.To2D(), EndPos.To2D());
                    if (info.IsOnSegment && Extensions.Distance(obj.ServerPosition.To2D(), info.SegmentPoint) <= Q.Width * 1.5f + obj.BoundingRadius / 2)
                    {
                        var pred = Q.GetPrediction(obj);
                        if (pred.HitChancePercent >= 70)
                        {
                            info = pred.CastPosition.To2D().ProjectOn(StartPos.To2D(), EndPos.To2D());
                            if (info.IsOnSegment && Extensions.Distance(pred.CastPosition.To2D(), info.SegmentPoint) <= Q.Width + obj.BoundingRadius / 2)
                            {
                                count++;
                                counted[obj.NetworkId] = true;
                            }
                        }
                    }
                }
            }
            return new Tuple<int, Dictionary<int, bool>>(count, counted);
        }
        private static Tuple<Vector3, int> BestHitQ(List<Obj_AI_Base> list, Obj_AI_Base target = null)
        {
            Vector3 BestPos = Vector3.Zero;
            int bestHit = 0;
            bool checktarget = target != null && target.IsValidTarget();
            if (Q.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(o => o.IsValidTarget(Q.Range + Q.Width)))
                {
                    var pred = Q.GetPrediction(obj);
                    if (pred.HitChancePercent >= 70)
                    {
                        var t = CountHitQ(Ball, pred.CastPosition, list);
                        var hit = t.Item1;
                        var counted = t.Item2;
                        bool b = true;
                        if (checktarget)
                        {
                            b = counted.ContainsKey(target.NetworkId);
                        }
                        if (hit > bestHit && b)
                        {
                            bestHit = hit;
                            BestPos = pred.CastPosition;
                            if (bestHit == list.Count)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return new Tuple<Vector3, int>(BestPos, bestHit);
        }
        private static Tuple<int, Dictionary<int, bool>> CountHitE(Vector3 StartPos, Vector3 EndPos, List<Obj_AI_Base> list)
        {
            int count = 0;
            Dictionary<int, bool> counted = new Dictionary<int, bool>();
            if (E.IsReady())
            {
                foreach (Obj_AI_Base obj in list.Where(o => o.IsValidTarget(E.Range)))
                {
                    var info = obj.ServerPosition.To2D().ProjectOn(StartPos.To2D(), EndPos.To2D());
                    if (info.IsOnSegment && Extensions.Distance(obj.ServerPosition.To2D(), info.SegmentPoint) <= E.Width * 1.5f + obj.BoundingRadius / 2)
                    {
                        var pred = E.GetPrediction(obj);
                        if (pred.HitChancePercent >= 50 && Extensions.Distance(pred.CastPosition, myHero) < E.Range)
                        {
                            info = pred.CastPosition.To2D().ProjectOn(StartPos.To2D(), EndPos.To2D());
                            if (info.IsOnSegment && Extensions.Distance(pred.CastPosition.To2D(), info.SegmentPoint) <= E.Width + obj.BoundingRadius / 2)
                            {
                                count++;
                                counted[obj.NetworkId] = true;
                            }
                        }
                    }
                }
            }
            return new Tuple<int, Dictionary<int, bool>>(count, counted);
        }
        private static Tuple<Obj_AI_Base, int> BestHitE(List<Obj_AI_Base> list, Obj_AI_Base target = null)
        {
            Obj_AI_Base bestAlly = null;
            int bestHit = 0;
            bool checktarget = target != null && target.IsValidTarget();
            if (E.IsReady())
            {
                foreach (Obj_AI_Base ally in HeroManager.Allies.Where(o => o.IsValid && Extensions.Distance(myHero, o) < E.Range))
                {
                    if (Extensions.Distance(Ball, ally) > 0)
                    {
                        var pred = E.GetPrediction(ally);
                        var info = CountHitE(Ball, pred.CastPosition, list);
                        var hit = info.Item1;
                        var counted = info.Item2;
                        bool b = true;
                        if (checktarget)
                        {
                            b = counted.ContainsKey(target.NetworkId);
                        }
                        if (hit > bestHit && b)
                        {
                            bestHit = hit;
                            bestAlly = ally;
                            if (bestHit == list.Count)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return new Tuple<Obj_AI_Base, int>(bestAlly, bestHit);
        }
        private static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady())
            {
                var pred = R.GetPrediction(target);
                if (pred.HitChancePercent >= 70 && Extensions.Distance(Ball, pred.CastPosition) <= R.Range)
                {
                    myHero.Spellbook.CastSpell(R.Slot);
                }
            }
        }


        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (e.Sender.Team == myHero.Team)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical, myHero.Position);
                if (SubMenu["Misc"]["E"].Cast<CheckBox>().CurrentValue && target.IsValidTarget())
                {
                    CastE(e.Sender);
                    LastGapclose = Game.Time;
                }
                //..
            }
        }

        private static void OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (e.Sender.Team != myHero.Team)
            {
                if (SubMenu["Misc"]["R"].Cast<CheckBox>().CurrentValue)
                {
                    if (Extensions.Distance(Ball, e.Sender) > R.Range)
                    {
                        ThrowBall(e.Sender);
                    }
                    else
                    {
                        CastR(e.Sender);
                    }
                }
                //..
            }
        }
        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation.ToLower().Equals("prop"))
                {
                    BallObject = sender;
                }
            }
        }

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.ToLower() == myHero.Spellbook.GetSpell(SpellSlot.E).SData.Name.ToLower())
                {
                    E_Target = args.Target;
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (myHero.IsDead) { return; }
            if (SubMenu["Draw"]["Ball"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(new ColorBGRA(0, 0, 255, 100), 120, Ball);
            }
            if (SubMenu["Draw"]["W"].Cast<CheckBox>().CurrentValue && W.IsReady())
            {
                Circle.Draw(new ColorBGRA(255, 255, 255, 100), W.Range, Ball);
            }
            if (SubMenu["Draw"]["R"].Cast<CheckBox>().CurrentValue && R.IsReady())
            {
                Circle.Draw(new ColorBGRA(255, 255, 255, 100), R.Range, Ball);
            }
        }

        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender.Name.ToLower().Contains("missile"))
                {
                    var missile = (MissileClient)sender;
                    if (missile.SpellCaster.IsMe && (missile.SData.Name.ToLower().Contains("orianaizuna") || missile.SData.Name.ToLower().Contains("orianaredact")))
                    {
                        BallObject = sender;
                    }
                }
                else if (sender.Name.ToLower().Contains(myHero.ChampionName.ToLower()))
                {
                    if (sender.Name.ToLower().Contains("yomu") && sender.Name.ToLower().Contains("green"))
                    {
                        BallObject = sender;
                    }
                }
            }
        }
        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender != null && sender.Name != null)
            {
                if (sender.Name.ToLower().Contains("missile"))
                {
                    var missile = (MissileClient)sender;
                    if (missile.SpellCaster.IsMe && missile.SData.Name.ToLower().Contains("orianaredact"))
                    {
                        BallObject = E_Target;
                    }
                }
            }
        }
        static float Damage(Obj_AI_Base target, SpellSlot slot)
        {
            if (target.IsValidTarget())
            {
                if (slot == SpellSlot.Q)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)30 * Q.Level + 30 + 0.5f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.W)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)45 * W.Level + 25 + 0.7f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.E)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)30 * E.Level + 30 + 0.3f * myHero.FlatMagicDamageMod);
                }
                else if (slot == SpellSlot.R)
                {
                    return myHero.CalculateDamageOnUnit(target, DamageType.Magical, (float)75 * R.Level + 75 + 0.7f * myHero.FlatMagicDamageMod);
                }
            }
            return myHero.GetSpellDamage(target, slot);
        }

        static DamageInfo GetComboDamage(Obj_AI_Base target, bool q, bool w, bool e, bool r)
        {
            var ComboDamage = 0f;
            var ManaWasted = 0f;
            if (target.IsValidTarget())
            {
                if (q)
                {
                    ComboDamage += Damage(target, Q.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;
                }
                if (w)
                {
                    ComboDamage += Damage(target, W.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.W).SData.Mana;
                }
                if (e)
                {
                    ComboDamage += Damage(target, E.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.E).SData.Mana;
                }
                if (r)
                {
                    ComboDamage += Damage(target, R.Slot);
                    ManaWasted += myHero.Spellbook.GetSpell(SpellSlot.R).SData.Mana;
                }
                if (Ignite != null && Ignite.IsReady())
                {
                    ComboDamage += myHero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite);
                }
                ComboDamage += myHero.GetAutoAttackDamage(target, true);
            }
            ComboDamage = ComboDamage * Overkill;
            return new DamageInfo(ComboDamage, ManaWasted);
        }

        static DamageInfo GetBestCombo(Obj_AI_Base target)
        {
            var q = Q.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var w = W.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var e = E.IsReady() ? new bool[] { false, true } : new bool[] { false };
            var r = R.IsReady() ? new bool[] { false, true } : new bool[] { false };
            if (target.IsValidTarget())
            {
                if (PredictedDamage.ContainsKey(target.NetworkId))
                {
                    var damageI = PredictedDamage[target.NetworkId];
                    if (Game.Time - damageI.Time <= RefreshTime)
                    {
                        return damageI;
                    }
                    else
                    {
                        bool[] best = new bool[] {
                            Q.IsReady (),
                            W.IsReady (),
                            E.IsReady (),
                            R.IsReady ()
                        };
                        var bestdmg = 0f;
                        var bestmana = 0f;
                        foreach (bool q1 in q)
                        {
                            foreach (bool w1 in w)
                            {
                                foreach (bool e1 in e)
                                {
                                    foreach (bool r1 in r)
                                    {
                                        DamageInfo damageI2 = GetComboDamage(target, q1, w1, e1, r1);
                                        float d = damageI2.Damage;
                                        float m = damageI2.Mana;
                                        if (myHero.Mana >= m)
                                        {
                                            if (bestdmg >= target.Health)
                                            {
                                                if (d >= target.Health && (d < bestdmg || m < bestmana))
                                                {
                                                    bestdmg = d;
                                                    bestmana = m;
                                                    best = new bool[] { q1, w1, e1, r1 };
                                                }
                                            }
                                            else
                                            {
                                                if (d >= bestdmg)
                                                {
                                                    bestdmg = d;
                                                    bestmana = m;
                                                    best = new bool[] { q1, w1, e1, r1 };
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        PredictedDamage[target.NetworkId] = new DamageInfo(best[0], best[1], best[2], best[3], bestdmg, bestmana, Game.Time);
                        return PredictedDamage[target.NetworkId];
                    }
                }
                else
                {
                    var damageI2 = GetComboDamage(target, Q.IsReady(), W.IsReady(), E.IsReady(), R.IsReady());
                    PredictedDamage[target.NetworkId] = new DamageInfo(false, false, false, false, damageI2.Damage, damageI2.Mana, Game.Time - Game.Ping * 2);
                    return GetBestCombo(target);
                }
            }
            return new DamageInfo(false, false, false, false, 0, 0, 0);
        }
    }
    public class DamageInfo
    {
        public bool Q;
        public bool W;
        public bool E;
        public bool R;
        public float Damage;
        public float Mana;
        public float Time;

        public DamageInfo(bool Q, bool W, bool E, bool R, float Damage, float Mana, float Time)
        {
            this.Q = Q;
            this.W = W;
            this.E = E;
            this.R = R;
            this.Damage = Damage;
            this.Mana = Mana;
            this.Time = Time;
        }
        public DamageInfo(float Damage, float Mana)
        {
            this.Damage = Damage;
            this.Mana = Mana;
        }
    }
    public class _Spell
    {
        public float LastCastTime = 0;
        public float LastSentTime = 0;
        public Vector3 End = Vector3.Zero;
    }
}
