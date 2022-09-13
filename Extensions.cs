using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Terraria;
using Terraria.GameContent;

namespace MapMarkers
{
    public static class Extensions
    {
        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Rect destinationRectangle, Color color)
        {
            spriteBatch.Draw(texture, destinationRectangle.Location, null, color, 0f, Vector2.Zero, destinationRectangle.Size / texture.Size(), SpriteEffects.None, 0);
        }

        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Rect destinationRectangle, Rectangle? sourceRectangle, Color color)
        {
            Vector2 size = sourceRectangle?.Size() ?? texture.Size();
            spriteBatch.Draw(texture, destinationRectangle.Location, sourceRectangle, color, 0f, Vector2.Zero, destinationRectangle.Size / size, SpriteEffects.None, 0);
        }

        public static void FillRectangle(this SpriteBatch spriteBatch, Rect rect, Color color)
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, color);
        }
        public static void FillRectangle(this SpriteBatch spriteBatch, float x, float y, float w, float h, Color color)
        {
            spriteBatch.FillRectangle(new(x, y, w, h), color);
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch, Rect rect, Color color, int thickness = 1)
        {
            spriteBatch.FillRectangle(rect.X + thickness, rect.Y, rect.Width - thickness, thickness, color);
            spriteBatch.FillRectangle(rect.X, rect.Y, thickness, rect.Height - thickness, color);
            spriteBatch.FillRectangle(rect.X, rect.Bottom - thickness, rect.Width - thickness, thickness, color);
            spriteBatch.FillRectangle(rect.Right - thickness, rect.Y + thickness, thickness, rect.Height - thickness, color);
        }

        public static string AppendLine(this string str, string line)
        {
            if (str is null || str == "")

                return line;

            return str + "\n" + line;
        }

        public static void RemoveWhere<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<KeyValuePair<TKey, TValue>, bool> selector)
        {
            List<TKey> remove = dict.Where(selector).Select(kvp => kvp.Key).ToList();
            foreach (TKey key in remove)
                dict.Remove(key);
        }
    }
}
