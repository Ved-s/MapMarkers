using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    class MapPlayer : ModPlayer
    {
        private MapMarkers MapMarkers => mod as MapMarkers;

        private Dictionary<int, List<MapMarker>> MyMarkers 
        {
            get 
            {
                int id = player.name.GetHashCode();

                if (MapMarkers.AllMarkers.ContainsKey(id))
                    return MapMarkers.AllMarkers[id];

                Dictionary<int, List<MapMarker>> markers = new Dictionary<int, List<MapMarker>>();
                MapMarkers.AllMarkers.Add(id, markers);
                return markers;
            }
        }

        static List<MapPlayer> Instances = new List<MapPlayer>();

        public MapPlayer() 
        {
            Instances.Add(this);
        }

        public override TagCompound Save()
        {
            TagCompound tag = new TagCompound();

            foreach (KeyValuePair<int, List<MapMarker>> world in MyMarkers)
            {
                string key = $"markers_{world.Key}";
                List<TagCompound> list = world.Value.Where(m => !m.IsServerSide).Select(x => x.GetData()).ToList();
                tag[key] = list;

                //mod.Logger.InfoFormat("Saved {0} markers into {1}", list.Count, key);
            }
            //mod.Logger.InfoFormat("{0} tags total", tag.Count);
            return tag;
        }

        public override void Load(TagCompound tag)
        {
            Dictionary<int, List<MapMarker>> markers = MyMarkers;
            markers.Clear();

            //mod.Logger.InfoFormat("[{0}] Found {1} tags", player.name, tag.Count);

            foreach (KeyValuePair<string, object> v in tag) 
            {
                //mod.Logger.InfoFormat("Found tag {0}", v.Key);

                if (v.Key.StartsWith("markers_")) 
                {
                    int wid = int.Parse(v.Key.Substring(8));
                    markers.Add(wid, new List<MapMarker>());

                    foreach (TagCompound d in (IList<TagCompound>)v.Value)
                        markers[wid].Add(MapMarker.FromData(d));
                    //mod.Logger.InfoFormat("Loaded {0} markers for world {1}", markers.Count, wid);
                }
            }
        }

        public override void OnEnterWorld(Player player)
        {
            base.OnEnterWorld(player);
            if (Main.dedServ) return;
            Dictionary<int, List<MapMarker>> markers = MyMarkers;

            if (!markers.ContainsKey(Main.worldID))
                markers.Add(Main.worldID, new List<MapMarker>());

            MapMarkers.CurrentMarkers = markers[Main.worldID];

            //mod.Logger.InfoFormat("Entered world {0}", Main.worldID);
            Net.MapClient.RequestMarkers();
        }

        public override void PostUpdate()
        {
            if (Main.dedServ) return;
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
                MapMarkers.CurrentMarkers.Add(m);
                MapMarkers.MarkerGui.SetMarker(m);
            }
        }
    }
}
