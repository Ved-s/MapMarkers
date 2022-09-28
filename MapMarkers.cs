using MapMarkers.Structures;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    /// <summary>
    /// Main mod class
    /// </summary>
    public class MapMarkers : Mod
    {
        internal Dictionary<Type, MapMarker> MarkerInstances = new();

        /// <summary>
        /// List of current player+world markers
        /// </summary>
        public Dictionary<Guid, MapMarker> Markers { get; } = new();

        public HashSet<Guid> PinnedMarkers { get; } = new();

        public ShortGuids MarkerGuids { get; } = new(2);

        public static TagCompound SaveMarker(MapMarker marker)
        {
            TagCompound tag = new();
            tag["id"] = marker.Id.ToByteArray();
            tag["name"] = marker.Name;
            tag["mod"] = marker.SaveModName;
            tag["enabled"] = marker.Enabled;
            TagCompound data = new();
            marker.SaveData(data);
            tag["data"] = data;
            return tag;
        }
        public static MapMarker? LoadMarker(TagCompound markerData, SaveLocation currentLocation)
        {
            if (!markerData.TryGet("name", out string name) || !markerData.TryGet("mod", out string mod))
                return null;

            MapMarker? marker;

            Mod? modInst = ModLoader.GetMod(mod);
            if (modInst is null)
            {
                marker = new UnloadedMarker(name, mod, currentLocation);
            }
            else 
            {
                marker = modInst.GetContent().FirstOrDefault(c => c is MapMarker m && m.Name == name) as MapMarker;
                if (marker is null)
                    marker = new UnloadedMarker(name, mod, currentLocation);
                else marker = marker.CreateInstance();
            }

            if (markerData.TryGet("enabled", out bool enabled))
                marker.Enabled = enabled;

            if (markerData.TryGet("id", out byte[] id))
                marker.Id = new(id);

            if (markerData.TryGet("data", out TagCompound data))
                marker.LoadData(data);

            return marker;
        }

        // TODO when Netcode: add net handling

        public void RemoveMarker(MapMarker marker)
        {
            Markers.Remove(marker.Id);
        }

        public void MoveMarker(MapMarker marker, Vector2 pos)
        {
            marker.Position = pos;
        }

        public void SetMarkerEnabled(MapMarker marker, bool enabled)
        {
            marker.Enabled = enabled;
        }
    }
}
