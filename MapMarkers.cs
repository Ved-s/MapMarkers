using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers
{
	public class MapMarkers : Mod
	{
        public static Dictionary<int, List<MapMarker>> Markers = new Dictionary<int, List<MapMarker>>();

        public static Hotkeys Hotkeys;

        public static MapMarkers Instance;

        public static MarkerGui MarkerGui;

        public MapMarkers() 
        {
            Instance = this;
        }

        public override void Load()
        {
            MarkerGui = new MarkerGui();
            Hotkeys = new Hotkeys();
        }

        public override void PostDrawFullscreenMap(ref string mouseText)
        {
            MapRenderer.PostDrawFullscreenMap(ref mouseText);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int i = layers.FindIndex((l) => (l.Name == "Vanilla: Mouse Text"));
            layers.Insert(i, new LegacyGameInterfaceLayer("MapMarkers: Gui", MarkerGui.Draw, InterfaceScaleType.UI));
        }

        public override void UpdateUI(GameTime gameTime)
        {
            base.UpdateUI(gameTime);
            MapRenderer.Update();
            MarkerGui.Update(gameTime);
        }
    }
}