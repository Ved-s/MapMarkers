using MapMarkers.Structures;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;

namespace MapMarkers
{
    public static class Keybinds
    {
        public static KeyboardState OldState;
        public static KeyboardState CurrentState;

        public static MouseState OldMouseState;
        public static MouseState CurrentMouseState;

        public static Keybind DebugReloadInterfaceKeybind { get; } = new(Keys.LeftControl, Keys.LeftShift, Keys.R);

        public static KeybindState MouseLeftKey => GetState(CurrentMouseState.LeftButton == ButtonState.Pressed, OldMouseState.LeftButton == ButtonState.Pressed);
        public static KeybindState MouseMiddleKey => GetState(CurrentMouseState.MiddleButton == ButtonState.Pressed, OldMouseState.MiddleButton == ButtonState.Pressed);
        public static KeybindState MouseRightKey => GetState(CurrentMouseState.RightButton == ButtonState.Pressed, OldMouseState.RightButton == ButtonState.Pressed);
        public static KeybindState MouseX1Key => GetState(CurrentMouseState.XButton1 == ButtonState.Pressed, OldMouseState.XButton1 == ButtonState.Pressed);
        public static KeybindState MouseX2Key => GetState(CurrentMouseState.XButton2 == ButtonState.Pressed, OldMouseState.XButton2 == ButtonState.Pressed);

        public static readonly Keys[] AnyCtrlKey = new[]  { Keys.LeftControl, Keys.RightControl };
        public static readonly Keys[] AnyAltKey = new[]   { Keys.LeftAlt, Keys.RightAlt };
        public static readonly Keys[] AnyShiftKey = new[] { Keys.LeftShift, Keys.RightShift };
        public static readonly Keys[] AnyWinKey = new[]   { Keys.LeftWindows, Keys.RightWindows };
        public static readonly Keys[] AnyModKey = new[]   { Keys.LeftShift, Keys.RightShift, Keys.LeftAlt, Keys.RightAlt, Keys.LeftControl, Keys.RightControl, Keys.LeftWindows, Keys.RightWindows };

        public static KeybindState CtrlKey => GetAnyKey(AnyCtrlKey);
        public static KeybindState AltKey => GetAnyKey(AnyAltKey);
        public static KeybindState ShiftKey => GetAnyKey(AnyShiftKey);
        public static KeybindState WinKey => GetAnyKey(AnyWinKey);
        public static KeybindState ModKey => GetAnyKey(AnyModKey);

        public static bool InputBlocked => Main.blockInput || Main.editChest || Main.editSign || Main.drawingPlayerChat;

        internal static void Update()
        {
            OldState = CurrentState;
            CurrentState = Keyboard.GetState();

            OldMouseState = CurrentMouseState;
            CurrentMouseState = Mouse.GetState();
        }

        public static KeybindState GetAnyKey(Keys[] keys)
        {
            KeybindState state = KeybindState.Released;

            foreach (Keys key in keys)
            {
                KeybindState keyState = GetKey(key);
                if (keyState == KeybindState.Pressed)
                    return KeybindState.Pressed;

                if (keyState == KeybindState.Released)
                    continue;

                if (keyState == KeybindState.JustPressed)
                    state = KeybindState.JustPressed;

                if (keyState == KeybindState.JustReleased && state != KeybindState.JustPressed)
                    state = KeybindState.JustReleased;
            }

            return state;
        }

        public static KeybindState GetKey(Keys key)
        {
            return GetState(CurrentState[key] == KeyState.Down, OldState[key] == KeyState.Down);
        }

        public static KeybindState GetState(bool newPressed, bool oldPressed)
{
            return (KeybindState)(newPressed.ToInt() << 1 | oldPressed.ToInt());
        }

        public static KeybindState GetKeybind(Keys[] keys)
        {
            KeybindState state = KeybindState.Pressed;

            foreach (Keys key in keys)
            {
                KeybindState keyState = GetKey(key);
                if (keyState == KeybindState.Released)
                    return KeybindState.Released;

                if (keyState == KeybindState.JustReleased && state >= KeybindState.JustPressed)
                    state = KeybindState.JustReleased;

                if (keyState == KeybindState.JustPressed && state > KeybindState.JustPressed)
                    state = KeybindState.JustPressed;
            }

            return state;
        }
    }
}
