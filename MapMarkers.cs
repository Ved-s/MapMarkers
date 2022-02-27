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
        public List<MapMarker> CurrentMarkers;
        public Hotkeys Hotkeys;
        public MarkerGui MarkerGui;
        public MapRenderer Renderer;

        public Dictionary<int, Dictionary<int, List<MapMarker>>> AllMarkers = new Dictionary<int, Dictionary<int,List<MapMarker>>>();

        public MapMarkers() 
        {
        }

        public override void Load()
        {
            MarkerGui = new MarkerGui(this);
            Hotkeys = new Hotkeys(this);
            Renderer = new MapRenderer(this);

            IL.Terraria.Main.DoUpdate += CanPauseGameIL;
        }

        public override void Unload()
        {
            IL.Terraria.Main.DoUpdate -= CanPauseGameIL;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (Main.dedServ) ModContent.GetInstance<Net.MapServer>().HandlePacket(reader, whoAmI);
            else Net.MapClient.HandlePacket(reader);
        }

        public override void PostDrawFullscreenMap(ref string mouseText)
        {
            Renderer.PostDrawFullscreenMap(ref mouseText);
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

        private void CanPauseGameIL(MonoMod.Cil.ILContext il)
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
                x=>x.MatchLdsfld<Main>("inFancyUI"),
                x=>x.MatchBrfalse(out _),
                x=>x.MatchLdsfld<Main>("autoPause"),
                x=>x.MatchBrfalse(out _)
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
            c.Emit<MapMarkers>(OpCodes.Call, "CanPauseGame");
            c.Emit(OpCodes.Brtrue, pauseCode);
        }

        private static bool CanPauseGame()
        {
            return ModContent.GetInstance<MapMarkers>().MarkerGui.Marker != null &&
                (ModContent.GetInstance<MapConfig>().AutopauseOnUI || Main.autoPause);
        }
    }
}