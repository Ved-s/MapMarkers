using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
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
            // Top
            spriteBatch.FillRectangle(rect.X + thickness, rect.Y, rect.Width - thickness, thickness, color);

            // Left
            spriteBatch.FillRectangle(rect.X, rect.Y, thickness, rect.Height - thickness, color);

            if (rect.Height > thickness)
                // Bottom
                spriteBatch.FillRectangle(rect.X, rect.Bottom - thickness, Math.Max(thickness, rect.Width - thickness), thickness, color);

            if (rect.Width > thickness)
                // Right
                spriteBatch.FillRectangle(rect.Right - thickness, rect.Y + thickness, thickness, Math.Max(thickness, rect.Height - thickness), color);
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

        public static IEnumerable<string> SplitByWhitespace(this string str, bool includeChar)
        {
            StringBuilder builder = new();

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (char.IsWhiteSpace(c))
                {
                    if (includeChar)
                        builder.Append(c);

                    yield return builder.ToString();
                    builder.Clear();
                    continue;
                }

                builder.Append(c);
            }

            if (builder.Length > 0)
                yield return builder.ToString();
        }
    }
}
