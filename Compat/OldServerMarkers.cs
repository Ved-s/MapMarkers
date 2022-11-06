using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers.Compat
{
    internal class OldServerMarkers : ModSystem
    {
        public override string Name => "MapServer";

        public override void LoadWorldData(TagCompound tag)
        {
            MarkerWorld newWorld = ModContent.GetInstance<MarkerWorld>();

            if (tag.TryGet("markers", out IList<TagCompound> markers))
                foreach (TagCompound marker in markers)
                    MapMarkers.Instance.AddMarker(OldClientMarkers.LoadOldMarker(marker), false);

            if (tag.TryGet("markerCap", out int markerCap))
                newWorld.PlayerMarkerCap = markerCap;
        }
        public override void SaveWorldData(TagCompound tag) { }
    }
}
