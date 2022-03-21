using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
                    Point newPos = MapHelper.ScreenToMap(Main.MouseScreen).ToPoint();

                    if (Captured is MapMarker mm && Net.MapClient.AllowPerm(mm, MarkerPerms.Edit) && Main.mouseMiddle && Main.mapFullscreen)
                        Net.MapClient.SetPos(mm, newPos);
                    else if (Captured.CanDrag)
                        Captured.Position = newPos;
                    else Captured = null;
                }
        }

        internal void PostDrawMap(ref string mouseText)
        {
#if DEBUG
            Point tl = MapHelper.MapToScreen(new Vector2(10)).ToPoint();
            Point br = MapHelper.MapToScreen(new Vector2(Main.maxTilesX - 11, Main.maxTilesY - 11)).ToPoint();

            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(tl.X, tl.Y, br.X - tl.X, 1), Color.Yellow);
            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(tl.X, tl.Y, 1, br.Y - tl.Y), Color.Yellow);

            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(tl.X, br.Y, br.X - tl.X, 1), Color.Yellow);
            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(br.X, tl.Y, 1, br.Y - tl.Y), Color.Yellow);
#endif
            if (MapMarkers.CurrentMarkers == null) return;

            Main.spriteBatch.End();

            bool hovered = false;

            foreach (AbstractMarker m in MapMarkers.CurrentMarkers.ToArray())
            {
                if (MapHelper.MapScale < m.MinZoom || !m.Active)
                    continue;

                Vector2 size = m.Size;
                Vector2 screenpos = MapHelper.MapToScreen(m.Position.ToVector2()) - size / 2;
                Rectangle screenRect = new Rectangle((int)screenpos.X, (int)screenpos.Y, (int)size.X, (int)size.Y);

                if (!MapHelper.IsVisibleWithoutClipping(screenRect))
                    continue;

                if (!hovered && screenRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    hovered = true;
                    string markerText = m.Name;

                    if (m.ShowPos)
                        markerText += "\n" + GetCenteredPosition(m.Position);
                    if (MapHelper.IsFullscreenMap)
                        MarkerHover(m, ref markerText);

                    if (!MapHelper.IsMiniMap)
                    {
                        Main.spriteBatch.Begin();
                        Utils.DrawBorderString(Main.spriteBatch, markerText, screenpos + new Vector2(size.X + 10, 0), Color.White);
                        Main.spriteBatch.End();
                    }
                    else mouseText = markerText;
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

            if (m is MapMarker mm)
            {
                bool edit = Net.MapClient.AllowPerm(mm, MarkerPerms.Edit);
                bool delete = Net.MapClient.AllowPerm(mm, MarkerPerms.Delete);

                if (mm.IsServerSide)
                {
                    text += "\nOwner: " + mm.ServerData.Owner;
                }

                if (edit || delete)
                {
                    if (Main.keyState.PressingShift())
                    {
                        if (delete)
                            text += "\n[Del] Delete";

                        if (edit)
                            text += "\n[Middle Mouse Button] Move\n[Right Mouse Button] Edit";
                    }
                    else
                        text += "\n[Shift] More";
                }

                if (delete && Main.keyState.IsKeyDown(Keys.Delete) && Main.oldKeyState.IsKeyUp(Keys.Delete))
                {
                    Net.MapClient.Delete(mm);
                    //if (mm.IsServerSide)
                    //{
                    //    if (mm.ServerData.Owner == Main.LocalPlayer.name)
                    //        Net.MapClient.SetGlobal(mm, false);
                    //    else 
                    //}
                    //MapMarkers.CurrentMarkers.Remove(m);
                }

                if (edit)
                {
                    if (MiddlePressed)
                    {
                        Captured = m;
                    }

                    else if (RightPressed)
                    {
                        Main.mapFullscreen = false;
                        MapMarkers.MarkerGui.SetMarker(mm);
                    }
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
    }
}
