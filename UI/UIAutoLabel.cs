using Terraria.UI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace MapMarkers.UI
{
    public class UIAutoLabel : UIElement
    {
        public string Text
        {
            get => text;
            set
            {
                text = value ?? "";
                RecalculateLines();
            }
        }

        public Vector2 TextAlign { get; set; }

        private CalculatedTextLine[]? TextLines;
        private static char[] WrapSplitters = new[] { ' ', '\t', '\n' };
        private string text = "";

        public void RecalculateLines()
        {
            //if (Text.Contains("Long"))
            //    Debugger.Break();

            CalculatedStyle dim = GetDimensions();

            if (dim.Width == 0)
            {
                TextLines = null;
                return;
            }

            List<TextSnippet> texts = ChatManager.ParseMessage(Text, Color.White);

            List<CalculatedTextLine> lines = new();
            List<TextSnippet> curLine = new();

            float lineWidth = 0;
            float lineHeight = 0;
            float totalHeight = 0;

            void NewLine()
            {
                lines.Add(new(curLine.ToArray(), new(lineWidth, lineHeight)));
                curLine.Clear();
                totalHeight += lineHeight;
                lineWidth = 0;
                lineHeight = 0;
            }

            void Append(TextSnippet snippet, Vector2 size)
            {
                curLine.Add(snippet);
                lineWidth += size.X;
                lineHeight = Math.Max(lineHeight, size.Y);
            }

            foreach (TextSnippet t in texts)
            {
                float remWidth = dim.Width - lineWidth;
                if (remWidth <= 0 && lineWidth > 0)
                {
                    NewLine();
                    remWidth = dim.Width - lineWidth;
                }

                if (t.UniqueDraw(true, out Vector2 size, Main.spriteBatch))
                {
                    if (size.X > remWidth && lineWidth > 0)
                        NewLine();

                    Append(t, size);
                    continue;
                }

                Vector2 textSize;
                if (!t.Text.Contains('\n'))
                {
                    textSize = FontAssets.MouseText.Value.MeasureString(t.Text) * t.Scale;
                    textSize.Y -= 4;
                    if (textSize.X < remWidth)
                    {
                        Append(t, textSize);
                        continue;
                    }
                }

                Vector2 prevTextSize = default;
                string prevText = "";
                string text = t.Text;

                while (text.Length > 0)
                {
                    int index = prevText.Length >= text.Length ? -1 : text.IndexOfAny(WrapSplitters, prevText.Length);
                    string testText = index < 0 ? text : text.Substring(0, index + 1);

                    textSize.X = FontAssets.MouseText.Value.MeasureString(testText).X * t.Scale;
                    textSize.Y = FontAssets.MouseText.Value.LineSpacing * t.Scale - 4;

                    if (textSize.X > remWidth && prevText.Length > 0 || index < 0)
                    {
                        if (index < 0)
                        {
                            t.Text = text;
                            Append(t, textSize);
                            break;
                        }

                        Append(CopySnippet(t, prevText), prevTextSize);
                        NewLine();

                        text = text[prevText.Length..];
                        remWidth = dim.Width - lineWidth;
                        prevText = "";

                        if (index < 0)
                            break;

                        continue;
                    }
                    if (testText.Length > 0 && testText[^1] == '\n')
                    {
                        Append(CopySnippet(t, testText), textSize);
                        NewLine();

                        text = text[testText.Length..];
                        remWidth = dim.Width - lineWidth;
                        prevText = "";
                        continue;
                    }

                    prevText = testText;
                    prevTextSize = textSize;
                }
            }

            if (curLine.Count > 0)
                NewLine();

            TextLines = lines.ToArray();
            MinHeight = new(totalHeight, 0);
        }

        public override void Recalculate()
        {
            base.Recalculate();
            RecalculateLines();
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (TextLines is null)
                return;

            CalculatedStyle dim = GetInnerDimensions();
            

            float height = TextLines.Sum(l => l.Size.Y);

            float y = dim.Y + TextAlign.Y * (dim.Height - height);

            foreach (CalculatedTextLine textLine in TextLines)
            {
                float x = dim.X + TextAlign.X * (dim.Width - textLine.Size.X);

                //spriteBatch.DrawRectangle(new(x, y, textLine.Size.X, textLine.Size.Y), new(y / dim.Height, 1 - y / dim.Height, 0));

                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    textLine.Texts,
                    new(x, y),
                    0f,
                    Vector2.Zero,
                    Vector2.One,
                    out _
                    );
                y += textLine.Size.Y;
            }
        }

        static TextSnippet CopySnippet(TextSnippet snippet, string newText)
        {
            return snippet.GetType() == typeof(PlainTagHandler.PlainSnippet) ?
                new PlainTagHandler.PlainSnippet(newText, snippet.Color, snippet.Scale) :
                new TextSnippet(newText, snippet.Color, snippet.Scale);
        }

        record struct CalculatedTextLine(TextSnippet[] Texts, Vector2 Size);
    }

    
}
