using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public class PlayerMarkerData
    {
        public static Dictionary<Guid, PlayerMarkerData> Data = new();

        public readonly Guid Id;

        public bool Enabled = true;
        public bool Pinned = false;

        public PlayerMarkerData(Guid id)
        {
            Id = id;
        }

        private bool IsDefault() => Enabled && !Pinned;

        public static PlayerMarkerData GetByMarkerId(Guid id)
        {
            if (!Data.TryGetValue(id, out PlayerMarkerData? data))
            {
                data = new(id);
                Data.Add(id, data);
            }
            return data;
        }

        internal static void Clear() 
        {
            Data.Clear();
        }

        internal static TagCompound Save()
        {
            TagCompound tag = new();
            foreach (var kvp in Data)
            {
                if (kvp.Value.IsDefault())
                    continue;

                BitsByte bits = new();
                bits[0] = kvp.Value.Enabled;
                bits[1] = kvp.Value.Pinned;
                tag[kvp.Key.ToString()] = (byte)bits;
            }
            return tag;
        }

        internal static void Load(TagCompound? tag)
        {
            Data.Clear();
            if (tag is null)
                return;

            foreach (var kvp in tag)
            {
                if (!Guid.TryParse(kvp.Key, out Guid id))
                    continue;

                BitsByte bits = (byte)kvp.Value;
                PlayerMarkerData data = new(id);

                data.Enabled = bits[0];
                data.Pinned = bits[1];

                Data[id] = data;
            }
        }
    }
}
