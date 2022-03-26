using MapMarkers.Buffs;
using MapMarkers.Items;
using Microsoft.Xna.Framework;
using System;
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

        public bool IsLocalPlayer => Main.netMode == NetmodeID.SinglePlayer || (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer);

        public static bool LocalPlayerHasTPDebuff { get; private set; }

        public static bool LocalPlayerHasTPPotion { get; private set; }
        public static int LocalPlayerTPPotionBank { get; private set; }
        public static int LocalPlayerTPPotionSlot { get; private set; }

        private Dictionary<int, List<AbstractMarker>> MyMarkers 
        {
            get 
            {
                int id = player.name.GetHashCode();

                if (MapMarkers.AllMarkers.ContainsKey(id))
                    return MapMarkers.AllMarkers[id];

                Dictionary<int, List<AbstractMarker>> markers = new Dictionary<int, List<AbstractMarker>>();
                MapMarkers.AllMarkers.Add(id, markers);
                return markers;
            }
        }

        public override void PreUpdate()
        {
            if (IsLocalPlayer)
            {
                LocalPlayerHasTPPotion = false;
                CheckForTPPotion(Main.LocalPlayer.inventory, 0);
                CheckForTPPotion(Main.LocalPlayer.bank?.item, 1);
                CheckForTPPotion(Main.LocalPlayer.bank2?.item, 2);
                CheckForTPPotion(Main.LocalPlayer.bank3?.item, 3);
            }
        }

        public override void PostUpdateBuffs()
        {
            if (IsLocalPlayer)
            {
                LocalPlayerHasTPDebuff = player.HasBuff(TPDisability.BuffType);
            }
        }

        private static void CheckForTPPotion(Item[] inv, int bank)
        {
            if (LocalPlayerHasTPPotion || inv is null) 
                return;

            int markertppType = MarkerTPPotion.ItemType;

            for (int i = 0; i < inv.Length; i++)
            {
                Item item = inv[i];
                if (item.active && !item.IsAir && item.type == markertppType)
                {
                    LocalPlayerTPPotionBank = bank;
                    LocalPlayerTPPotionSlot = i;
                    LocalPlayerHasTPPotion = true;
                    return;
                }
            }
        }

        public override TagCompound Save()
        {
            TagCompound tag = new TagCompound();

            foreach (KeyValuePair<int, List<AbstractMarker>> world in MyMarkers)
            {
                string key = $"markers_{world.Key}";
                List<TagCompound> list = world.Value.Where(m => m is MapMarker mm && !mm.IsServerSide).Select(x => (x as MapMarker).GetData()).ToList();
                tag[key] = list;
            }
            return tag;
        }

        public override void Load(TagCompound tag)
        {
            Dictionary<int, List<AbstractMarker>> markers = MyMarkers;
            markers.Clear();

            foreach (KeyValuePair<string, object> v in tag)
            {
                if (v.Key.StartsWith("markers_"))
                {
                    int wid = int.Parse(v.Key.Substring(8));
                    markers.Add(wid, new List<AbstractMarker>());

                    foreach (TagCompound d in (IList<TagCompound>)v.Value)
                        markers[wid].Add(MapMarker.FromData(d));
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

            MapMarkers.CurrentMarkers = markers[Main.worldID];

            Net.MapClient.RequestMarkers();

            MapMarkers.AddSpecialMarkers();
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

                if (MapHelper.IsFullscreenMap)
                    m.Position = MapHelper.ScreenToMap(Main.MouseScreen).ToPoint();
                else m.Position = (Main.LocalPlayer.position / new Vector2(16)).ToPoint();

                Main.mapFullscreen = false;
                m.BrandNew = true;
                MapMarkers.CurrentMarkers.Add(m);
                MapMarkers.MarkerGui.SetMarker(m);
            }
        }
    }
}
