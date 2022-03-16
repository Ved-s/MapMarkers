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

        static FieldInfo MapOverlayDrawContext_mapScale = null;

        public static float GetMapScale(this MapOverlayDrawContext context) 
        {
            if (MapOverlayDrawContext_mapScale is null)
            {
                MapOverlayDrawContext_mapScale = typeof(MapOverlayDrawContext).GetField("_mapScale", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return (float)MapOverlayDrawContext_mapScale.GetValue(context);
        }
    }
}
