using MapMarkers.Structures;
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
            tag["pinned"] = MapMarkers.PinnedMarkers.Select(g => g.ToByteArray()).ToList();

            if (Main.gameMenu) // Player exiting to menu
            {
                MapMarkers.Markers.RemoveWhere(kvp => kvp.Value.SaveLocation != SaveLocation.Server);
                MapMarkers.PinnedMarkers.Clear();
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("markers", out TagCompound markers))
                CurrentMarkerData = markers;
            if (tag.TryGet("pinned", out List<byte[]> pinned))
            {
                MapMarkers.PinnedMarkers.Clear();
                MapMarkers.PinnedMarkers.UnionWith(pinned.Select(b => new Guid(b)));
            }
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
