using MapMarkers.Markers;
using MapMarkers.Structures;
using MapMarkers.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MapMarkers
{
    public class MarkerWorld : ModSystem
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();
        internal static ModKeybind CreateMarkerKeybind = null!;

        public override void Load()
        {
            CreateMarkerKeybind = KeybindLoader.RegisterKeybind(MapMarkers, "Create marker", Keys.OemPeriod);
        }

        public override void OnWorldLoad()
        {
            MapMarkers.MarkerGuids.Clear();
            Networking.OnJoinWorld();
        }

        public override void OnWorldUnload()
        {
            MapMarkers.Markers.RemoveWhere(kvp => kvp.Value.SaveLocation != SaveLocation.Client);
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["markers"] = MapMarkers.Markers.Values
                .Where(m => m.SaveLocation == SaveLocation.Server)
                .Select(m => MapMarkers.SaveMarker(m))
                .ToList();
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.TryGet("markers", out List<TagCompound> markers))
                foreach (TagCompound markerTag in markers)
                {
                    MapMarker? marker = MapMarkers.LoadMarker(markerTag, SaveLocation.Server);
                    if (marker is null)
                        continue;

                    MapMarkers.AddMarker(marker, false);
                }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (index >= 0)
                layers.Insert(index, new LegacyGameInterfaceLayer("MapMarkers: UI", MarkerEditMenu.Draw, InterfaceScaleType.UI));
        }

        public override void UpdateUI(GameTime gameTime)
        {
            MarkerMenu.Update(gameTime);
            MarkerEditMenu.Update(gameTime);
        }

        public override void PostUpdateInput()
        {
            Keybinds.Update();

            if (CreateMarkerKeybind.JustPressed && !MarkerEditMenu.Visible && !Keybinds.InputBlocked)
            {
                Vector2? pos = null;
                if (Helper.MapVisibleScreenRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    pos = Helper.ScreenToMap(Main.MouseScreen);
                    if (!Helper.MapScreenRect.Contains(pos.Value))
                        pos = null;
                }
                else
                    pos = Main.LocalPlayer.Center / 16;

                if (pos.HasValue)
                {
                    PlacedMarker marker = new();
                    marker.DisplayItemType = ItemID.TrifoldMap;
                    marker.Position = pos.Value;
                    MapMarkers.AddMarker(marker, true);

                    MarkerEditMenu.Show(marker);
                }
            }
        }
    }
}
