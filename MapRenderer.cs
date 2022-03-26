using MapMarkers.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Text;
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

        internal bool StopDrawingAfterHover = false;

        internal static bool PressingCtrl => Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl);

        private const string CannotTeleport = "[[c/00ff00:Map Markers]] [c/ff0000:Not enough space to teleport]";

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

                    if (Captured is MapMarker mm && Net.MapClient.AllowPerm(mm, MarkerPerms.Edit) && Main.mouseMiddle && Main.mapFullscreen)
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
            StringBuilder markerText = new();

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
                    markerText.Append(m.Name);

                    if (m.ShowPos)
                    {
                        markerText.AppendLine();
                        markerText.Append(GetCenteredPosition(m.Position));
                    }

                    if (MapHelper.IsFullscreenMap)
                        MarkerHover(m, markerText);

                    textPos = screenpos + new Vector2(size.X + 10, 0);
                }
            }

            if (hovered)
            {
                if (MapHelper.IsFullscreenMap)
                {
                    Utils.DrawBorderString(Main.spriteBatch, markerText.ToString(), textPos, Color.White);
                }
                else text = markerText.ToString();
            }
        }

        private void MarkerHover(AbstractMarker m, StringBuilder mouseText)
        {
            m.Hover(mouseText);

            bool shift = Main.keyState.PressingShift();
            bool ctrl = PressingCtrl;

            bool addShiftForMore = false;

            if (m is MapMarker mm)
            {
                bool edit = Net.MapClient.AllowPerm(mm, MarkerPerms.Edit);
                bool delete = Net.MapClient.AllowPerm(mm, MarkerPerms.Delete);

                if (mm.IsServerSide)
                {
                    mouseText.AppendLine();
                    mouseText.Append("Owner: ");
                    mouseText.Append(mm.ServerData.Owner);
                }

                if (edit || delete)
                {
                    addShiftForMore = true;
                    if (shift)
                    {
                        if (delete)
                        {
                            mouseText.AppendLine();
                            mouseText.Append("[Del] Delete");
                        }

                        if (edit)
                        {
                            mouseText.AppendLine();
                            mouseText.Append("[Middle Click] Move");
                            mouseText.AppendLine();
                            mouseText.Append("[Right Click] Edit");
                        }
                    }
                }

                if (delete && Main.keyState.IsKeyDown(Keys.Delete) && Main.oldKeyState.IsKeyUp(Keys.Delete))
                {
                    Net.MapClient.Delete(mm);
                }

                if (edit)
                {
                    if (MiddlePressed)
                    {
                        Captured = m;
                    }
                    else if (RightPressed && !ctrl)
                    {
                        Main.mapFullscreen = false;
                        StopDrawingAfterHover = true;
                        MapSystem.MarkerGui.SetMarker(mm);
                    }
                }
            }

            else if (MiddlePressed && m.CanDrag)
            {
                Captured = m;
            }

            if (m.CanTeleport && MapPlayer.LocalPlayerHasTPPotion && !MapPlayer.LocalPlayerHasTPDebuff)
            {
                addShiftForMore = true;

                if (shift)
                {
                    mouseText.AppendLine();
                    mouseText.Append("[Ctrl+Right Click] Teleport");
                }

                if (RightPressed && ctrl)
                {
                    Vector2? pos = TryGetTeleportPos(m);

                    if (pos.HasValue)
                    {
                        MarkerTPPotion.UsedOnMarker(m, pos.Value);
                    }
                    else
                    {
                        Main.NewText(CannotTeleport);
                    }
                    Main.mapFullscreen = false;
                    StopDrawingAfterHover = true;
                }
            }

            if (!shift && addShiftForMore)
            {
                mouseText.AppendLine();
                mouseText.Append("[Shift] More");
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

        public static Vector2? TryGetTeleportPos(AbstractMarker m)
        {
            Player local = Main.LocalPlayer;

            Vector2 tpTarget = (m.Size / 2) + (m.Position.ToVector2() * 16) - (local.Size / 2);

            Vector2 tpRadius = local.Size + m.Size;

            Vector2 start = tpTarget - tpRadius;
            Vector2 area = tpRadius * 2 - local.Size;

            Vector2 closestPos = new Vector2();
            float closestDistSQ = float.MaxValue;
            bool anyPos = false;

            for (float y = start.Y; y < start.Y + area.Y; y += 8)
                for (float x = start.X; x < start.X + area.X; x += 8)
                {
                    Vector2 pos = new Vector2(x, y);
                    if (!Collision.SolidCollision(pos, local.width, local.height))
                    {
                        Vector2 diff = tpTarget - pos;

                        float distSQ = diff.Y * diff.Y + diff.X * diff.X;
                        if (distSQ < closestDistSQ)
                        {
                            anyPos = true;
                            closestDistSQ = distSQ;
                            closestPos = pos;
                        }
                    }
                }

            if (anyPos)
                return closestPos;
            return null;

        }
    }
}
