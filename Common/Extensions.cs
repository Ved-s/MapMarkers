using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.Map;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public static class Extensions
    {
        public static bool TryLoad<T>(this TagCompound tag, string key, ref T value)
        {
            if (tag.ContainsKey(key))
            {
                value = tag.Get<T>(key);
                return true;
            }
            return false;
        }

        public static void MoveInside(this ref Rectangle rect, Rectangle bounds)
        {
            if (rect.X < bounds.X)
                rect.X = bounds.X;

            if (rect.Y < bounds.Y)
                rect.Y = bounds.Y;

            if (rect.Right > bounds.Right)
                rect.X = bounds.Right - rect.Width;

            if (rect.Bottom > bounds.Bottom)
                rect.Y = bounds.Bottom - rect.Height;
        }

        public static void MoveInside(this ref Vector2 vec, Rectangle bounds)
        {
            if (vec.X < bounds.X)
                vec.X = bounds.X;

            if (vec.Y < bounds.Y)
                vec.Y = bounds.Y;

            if (vec.X > bounds.Right)
                vec.X = bounds.Right;

            if (vec.Y > bounds.Bottom)
                vec.Y = bounds.Bottom;
        }
    }
}
