using Microsoft.Xna.Framework;
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
    }
}