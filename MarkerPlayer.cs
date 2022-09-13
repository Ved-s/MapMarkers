using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public class MarkerPlayer : ModPlayer
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public TagCompound CurrentMarkerData { get; private set; } = new();

        public override void SaveData(TagCompound tag)
        {
            CurrentMarkerData[Main.worldID.ToString()] = MapMarkers.Markers.Values
                .Where(m => m.SaveLocation == SaveLocation.Client)
                .Select(m => MapMarkers.SaveMarker(m))
                .ToList();

            tag["markers"] = CurrentMarkerData;

            if (Main.gameMenu) // Player exiting to menu
                MapMarkers.Markers.RemoveWhere(kvp => kvp.Value.SaveLocation != SaveLocation.Server);
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("markers", out TagCompound markers))
                CurrentMarkerData = markers;
        }

        public override void OnEnterWorld(Player player)
        {
            if (CurrentMarkerData.TryGet(Main.worldID.ToString(), out List<TagCompound> markers))
            {
                foreach (TagCompound markerData in markers)
                {
                    MapMarker? marker = MapMarkers.LoadMarker(markerData);
                    if (marker is null)
                        continue;

                    MapMarkers.Markers[marker.Id] = marker;
                }
            }
        }
    }
}
