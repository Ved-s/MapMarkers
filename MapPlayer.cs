using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    class MapPlayer : ModPlayer
    {
        public override TagCompound Save()
        {
            TagCompound tag = new TagCompound();

            foreach (KeyValuePair<int, List<MapMarker>> world in MapMarkers.Markers)
            {
                tag[$"markers_{world.Key}"] = world.Value.Select(x => x.GetData()).ToList();
            }

            return tag;

        }

        public override void Load(TagCompound tag)
        {
            MapMarkers.Markers.Clear();

            foreach (KeyValuePair<string, object> v in tag) 
            {
                if (v.Key.StartsWith("markers_")) 
                {
                    int id = int.Parse(v.Key.Substring(8));
                    MapMarkers.Markers.Add(id, new List<MapMarker>());

                    foreach (TagCompound d in (IList<TagCompound>)v.Value)
                        MapMarkers.Markers[id].Add(MapMarker.FromData(d));
                }
            }

        }

        public override void PostUpdate()
        {
            if (MapMarkers.Hotkeys.CreateMarker.JustPressed)
            {
                if (MapMarkers.MarkerGui.Marker != null) return;

                Item i = new Item();
                i.SetDefaults(ItemID.TrifoldMap);
                MapMarker m = new MapMarker("New marker", Point.Zero, i);

                if (Main.mapFullscreen)
                    m.Position = MapRenderer.ScreenToMap(Main.MouseScreen).ToPoint();
                else m.Position = (Main.LocalPlayer.position / new Vector2(16)).ToPoint();

                Main.mapFullscreen = false;
                m.BrandNew = true;
                MapMarkers.Markers[Main.worldID].Add(m);
                MapMarkers.MarkerGui.SetMarker(m);
            }
        }
    }
}
