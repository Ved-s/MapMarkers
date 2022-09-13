using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameInput;

namespace MapMarkers
{
    public static class Helper
    {
        /// <summary>
        /// Current map scale
        /// </summary>
        public static float MapScale =>
                Main.mapFullscreen ? Main.mapFullscreenScale :
                Main.mapStyle == 1 ? Main.mapMinimapScale :
                Main.mapOverlayScale;

        /// <summary>
        /// Visible map rectangle in screen coordinates
        /// </summary>
        public static Rectangle MapVisibleScreenRect =>
            IsMiniMap ? new(Main.miniMapX, Main.miniMapY, Main.miniMapWidth, Main.miniMapHeight) :
                    new(0, 0, Main.screenWidth, Main.screenHeight);

        /// <summary>
        /// 0,0 point on map in screen coordinates 
        /// </summary>
        public static Vector2 MapScreenPos
        {
            get
            {
                Vector2 screenCenter = new Vector2(PlayerInput.RealScreenWidth, PlayerInput.RealScreenHeight) / 2;

                if (Main.mapFullscreen)
                    return screenCenter - Main.mapFullscreenPos * Main.mapFullscreenScale;

                Vector2 screenPos = (Main.screenPosition + screenCenter) / 16;

                if (Main.mapStyle == 1)
                {
                    Vector2 miniMapCenter = new Vector2(Main.miniMapX, Main.miniMapY) + new Vector2(Main.miniMapWidth, Main.miniMapHeight) / 2;
                    return miniMapCenter - screenPos * Main.mapMinimapScale;
                }

                Vector2 overlayMapCenter = Main.ScreenSize.ToVector2() / 2;
                return overlayMapCenter - screenPos * Main.mapOverlayScale;
            }
        }

        /// <summary>
        /// Map rectangle in screen coordinates
        /// </summary>
        public static Rect MapScreenRect => new(MapScreenPos, new Vector2(Main.maxTilesX, Main.maxTilesY) * MapScale);

        public static float MapAlpha =>
            Main.mapFullscreen ? 1f :
            Main.mapStyle == 1 ? Main.mapMinimapAlpha :
            Main.mapOverlayAlpha;

        public static bool IsFullscreenMap => Main.mapFullscreen;
        public static bool IsMiniMap => !Main.mapFullscreen && Main.mapStyle == 1;
        public static bool IsOverlayMap => !Main.mapFullscreen && Main.mapStyle == 2;

        public static Rect MapToScreen(Rect rect)
        {
            rect.Location = MapToScreen(rect.Location);
            rect.Size *= MapScale;
            return rect;
        }

        public static Vector2 MapToScreen(Vector2 mapTilePos)
        {
            mapTilePos *= MapScale;
            mapTilePos += MapScreenPos;
        
            return mapTilePos;
        }

        public static Vector2 ScreenToMap(Vector2 screenPos)
        {
            screenPos -= MapScreenPos;
            screenPos /= MapScale;
            return screenPos;
        }

        //public static bool IsVisibleWithoutClipping(Rectangle screenRect)
        //{
        //    Rectangle mapRect = MapScreenRect;
        //
        //    if (mapRect.X > screenRect.X || mapRect.Y > screenRect.Y)
        //        return false;
        //
        //    if (mapRect.Right <= screenRect.Right || mapRect.Bottom <= screenRect.Bottom)
        //        return false;
        //
        //    return true;
        //}
    }
}
