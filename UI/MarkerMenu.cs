using MapMarkers.Items;
using MapMarkers.Markers;
using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers.UI
{
    public static class MarkerMenu
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

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
        static Vector2 MarkerPosUICache;

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
            MarkerPosUICache = marker.Position;

            SoundEngine.PlaySound(SoundID.MenuOpen);
        }
        public static void Hide()
        {
            UI.IsVisible = false;
            Marker = null;
            SoundEngine.PlaySound(SoundID.MenuClose);
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
                string description = Language.GetTextValue(def.Description);
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
                    Text = Language.GetTextValue(def.Text)
                };
                Action callback = def.Callback;
                label.OnClick += (ev, el) => { callback(); Hide(); };

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
            if (Marker is null)
                yield break;

            foreach (MenuItemDefinition def in Marker.GetMenuItems())
                yield return def;

            if (Marker.CanDelete(Main.myPlayer))
                yield return new(
                    "Mods.MapMarkers.MarkerMenuItem.Delete",
                    "Mods.MapMarkers.MarkerMenuItemDesc.Delete",
                    () => { MapMarkers.RemoveMarker(Marker, true); });

            yield return new(
                Marker.PlayerData.Pinned ? "Mods.MapMarkers.MarkerMenuItem.Unpin" : "Mods.MapMarkers.MarkerMenuItem.Pin",
                Marker.PlayerData.Pinned ? "Mods.MapMarkers.MarkerMenuItemDesc.Unpin" : "Mods.MapMarkers.MarkerMenuItemDesc.Pin",
                () => Marker.PlayerData.Pinned = !Marker.PlayerData.Pinned);

            yield return new(
                Marker.PlayerData.Enabled ? "Mods.MapMarkers.MarkerMenuItem.Disable" : "Mods.MapMarkers.MarkerMenuItem.Enable",
                Marker.PlayerData.Enabled ? "Mods.MapMarkers.MarkerMenuItemDesc.Disable" : "Mods.MapMarkers.MarkerMenuItemDesc.Enable",
                () => Marker.PlayerData.Enabled = !Marker.PlayerData.Enabled);

            if (MarkerPlayer.LocalInstance.CanTeleport(Marker))
                yield return new(
                    "Mods.MapMarkers.MarkerMenuItem.Teleport",
                    "Mods.MapMarkers.MarkerMenuItemDesc.Teleport",
                    () => MarkerPlayer.LocalInstance.Teleport(Marker));
        }

        internal static void Draw()
        {
            if (UI.IsVisible)
                UI.Draw(Main.spriteBatch, Main.gameTimeCache);
        }

        internal static void Update(GameTime time)
        {
            if (!Helper.IsFullscreenMap && UI.IsVisible)
                Hide();

            if (Keybinds.DebugReloadInterfaceKeybind.State == KeybindState.JustPressed)
                InitInterface();

            PrevHovering = Hovering;
            UIElement? e = UI.IsVisible ? State?.GetElementAt(Main.MouseScreen) : null;
            Hovering = e is not null and not UIState;

            if (UI.IsVisible)
            {
                if (Marker is null
                    || !MapMarkers.Markers.ContainsKey(Marker.Id)
                    || (State is not null && !Hovering && Keybinds.MouseLeftKey == KeybindState.Pressed
                    || MapPosUICache != Main.mapFullscreenPos
                    || MapScaleUICache != Main.mapFullscreenScale
                    || MarkerPosUICache != Marker.Position))
                {
                    Hide();
                    return;
                }
                UI.Update(time);
            }
        }

        public record struct MenuItemDefinition(string Text, string Description, Action Callback);
    }
}
