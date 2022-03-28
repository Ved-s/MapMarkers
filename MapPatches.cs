using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace MapMarkers
{
    public static class MapPatches
    {
        public static MapMarkers Mod => ModContent.GetInstance<MapMarkers>();

        public static void Apply()
        {
            IL.Terraria.Main.DoUpdate += CanPauseGameIL;
            IL.Terraria.Main.DrawMap += DrawMapPatches;
        }

        public static void Remove()
        {
            IL.Terraria.Main.DoUpdate -= CanPauseGameIL;
            IL.Terraria.Main.DrawMap -= DrawMapPatches;
        }

        private static void DrawMapPatches(ILContext il) 
        {
            MapPositionPatch(il);
            DrawingOnAnyMap(il);
            AfterAllMapDraws(il);
        }
        private static void CanPauseGameIL(ILContext il)
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
                Mod.Logger.WarnFormat("Patch error: {0} (1)", il.Method.FullName);
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
                Mod.Logger.WarnFormat("Patch error: {0} (2)", il.Method.FullName);
                return;
            }

            c.Index += 2;
            c.Emit(OpCodes.Call, PatchMethod(nameof(CanPauseGame)));
            c.Emit(OpCodes.Brtrue, pauseCode);
        }

        private static void AfterAllMapDraws(ILContext il) 
        {
            /*
              IL_323D: ldloc.0
	          IL_323E: ldstr     ""
	          IL_3243: call      bool [mscorlib]System.String::op_Inequality(string, string)
	          IL_3248: brfalse.s IL_3257

	          IL_324A: ldarg.0
	          IL_324B: ldloc.0
	          IL_324C: ldc.i4.0
	          IL_324D: ldc.i4.0
	          IL_324E: ldc.i4.m1
	          IL_324F: ldc.i4.m1
	          IL_3250: ldc.i4.m1
	          IL_3251: ldc.i4.m1
	          IL_3252: call      instance void Terraria.Main::MouseText(string, int32, uint8, int32, int32, int32, int32)
            */

            ILCursor c = new ILCursor(il);

            int mouseText = -1;

            if (!c.TryGotoNext(
                x=>x.MatchLdloc(out mouseText),
                x=>x.MatchLdstr(""),
                x=>x.MatchCall(out _),
                x=>x.MatchBrfalse(out _),

                x=>x.MatchLdarg(0),
                x=>x.MatchLdloc(mouseText),
                x=>x.MatchLdcI4(0),
                x=>x.MatchLdcI4(0),
                x=>x.MatchLdcI4(-1),
                x=>x.MatchLdcI4(-1),
                x=>x.MatchLdcI4(-1),
                x=>x.MatchLdcI4(-1),
                x=>x.MatchCall<Main>("MouseText")
                )) 
            {
                Mod.Logger.WarnFormat("Patch error: {0} (after all map draws)", il.Method.FullName);
                return;
            }

            c.Index++;

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloca, mouseText);
            c.Emit(OpCodes.Call, PatchMethod(nameof(PostDrawFullMap)));
            c.Emit(OpCodes.Ldloc, mouseText);
        }
        private static void DrawingOnAnyMap(ILContext il)
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
                x => x.MatchLdloc(out mouseText),
                x => x.MatchLdcI4(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchCall<Main>("MouseText")
                ))
            {
                Mod.Logger.WarnFormat("Patch error: {0} (get MouseText)", il.Method.FullName);
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
                Mod.Logger.WarnFormat("Patch error: {0} (patch location)", il.Method.FullName);
                return;
            }

            c.Index++;

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloca, mouseText);
            c.Emit(OpCodes.Call, PatchMethod(nameof(PostDrawMap)));
            c.Emit<Main>(OpCodes.Ldsfld, "mapFullscreen");
        }
        private static void MapPositionPatch(ILContext il) 
        {
            /*
                                                                if (Main.MapStyle == 1
	         IL_0F1C: ldsfld    int32 Terraria.Main::mapStyle
	         IL_0F21: ldc.i4.1
	         IL_0F22: bne.un.s  IL_0F4D
                                                                    && !Main.mapFullscreen)
	         IL_0F24: ldsfld    bool Terraria.Main::mapFullscreen
	         IL_0F29: brtrue.s  IL_0F4D
                                                                {
                                                                    if (num13 < num7)
	         IL_0F2B: ldloc.s   num13
	         IL_0F2D: ldloc.s   num7
	         IL_0F2F: bge.un.s  IL_0F3C
                                                                        num1 -= (num13 - num7) * num5; // we need num1 -- Map pos X
	         IL_0F31: ldloc.1
	         IL_0F32: ldloc.s   num13
	         IL_0F34: ldloc.s   num7
	         IL_0F36: sub
	         IL_0F37: ldloc.s   num5
	         IL_0F39: mul
	         IL_0F3A: sub
	         IL_0F3B: stloc.1
                                                                    if (num14 < num8)
	         IL_0F3C: ldloc.s   num14
	         IL_0F3E: ldloc.s   num8
	         IL_0F40: bge.un.s  IL_0F4D
                                                                        num2 -= (num14 - num8) * num5; // num2 -- Map pos Y
	         IL_0F42: ldloc.2
	         IL_0F43: ldloc.s   num14
	         IL_0F45: ldloc.s   num8
	         IL_0F47: sub
	         IL_0F48: ldloc.s   num5
	         IL_0F4A: mul
	         IL_0F4B: sub
	         IL_0F4C: stloc.2
                                                                }
            */

            ILCursor c = new ILCursor(il);

            int mapPosX = -1, mapPosY = -1;

            if (!c.TryGotoNext(
                MoveType.After,

                x=>x.MatchLdsfld<Main>("mapStyle"),
                x=>x.MatchLdcI4(1),
                x=>x.MatchBneUn(out _),

                x=>x.MatchLdsfld<Main>("mapFullscreen"),
                x=>x.MatchBrtrue(out _),

                x=>x.MatchLdloc(out _),
                x=>x.MatchLdloc(out _),
                x=>x.MatchBgeUn(out _),

                x=>x.MatchLdloc(out mapPosX),
                x=>x.MatchLdloc(out _),
                x=>x.MatchLdloc(out _),
                x=>x.MatchSub(),
                x=>x.MatchLdloc(out _),
                x=>x.MatchMul(),
                x=>x.MatchSub(),
                x=>x.MatchStloc(mapPosX),

                x=>x.MatchLdloc(out _),
                x=>x.MatchLdloc(out _),
                x=>x.MatchBgeUn(out _),

                x=>x.MatchLdloc(out mapPosY),
                x=>x.MatchLdloc(out _),
                x=>x.MatchLdloc(out _),
                x=>x.MatchSub(),
                x=>x.MatchLdloc(out _),
                x=>x.MatchMul(),
                x=>x.MatchSub(),
                x=>x.MatchStloc(mapPosY)

                ))
            {
                Mod.Logger.WarnFormat("Patch error: {0} (patch map pos)", il.Method.FullName);
                return;
            }

            c.Index += 8;
            c.Emit(OpCodes.Ldloca, mapPosX);
            c.Emit(OpCodes.Ldloca, mapPosY);
            c.Emit(OpCodes.Call, PatchMethod(nameof(FixOverlayMapPosition)));
        }
        
        private static void PostDrawMap(ref string text)
        {
            ModContent.GetInstance<MapMarkers>().Renderer.PostDrawMap(ref text);
        }
        private static void PostDrawFullMap(ref string text)
        {
            ModContent.GetInstance<MapMarkers>().Renderer.PostDrawFullMap(ref text);
        }
        private static bool CanPauseGame()
        {
            return ModContent.GetInstance<MapMarkers>().MarkerGui.Marker != null &&
                (ModContent.GetInstance<MapConfig>().AutopauseOnUI || Main.autoPause);
        }
        private static void FixOverlayMapPosition(ref float mapX, ref float mapY)
        {
            if (MapHelper.IsOverlayMap)
            {
                Vector2 screen = new Vector2(Main.screenWidth, Main.screenHeight);

                Vector2 diff = (Main.screenPosition + screen / 2) - Main.LocalPlayer.Center;

                float f = (16 - Main.mapOverlayScale) / 16;

                diff *= f;

                mapX -= diff.X;
                mapY -= diff.Y;
            }
            MapHelper.OverlayMapScreen = new Vector2(mapX, mapY);
        }

        private static MethodInfo PatchMethod(string name) =>
            typeof(MapPatches).GetMethod(name, (BindingFlags)(-1));
    }
}
