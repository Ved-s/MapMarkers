using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers
{
    public class MapMarkers : Mod
    {
        public PlayerWorldData CurrentPlayerWorldData;

        public ModHotKey CreateMarkerKeybind;

        public MarkerGui MarkerGui;
        public MapRenderer Renderer;

        public Dictionary<int, Dictionary<int, PlayerWorldData>> AllPlayerWorldData = new Dictionary<int, Dictionary<int, PlayerWorldData>>();

        public override void Load()
        {
            CreateMarkerKeybind = RegisterHotKey("Create Marker", "/");

            MarkerGui = new MarkerGui(this);
            Renderer = new MapRenderer(this);

            MapPatches.Apply();
        }

        public override void Unload()
        {
            MapPatches.Remove();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (Main.dedServ) ModContent.GetInstance<Net.MapServer>().HandlePacket(reader, whoAmI);
            else Net.MapClient.HandlePacket(reader);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int i = layers.FindIndex((l) => (l.Name == "Vanilla: Mouse Text"));
            layers.Insert(i, new LegacyGameInterfaceLayer("MapMarkers: Gui", MarkerGui.Draw, InterfaceScaleType.UI));
        }

        public override void UpdateUI(GameTime gameTime)
        {
            base.UpdateUI(gameTime);
            Renderer.Update();
            MarkerGui.Update(gameTime);
        }

        internal void AddSpecialMarkers()
        {
            MapConfig conf = ModContent.GetInstance<MapConfig>();

            CurrentPlayerWorldData.AddMarker(new SpawnMarker());

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (conf.AddChestMarkers) AddChesMarkers();
                if (conf.AddStatueMarkers) AddStatueMarkers();
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
            Logger.InfoFormat("Added {0} statue markers", statues);
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

                if (Chest.isLocked(chest.x, chest.y))
                {
                    CurrentPlayerWorldData.AddMarker(new LockedChestMarker(i));
                    chests++;
                }
            }

#if DEBUG
            Logger.InfoFormat("Added {0} locked chest markers", chests);
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

        private bool IsStatueTile(Tile t)
        {
            if (t.type != TileID.Statues) return false;
            if (t.frameX % 36 != 0) return false;
            if (t.frameY % 54 != 0) return false;
            return true;
        }
        private int StatueTileToItem(Tile t)
        {
            int id = t.frameX / 36;
            id += t.frameY / 54 * 55;
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