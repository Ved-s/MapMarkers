using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace MapMarkers
{
    static class MapRenderer
    {
        internal static bool MiddlePressed = false;
        internal static bool RightPressed = false;

        internal static void Update() 
        {
            MiddlePressed = Main.mouseMiddle && Main.mouseMiddleRelease;
            RightPressed = Main.mouseRight && Main.mouseRightRelease;

            if (!MapMarkers.Markers.ContainsKey(Main.worldID))
                MapMarkers.Markers.Add(Main.worldID, new List<MapMarker>());

            foreach (MapMarker m in MapMarkers.Markers[Main.worldID]) 
            {
                if (m.Captured) 
                {
                    if (Main.mouseMiddle && Main.mapFullscreen) m.Position = ScreenToMap(Main.MouseScreen).ToPoint();
                    else m.Captured = false;
                }
            }
        }

        internal static void PostDrawFullscreenMap(ref string mouseText)
        {
            Main.spriteBatch.End();

            if (!MapMarkers.Markers.ContainsKey(Main.worldID))
                MapMarkers.Markers.Add(Main.worldID, new List<MapMarker>());

            foreach (MapMarker m in MapMarkers.Markers[Main.worldID].ToArray())
            {
                Texture2D tex = Main.itemTexture[m.Item.type];

                Vector2 size = tex.Size();
                Vector2 screenpos = MapToScreen(m.Position.ToVector2()) - size / 2;
                Rectangle screenRect = new Rectangle((int)screenpos.X, (int)screenpos.Y, tex.Width, tex.Height);

                if (screenRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    MarkerHover(m);
                    string markerText = m.Name + "\n" + GetCenteredPosition(m.Position);

                    if (Main.keyState.PressingShift())
                        markerText += "\n[Del] Delete\n[Middle Mouse Button] Move\n[Right Mouse Button] Edit";
                    else
                        markerText += "\n[Shift] More";
                    Main.spriteBatch.Begin();
                    Utils.DrawBorderString(Main.spriteBatch, markerText, screenpos + new Vector2(size.X + 10, 0), Color.White);
                    Main.spriteBatch.End();


                }

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                Main.spriteBatch.Draw(tex, screenpos, FixItemColor(m.Item.color));
                Main.spriteBatch.End();
            }

            Main.spriteBatch.Begin();
            MapMarkers.MarkerGui.Draw();
        }

        private static void MarkerHover(MapMarker m)
        {
            if (Main.keyState.IsKeyDown(Keys.Delete) && Main.oldKeyState.IsKeyUp(Keys.Delete))
            {
                MapMarkers.Markers[Main.worldID].Remove(m);
            }
            else if (MiddlePressed)
            {
                m.Captured = true;
            }
            else if (RightPressed)
            {
                Main.mapFullscreen = false;
                MapMarkers.MarkerGui.SetMarker(m);
            }
        }

        private static Color FixItemColor(Color c)
        {
            if (c.R + c.G + c.B == 0) return Color.White;
            return c;
        }

        public static string GetCenteredPosition(Point pos) 
        {
            int x = (int)(pos.X * 2f - Main.maxTilesX);
            int y = (int)(pos.Y * 2f - Main.maxTilesY);

            string xs = 
                (x > 0) ? Language.GetTextValue("GameUI.CompassEast", x) :
                ((x >= 0) ? Language.GetTextValue("GameUI.CompassCenter") :
                Language.GetTextValue("GameUI.CompassWest", -x));

            int depth = (pos.Y * 2) - (int)Main.worldSurface * 2;

            float wsq = Main.maxTilesX / 4200;
            wsq *= wsq;
            float space = (pos.Y - (65f + 10f * wsq)) / ((float)Main.worldSurface / 5f);

            string ys = (pos.Y > Main.maxTilesY - 204) ? Language.GetTextValue("GameUI.LayerUnderworld") :
                (pos.Y > Main.rockLayer + 601) ? Language.GetTextValue("GameUI.LayerCaverns") :
                ((depth > 0) ? Language.GetTextValue("GameUI.LayerUnderground") :
                ((space < 1f) ? Language.GetTextValue("GameUI.LayerSpace") :
                Language.GetTextValue("GameUI.LayerSurface")));
            depth = Math.Abs(depth);
            ys = ((depth != 0) ? Language.GetTextValue("GameUI.Depth", depth) : Language.GetTextValue("GameUI.DepthLevel")) + " " + ys;

            return xs + "\n" + ys;
        }

        public static Rectangle MapToScreen(Rectangle rect)
        {
            Vector2 tl = MapToScreen(rect.TopLeft());
            Vector2 br = MapToScreen(rect.BottomRight());

            Vector2 diff = br - tl;

            return new Rectangle((int)tl.X, (int)tl.Y, (int)diff.X, (int)diff.Y);
        }
        public static Vector2 MapToScreen(Vector2 vec)
        {
            Vector2 screen = new Vector2(Main.screenWidth, Main.screenHeight);

            vec -= Main.mapFullscreenPos;
            vec /= 16 / Main.mapFullscreenScale;
            vec *= 16;
            vec += screen / 2;

            return vec;
        }
        public static Vector2 ScreenToMap(Vector2 vec)
        {
            Vector2 screen = new Vector2(Main.screenWidth, Main.screenHeight);

            vec -= screen / 2;
            vec /= 16;
            vec *= 16 / Main.mapFullscreenScale;
            return Main.mapFullscreenPos + vec;
        }

    }
}
