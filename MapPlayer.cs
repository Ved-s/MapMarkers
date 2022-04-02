using MapMarkers.Buffs;
using MapMarkers.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
        private MapSystem MapSystem => ModContent.GetInstance<MapSystem>();

        public bool IsLocalPlayer => Main.netMode == NetmodeID.SinglePlayer || (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer);

        public static bool LocalPlayerHasTPDebuff { get; private set; }

        public static bool LocalPlayerHasTPPotion { get; private set; }
        public static int LocalPlayerTPPotionBank { get; private set; }
        public static int LocalPlayerTPPotionSlot { get; private set; }

        private Dictionary<int, PlayerWorldData> MyWorldData
        {
            get
            {
                int id = Player.name.GetHashCode();

                if (MapSystem.AllPlayerWorldData.ContainsKey(id))
                    return MapSystem.AllPlayerWorldData[id];

                Dictionary<int, PlayerWorldData> data = new Dictionary<int, PlayerWorldData>();
                MapSystem.AllPlayerWorldData.Add(id, data);
                return data;
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
                LocalPlayerHasTPDebuff = Player.HasBuff(TPDisability.BuffType);
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

        public override void SaveData(TagCompound tag)
        {
            foreach (KeyValuePair<int, PlayerWorldData> world in MyWorldData)
            {
                tag[$"world_{world.Key}"] = world.Value.Save();
            }
        }

        public override void LoadData(TagCompound tag)
        {
            Dictionary<int, PlayerWorldData> data = MyWorldData;
            data.Clear();

            foreach (KeyValuePair<string, object> v in tag)
            {
                if (v.Key.StartsWith("markers_"))
                {
                    int wid = int.Parse(v.Key.Substring(8));

                    if (!data.TryGetValue(wid, out PlayerWorldData pwd))
                    {
                        pwd = new PlayerWorldData();
                        data.Add(wid, pwd);
                    }

                    foreach (TagCompound d in (IList<TagCompound>)v.Value)
                        pwd.Markers.Add(MapMarker.FromData(d));
                }

                else if (v.Key.StartsWith("world_"))
                {
                    int wid = int.Parse(v.Key.Substring(6));

                    if (!data.TryGetValue(wid, out PlayerWorldData pwd))
                    {
                        pwd = new PlayerWorldData();
                        data.Add(wid, pwd);
                    }

                    pwd.Load(v.Value as TagCompound);
                }
            }
        }

        public override void OnEnterWorld(Player player)
        {
            if (Main.dedServ) return;
            Dictionary<int, PlayerWorldData> data = MyWorldData;

            if (!data.ContainsKey(Main.worldID))
                data.Add(Main.worldID, new PlayerWorldData());

            MapSystem.CurrentPlayerWorldData = data[Main.worldID];

            Net.MapClient.RequestMarkers();

            MapSystem.AddSpecialMarkers();
        }

        public override void PostUpdate()
        {
            if (Main.dedServ) return;
            if (MapSystem.CreateMarkerKeybind.JustPressed)
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
                MapSystem.CurrentPlayerWorldData.AddMarker(m);
                MapSystem.MarkerGui.SetMarker(m);
            }
        }
    }
    public class PlayerWorldData
    {
        const string MarkersTagKey = "markers";
        const string PinnedTagKey = "pinned";

        public List<AbstractMarker> Markers = new List<AbstractMarker>();
        public HashSet<Guid> Pinned = new HashSet<Guid>();

        public Utilities.ShortGuids ShortGuids = new Utilities.ShortGuids();

        public void AddMarker(AbstractMarker m)
        {
            Markers.RemoveAll(x => x.Id == m.Id);

            Markers.Add(m);
            ShortGuids.AddToDictionary(m.Id);
        }

        public void AddMarkers(IEnumerable<AbstractMarker> m)
        {
            foreach (AbstractMarker am in m)
                AddMarker(am);
        }

        public void Load(TagCompound tag)
        {
            if (tag.ContainsKey(MarkersTagKey))
            {
                foreach (TagCompound m in tag.GetList<TagCompound>(MarkersTagKey))
                {
                    Markers.Add(MapMarker.FromData(m));
                }
            }
            if (tag.ContainsKey(PinnedTagKey))
            {
                Pinned.UnionWith(tag.GetList<string>(PinnedTagKey).Select(s => Guid.Parse(s)));
            }
        }

        public TagCompound Save()
        {
            HashSet<Guid> existingMarkers = new HashSet<Guid>(Markers.Select(m => m.Id));

            return new TagCompound()
            {
                [MarkersTagKey] = Markers.Where(m => m is MapMarker mm && !mm.IsServerSide).Select(x => (x as MapMarker).GetData()).ToList(),
                [PinnedTagKey] = Pinned.Where(p => existingMarkers.Contains(p)).Select(p => p.ToString()).ToList()
            };
        }

        public void Clear()
        {
            Markers.Clear();
            Pinned.Clear();
            ShortGuids.Clear();
        }

    }
}
