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

namespace Template
{
    public static class DrawManager
    {
        public static Menu Menu
        {
            get
            {
                return MenuManager.GetSubMenu("Drawings");
            }
        }
        public static void Init(EventArgs args)
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Util.MyHero.IsDead) { return; }
            if (Menu.GetCheckBoxValue("Disable")) { return; }
            var target = TargetSelector.Target;
            if (Menu.GetCheckBoxValue("Target") && target.IsValidTarget())
            {
                Circle.Draw(Color.Red, 120f, 5, target.Position);
            }
            if (Menu.GetSliderValue("E.Lines") > 0 && SpellSlot.E.IsReady())
            {
                switch (Menu.GetSliderValue("E.Lines"))
                {
                    case 1:
                        foreach (Ball b in BallManager.Balls.Where(m => m.IsIdle && m.ObjectIsValid && m.E_IsOnRange))
                        {
                            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(m => b.E_WillHit(m)))
                            {
                                Drawing.DrawLine(b.Position.WorldToScreen(), b.E_EndPosition.WorldToScreen(), SpellManager.QE.Width, System.Drawing.Color.FromArgb(100, 255, 255, 255));
                            }
                        }
                        break;
                    case 2:
                        foreach (Ball b in BallManager.Balls.Where(m => m.IsIdle && m.ObjectIsValid && m.E_IsOnRange))
                        {
                            Drawing.DrawLine(b.Position.WorldToScreen(), b.E_EndPosition.WorldToScreen(), SpellManager.QE.Width, System.Drawing.Color.FromArgb(100, 255, 255, 255));
                        }
                        break;
                }
            }
            if (Menu.GetCheckBoxValue("W.Object") && SpellManager.W_Object != null)
            {
                Circle.Draw(Color.Blue, SpellManager.W_Width1, 1, SpellManager.W_Object.Position);
            }
            if (Menu.GetCheckBoxValue("Killable") && SpellSlot.R.IsReady())
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(SpellManager.R.Range) && SpellSlot.R.GetSpellDamage(m) >= m.Health))
                {
                    Drawing.DrawText(enemy.Position.WorldToScreen(), System.Drawing.Color.Red, "R KILLABLE", 15);
                }
            }
        }
    }
}
