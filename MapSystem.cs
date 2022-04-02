using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers
{
    public class MapSystem : ModSystem
    {
        public PlayerWorldData CurrentPlayerWorldData;

        public ModKeybind CreateMarkerKeybind;

        public MarkerGui MarkerGui;
        public MapRenderer MapRenderer => ModContent.GetInstance<MapRenderer>();

        public Dictionary<int, Dictionary<int, PlayerWorldData>> AllPlayerWorldData = new();

        public override void Load()
        {
            CreateMarkerKeybind = KeybindLoader.RegisterKeybind(Mod, "Create Marker", "/");

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

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (conf.AddChestMarkers)
                    AddChesMarkers();
                if (conf.AddStatueMarkers)
                    AddStatueMarkers();
            }
        }

        internal void AddStatueMarkers()
        {
            if (CurrentPlayerWorldData is null)
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
                        CurrentPlayerWorldData.AddMarker(new StatueMarker(item, x, y));
                        statues++;
                    }
                }
#if DEBUG
            Mod.Logger.InfoFormat("Added {0} statue markers", statues);
#endif
        }
        internal void AddChesMarkers()
        {
            if (CurrentPlayerWorldData is null)
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
                    CurrentPlayerWorldData.AddMarker(new LockedChestMarker(i));
                    chests++;
                }
            }
#if DEBUG
            Mod.Logger.InfoFormat("Added {0} locked chest markers", chests);
#endif
        }

        internal void ResetStatueMarkers()
        {
            if (CurrentPlayerWorldData is null)
                return;

            CurrentPlayerWorldData.Markers.RemoveAll(m => m is StatueMarker);
        }
        internal void ResetChestMarkers()
        {
            if (CurrentPlayerWorldData is null)
                return;

            CurrentPlayerWorldData.Markers.RemoveAll(m => m is LockedChestMarker);
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
            int x = t.TileFrameX / 36;
            int y = (t.TileFrameY / 54) % 3;
            int id = x + y * 55;

            if (id >= 51 && id <= 62)
                return 3651 + id - 51;

            if (id >= 63 && id <= 75)
                return 3708 + id - 63;

            switch (id)
            {
                case 0: return 360;
                case 1: return 52;
                case 43: return 1152;
                case 44: return 1153;
                case 45: return 1154;
                case 46: return -1; // BlueDungeonVase 1408;
                case 47: return -1; // GreenDungeonVase 1409;
                case 48: return -1; // PinkDungeonVase 1410;
                case 49: return -1; // ObsidianVase 1462;
                case 50: return 2672;
                case 76: return 4397;
                case 77: return 4360;
                case 78: return 4342;
                case 79: return 4466;
                default: return 438 + id - 2;

            }
        }
    }
}
