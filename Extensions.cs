using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
