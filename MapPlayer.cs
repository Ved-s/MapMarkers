using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
        private MapSystem MapSystem => ModContent.GetInstance<MapSystem>();

        public ModKeybind CreateMarker;

        private Dictionary<int, List<AbstractMarker>> MyMarkers
        {
            get
            {
                int id = Player.name.GetHashCode();

                if (MapSystem.AllMarkers.ContainsKey(id))
                    return MapSystem.AllMarkers[id];

                Dictionary<int, List<AbstractMarker>> markers = new();
                MapSystem.AllMarkers.Add(id, markers);
                return markers;
            }
        }


        public override void Load()
        {
            CreateMarker = KeybindLoader.RegisterKeybind(Mod, "Create Marker", Keys.B);
        }

        public override void SaveData(TagCompound tag)
        {
            foreach (KeyValuePair<int, List<AbstractMarker>> world in MyMarkers)
            {
                string key = $"markers_{world.Key}";
                List<TagCompound> list = world.Value.Where(x => x is MapMarker).Select(x => (x as MapMarker).GetData()).ToList();
                tag[key] = list;

                //Mod.Logger.DebugFormat("Saved {0} markers into {1}", list.Count, key);
            }
            //Mod.Logger.DebugFormat("{0} tags total", tag.Count);
        }

        public override void LoadData(TagCompound tag)
        {
            Dictionary<int, List<AbstractMarker>> markers = MyMarkers;
            markers.Clear();

            //Mod.Logger.DebugFormat("[{0}] Found {1} tags", Player.name, tag.Count);

            foreach (KeyValuePair<string, object> v in tag)
            {
                //Mod.Logger.DebugFormat("Found tag {0}", v.Key);

                if (v.Key.StartsWith("markers_"))
                {
                    int wid = int.Parse(v.Key.Substring(8));
                    markers.Add(wid, new List<AbstractMarker>());

                    foreach (TagCompound d in (IList<TagCompound>)v.Value)
                        markers[wid].Add(MapMarker.FromData(d));
                    //Mod.Logger.DebugFormat("Loaded {0} markers for world {1}", markers.Count, wid);
                }
            }
        }

        public override void OnEnterWorld(Player player)
        {
            base.OnEnterWorld(player);
            if (Main.dedServ) return;
            Dictionary<int, List<AbstractMarker>> markers = MyMarkers;

            if (!markers.ContainsKey(Main.worldID))
                markers.Add(Main.worldID, new List<AbstractMarker>());

            MapSystem.CurrentMarkers = markers[Main.worldID];

            //Mod.Logger.DebugFormat("Entered world {0}", Main.worldID);
            Net.MapClient.RequestMarkers();

            MapSystem.AddSpecialMarkers();
        }

        public override void PostUpdate()
        {
            if (Main.dedServ) return;
            if (CreateMarker.JustPressed)
            {
                if (MapSystem.MarkerGui.Marker != null) return;

                Item i = new Item();
                i.SetDefaults(ItemID.TrifoldMap);
                MapMarker m = new MapMarker("New marker", Point.Zero, i);

                if (MapHelper.IsFullscreenMap)
                    m.Position = MapHelper.ScreenToMap(Main.MouseScreen).ToPoint();
                else m.Position = (Main.LocalPlayer.position / new Vector2(16)).ToPoint();

                Main.mapFullscreen = false;
                m.BrandNew = true;
                MapSystem.CurrentMarkers.Add(m);
                MapSystem.MarkerGui.SetMarker(m);
            }
        }
    }
}
