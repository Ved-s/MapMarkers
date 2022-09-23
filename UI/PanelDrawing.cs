using Terraria;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReLogic.Content;
using MapMarkers.Structures;
using Microsoft.Xna.Framework;

namespace MapMarkers.UI
{
    public static class PanelDrawing
    {
        static Asset<Texture2D> PanelBackground = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground", AssetRequestMode.AsyncLoad);
        static Asset<Texture2D> PanelBorder = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder", AssetRequestMode.AsyncLoad);

		private static int CornerSize = 12;
		private static int BarSize = 4;

		public static void Draw(SpriteBatch spriteBatch, Rect rect, Color color, bool border = false)
		{
			Asset<Texture2D> asset = border ? PanelBorder : PanelBackground;
			if (asset.State == AssetState.Loading)
				asset.Wait();
			Texture2D texture = asset.Value;

			Vector2 innerSize = rect.Size - new Vector2(CornerSize) * 2;
			Vector2 endPos = new(rect.Right - CornerSize, rect.Bottom - CornerSize);

			spriteBatch.Draw(texture, new Rect(rect.X, rect.Y, CornerSize, CornerSize), new Rectangle(0, 0, CornerSize, CornerSize), color);
			spriteBatch.Draw(texture, new Rect(endPos.X, rect.Y, CornerSize, CornerSize), new Rectangle(CornerSize + BarSize, 0, CornerSize, CornerSize), color);
			spriteBatch.Draw(texture, new Rect(rect.X, endPos.Y, CornerSize, CornerSize), new Rectangle(0, CornerSize + BarSize, CornerSize, CornerSize), color);
			spriteBatch.Draw(texture, new Rect(endPos.X, endPos.Y, CornerSize, CornerSize), new Rectangle(CornerSize + BarSize, CornerSize + BarSize, CornerSize, CornerSize), color);
			spriteBatch.Draw(texture, new Rect(rect.X + CornerSize, rect.Y, innerSize.X, CornerSize), new Rectangle(CornerSize, 0, BarSize, CornerSize), color);
			spriteBatch.Draw(texture, new Rect(rect.X + CornerSize, endPos.Y, innerSize.X, CornerSize), new Rectangle(CornerSize, CornerSize + BarSize, BarSize, CornerSize), color);
			spriteBatch.Draw(texture, new Rect(rect.X, rect.Y + CornerSize, CornerSize, innerSize.Y), new Rectangle(0, CornerSize, CornerSize, BarSize), color);
			spriteBatch.Draw(texture, new Rect(endPos.X, rect.Y + CornerSize, CornerSize, innerSize.Y), new Rectangle(CornerSize + BarSize, CornerSize, CornerSize, BarSize), color);
			spriteBatch.Draw(texture, new Rect(rect.X + CornerSize, rect.Y + CornerSize, innerSize.X, innerSize.Y), new Rectangle(CornerSize, CornerSize, BarSize, BarSize), color);
		}
	}
}
