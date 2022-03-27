using Microsoft.Xna.Framework;
using Terraria;

namespace MapMarkers
{
    public static class MapHelper
    {
        internal static Vector2? OverlayMapScreen = null;

        public static Vector2 ScreenCenterWorld => (Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2) / 16;

        public static float MapScale =>
                Main.mapFullscreen ? Main.mapFullscreenScale :
                Main.mapStyle == 1 ? Main.mapMinimapScale : Main.mapOverlayScale;

        public static Vector2 MapWorldPos =>
            Main.mapFullscreen ? 
                Main.mapFullscreenPos :
            Main.mapStyle == 1 ? 
                ScreenCenterWorld :
            OverlayMapScreen.HasValue ? 
                new Vector2(10) : 
            ScreenCenterWorld;

        public static Vector2 MapScreenPos =>
            (Main.mapFullscreen || !OverlayMapScreen.HasValue) ?
                new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) :
            Main.mapStyle == 1 ?
                new Vector2(Main.miniMapX + Main.miniMapWidth / 2, Main.miniMapY + Main.miniMapHeight / 2) :
            OverlayMapScreen.Value;

        public static Rectangle MapScreenClipRect =>
            IsMiniMap ? new Rectangle(Main.miniMapX, Main.miniMapY, Main.miniMapWidth, Main.miniMapHeight) :
            new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

        public static float MapAlpha =>
            Main.mapFullscreen ? 1f :
            Main.mapStyle == 1 ? Main.mapMinimapAlpha :
            Main.mapOverlayAlpha;

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
            vec -= MapWorldPos;
            vec /= 16 / MapScale;
            vec *= 16;
            return vec + MapScreenPos;
        }

        public static Vector2 ScreenToMap(Vector2 vec)
        {
            vec -= MapScreenPos;
            vec /= 16;
            vec *= 16 / MapScale;
            return MapWorldPos + vec;
        }

        public static bool IsVisibleWithoutClipping(Rectangle screenRect)
        {
            if (IsOverlayMap || Main.mapFullscreen)
                return true;

            if (Main.miniMapX > screenRect.X || Main.miniMapY > screenRect.Y)
                return false;

            if ((Main.miniMapX + Main.miniMapWidth) <= screenRect.Right || (Main.miniMapY + Main.miniMapHeight) <= screenRect.Bottom)
                return false;

            return true;
        }
    }
}
