using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace MapMarkers
{
    public class MapRenderer : ModMapLayer
    {
        internal bool MiddlePressed = false;
        internal bool RightPressed = false;

        private MapSystem MapSystem => ModContent.GetInstance<MapSystem>();

        internal AbstractMarker Captured;

        internal void Update()
        {
            MiddlePressed = Main.mouseMiddle && Main.mouseMiddleRelease;
            RightPressed = Main.mouseRight && Main.mouseRightRelease;

            if (MapSystem.CurrentMarkers != null)
                if (Captured is not null)
                {
                    Point newPos = MapHelper.ScreenToMap(Main.MouseScreen).ToPoint();

                    if (Captured is MapMarker mm && Net.MapClient.AllowEdit(mm) && Main.mouseMiddle && Main.mapFullscreen)
                        Net.MapClient.SetPos(mm, newPos);
                    else if (Captured.CanDrag)
                        Captured.Position = newPos;
                    else Captured = null;
                }
        }

        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (MapSystem.CurrentMarkers is null) return;

            bool hovered = false;

            Vector2 textPos = default;
            string markerText = "";

            foreach (AbstractMarker m in MapSystem.CurrentMarkers.ToArray())
            {
                if (MapHelper.MapScale < m.MinZoom || !m.Active)
                    continue;

                Vector2 size = m.Size;
                Vector2 screenpos = MapHelper.MapToScreen(m.Position.ToVector2()) - size / 2;
                Rectangle screenRect = new Rectangle((int)screenpos.X, (int)screenpos.Y, (int)size.X, (int)size.Y);

                if (!MapHelper.IsVisibleWithoutClipping(screenRect))
                    continue;

                m.Draw(screenpos);

                if (!hovered && screenRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    hovered = true;
                    markerText = m.Name;

                    if (m.ShowPos)
                        markerText += "\n" + GetCenteredPosition(m.Position);

                    if (MapHelper.IsFullscreenMap)
                        MarkerHover(m, ref markerText);

                    textPos = screenpos + new Vector2(size.X + 10, 0);
                }
            }

            if (hovered)
            {
                if (MapHelper.IsFullscreenMap)
                {
                    Utils.DrawBorderString(Main.spriteBatch, markerText, textPos, Color.White);
                }
                else text = markerText;
            }
        }

        private void MarkerHover(AbstractMarker m, ref string markerText)
        {
            if (m is MapMarker mm && Net.MapClient.AllowEdit(mm))
            {
                if (mm.IsServerSide)
                {
                    markerText += "\nOwner: " + mm.ServerData.Owner;
                }

                if (Net.MapClient.AllowEdit(mm))
                {
                    if (Main.keyState.PressingShift())
                        markerText += "\n[Del] Delete\n[Middle Mouse Button] Move\n[Right Mouse Button] Edit";
                    else
                        markerText += "\n[Shift] More";
                }

                if (Main.keyState.IsKeyDown(Keys.Delete) && Main.oldKeyState.IsKeyUp(Keys.Delete))
                {
                    if (mm.IsServerSide) Net.MapClient.SetGlobal(mm, false);
                    MapSystem.CurrentMarkers.Remove(m);
                }
                else if (MiddlePressed)
                {
                    Captured = m;
                }
                else if (RightPressed)
                {
                    Main.mapFullscreen = false;
                    MapSystem.MarkerGui.SetMarker(mm);
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
