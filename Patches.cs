using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Diagnostics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace MapMarkers
{
    public class Patches : ILoadable
    {
        public static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public void Load(Mod mod)
        {
            IL.Terraria.Main.DrawMap += Main_DrawMap;
        }

        public void Unload()
        {
            IL.Terraria.Main.DrawMap -= Main_DrawMap;
        }

        private void Main_DrawMap(ILContext il)
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
              
                   +#: br AfterCall // to not call DrawMap_AfterIcons second time

	          IL_2E69: ldloc.s   num18   <- if (Main.mapFullscreen) ends here
                  
                   +#: pop
                   +#: ldloca text
                   +#: ldc.i4 1
                   +#: call MapMarkers.Patches.DrawMap_AfterIcons(string&, bool)
        AfterCall: +#: ldloc num18

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

        private static void DrawMap(ref string mouseText, bool onTop)
        {
            ModContent.GetInstance<MarkerRenderer>().DrawMarkers(ref mouseText, onTop);
        }
    }
}
