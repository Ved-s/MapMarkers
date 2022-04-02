using MapMarkers.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;

namespace MapMarkers
{
    public class MapRenderer
    {
        public static bool UseMouseTextOnMinimap = true;

        internal bool MiddlePressed = false;
        internal bool RightPressed = false;

        internal bool StopDrawingAfterHover = false;

        internal static bool PressingCtrl => Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl);

        private string CannotTeleport = "[[c/00ff00:Map Markers]] [c/ff0000:" + Language.GetTextValue("Mods.MapMarkers.Chat.NotEnoughTPSpace") + "]";

        private MapMarkers MapMarkers;

        internal AbstractMarker Captured;

        internal string HoverMarkerText;
        internal Vector2 HoverMarkerTextPos = new Vector2();

        public MapRenderer(MapMarkers mod)
        {
            MapMarkers = mod;
        }

        internal void Update()
        {
            MiddlePressed = PlayerInput.MouseInfo.MiddleButton == ButtonState.Pressed && PlayerInput.MouseInfoOld.MiddleButton == ButtonState.Released;
            RightPressed = PlayerInput.MouseInfo.RightButton == ButtonState.Pressed && PlayerInput.MouseInfoOld.RightButton == ButtonState.Released;

            if (MapMarkers.CurrentPlayerWorldData != null)
                if (Captured != null)
                {
                    Point newPos = MapHelper.ScreenToMap(Main.MouseScreen).ToPoint();

                    if (Captured is MapMarker mm && mm.AllowPerm(MarkerPerms.Edit) && Main.mouseMiddle && Main.mapFullscreen)
                        Net.MapClient.SetPos(mm, newPos);
                    else if (Captured.CanDrag)
                        Captured.Position = newPos;
                    else Captured = null;
                }
        }

