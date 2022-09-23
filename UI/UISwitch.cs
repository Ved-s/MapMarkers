using Terraria;
using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace MapMarkers.UI
{
    public class UISwitch : UIElement
    {
        public Color DotColor = Color.Lime;
        public Color BackColor = new Color(63, 82, 151) * 0.7f;

        public Vector2 DotPaddings = new(4);

        public string? RadioGroup = null;
        public bool State = false;

        private string text = "";
        private TextSnippet[] TextSnippets = Array.Empty<TextSnippet>();
        private bool SetBlockInput = false;

        public string Text
        {
            get => text;
            set { text = value; TextSnippets = ChatManager.ParseMessage(value, Color.White).ToArray(); }
        }
        public event Action? StateChangedByUser;

        public UISwitch()
        {
            Width = new(32, 0);
            Height = new(32, 0);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rect rect = GetDimensions().ToRectangle();

            float side = Math.Min(rect.Width, rect.Height);
            Rect square = new(rect.X, rect.Y, side, side);

            PanelDrawing.Draw(spriteBatch, square.Floor(), BackColor, false);
            PanelDrawing.Draw(spriteBatch, square.Floor(), Color.Black, true);

            if (State)
            {
                square.Location += DotPaddings;
                square.Size -= DotPaddings * 2;

                PanelDrawing.Draw(spriteBatch, square, DotColor, false);
            }

            Vector2 textSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, TextSnippets, Vector2.One);
            textSize.Y -= 4;
            Vector2 textPos = new(rect.X + side + 4, rect.Y + (rect.Height - textSize.Y) / 2);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, TextSnippets, textPos, 0f, Vector2.Zero, Vector2.One, out _);
        }

        public override void Click(UIMouseEvent evt)
        {
            if (!State || RadioGroup is null)
            {
                State = !State;
                SoundEngine.PlaySound(SoundID.MenuTick);
                StateChangedByUser?.Invoke();

                if (RadioGroup is not null && Parent is not null)
                    foreach (UIElement element in Parent.Children)
                        if (element is UISwitch sw && sw != this && sw.RadioGroup == RadioGroup)
                        {
                            sw.State = false;
                            sw.StateChangedByUser?.Invoke();
                        }
            }

            base.Click(evt);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            if (!Main.blockInput && !SetBlockInput)
            {
                SetBlockInput = true;
                Main.blockInput = true;
            }
        }
        public override void MouseOut(UIMouseEvent evt)
        {
            if (SetBlockInput)
            {
                SetBlockInput = false;
                Main.blockInput = false;
            }
            base.MouseOut(evt);
        }
    }
}
