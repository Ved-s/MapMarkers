using Terraria.GameContent;
using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MapMarkers.UI
{
    public static class MarkerMenu
    {
        static UserInterface UI = new();

        public static MapMarker? Marker;

        public static UIState? State;
        public static UIPanel? Panel;

        public static UIPanel? MarkerNamePanel;
        public static UIText? MarkerName;

        public static bool PrevHovering;
        public static bool Hovering;

        public static void Show(MapMarker? marker)
        {
            if (!Helper.IsFullscreenMap)
                return;

            if (marker is null || Marker is not null && Marker.Id == marker.Id)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                Marker = null;
                UI.IsVisible = false;
                return;
            }

            Marker = marker;

            if (State is null)
                InitInterface();

            UI.IsVisible = true;

            float width = FontAssets.MouseText.Value.MeasureString(marker.DisplayName).X + MarkerNamePanel!.PaddingLeft + MarkerNamePanel.PaddingRight;
            width = Math.Max(width, 150);

            MarkerNamePanel.Width = new(width, 0);
            Panel!.Width = new(width, 0);

            MarkerName!.SetText(marker.DisplayName);
            State!.Top.Set(marker.ScreenRect.Y, 0);
            State!.Left.Set(marker.ScreenRect.Right + 4, 0);
            State.Recalculate();
            InitMenuItems();

            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        internal static void InitInterface()
        {
            State = new();
            UI.SetState(State);

            MarkerNamePanel = new()
            {
                Width = new(150, 0),
                Height = new(24, 0),
                PaddingTop = 4,
                PaddingBottom = 0,
                BackgroundColor = new(30, 30, 30, 200)
            };
            MarkerNamePanel.Append(MarkerName = new("")
            {
                Width = new(0, 1),
                Height = new(0, 1),
            });

            State.Append(MarkerNamePanel);

            State.Append(Panel = new UIPanel
            {
                Top = new(26, 0),
                Width = new(150, 0),
                Height = new(300, 0),

                PaddingTop = 6,
                PaddingBottom = 6,
                PaddingLeft = 6,
                PaddingRight = 6,

                BackgroundColor = new Color(36, 36, 36, 200),
            });

            if (Marker is not null)
            {
                MarkerName!.SetText(Marker.DisplayName);
                State!.Top.Set(Marker.ScreenRect.Y, 0);
                State!.Left.Set(Marker.ScreenRect.Right + 4, 0);
            }

            if (Marker is not null)
                Show(Marker);
            else InitMenuItems();
        }

        internal static void InitMenuItems()
        {
            if (Panel is null)
                return;

            Panel.RemoveAllChildren();

            if (Marker is null)
            {
                Panel.Append(new UIAutoLabel { Width = new(0, 1), Height = new(0, 1), Text = "No marker selected", TextAlign = new(.5f, .5f) });
                Panel.Height = new(100, 0);
                return;
            }

            float y = 0;
            foreach (var def in EnumerateMenuItems())
            {
                UIPanel labelPanel = new()
                {
                    Width = new(0, 1),
                    Top = new(y, 0),
                    PaddingTop = 2,
                    PaddingBottom = 0,

                    BackgroundColor = new Color(64, 64, 64, 200),
                };
                labelPanel.OnMouseOver += (ev, el) =>
                {
                    (el as UIPanel)!.BackgroundColor = new Color(96, 96, 96, 200);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };

                labelPanel.OnMouseOut += (ev, el) =>
                {
                    (el as UIPanel)!.BackgroundColor = new Color(64, 64, 64, 200);
                };

                Panel.Append(labelPanel);

                UIAutoLabel label = new()
                {
                    Width = new(0, 1),
                    Text = def.Text
                };
                Action callback = def.Callback;
                label.OnClick += (ev, el) => callback();

                labelPanel.Append(label);

                label.Recalculate();
                labelPanel.Height = new(label.GetOuterDimensions().Height + labelPanel.PaddingTop + labelPanel.PaddingBottom - 6, 0);

                labelPanel.Recalculate();
                y += labelPanel.GetOuterDimensions().Height;
                y += 2;
            }

            y -= 2;
            Panel.Height = new(y + Panel.PaddingTop + Panel.PaddingBottom, 0);

            Panel.Recalculate();
        }

        internal static IEnumerable<MenuItemDefinition> EnumerateMenuItems()
        {
            yield return new("Menu item 1", () => { Main.NewText("Action 1"); });
            yield return new("Menu item 2", () => { Main.NewText("Action 2"); });
            yield return new("Menu item 3", () => { Main.NewText("Action 3"); });
            yield return new("Long menu item name", () => { Main.NewText("Action 3"); });
        }

        internal static void Draw()
        {
            if (UI.IsVisible)
                UI.Draw(Main.spriteBatch, Main.gameTimeCache);
        }

        internal static void Update(GameTime time)
        {
            if (!Helper.IsFullscreenMap && UI.IsVisible)
                UI.IsVisible = false;

            if (Keybinds.GetKeybind(KeybindId.Debug_ReloadInterface) == KeybindState.JustPressed)
                InitInterface();

            PrevHovering = Hovering;
            UIElement? e = UI.IsVisible ? State?.GetElementAt(Main.MouseScreen) : null;
            Hovering = e is not null and not UIState;

            if (UI.IsVisible && Keybinds.MouseLeftKey == KeybindState.Pressed && State is not null && !Hovering)
            {
                UI.IsVisible = false;
                SoundEngine.PlaySound(SoundID.MenuClose);
            }

            if (!PrevHovering && Hovering)
                HerosIntegration.Instance.AllowTp = false;

            if (PrevHovering && !Hovering)
                HerosIntegration.Instance.AllowTp = true;

            if (UI.IsVisible)
                UI.Update(time);
        }

        public record struct MenuItemDefinition(string Text, Action Callback);
    }
}
