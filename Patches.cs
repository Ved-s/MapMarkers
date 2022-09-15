using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Diagnostics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace MapMarkers
{
    public class Patches : ILoadable
    {
        public static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public void Load(Mod mod)
        {
            IL.Terraria.Main.DrawMap += Main_DrawMap;
            IL.Terraria.Player.Update += Player_Update;
        }

        public void Unload()
        {
            IL.Terraria.Main.DrawMap -= Main_DrawMap;
            IL.Terraria.Player.Update -= Player_Update;
        }

        private void Main_DrawMap(ILContext il)
        {
            Patch_Main_DrawMap_MapRendering(il);
            Patch_Main_DrawMap_Pinging(il);
            Patch_Main_DrawMap_MapDrag(il);
        }

        private void Player_Update(ILContext il)
        {
            Patch_Player_Update_MapZoom(il);
        }

        private static void Patch_Main_DrawMap_MapRendering(ILContext il)
        {
            ILCursor c = new(il);

            /*
              IL_2E56: ldloca.s  text
	          IL_2E58: call      void Terraria.ModLoader.SystemLoader::PostDrawFullscreenMap(string&)

                   +#: ldloca text
                   +#: ldc.i4 1
                   +#: call MapMarkers.Patches.DrawMap_AfterIcons(string&, bool)

	          IL_2E5D: ldc.i4.0
	          IL_2E5E: call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Main::DrawThickCursor(bool)
	          IL_2E63: ldc.i4.0
	          IL_2E64: call      void Terraria.Main::DrawCursor(valuetype [FNA]Microsoft.Xna.Framework.Vector2, bool)
              
            +----- +#: br AfterCall // to not call DrawMap_AfterIcons second time
            |
	        | IL_2E69: ldloc.s   num18   <- if (Main.mapFullscreen) ends here
            |     
            |      +#: pop
            |      +#: ldloca text
            |      +#: ldc.i4 1
            |      +#: call MapMarkers.Patches.DrawMap_AfterIcons(string&, bool)
AfterCall:  +----> +#: ldloc num18

             */
            int mouseText = -1;
            int num18 = -1;

            if (!c.TryGotoNext(
                x=>x.MatchLdloca(out mouseText),
                x=>x.MatchCall(typeof(SystemLoader), nameof(SystemLoader.PostDrawFullscreenMap)),

                x=>x.MatchLdcI4(out _),
                x=>x.MatchCall<Main>(nameof(Main.DrawThickCursor)),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchCall<Main>(nameof(Main.DrawCursor)),

                x=>x.MatchLdloc(out num18)
                ))
            {
                MapMarkers.Logger.Warn("Patch error in Main.DrawMap: DrawAfterIcons");
                if (Debugger.IsAttached) Debugger.Break();
                return;
            }

            c.Index += 2;
            c.Emit(OpCodes.Ldloca, mouseText);
            c.Emit(OpCodes.Ldc_I4_1);
            c.Emit<Patches>(OpCodes.Call, nameof(DrawMap));
            c.Index += 4;
            ILLabel afterCall = c.DefineLabel();
            c.Emit(OpCodes.Br, afterCall);
            c.Index++;
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloca, mouseText);
            c.Emit(OpCodes.Ldc_I4_1);
            c.Emit<Patches>(OpCodes.Call, nameof(DrawMap));
            c.MarkLabel(afterCall);
            c.Emit(OpCodes.Ldloc, num18);

            c.Index = 0;

            /*
              IL_0E65: ldloc.s   flag

                   +#: ldloca text
                   +#: ldc.i4 0
                   +#: call MapMarkers.Patches.DrawMap_BeforeIcons(string&, bool)

	          IL_0E67: brfalse.s IL_0E95
              
	          IL_0E69: ldsfld    class [FNA]Microsoft.Xna.Framework.Graphics.SpriteBatch Terraria.Main::spriteBatch
	          IL_0E6E: callvirt  instance void [FNA]Microsoft.Xna.Framework.Graphics.SpriteBatch::End()
             */

            if (!c.TryGotoNext(
                x => x.MatchLdloc(out _),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
                x => x.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
                ))
            {
                MapMarkers.Logger.Warn("Patch error in Main.DrawMap: DrawBeforeIcons");
                if (Debugger.IsAttached) Debugger.Break();
                return;
            }

            c.Index++;
            c.Emit(OpCodes.Ldloca, mouseText);
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit<Patches>(OpCodes.Call, nameof(DrawMap));
        }
        private static void Patch_Main_DrawMap_Pinging(ILContext il)
        {
            ILCursor c = new(il);

            /*
              IL_085C: ldsfld    valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Main::_lastPingMousePosition
	          IL_0861: call      float32 [FNA]Microsoft.Xna.Framework.Vector2::Distance(valuetype [FNA]Microsoft.Xna.Framework.Vector2, valuetype [FNA]Microsoft.Xna.Framework.Vector2)
	          IL_0866: ldc.r4    2
	          IL_086B: bge.un.s  IL_089C

                   +#: ldsfld MapMarkers.UI.MarkerMenu::Hovering
                   +#: brtrue IL_089C

	          IL_086D: call      valuetype [FNA]Microsoft.Xna.Framework.Vector2 Terraria.Main::get_MouseScreen()
             */

            ILLabel ifEndLabel = null!;

            if (!c.TryGotoNext(
                MoveType.After,
                x=>x.MatchLdsfld<Main>("_lastPingMousePosition"),
                x=>x.MatchCall<Vector2>(nameof(Vector2.Distance)),
                x=>x.MatchLdcR4(out _),
                x=>x.MatchBgeUn(out ifEndLabel),

                x=>x.MatchCall<Main>("get_MouseScreen")
                ))
            {
                MapMarkers.Logger.Warn("Patch error in Main.DrawMap: Pinging");
                if (Debugger.IsAttached) Debugger.Break();
                return;
            }
            c.Index--;

            c.Emit(OpCodes.Ldsfld, typeof(UI.MarkerMenu).GetField("Hovering"));
            c.Emit(OpCodes.Brtrue, ifEndLabel);
        }
        private static void Patch_Main_DrawMap_MapDrag(ILContext il)
        {
            ILCursor c = new(il);
            /*
              IL_028E: ldsfld    bool Terraria.Main::mapFullscreen
	          IL_0293: brfalse   IL_08D3
              
	          IL_0298: ldsfld    bool Terraria.Main::mouseLeft
	          IL_029D: brfalse   IL_0359
             */

            ILLabel ifEndLabel = null!;

            if (!c.TryGotoNext(
                x=>x.MatchLdsfld<Main>(nameof(Main.mapFullscreen)),
                x=>x.MatchBrfalse(out _),

                x=>x.MatchLdsfld<Main>(nameof(Main.mouseLeft)),
                x => x.MatchBrfalse(out ifEndLabel)
                ))
            {
                MapMarkers.Logger.Warn("Patch error in Main.DrawMap: MapDrag");
                if (Debugger.IsAttached) Debugger.Break();
                return;
            }

            c.Index += 2;

            c.Emit(OpCodes.Ldsfld, typeof(UI.MarkerMenu).GetField("Hovering"));
            c.Emit(OpCodes.Brtrue, ifEndLabel);
        }
        private static void Patch_Player_Update_MapZoom(ILContext il)
        {
            ILCursor c = new(il);
            /*
              IL_12F7: ldsfld    bool Terraria.Main::mapFullscreen
		      IL_12FC: brfalse.s IL_1363

                   +#: ldsfld MapMarkers.UI.MarkerMenu::Hovering
          +------- +#: brfalse NoHover
          |        +#: ldc.r4 0
          |+------ +#: br Set
          ||
NoHover:  ++> IL_12FE: ldsfld    int32 Terraria.GameInput.PlayerInput::ScrollWheelDelta
 	       |  IL_1303: ldc.i4.s  120
 	       |  IL_1305: div
 	       |  IL_1306: conv.r4
Set:       +> IL_1307: stloc.s   num7


             */

            ILLabel noHover = c.DefineLabel();
            ILLabel set = c.DefineLabel();

            if (!c.TryGotoNext(
                x=>x.MatchLdsfld<Main>(nameof(Main.mapFullscreen)),
                x=>x.MatchBrfalse(out _),

                x=>x.MatchLdsfld<PlayerInput>(nameof(PlayerInput.ScrollWheelDelta)),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchDiv(),
                x=>x.MatchConvR4(),
                x=>x.MatchStloc(out _)
                ))
            {
                MapMarkers.Logger.Warn("Patch error in PLayer.Update: MapZoom");
                if (Debugger.IsAttached) Debugger.Break();
                return;
            }

            c.Index += 2;

            c.Emit(OpCodes.Ldsfld, typeof(UI.MarkerMenu).GetField("Hovering"));
            c.Emit(OpCodes.Brfalse, noHover);
            c.Emit(OpCodes.Ldc_R4, 0f);
            c.Emit(OpCodes.Br, set);
            c.MarkLabel(noHover);

            c.Index += 4;
            c.MarkLabel(set);
        }

        private static void DrawMap(ref string mouseText, bool onTop)
        {
            ModContent.GetInstance<MarkerRenderer>().DrawMarkers(ref mouseText, onTop);
        }
    }
}
