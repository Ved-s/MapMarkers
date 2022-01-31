using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.GameContent;
using ReLogic.Content;
using Terraria.Initializers;

namespace MapMarkers
{
    public class MapRenderer : ModMapLayer
    {
        internal bool MiddlePressed = false;
        internal bool RightPressed = false;

        private MapSystem MapSystem => ModContent.GetInstance<MapSystem>();

        internal MapMarker Captured;

        internal void Update()
        {
            MiddlePressed = Main.mouseMiddle && Main.mouseMiddleRelease;
            RightPressed = Main.mouseRight && Main.mouseRightRelease;

            if (MapSystem.CurrentMarkers != null)
                
                    if (Captured is not null)
                    {
                        if (Main.mouseMiddle && Main.mapFullscreen) Captured.Position = ScreenToMap(Main.MouseScreen).ToPoint();
                        else Captured = null;
                    }
                
        }

        public override void Draw(ref MapOverlayDrawContext context, ref string text)
        {
            if (MapSystem.CurrentMarkers is null) return;

            foreach (MapMarker m in MapSystem.CurrentMarkers.ToArray())
            {
                Asset<Texture2D> asset = TextureAssets.Item[m.Item.type];

                if (!asset.IsLoaded) asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.ImmediateLoad);

                Texture2D tex = asset.Value;
                
                Vector2 size = tex.Size();
                Vector2 screenpos = MapToScreen(m.Position.ToVector2()) - size / 2;

                MapOverlayDrawContext.DrawResult result = context.Draw(tex, m.Position.ToVector2(), Terraria.UI.Alignment.Center);

                if (result.IsMouseOver)
                {
                    MarkerHover(m);
                    string markerText = m.Name + "\n" + GetCenteredPosition(m.Position);
            
                    if (Main.keyState.PressingShift())
                        markerText += "\n[Del] Delete\n[Middle Mouse Button] Move\n[Right Mouse Button] Edit";
                    else
                        markerText += "\n[Shift] More";

                    if (Main.mapFullscreen)
                        Utils.DrawBorderString(Main.spriteBatch, markerText, screenpos + new Vector2(size.X + 10, 0), Color.White);
                    else text = markerText;
                }
            }
        }

        private void MarkerHover(MapMarker m)
        {
            if (Main.keyState.IsKeyDown(Keys.Delete) && Main.oldKeyState.IsKeyUp(Keys.Delete))
            {
                MapSystem.CurrentMarkers.Remove(m);
            }
            else if (MiddlePressed)
            {
                Captured = m;
            }
            else if (RightPressed)
            {
                Main.mapFullscreen = false;
                MapSystem.MarkerGui.SetMarker(m);
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
