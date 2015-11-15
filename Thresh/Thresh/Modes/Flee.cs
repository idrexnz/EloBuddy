using System;
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



namespace Thresh
{
    public static class Flee
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Flee");
            }
        }
        public static bool IsActive
        {
            get
            {
                return ModeManager.IsFlee;
            }
        }
        public static void Execute()
        {
            if (Menu.GetCheckBoxValue("W"))
            {
                var ally = EntityManager.Heroes.Allies.Where(h => h.IsValidTarget(TargetSelector.Range) && !h.IsMe).OrderByDescending(h => h.CountEnemiesInRange(h.GetAutoAttackRange() + 250f) * h.GetPriority() / h.HealthPercent).FirstOrDefault();
                if (ally != null && ally.CountEnemiesInRange(ally.GetAutoAttackRange() + 250f) > Util.MyHero.CountEnemiesInRange(Util.MyHero.GetAutoAttackRange()))
                {
                    SpellManager.CastW(ally);
                }
            }
            if (Menu.GetCheckBoxValue("E"))
            {
                foreach (var h in EntityManager.Heroes.Enemies.Where(h => h.IsValidTarget(SpellManager.E.Range)))
                {
                    SpellManager.Push(h);
                }
            }
        }
    }
}
