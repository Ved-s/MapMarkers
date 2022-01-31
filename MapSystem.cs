using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers
{
    public class MapSystem : ModSystem
    {
        public List<MapMarker> CurrentMarkers;
        public MarkerGui MarkerGui;
        public MapRenderer MapRenderer => ModContent.GetInstance<MapRenderer>();
        
        public Dictionary<int, Dictionary<int, List<MapMarker>>> AllMarkers = new Dictionary<int, Dictionary<int,List<MapMarker>>>();

        public override void Load()
        {
            MarkerGui = new(this);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            MapRenderer.Update();
            MarkerGui.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int i = layers.FindIndex((l) => (l.Name == "Vanilla: Mouse Text"));
            layers.Insert(i, new LegacyGameInterfaceLayer("MapMarkers: Gui", MarkerGui.Draw, InterfaceScaleType.UI));
        }


    }
}
