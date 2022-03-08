using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Localization;

namespace MapMarkers
{
    public class MapRenderer
    {
        internal bool MiddlePressed = false;
        internal bool RightPressed = false;

        private MapMarkers MapMarkers;

        internal AbstractMarker Captured;

        public MapRenderer(MapMarkers mod)
        {
            MapMarkers = mod;
        }

        internal void Update()
        {
            MiddlePressed = Main.mouseMiddle && Main.mouseMiddleRelease;
            RightPressed = Main.mouseRight && Main.mouseRightRelease;

            if (MapMarkers.CurrentMarkers != null)
                if (Captured != null)
                {
                    Point newPos = ScreenToMap(Main.MouseScreen).ToPoint();

                    if (Captured is MapMarker mm && Net.MapClient.AllowEdit(mm) && Main.mouseMiddle && Main.mapFullscreen)
                        Net.MapClient.SetPos(mm, newPos);
                    else if (Captured.CanDrag)
                        Captured.Position = newPos;
                    else Captured = null;
                }
        }

        internal void PostDrawMap(ref string mouseText)
        {
            if (MapMarkers.CurrentMarkers == null) return;

            Main.spriteBatch.End();

            bool hovered = false;

            foreach (AbstractMarker m in MapMarkers.CurrentMarkers.ToArray())
            {
                if (Main.mapFullscreenScale < m.MinZoom || !m.Active)
                    continue;

                Vector2 size = m.Size;
                Vector2 screenpos = MapToScreen(m.Position.ToVector2()) - size / 2;
                Rectangle screenRect = new Rectangle((int)screenpos.X, (int)screenpos.Y, (int)size.X, (int)size.Y);

                if (!hovered && screenRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    hovered = true;
                    string markerText = m.Name;

                    if (m.ShowPos)
                        markerText += "\n" + GetCenteredPosition(m.Position);

                    MarkerHover(m, ref markerText);

                    Main.spriteBatch.Begin();
                    Utils.DrawBorderString(Main.spriteBatch, markerText, screenpos + new Vector2(size.X + 10, 0), Color.White);
                    Main.spriteBatch.End();
                }

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                m.Draw(screenpos);
                Main.spriteBatch.End();
            }

            Main.spriteBatch.Begin();
            MapMarkers.MarkerGui.Draw();
        }

        private void MarkerHover(AbstractMarker m, ref string text)
        {
            m.Hover(ref text);

            if (m is MapMarker mm && Net.MapClient.AllowEdit(mm))
            {
                if (mm.IsServerSide)
                {
                    text += "\nOwner: " + mm.ServerData.Owner;
                }

                if (Net.MapClient.AllowEdit(mm))
                {
                    if (Main.keyState.PressingShift())
                        text += "\n[Del] Delete\n[Middle Mouse Button] Move\n[Right Mouse Button] Edit";
                    else
                        text += "\n[Shift] More";
                }

                if (Main.keyState.IsKeyDown(Keys.Delete) && Main.oldKeyState.IsKeyUp(Keys.Delete))
                {
                    if (mm.IsServerSide) Net.MapClient.SetGlobal(mm, false);
                    MapMarkers.CurrentMarkers.Remove(m);
                }
                else if (MiddlePressed)
                {
                    Captured = m;
                }
                else if (RightPressed)
                {
                    Main.mapFullscreen = false;
                    MapMarkers.MarkerGui.SetMarker(mm);
                }
            }

            else if (MiddlePressed && m.CanDrag)
            {
                Captured = m;
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
