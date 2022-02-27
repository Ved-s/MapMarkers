using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.Config;

namespace MapMarkers
{
    internal class MapConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Label("Autopause on Marker UI")]
        public bool AutopauseOnUI { get; set; }
    }
}
