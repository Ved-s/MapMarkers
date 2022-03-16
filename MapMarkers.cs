using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
        public List<AbstractMarker> CurrentMarkers;
        public Hotkeys Hotkeys;
        public MarkerGui MarkerGui;
        public MapRenderer Renderer;

        public Dictionary<int, Dictionary<int, List<AbstractMarker>>> AllMarkers = new Dictionary<int, Dictionary<int, List<AbstractMarker>>>();

        public MapMarkers()
        {
        }

        public override void Load()
        {
            MarkerGui = new MarkerGui(this);
            Hotkeys = new Hotkeys(this);
            Renderer = new MapRenderer(this);

            IL.Terraria.Main.DoUpdate += CanPauseGameIL;
            IL.Terraria.Main.DrawMap += DrawMapIL;
        }

        public override void Unload()
        {
            IL.Terraria.Main.DoUpdate -= CanPauseGameIL;
            IL.Terraria.Main.DrawMap -= DrawMapIL;
        }


        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (Main.dedServ) ModContent.GetInstance<Net.MapServer>().HandlePacket(reader, whoAmI);
            else Net.MapClient.HandlePacket(reader);
        }

        //public override void PostDrawFullscreenMap(ref string mouseText)
        //{
        //    Renderer.PostDrawMap(ref mouseText);
        //}

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

            CurrentMarkers.Add(new SpawnMarker());

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
            Logger.InfoFormat("Added {0} statue markers", statues);
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

                if (Chest.isLocked(chest.x, chest.y))
                {
                    CurrentMarkers.Add(new LockedChestMarker(i));
                    chests++;
                }
            }

#if DEBUG
            Logger.InfoFormat("Added {0} locked chest markers", chests);
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

        private void DrawMapIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            /* Get mouseText variable index
	          IL_324B: ldloc.0
	          IL_324C: ldc.i4.0
	          IL_324D: ldc.i4.0
	          IL_324E: ldc.i4.m1
	          IL_324F: ldc.i4.m1
	          IL_3250: ldc.i4.m1
	          IL_3251: ldc.i4.m1
	          IL_3252: call      instance void Terraria.Main::MouseText(string, int32, uint8, int32, int32, int32, int32)
             */

            int mouseText = -1;

            if (!c.TryGotoNext(
                x=>x.MatchLdloc(out mouseText),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchCall<Main>("MouseText")
                )) 
            {
                Logger.WarnFormat("Patch error: {0} (1)", il.Method.FullName);
                return;
            }

            /* Patch
	          IL_11A4: ldsfld    bool Terraria.Main::mapFullscreen
	          IL_11A9: brtrue    IL_2348
             
	          IL_11AE: ldsfld    int32 Terraria.Main::mapStyle
	          IL_11B3: ldc.i4.2
             */

            if (!c.TryGotoPrev(
                x => x.MatchLdsfld<Main>("mapFullscreen"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdsfld<Main>("mapStyle"),
                x => x.MatchLdcI4(2)
                ))
            {
                Logger.WarnFormat("Patch error: {0} (2)", il.Method.FullName);
                return;
            }

            c.Emit(OpCodes.Ldloca, mouseText);
            c.Emit<MapMarkers>(OpCodes.Call, nameof(PostDrawMap));
        }

        private void CanPauseGameIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel pauseCode = c.DefineLabel();

            /*
                ldsfld    bool Terraria.Main::inFancyUI
                brfalse   IL_1829
                
                ldsfld    bool Terraria.Main::autoPause
                brfalse   IL_1829
             */

            if (!c.TryGotoNext(
                x => x.MatchLdsfld<Main>("inFancyUI"),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdsfld<Main>("autoPause"),
                x => x.MatchBrfalse(out _)
                ))
            {
                Logger.WarnFormat("Patch error: {0} (1)", il.Method.FullName);
                return;
            }
            c.Index += 4;
            c.MarkLabel(pauseCode);

            /*
                ldsfld    int32 Terraria.Main::netMode
                brtrue    IL_1829
                
                ldsfld    bool Terraria.Main::playerInventory
                brtrue.s  IL_1455
             */

            if (!c.TryGotoPrev(
                x => x.MatchLdsfld<Main>("netMode"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdsfld<Main>("playerInventory"),
                x => x.MatchBrtrue(out _)
                ))
            {
                Logger.WarnFormat("Patch error: {0} (2)", il.Method.FullName);
                return;
            }

            c.Index += 2;
            c.Emit<MapMarkers>(OpCodes.Call, nameof(CanPauseGame));
            c.Emit(OpCodes.Brtrue, pauseCode);
        }

        private static void PostDrawMap(ref string text)
        {
            ModContent.GetInstance<MapMarkers>().Renderer.PostDrawMap(ref text);
        }

        private static bool CanPauseGame()
        {
            return ModContent.GetInstance<MapMarkers>().MarkerGui.Marker != null &&
                (ModContent.GetInstance<MapConfig>().AutopauseOnUI || Main.autoPause);
        }
    }
}