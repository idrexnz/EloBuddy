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


namespace LeeSin
{
    public static class Insec
    {
        // T O D O
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Insec");
            }
        }
        private static int Priority
        {
            get
            {
                return Menu.GetSliderValue("Priority");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsInsec;
            }
        }
        private static Obj_AI_Base AllySelected;
        private static Vector3 PositionSelected;
        private static float LastGapcloseAttempt = 0;
        private static float LastSetPositionTime = 0;
        private static float Offset = 80f;
        public static void Init()
        {
            Game.OnWndProc += Game_OnWndProc;
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name.Equals(SpellSlot.R.GetSpellDataInst().Name))
                {
                    if (Menu.GetCheckBoxValue("Flash") && SpellManager.Flash_IsReady && IsActive)
                    {
                        SpellManager.Flash.Cast(ExpectedEndPosition);
                    }
                }
            }
        }

        public static void Execute()
        {
            if (Orbwalker.CanMove && Game.Time - LastGapcloseAttempt > 0.25f)
            {
                Orbwalker.MoveTo(Util.mousePos);
            }
            var target = TargetSelector.Target;
            if (target.IsValidTarget())
            {
                if (IsReady)
                {
                    if (IsActive)
                    {
                        if (SpellManager.CanCastQ1)
                        {
                            var predtarget = SpellManager.Q1.GetPrediction(target);
                            if (Menu.GetCheckBoxValue("Object") && predtarget.CollisionObjects.Count() > 1)
                            {
                                foreach (Obj_AI_Base minion in EntityManager.MinionsAndMonsters.Get(EntityManager.MinionsAndMonsters.EntityType.Both, EntityManager.UnitTeam.Enemy, Util.myHero.Position, SpellManager.Q2.Range).Where(m => m.IsValidTarget() && SpellSlot.Q.GetSpellDamage(m) < Prediction.Health.GetPrediction(m, SpellManager.Q1.CastDelay + 1000 * (int) (Extensions.Distance(Util.myHero, m)/SpellManager.Q1.Speed)) && Extensions.Distance(Util.myHero, target, true) > Extensions.Distance(m, target, true) && Extensions.Distance(m, target, true) < Math.Pow(WardManager.WardRange - DistanceBetween - Offset, 2)).OrderBy(m => Extensions.Distance(target, m, true)))
                                {
                                    var pred = SpellManager.Q1.GetPrediction(minion);
                                    if (pred.HitChancePercent >= SpellSlot.Q.HitChancePercent())
                                    {
                                        SpellManager.Q1.Cast(pred.CastPosition);
                                    }
                                }
                                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(m => m.NetworkId != target.NetworkId && m.IsValidTarget(SpellManager.Q2.Range) && SpellSlot.Q.GetSpellDamage(m) < m.Health && Extensions.Distance(Util.myHero, target, true) > Extensions.Distance(m, target, true) && Extensions.Distance(m, target, true) < Math.Pow(WardManager.WardRange - DistanceBetween - Offset, 2)).OrderBy(m => Extensions.Distance(target, m, true)))
                                {
                                    SpellManager.CastQ1(enemy);
                                }
                            }
                        }
                        if (Extensions.Distance(Util.myHero, target, true) > Math.Pow(WardManager.WardRange - DistanceBetween, 2))
                        {
                            if (_Q.HasQ2Buff)
                            {
                                if (_Q.IsValidTarget && Extensions.Distance(target, _Q.Target, true) < Math.Pow(WardManager.WardRange - DistanceBetween - Offset, 2))
                                {
                                    Champion.ForceQ2(target);
                                }
                            }
                        }
                        SpellManager.CastQ1(target);
                    }
                    if (Extensions.Distance(Util.myHero, target, true) < Math.Pow(WardManager.WardRange - DistanceBetween, 2) && !IsRecent)
                    {
                        switch (Priority)
                        {
                            case 0:
                                if (WardManager.CanWardJump)
                                {
                                    WardJump(target);
                                }
                                else if (SpellManager.Flash_IsReady)
                                {
                                    Flash(target);
                                }
                                break;
                            case 1:
                                if (SpellManager.Flash_IsReady)
                                {
                                    Flash(target);
                                }
                                else if (WardManager.CanWardJump)
                                {
                                    WardJump(target);
                                }
                                break;
                        }
                    }
                    CastR(target);
                }
                else
                {
                    NormalCombo.Execute();
                }
            }
        }
        private static void Flash(Obj_AI_Base target)
        {
            if (SpellManager.Flash_IsReady)
            {
                var gapclosepos = target.Position + (target.Position - ExpectedEndPosition).Normalized() * DistanceBetween;
                if (Extensions.Distance(gapclosepos, target, true) <= Math.Pow(SpellManager.R.Range, 2) && Extensions.Distance(gapclosepos, target, true) < Extensions.Distance(gapclosepos, ExpectedEndPosition, true))
                {
                    if (Orbwalker.CanMove)
                    {
                        LastGapcloseAttempt = Game.Time;
                        //Orbwalker.MoveTo(gapclosepos + (gapclosepos - ExpectedEndPosition).Normalized() * (DistanceBetween + Util.myHero.BoundingRadius / 2));
                    }
                    AllySelected = null;
                    PositionSelected = EndPosition;
                    LastSetPositionTime = Game.Time;
                    Util.myHero.Spellbook.CastSpell(SpellManager.Flash.Slot, gapclosepos);
                }
            }
        }

        private static void WardJump(Obj_AI_Base target)
        {
            var pred = SpellManager.W1.GetPrediction(target);
            if (WardManager.CanWardJump && pred.HitChancePercent >= 30f)
            {
                var gapclosepos = pred.CastPosition + (pred.CastPosition - ExpectedEndPosition).Normalized() * DistanceBetween;
                if (Extensions.Distance(gapclosepos, Util.myHero, true) <= Math.Pow(WardManager.WardRange, 2) && Extensions.Distance(gapclosepos, target, true) <= Math.Pow(SpellManager.R.Range, 2) && Extensions.Distance(gapclosepos, target, true) < Extensions.Distance(gapclosepos, ExpectedEndPosition, true))
                {
                    if (Orbwalker.CanMove)
                    {
                        LastGapcloseAttempt = Game.Time;
                        Orbwalker.MoveTo(gapclosepos + (gapclosepos - ExpectedEndPosition).Normalized() * (DistanceBetween + Util.myHero.BoundingRadius / 2));
                    }
                    AllySelected = null;
                    PositionSelected = EndPosition;
                    LastSetPositionTime = Game.Time;
                    var obj = Champion.GetBestObjectNearTo(gapclosepos);
                    if (obj != null)
                    {
                        SpellManager.CastW1(obj);
                    }
                    else
                    {
                        WardManager.CastWardTo(gapclosepos);
                    }
                }
            }
        }
        private static void CastR(Obj_AI_Base target)
        {
            if (SpellSlot.R.IsReady() && target.IsValidTarget(SpellManager.R.Range) && Extensions.Distance(Util.myHero, ExpectedEndPosition, true) > Extensions.Distance(target, ExpectedEndPosition, true))
            {
                var extended = ExpectedEndPosition + (ExpectedEndPosition - target.Position).Normalized() * SpellManager.RKick.Range * 0.5f;
                var realendpos = target.Position + (target.Position - Util.myHero.Position).Normalized() * SpellManager.RKick.Range;
                var info = realendpos.To2D().ProjectOn(target.Position.To2D(), extended.To2D());
                if (info.IsOnSegment && Extensions.Distance(info.SegmentPoint, ExpectedEndPosition.To2D(), true) <= Math.Pow(SpellManager.RKick.Range * 0.5f, 2))
                {
                    SpellManager.CastR(target);
                }
            }
        }
        private static float DistanceBetween
        {
            get
            {
                if (TargetSelector.Target.IsValidTarget())
                {
                    return Math.Min((Util.myHero.BoundingRadius + TargetSelector.Target.BoundingRadius + 50f) * (100 + Menu.GetSliderValue("DistanceBetweenPercent")) / 100, SpellManager.R.Range);
                }
                return 0;
            }
        }
        public static Vector3 EndPosition
        {
            get
            {
                var target = TargetSelector.Target;
                if (target.IsValidTarget())
                {
                    return target.Position + (ExpectedEndPosition - target.Position).Normalized() * SpellManager.RKick.Range;
                }
                return Vector3.Zero;
            }
        }
        public static Vector3 ExpectedEndPosition
        {
            get
            {
                if (AllySelected != null)
                {
                    return AllySelected.Position;
                }
                if (PositionSelected != Vector3.Zero)
                {
                    return PositionSelected;
                }
                if (TargetSelector.Target.IsValidTarget())
                {
                    switch (Menu.GetSliderValue("Position"))
                    {
                        case 1:
                            return Util.mousePos;
                        case 2:
                            return Util.myHero.Position;
                        default:
                            var target = TargetSelector.Target;
                            var turret = EntityManager.Turrets.Allies.OrderBy(m => Extensions.Distance(Util.myHero, m, true)).FirstOrDefault();
                            if (turret != null)
                            {
                                if (Extensions.Distance(turret, target) - SpellManager.RKick.Range < turret.GetAutoAttackRange() + 750 + 200)
                                {
                                    return turret.Position;
                                }
                            }
                            var allies = EntityManager.Heroes.Allies.Where(m => m.IsValidAlly() && Extensions.Distance(Util.myHero, m, true) < Math.Pow(SpellManager.RKick.Range + 500f, 2)).OrderBy(m => m.GetPriority());
                            if (allies.Count() > 0)
                            {
                                var ally = allies.LastOrDefault();
                                var pos = ally.Position + (target.Position - ally.Position).Normalized().To2D().Perpendicular().To3D() * ally.GetAutoAttackRange(target) / 2;
                                return pos;
                            }
                            break;

                    }
                }
                return Util.myHero.Position;
            }
        }
        public static bool IsReady
        {
            get
            {
                return (WardManager.CanWardJump || SpellManager.Flash_IsReady || IsRecent) && SpellSlot.R.IsReady() && EndPosition != Vector3.Zero;
            }
        }
        public static bool IsRecent
        {
            get
            {
                return Game.Time - SpellManager.W_LastCastTime < 5 || Game.Time - SpellManager.Flash_LastCastTime < 5 || Game.Time - WardManager.LastWardCreated < 5;
            }
        }
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowMessages.LeftButtonDown)
            {
                if (IsReady)
                {
                    var target = EloBuddy.SDK.TargetSelector.GetTarget(250f, TargetSelector.damageType, Util.mousePos);
                    if (target.IsValidTarget())
                    {

                    }
                    else
                    {
                        var ally = AllyHeroManager.GetNearestTo(Util.mousePos);
                        if (ally != null && Extensions.Distance(ally, Util.mousePos) < 250f)
                        {
                            AllySelected = ally;
                            PositionSelected = Vector3.Zero;
                            LastSetPositionTime = Game.Time;
                        }
                        else
                        {
                            AllySelected = null;
                            PositionSelected = Util.mousePos;
                            LastSetPositionTime = Game.Time;
                        }
                    }
                }
            }
        }
        private static void Game_OnTick(EventArgs args)
        {
            if (Game.Time - LastSetPositionTime > 10 && LastSetPositionTime > 0)
            {
                AllySelected = null;
                PositionSelected = Vector3.Zero;
                LastSetPositionTime = 0;
            }
        }
    }
}
