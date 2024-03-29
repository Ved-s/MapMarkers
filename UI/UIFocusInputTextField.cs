﻿using Ionic.Zlib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace MapMarkers.UI
{
    public class UIFocusInputTextField : UIElement
	{
		internal bool Focused;

		public string CurrentString = "";

		private int _textBlinkerCount;

		private int _textBlinkerState;

		public event Action? OnTextChange;

		public Color BackgroundColor = Color.White;

		static char[] Whitespaces = new[] { '\t', ' ' };

		public UIFocusInputTextField() 
		{
			PaddingTop = 0;
			PaddingLeft = 0;
			PaddingRight = 0;
			PaddingBottom = 0;
		}

		public void SetText(string text)
		{
			if (text == null)
			{
				text = "";
			}
			if (CurrentString != text)
			{
				CurrentString = text;
			}
		}

		public override void Click(UIMouseEvent evt)
		{
			Main.clrInput();
			Focused = true;
			Main.blockInput = true;
		}

		public override void Update(GameTime gameTime)
		{
			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (!ContainsPoint(MousePosition) && Main.mouseLeft)
			{
				Focused = false;
				Main.blockInput = false;
			}
			base.Update(gameTime);
		}

		private static bool JustPressed(Keys key)
		{
			return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			PanelDrawing.Draw(spriteBatch, GetDimensions().ToRectangle(), BackgroundColor, false);
			PanelDrawing.Draw(spriteBatch, GetDimensions().ToRectangle(), Color.Black, true);

			if (Focused)
			{
				PlayerInput.WritingText = true;
				Main.instance.HandleIME();
				string newString = Main.GetInputText(CurrentString, false);
				if (newString != CurrentString)
				{
					CurrentString = newString;
					OnTextChange?.Invoke();
				}
				if (JustPressed(Keys.Back) && Main.inputText.PressingControl())
				{
					int index = CurrentString.LastIndexOfAny(Whitespaces);
					if (index < 0)
						CurrentString = "";
					else 
						CurrentString = CurrentString[..index];
				}
				int num = _textBlinkerCount + 1;
				_textBlinkerCount = num;
				if (num >= 20)
				{
					_textBlinkerState = (_textBlinkerState + 1) % 2;
					_textBlinkerCount = 0;
				}
			}
			string displayString = CurrentString;
			if (_textBlinkerState == 1 && Focused)
			{
				displayString += "|";
			}
			CalculatedStyle space = GetDimensions();
			if (CurrentString.Length == 0 && !Focused)
				return;
			
			Utils.DrawBorderString(spriteBatch, displayString, new Vector2(space.X + 4, space.Y + (space.Height - (FontAssets.MouseText.Value.LineSpacing - 4)) / 2), Color.White, 1f, 0f, 0f, -1);
		}
	}
}
