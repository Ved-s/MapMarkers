using MapMarkers.Structures;
using MapMarkers.UI;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MapMarkers
{
    internal class MarkerRenderer : INeedRenderTargetContent, ILoadable
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public bool IsReady { get; set; }

        private StringBuilder MouseTextBuilder = new();
        private MapMarker? PrevHoveredMarker = null;
        private MapMarker? HoveredMarker = null;
        private RasterizerState Scissors = new RasterizerState { ScissorTestEnable = true };

        private List<MapMarker> VisibleMarkers = new();
        private RenderTarget2D? HoverMarkerRenderTarget;
        private Point RenderTargetSize;
        private Guid RenderTargetMarkerId;
        const float MarkerHoverScale = 1.1f;

        private Effect Highlighter = null!;
        private MapMarker? GrabbedMarker;

        private bool MarkerHoverBlocked = false;

        internal void UpdateMarkers()
        {
            PrevHoveredMarker = HoveredMarker;
            HoveredMarker = null;
            VisibleMarkers.Clear();

            Rect mapRect = Helper.MapVisibleScreenRect;
            if (GrabbedMarker is not null && (!Helper.IsFullscreenMap || Keybinds.MouseMiddleKey == KeybindState.Released || !GrabbedMarker.CanMove(Main.myPlayer)))
                GrabbedMarker = null;

            foreach (MapMarker marker in MapMarkers.Markers.Values)
            {
                if (!ShouldDrawMarker(marker))
                {
                    marker.Hovered = false;
                    continue;
                }

                Vector2 markerCenter = Helper.MapToScreen(marker.Position);
                Rect screenRect = new(default, marker.Size);
                screenRect.Center = markerCenter;
                marker.ScreenRect = screenRect;
                if (marker.PlayerData.Pinned)
                {
                    Rect markerScreenBoundary = new(0, 0, Main.screenWidth, Main.screenHeight);
                    markerScreenBoundary.Location += screenRect.Size / 2;
                    markerScreenBoundary.Size -= screenRect.Size;

                    Vector2 center = marker.ScreenRect.Center;

                    if (!markerScreenBoundary.Contains(center))
                    {
                        center = center.ClampToRect(markerScreenBoundary);
                        screenRect.Center = center;
                        marker.ScreenRect = screenRect;
                    }

                    if (!mapRect.Contains(center))
                    {
                        center = center.ClampToRect(mapRect);
                        screenRect.Center = center;
                        marker.ScreenRect = screenRect;
                    }
                }
                else if (!screenRect.Intersects(mapRect))
                {
                    marker.Hovered = false;
                    continue;
                }

                marker.Hovered = GrabbedMarker is null ? screenRect.Contains(Main.MouseScreen) : marker.Id == GrabbedMarker.Id;

                if (!MarkerMenu.Hovering && marker.Hovered && (HoveredMarker is null || HoveredMarker.DrawTopMost == marker.DrawTopMost || !HoveredMarker.DrawTopMost && marker.DrawTopMost))
                {
                    if (HoveredMarker is not null)
                        HoveredMarker.Hovered = false;

                    HoveredMarker = marker;
                }

                bool visible = marker.PlayerData.Pinned || !marker.ClipToMap || marker.ScreenRect.Intersects(mapRect);
                if (visible)
                    VisibleMarkers.Add(marker);
            }

            if (HoveredMarker is not null && PrevHoveredMarker is null && Keybinds.MouseRightKey == KeybindState.Pressed)
                MarkerHoverBlocked = true;

            else if (HoveredMarker is null)
                MarkerHoverBlocked = false;

            if (HoveredMarker is not null && !MarkerHoverBlocked)
            {
                Rect screenRect = HoveredMarker.ScreenRect;
                Vector2 newSize = screenRect.Size * MarkerHoverScale;

                screenRect.Location -= (newSize - screenRect.Size) / 2;
                screenRect.Size = newSize;
                HoveredMarker.ScreenRect = screenRect;
            }
        }

        internal void DrawMarkers(ref string mouseText, bool onTop)
        {
            if (!onTop)
                UpdateMarkers();

            Main.spriteBatch.End();
            Rectangle scissors = Main.graphics.GraphicsDevice.ScissorRectangle;
            Main.graphics.GraphicsDevice.ScissorRectangle = Helper.MapVisibleScreenRect;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Scissors, null, Main.UIScaleMatrix);

            foreach (MapMarker marker in VisibleMarkers)
                if (marker.ClipToMap && !marker.PlayerData.Pinned && marker.DrawTopMost == onTop)
                    DrawMarker(marker);

            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.ScissorRectangle = scissors;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            foreach (MapMarker marker in VisibleMarkers)
                if (!marker.ClipToMap && marker.DrawTopMost == onTop || marker.PlayerData.Pinned && onTop)
                    DrawMarker(marker);

            if (onTop && HoveredMarker is not null && !MarkerHoverBlocked)
                HoverMarker(HoveredMarker, ref mouseText);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            if (IsReady && onTop && HoveredMarker is not null && HoverMarkerRenderTarget is not null && RenderTargetMarkerId == HoveredMarker.Id)
            {
                Highlighter.Parameters["texPix"].SetValue(new Vector2(2) / HoverMarkerRenderTarget.Size());
                Highlighter.Parameters["outlineColor"].SetValue(HoveredMarker.OutlineColor.ToVector3());
                Highlighter.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(HoverMarkerRenderTarget, HoveredMarker.ScreenRect.Location - new Vector2(2),
                    new Rectangle(0, 0, RenderTargetSize.X, RenderTargetSize.Y),
                    Color.White);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            if (onTop)
                MarkerMenu.Draw();

            if (HoveredMarker is not null && !MarkerHoverBlocked || MarkerMenu.Hovering)
            {
                Main.mouseRight = false;
                Main.mouseRightRelease = false;
            }
        }

        private void DrawMarker(MapMarker marker)
        {
            if (IsReady && HoverMarkerRenderTarget is not null && RenderTargetMarkerId == marker.Id && !MarkerHoverBlocked)
                return;

            marker.Draw();
        }

        private void HoverMarker(MapMarker marker, ref string mouseText)
        {
            if (GrabbedMarker is not null && Helper.IsFullscreenMap)
            {
                if (marker.Id != GrabbedMarker.Id)
                    return;

                MapMarkers.MoveMarker(marker, Helper.ScreenToMap(Main.MouseScreen), true);
                return;
            }

            if (Helper.IsFullscreenMap && Keybinds.GetKey(Keys.Delete) == KeybindState.JustPressed)
            {
                MapMarkers.RemoveMarker(marker, true);
                return;
            }

            MouseTextBuilder.Clear();

            if (mouseText.Length > 0)
                MouseTextBuilder.Append("\n\n");

            MouseTextBuilder.AppendLine(marker.DisplayName);
            MouseTextBuilder.AppendLine(GetCenteredPosition(marker.Position));

            if (Main.keyState.PressingShift())
            {
                if (marker.PlayerData.Pinned && !marker.PlayerData.Enabled)
                    MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.PinnedDisabled"));
                else if (marker.PlayerData.Pinned)
                    MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.Pinned"));
                else if (!marker.PlayerData.Enabled)
                    MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.Disabled"));


                MouseTextBuilder.AppendFormat("[c/aaaaaa:{0} ({1}) [{2}][c/bbbbbb:]]\n", marker.Name, marker.Mod.Name, MapMarkers.MarkerGuids.GetShortGuid(marker.Id));

                if (Helper.IsFullscreenMap)
                {
                    MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.OpenMenu"));

                    if (marker.CanMove(Main.myPlayer))
                        MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.Move"));
                }
                else
                {
                    MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.NotFSMap"));
                }
            }
            else
            {
                MouseTextBuilder.AppendLine(MapMarkers.GetLangValue("Marker.ShiftInfo"));
            }

            marker.Hover(MouseTextBuilder);

            mouseText += MouseTextBuilder.ToString();

            if (Helper.IsFullscreenMap)
            {
                if (Keybinds.MouseRightKey == KeybindState.JustPressed)
                    MarkerMenu.Show(marker);
                else if (Keybinds.MouseMiddleKey == KeybindState.JustPressed && marker.CanMove(Main.myPlayer))
                    GrabbedMarker = marker;
            }
        }

        public void PrepareRenderTarget(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            IsReady = false;
            if (HoveredMarker is null || Main.gameMenu || MarkerHoverBlocked)
                return;

            Vector2 renderTargetMinSizeVec = HoveredMarker.ScreenRect.Size + new Vector2(4, 4);
            Point renderTargetMinSize = new((int)Math.Ceiling(renderTargetMinSizeVec.X), (int)Math.Ceiling(renderTargetMinSizeVec.Y));

            if (HoverMarkerRenderTarget is null
                    || HoverMarkerRenderTarget.Width < renderTargetMinSize.X
                    || HoverMarkerRenderTarget.Height < renderTargetMinSize.Y
                    )
            {
                HoverMarkerRenderTarget?.Dispose();
                HoverMarkerRenderTarget =
                    new(Main.graphics.GraphicsDevice, renderTargetMinSize.X, renderTargetMinSize.Y);
            }

            Main.graphics.GraphicsDevice.SetRenderTarget(HoverMarkerRenderTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            Rect screenRect = HoveredMarker.ScreenRect;
            Vector2 rectLocation = screenRect.Location - new Vector2(2);
            screenRect.Location = new(2);
            HoveredMarker.ScreenRect = screenRect;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null);
            HoveredMarker.Draw();
            Main.spriteBatch.End();

            Main.graphics.GraphicsDevice.SetRenderTarget(null);

            RenderTargetMarkerId = HoveredMarker.Id;
            RenderTargetSize = renderTargetMinSize;
            IsReady = true;
        }

        public void Load(Mod mod)
        {
            Main.ContentThatNeedsRenderTargets.Add(this);
            Highlighter = ModContent.Request<Effect>("MapMarkers/Effects/Highlighter", AssetRequestMode.ImmediateLoad).Value;
        }

        public void Unload()
        {
            Main.ContentThatNeedsRenderTargets.Remove(this);
        }

        private static string GetCenteredPosition(Vector2 pos)
        {
            int x = (int)(pos.X * 2f - Main.maxTilesX);
            int y = (int)(pos.Y * 2f - Main.maxTilesY);

            string xs =
                (x > 0) ? Language.GetTextValue("GameUI.CompassEast", x) :
                ((x >= 0) ? Language.GetTextValue("GameUI.CompassCenter") :
                Language.GetTextValue("GameUI.CompassWest", -x));

            int depth = (int)(pos.Y * 2) - (int)Main.worldSurface * 2;

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

        private static bool ShouldDrawMarker(MapMarker marker)
        {
            if (!marker.Active)
                return false;

            if (!marker.PlayerData.Enabled)
            {
                if (!Helper.IsFullscreenMap)
                    return false;

                if (Main.keyState.PressingShift())
                    return true;

                if (MarkerMenu.Marker is not null && MarkerMenu.Marker.Id == marker.Id)
                    return true;

                return false;
            }
            return true;
        }

    }
}