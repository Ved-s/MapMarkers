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
        public ModHotKey CreateMarker = MapMarkers.Instance.RegisterHotKey("Create Marker", "B");
    }
}