        internal void PostDrawMap(ref string mouseText)
        {
            HoverMarkerText = null;

            if (MapMarkers.CurrentPlayerWorldData == null) return;

            StopDrawingAfterHover = false;
            bool hovered = false;

            StringBuilder markerText = new StringBuilder();
            Vector2? markerTextPos = null;

            foreach (AbstractMarker m in MapMarkers.CurrentPlayerWorldData.Markers.ToArray())
            {
                if (MapHelper.MapScale < m.MinZoom || !m.Active)
                    continue;

                Vector2 size = m.Size;
                Vector2 screenpos = MapHelper.MapToScreen(m.Position.ToVector2()) - size / 2;
                Rectangle screenRect = new Rectangle((int)screenpos.X, (int)screenpos.Y, (int)size.X, (int)size.Y);

                bool pinned = MapMarkers.CurrentPlayerWorldData.Pinned.Contains(m.Id);

                if (pinned)
                {
                    screenRect.MoveInside(MapHelper.MapScreenClipRect);

                    Rectangle mapPosClipRect = MapHelper.MapScreenClipRect;
                    mapPosClipRect.Width -= (int)size.X;
                    mapPosClipRect.Height -= (int)size.Y;
                    screenpos.MoveInside(mapPosClipRect);
                }

                else if (!MapHelper.IsVisibleWithoutClipping(screenRect))
                    continue;

                if (!hovered && screenRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    hovered = true;
                    markerText.Append(m.Name);

#if DEBUG
                    markerText.Append(" [");
                    markerText.Append(MapMarkers.CurrentPlayerWorldData.ShortGuids.GetShortGuid(m.Id));
                    markerText.Append(']');
#endif
                    if (pinned)
                    {
                        markerText.AppendLine();
                        markerText.Append(Language.GetTextValue("Mods.MapMarkers.Marker.Pinned"));
                    }

                    if (m.ShowPos)
                    {
                        markerText.AppendLine();
                        markerText.Append(GetCenteredPosition(m.Position));
                    }

                    if (MapHelper.IsFullscreenMap)
                        MarkerHover(m, markerText);

                    if (StopDrawingAfterHover)
                        break;
                    markerTextPos = screenpos + new Vector2(size.X + 10, 0);
#if DEBUG
                    if (m.CanTeleport)
                    {
                        DrawTPBoundary(m);
                    }
#endif
                }
                m.Draw(screenpos);
            }

            if (markerText.Length > 0)
            {
                if (MapHelper.IsMiniMap && UseMouseTextOnMinimap)
                    mouseText = markerText.ToString();
                else
                {
                    HoverMarkerText = markerText.ToString();
                    HoverMarkerTextPos = markerTextPos.Value;
                }
            }

            MapMarkers.MarkerGui.Draw();
        }
        internal void PostDrawFullMap(ref string mouseText)
        {
#if DEBUG
            Point tl = MapHelper.MapToScreen(new Vector2(10)).ToPoint();
            Point br = MapHelper.MapToScreen(new Vector2(Main.maxTilesX - 11, Main.maxTilesY - 11)).ToPoint();

            DrawRect(new Rectangle(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), Color.Yellow);
            DrawRect(MapHelper.MapScreenClipRect, Color.Red);
#endif

            if (HoverMarkerText != null)
            {
                float posScale = 1f;
                if (MapHelper.IsFullscreenMap)
                    posScale = Main.UIScale;

                Utils.DrawBorderString(Main.spriteBatch, HoverMarkerText, HoverMarkerTextPos / posScale, Color.White);
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
                bool edit = mm.AllowPerm(MarkerPerms.Edit);
                bool delete = mm.AllowPerm(MarkerPerms.Delete);

                if (mm.IsServerSide)
                {
                    mouseText.AppendLine();
                    mouseText.Append(Language.GetTextValue("Mods.MapMarkers.Marker.Owner"));
                    mouseText.Append(" ");
                    mouseText.Append(mm.ServerData.Owner);
                }

                if (delete)
                {
                    addShiftForMore = true;
                    if (shift)
                    {
                        mouseText.AppendLine();
                        mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.Delete"));
                    }
                }

                if (edit)
                {
                    addShiftForMore = true;
                    if (shift)
                    {
                        mouseText.AppendLine();
                        mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.Move"));
                        mouseText.AppendLine();
                        mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.Edit"));
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

                    else if (RightPressed && !ctrl && !shift)
                    {
                        Main.mapFullscreen = false;
                        StopDrawingAfterHover = true;
                        MapMarkers.MarkerGui.SetMarker(mm);
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
                    mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.Teleport"));
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

            if (m.CanPin)
            {
                addShiftForMore = true;
                if (shift)
                {
                    bool pin = MapMarkers.CurrentPlayerWorldData.Pinned.Contains(m.Id);

                    mouseText.AppendLine();

                    if (pin) mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.Unpin"));
                    else mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.Pin"));

                    if (RightPressed)
                    {
                        if (pin)
                            MapMarkers.CurrentPlayerWorldData.Pinned.Remove(m.Id);
                        else
                            MapMarkers.CurrentPlayerWorldData.Pinned.Add(m.Id);
                    }
                }

            }

            if (!shift && addShiftForMore)
            {
                mouseText.AppendLine();
                mouseText.Append(Language.GetTextValue("Mods.MapMarkers.MarkerKeys.More"));
            }
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

        private static void DrawTPBoundary(AbstractMarker m)
        {
            Vector2 tpTarget = (m.Size / 2) + (m.Position.ToVector2() * 16) - (Main.LocalPlayer.Size / 2);

            Vector2 tpRadius = Main.LocalPlayer.Size + m.Size;

            Vector2 start = (tpTarget - tpRadius) / 16;
            Vector2 end = MapHelper.MapToScreen(tpRadius / 8 + start);
            start = MapHelper.MapToScreen(start);

            DrawRect(
                new Rectangle((int)start.X, (int)start.Y, (int)(end.X - start.X), (int)(end.Y - start.Y)),
                Color.Yellow * 0.3f
                );
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

        public static void DrawRect(Rectangle rect, Color color, int thick = 1)
        {
            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(rect.Left, rect.Top, rect.Width, thick), color);
            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(rect.Left, rect.Top, thick, rect.Height), color);

            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(rect.Left, rect.Bottom - thick, rect.Width, thick), color);
            Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(rect.Right - thick, rect.Top, thick, rect.Height), color);
        }
    }
}
