using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace MapMarkers
{
	public class MapMarkers : Mod
	{
        public override void Load()
        {
            IL.Terraria.Main.CanPauseGame += CanPauseGameIL;
        }

        public override void Unload()
        {
            IL.Terraria.Main.CanPauseGame -= CanPauseGameIL;
        }

        private void CanPauseGameIL(ILContext il)
        {
            ILCursor c = new(il);

            /*
               IL_0002: ldsfld    int32 Terraria.Main::netMode
               IL_0007: brtrue.s  IL_0077
             */

            int pauseFlag = -1;

            if (!c.TryGotoNext(
                x => x.MatchLdsfld<Main>("netMode"),
                x => x.MatchBrtrue(out _),
                x => x.MatchLdloc(out pauseFlag)
                ))
            {
                Logger.WarnFormat("Patch error: {0}", il.Method.FullName);
                return;
            }

            c.Index += 2;
            c.Emit(OpCodes.Ldloc, pauseFlag);
            c.Emit<MapMarkers>(OpCodes.Call, "CanPauseGame");
            c.Emit(OpCodes.Or);
            c.Emit(OpCodes.Stloc, pauseFlag);
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (Main.dedServ) ModContent.GetInstance<Net.MapServer>().HandlePacket(reader, whoAmI);
            else Net.MapClient.HandlePacket(reader);
        }

        private static bool CanPauseGame()
        {
            return ModContent.GetInstance<MapSystem>().MarkerGui.Marker is not null &&
                (ModContent.GetInstance<MapConfig>().AutopauseOnUI || Main.autoPause);
        }
    }
}