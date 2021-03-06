using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.UI;
using Hjson;
using Terraria.Localization;

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
            LoadTranslations();

            CreateMarkerKeybind = RegisterHotKey("Create Marker", "/");

            MarkerGui = new MarkerGui(this);
            Renderer = new MapRenderer(this);

            MapPatches.Apply();
        }

        private void LoadTranslations() 
        {
            TmodFile tmodFile = typeof(Mod).GetProperty("File", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this) as TmodFile;

            Dictionary<string, ModTranslation> dictionary = new Dictionary<string, ModTranslation>();

            foreach (TmodFile.FileEntry fe in tmodFile.Where(e => Path.GetExtension(e.Name) == ".hjson"))
            {
                StreamReader sr = new StreamReader(tmodFile.GetStream(fe));
                string content = sr.ReadToEnd();
                sr.Close();

                int culture = GameCulture.FromName(Path.GetFileNameWithoutExtension(fe.Name)).LegacyId;

                RecursivelyLoadTranslations(HjsonValue.Parse(content), culture, "", dictionary);
            }

            foreach (ModTranslation mt in dictionary.Values)
                AddTranslation(mt);
                
        }

        private void RecursivelyLoadTranslations(JsonValue value, int culture, string path, Dictionary<string, ModTranslation> dictionary) 
        {
            if (value is JsonObject obj)
            {
                foreach (var v in obj)
                {
                    string objpath = (path.Length > 0 ? path + "." : "") + v.Key;
                    RecursivelyLoadTranslations(v.Value, culture, objpath, dictionary);
                }
            }
            else
            {
                string v = value.Qstr();
                ModTranslation mt = AddModTranslation(dictionary, path);
                mt.AddTranslation(culture, v);
            }
        }

        private ModTranslation AddModTranslation(Dictionary<string, ModTranslation> dictionary, string key) 
        {
            ModTranslation mt;
            if (!dictionary.TryGetValue(key, out mt))
            {
                Logger.InfoFormat("Adding key: {0}", key);

                mt = (ModTranslation)Activator.CreateInstance(typeof(ModTranslation), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { key, false }, null);
                dictionary.Add(key, mt);
            };
            return mt;

            
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

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                CurrentPlayerWorldData.AddMarker(new SpawnMarker());

                if (conf.AddChestMarkers) AddChestMarkers();
                if (conf.AddStatueMarkers) AddStatueMarkers();
            }
            else if (Main.netMode == NetmodeID.Server) 
            {
                AddChestMarkers();
                AddStatueMarkers();
            }
        }

        internal void AddStatueMarkers()
        {
            if (CurrentPlayerWorldData is null && Main.netMode != NetmodeID.Server)
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

                        AddSpecialMarker(new StatueMarker(item, x, y));
                        statues++;
                    }
                }
#if DEBUG
            Logger.InfoFormat("Added {0} statue markers", statues);
#endif
        }
        internal void AddChestMarkers()
        {
            if (CurrentPlayerWorldData is null && Main.netMode != NetmodeID.Server)
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
                    AddSpecialMarker(new LockedChestMarker(i));
                    chests++;
                }
            }

#if DEBUG
            Logger.InfoFormat("Added {0} locked chest markers", chests);
#endif
        }

        public void AddSpecialMarker(AbstractMarker m) 
        {
            if (Main.netMode == NetmodeID.Server)
                ModContent.GetInstance<Net.MapServer>().SpecialMarkers.Add(m);
            else CurrentPlayerWorldData.AddMarker(m);
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