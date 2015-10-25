using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace Syndra
{
    public static class DamageIndicator
    {
        public static void Draw()
        {
            foreach (var enemy in EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget() && m.VisibleOnScreen))
            {
                var dmgR = enemy.GetBestCombo();
            }
        }
    }
}
