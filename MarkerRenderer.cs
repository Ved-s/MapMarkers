using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace MapMarkers
{
    internal class MarkerRenderer : INeedRenderTargetContent, ILoadable
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public bool IsReady { get; set; }

        private StringBuilder MouseTextBuilder = new();
        private MapMarker? HoveredMarker = null;
        private RasterizerState Scissors = new RasterizerState { ScissorTestEnable = true };

        private List<MapMarker> VisibleMarkers = new();
        private RenderTarget2D? HoverMarkerRenderTarget;
        private Point RenderTargetSize;
        private Guid RenderTargetMarkerId;
        const float MarkerHoverScale = 1.1f;

        private Effect Highlighter = null!;

        internal void UpdateMarkers()
        {
            HoveredMarker = null;
            VisibleMarkers.Clear();

            Rect mapRect = Helper.MapVisibleScreenRect;

            foreach (MapMarker marker in MapMarkers.Markers.Values)
            {
                Vector2 markerCenter = Helper.MapToScreen(marker.Position);
                Rect screenRect = new(markerCenter, marker.Size);
                screenRect.Location -= screenRect.Size / 2;

                marker.ScreenRect = screenRect;
                marker.Hovered = screenRect.Contains(Main.MouseScreen);

                if (marker.Hovered && (HoveredMarker is null || HoveredMarker.DrawTopMost == marker.DrawTopMost || !HoveredMarker.DrawTopMost && marker.DrawTopMost))
                {
                    if (HoveredMarker is not null)
                        HoveredMarker.Hovered = false;

                    HoveredMarker = marker;
                }

                bool visible = !marker.ClipToMap || marker.ScreenRect.Intersects(mapRect);
                if (visible)
                    VisibleMarkers.Add(marker);
            }

            if (HoveredMarker is not null)
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
                if (marker.ClipToMap && marker.DrawTopMost == onTop)
                    DrawMarker(marker, Scissors);

            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.ScissorRectangle = scissors;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            foreach (MapMarker marker in VisibleMarkers)
                if (!marker.ClipToMap && marker.DrawTopMost == onTop)
                    DrawMarker(marker, RasterizerState.CullCounterClockwise);

            if (onTop && HoveredMarker is not null)
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
        }

        void DrawMarker(MapMarker marker, RasterizerState currentRasterizer)
        {
            if (IsReady && HoverMarkerRenderTarget is not null && RenderTargetMarkerId == marker.Id)
                return;

            marker.Draw();
        }

        void HoverMarker(MapMarker marker, ref string mouseText)
        {
            MouseTextBuilder.Clear();

            if (mouseText.Length > 0)
                MouseTextBuilder.Append("\n\n");

            MouseTextBuilder.AppendLine(marker.DisplayName);
            MouseTextBuilder.AppendFormat("{0} [{1}]", marker.GetType().FullName, MapMarkers.MarkerGuids.GetShortGuid(marker.Id));

            marker.Hover(MouseTextBuilder);

            mouseText += MouseTextBuilder.ToString();
        }

        public void PrepareRenderTarget(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            IsReady = false;
            if (HoveredMarker is null || Main.gameMenu)
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
    }
}
