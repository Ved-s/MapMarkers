using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace MapMarkers
{
    public static class MapHelper
    {
        public static float MapScale =>
                Main.mapFullscreen ? Main.mapFullscreenScale :
                Main.mapStyle == 1 ? Main.mapMinimapScale : Main.mapOverlayScale;

        public static Vector2 MapWorldPos => 
            Main.mapFullscreen ? Main.mapFullscreenPos : Main.LocalPlayer.position / 16;

        public static Rectangle MapScreenRect => 
            (!Main.mapFullscreen && Main.mapStyle == 1) ? 
            new(Main.miniMapX, Main.miniMapY, Main.miniMapWidth, Main.miniMapHeight) :
                new(0, 0, Main.screenWidth, Main.screenHeight);

        public static float MapAlpha => IsOverlayMap ? Main.mapOverlayAlpha : 1f;

        public static bool IsFullscreenMap => Main.mapFullscreen;
        public static bool IsMiniMap => !Main.mapFullscreen && Main.mapStyle == 1;
        public static bool IsOverlayMap => !Main.mapFullscreen && Main.mapStyle == 2;


        public static Rectangle MapToScreen(Rectangle rect)
        {
            Vector2 tl = MapToScreen(rect.TopLeft());
            Vector2 br = MapToScreen(rect.BottomRight());

            Vector2 diff = br - tl;

            return new Rectangle((int)tl.X, (int)tl.Y, (int)diff.X, (int)diff.Y);
        }
        public static Vector2 MapToScreen(Vector2 vec)
        {
            Rectangle rect = MapScreenRect;

            vec -= MapWorldPos;
            vec /= 16 / MapScale;
            vec *= 16;

            vec += rect.Location.ToVector2() + rect.Size() / 2;

            return vec;
        }
        public static Vector2 ScreenToMap(Vector2 vec)
        {
            Rectangle rect = MapScreenRect;

            vec -= rect.Location.ToVector2() + rect.Size() / 2;
            vec /= 16;
            vec *= 16 / MapScale;
            return MapWorldPos + vec;
        }

        public static bool IsVisibleWithoutClipping(Rectangle screenRect) 
        {
            Rectangle mapRect = MapScreenRect;

            if (mapRect.X > screenRect.X || mapRect.Y > screenRect.Y)
                return false;

            if (mapRect.Right <= screenRect.Right || mapRect.Bottom <= screenRect.Bottom)
                return false;

            return true;
        }
    }
}
