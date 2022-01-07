using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace MapMarkers
{
    public class Hotkeys
    {
        public Hotkeys(MapMarkers mod) 
        {
            CreateMarker = mod.RegisterHotKey("Create Marker", "B");
        }

        public ModHotKey CreateMarker;
    }
}
