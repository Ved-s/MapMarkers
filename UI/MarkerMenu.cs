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
using Terraria.GameInput;

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

        public static UIAutoLabel? DescriptionLabel;
        public static UIPanel? DescriptionPanel;

        public static string DescriptionText 
        {
            get => DescriptionLabel?.Text ?? "";
            set 
            {
                if (DescriptionLabel is null || Panel is null)
                    return;

                DescriptionLabel.Text = value;
                UpdateDescriptionPos();
            }
        }
        public static float DescriptionY;

        public static bool PrevHovering;
        public static bool Hovering;

        static Vector2 MapPosUICache;
        static float MapScaleUICache;

        public static void Show(MapMarker? marker)
        {
            if (!Helper.IsFullscreenMap)
                return;

            if (State is null)
                InitInterface();

            if (marker is null || Marker is not null && Marker.Id == marker.Id)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                Marker = null;
                UI.IsVisible = false;
                return;
            }

            Marker = marker;

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

            MapPosUICache = Main.mapFullscreenPos;
            MapScaleUICache = Main.mapFullscreenScale;

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

            string desc = DescriptionText;

            DescriptionLabel = new()
            {
                Width = new(0, 1)
            };
            DescriptionPanel = new()
            {
                Width = new(200, 0),
                BackgroundColor = new(30, 30, 30, 200),
                PaddingTop = 2,
                PaddingBottom = 0
            };
            DescriptionPanel.Append(DescriptionLabel);

            DescriptionText = desc;
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
                string description = def.Description;
                float desY = y + Panel.PaddingTop;
                labelPanel.OnMouseOver += (ev, el) =>
                {
                    DescriptionY = desY;
                    DescriptionText = description;
                    (el as UIPanel)!.BackgroundColor = new Color(96, 96, 96, 200);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };

                labelPanel.OnMouseOut += (ev, el) =>
                {
                    DescriptionText = "";
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
                labelPanel.Height = new(label.GetOuterDimensions().Height + labelPanel.PaddingTop + labelPanel.PaddingBottom - 2, 0);

                labelPanel.Recalculate();
                y += labelPanel.GetOuterDimensions().Height;
                y += 2;
            }

            y -= 2;
            Panel.Height = new(y + Panel.PaddingTop + Panel.PaddingBottom, 0);

            Panel.Recalculate();
            UpdateDescriptionPos();
        }

        internal static void UpdateDescriptionPos()
        {
            if (Panel is null || State is null || DescriptionLabel is null || DescriptionPanel is null)
                return;

            if (DescriptionText == "")
            {
                State.RemoveChild(DescriptionPanel);
                return;
            }

            State.Append(DescriptionPanel);

            DescriptionLabel.Recalculate();
            DescriptionPanel.Height = new(DescriptionLabel.GetOuterDimensions().Height + DescriptionPanel.PaddingTop + DescriptionPanel.PaddingBottom, 0);
            DescriptionPanel.Left = new(Panel.GetOuterDimensions().ToRectangle().Right + 2 - State.GetDimensions().X, 0);
            DescriptionPanel.Top = new(Panel.GetOuterDimensions().Y + DescriptionY - State.GetDimensions().Y, 0);

            DescriptionPanel.Recalculate();
        }

        internal static IEnumerable<MenuItemDefinition> EnumerateMenuItems()
        {
            yield return new(
                "Delete",
                "Delete marker", 
                () => { Main.NewText("Not yet implemented"); });
            yield return new(
                "Pin",
                "Pin/unpin marker\nPinned markers are always visible on the map", 
                () => { Main.NewText("Not yet implemented"); });
            yield return new(
                "Teleport",
                "Teleport to the marker\nWill consume one Marker Teleport Potion",
                () => { Main.NewText("Not yet implemented"); });
        }

        internal static void Draw()
        {
            if (UI.IsVisible)
                UI.Draw(Main.spriteBatch, Main.gameTimeCache);
        }

        internal static void Update(GameTime time)
        {
            if (!Helper.IsFullscreenMap && UI.IsVisible)
            {
                UI.IsVisible = false;
                Marker = null;
                SoundEngine.PlaySound(SoundID.MenuClose);
            }

            if (Keybinds.GetKeybind(KeybindId.Debug_ReloadInterface) == KeybindState.JustPressed)
                InitInterface();

            PrevHovering = Hovering;
            UIElement? e = UI.IsVisible ? State?.GetElementAt(Main.MouseScreen) : null;
            Hovering = e is not null and not UIState;

            if (UI.IsVisible && 
                (State is not null && !Hovering && Keybinds.MouseLeftKey == KeybindState.Pressed
                || MapPosUICache != Main.mapFullscreenPos
                || MapScaleUICache != Main.mapFullscreenScale))
            {
                UI.IsVisible = false;
                Marker = null;
                SoundEngine.PlaySound(SoundID.MenuClose);
            }

            if (!PrevHovering && Hovering)
                HerosIntegration.Instance.AllowTp = false;

            if (PrevHovering && !Hovering)
                HerosIntegration.Instance.AllowTp = true;

            if (UI.IsVisible)
                UI.Update(time);
        }

        public record struct MenuItemDefinition(string Text, string Description, Action Callback);
    }
}
