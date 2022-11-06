using MapMarkers.Markers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers.Compat
{
    internal class OldClientMarkers : ModPlayer
    {
        public override string Name => "MapPlayer";

        [MemberNotNullWhen(true, nameof(Pins), nameof(Markers))]
        public bool HasData { get; private set; } = false;
        public HashSet<Guid>? Pins { get; private set; }
        public Dictionary<int, List<PlacedMarker>>? Markers { get; private set; }

        public override void LoadData(TagCompound tag)
        {
            HasData = true;
            Pins = new();
            Markers = new();

            foreach (KeyValuePair<string, object> v in tag)
            {
                if (v.Key.StartsWith("markers_"))
                {
                    int wid = int.Parse(v.Key.Substring(8));

                    if (!Markers.TryGetValue(wid, out var worldData))
                    {
                        worldData = new();
                        Markers[wid] = worldData;
                    }

                    foreach (TagCompound d in (IList<TagCompound>)v.Value)
                        worldData.Add(LoadOldMarker(d));
                }

                else if (v.Key.StartsWith("world_") && v.Value is TagCompound worldTag)
                {
                    int wid = int.Parse(v.Key.Substring(6));

                    if (!Markers.TryGetValue(wid, out var worldData))
                    {
                        worldData = new();
                        Markers[wid] = worldData;
                    }

                    if (worldTag.TryGet("markers", out IList<TagCompound> markersTag))
                        worldData.AddRange(markersTag.Select(LoadOldMarker));
                    
                    if (worldTag.TryGet("pinned", out IList<string> pinned))
                        Pins.UnionWith(pinned.Select(Guid.Parse));
                }
            }
        }

        // It's there because tML checks for both Save/Load data being present
        public override void SaveData(TagCompound tag) { }

        public static PlacedMarker LoadOldMarker(TagCompound data)
        {
            PlacedMarker marker = new();
            marker.IgnoreSetChecks = true;

            Item item = new Item();
            object i = data["item"];
            if (i is int id)
                item.SetDefaults(id);
            else if (i is TagCompound tag)
                ItemIO.Load(item, tag);

            marker.DisplayItem = item;

            if (data.TryGet("id", out string idstr))
            {
                idstr = data.GetString("id");
                marker.Id = Guid.Parse(idstr);
            }

            if (data.TryGet("name", out string name))
                marker.DisplayName = name;

            Vector2 pos = new();
            if (data.TryGet("x", out int x))
                pos.X = x;

            if (data.TryGet("y", out int y))
                pos.Y = y;

            marker.Position = pos;

            if (data.TryGet("server", out TagCompound servd))
            {
                if (servd.TryGet("id", out idstr))
                    marker.Id = Guid.Parse(idstr);

                if (servd.TryGet("owner", out string owner))
                    marker.Owner = owner;

                if (servd.TryGet("edit", out bool edit))
                    marker.AnyoneCanEdit = edit;

                if (servd.TryGet("perms", out int perms))
                {
                    marker.AnyoneCanEdit = (perms & 1) != 0;
                    marker.AnyoneCanRemove = (perms & 2) != 0;
                }

                marker.ServerSide = true;
            }

            marker.IgnoreSetChecks = false;

            return marker;
        }
    }
}
