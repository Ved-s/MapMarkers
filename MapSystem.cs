using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers
{
    public class MapSystem : ModSystem
    {
        public List<AbstractMarker> CurrentMarkers;
        public MarkerGui MarkerGui;
        public MapRenderer MapRenderer => ModContent.GetInstance<MapRenderer>();
        
        public Dictionary<int, Dictionary<int, List<AbstractMarker>>> AllMarkers = new();

        public override void Load()
        {
            MarkerGui = new(this);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            MapRenderer.Update();
            MarkerGui.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int i = layers.FindIndex((l) => (l.Name == "Vanilla: Mouse Text"));
            layers.Insert(i, new LegacyGameInterfaceLayer("MapMarkers: Gui", MarkerGui.Draw, InterfaceScaleType.UI));
        }

        internal void AddSpecialMarkers()
        {
            MapConfig conf = ModContent.GetInstance<MapConfig>();

            //CurrentMarkers.Add(new SpawnMarker());

            if (conf.AddChestMarkers) AddChesMarkers();
            if (conf.AddStatueMarkers) AddStatueMarkers();
        }

        internal void AddStatueMarkers()
        {
            if (CurrentMarkers is null)
                return;

            int statues = 0;
            for (int x = 0; x < Main.maxTilesX; x++)
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Tile t = Main.tile[x, y];
                    if (t == null)
                        continue;
                    if (IsStatueTile(t))
                    {
                        int item = StatueTileToItem(t);
                        if (item < 0) continue;
                        CurrentMarkers.Add(new StatueMarker(item, x, y));
                        statues++;
                    }
                }
#if DEBUG
            Mod.Logger.InfoFormat("Added {0} statue markers", statues);
#endif
        }
        internal void AddChesMarkers()
        {
            if (CurrentMarkers is null)
                return;

            int chests = 0;
            for (int i = 0; i < Main.chest.Length; i++)
            {
                Chest chest = Main.chest[i];
                if (chest is null) continue;
                if (Main.tile[chest.x, chest.y] == null)
                    continue;

                if (Chest.IsLocked(chest.x, chest.y))
                {
                    CurrentMarkers.Add(new LockedChestMarker(i));
                    chests++;
                }
            }

#if DEBUG
            Mod.Logger.InfoFormat("Added {0} locked chest markers", chests);
#endif
        }

        internal void ResetStatueMarkers()
        {
            if (CurrentMarkers is null)
                return;

            CurrentMarkers.RemoveAll(m => m is StatueMarker);
        }
        internal void ResetChestMarkers()
        {
            if (CurrentMarkers is null)
                return;

            CurrentMarkers.RemoveAll(m => m is LockedChestMarker);
        }

        private static bool IsStatueTile(Tile t)
        {
            if (t.TileType != TileID.Statues) return false;
            if (t.TileFrameX % 36 != 0) return false;
            if (t.TileFrameY % 54 != 0) return false;
            return true;
        }
        private static int StatueTileToItem(Tile t)
        {
            int id = t.TileFrameX / 36;
            id += t.TileFrameY / 54 * 55;
            if (id == 0)
            {
                return -1; // PinkVase
            }
            else if (id == 1)
            {
                return 52;
            }
            else switch (id)
                {
                    case 43: return 1152;
                    case 44: return 1153;
                    case 45: return 1154;
                    case 46: return -1; // BlueDungeonVase
                    case 47: return -1; // GreenDungeonVase
                    case 48: return -1; // PinkDungeonVase
                    case 49: return -1; // ObsidianVase
                    case 50: return 2672;

                    case 51:
                    case 52:
                    case 53:
                    case 54:
                    case 55:
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62: return 3651 + id - 51;

                    default: return ((id < 63 || id > 75) ? (438 + id - 2) : (3708 + id - 63));

                }
        }
    }
}
