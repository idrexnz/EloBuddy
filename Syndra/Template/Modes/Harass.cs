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

namespace Template
{
    public static class Harass
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Harass");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsHarass;
            }
        }
        public static void Execute()
        {
            if (Menu.GetSliderValue("Mana") <= Util.MyHero.ManaPercent)
            {
                var target = TargetSelector.Target;
                if (target.IsValidTarget())
                {
                    if (Menu.GetCheckBoxValue("Turret") && target.IsInEnemyTurret() && Util.MyHero.IsInEnemyTurret()) { return; }
                    if (Menu.GetCheckBoxValue("E")) { SpellManager.CastE(target); }
                    if (Menu.GetCheckBoxValue("W")) { SpellManager.CastW(target); }
                    if (Menu.GetCheckBoxValue("Q")) { SpellManager.CastQ(target); }
                    if (Menu.GetCheckBoxValue("WE")) { SpellManager.CastWE(target); }
                    if (Menu.GetCheckBoxValue("QE")) { SpellManager.CastQE(target); }
                }
            }
        }
    }
}
