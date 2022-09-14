using MapMarkers.Structures;
using MapMarkers.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public class MarkerWorld : ModSystem
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public override void OnWorldLoad()
        {
            MapMarkers.MarkerGuids.Clear();
            //MapMarkers.Markers.Clear();
            //
            //Random rng = new(1);
            //
            //for (int i = 0; i < 100; i++)
            //{
            //    PlacedMarker marker = new();
            //    marker.DisplayItem.SetDefaults(rng.Next(1, ItemLoader.ItemCount));
            //    marker.Position = new(rng.Next(Main.maxTilesX), rng.Next(Main.maxTilesY));
            //    marker.DisplayName = ((int)marker.Position.X).ToString("x") + ((int)marker.Position.Y).ToString("x");
            //
            //    MapMarkers.Markers[marker.Id] = marker;
            //}

            base.OnWorldLoad();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["markers"] = MapMarkers.Markers.Values
                .Where(m => m.SaveLocation == SaveLocation.Server)
                .Select(m => MapMarkers.SaveMarker(m))
                .ToList();
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.TryGet("markers", out List<TagCompound> markers))
                foreach (TagCompound markerTag in markers)
                {
                    MapMarker? marker = MapMarkers.LoadMarker(markerTag);
                    if (marker is null)
                        continue;

                    MapMarkers.Markers[marker.Id] = marker;
                }
        }

        public override void OnWorldUnload()
        {
            MapMarkers.Markers.RemoveWhere(kvp => kvp.Value.SaveLocation != SaveLocation.Client);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            MarkerMenu.Update(gameTime);
        }

        public override void PostUpdateInput()
        {
            Keybinds.Update();
        }
    }
}
