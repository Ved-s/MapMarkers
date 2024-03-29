﻿using Terraria.Localization;
using MapMarkers.Markers;
using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MapMarkers.UI
{
    public static class MarkerEditMenu
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        static UserInterface UI = new();

        static UIState? State;
        static UIPanel? MainPanel;
        static UIElement? LeftBox;

        static UIFocusInputTextField? NameInput;

        static UIFocusInputTextField? ItemSearchInput;
        static UIItemDisplay? ItemDisplay;
        static UIElement? SearchResults;

        static UISwitch? EnabledSwitch, PinnedSwitch;
        static UIAutoLabel? Description;

        static UIText? MultiplayerLimitedLabel;
        static UIText? MultiplayerLabel;
        static UISwitch? LocalSwitch, GlobalSwitch;

        static UIText? PermissionsLabel;
        static UISwitch? EditSwitch, DeleteSwitch;

        static Keybind Debug_ToggleVisibility = new(Keys.LeftControl, Keys.LeftShift, Keys.E, Keys.V);

        static PlacedMarker? Marker;

        public static bool Visible => State is not null && UI.IsVisible;

        static bool Hovering = false;
        static bool PrevHovering = false;

        static bool DrawDebugHitboxes = false;

        static bool Initialized = false;
        static void InitInterface()
        {
            State = new();
            UI.SetState(State);

            State.Append(MainPanel = new()
            {
                Width = new(600, 0),
                Height = new(400, 0),

                Top = new(-200, .5f),
                Left = new(-300, .5f),

                BackgroundColor = new(32, 32, 64, 200)
            });

            MainPanel.Append(new UIText(MapMarkers.GetLangValue("EditUI.Title"), 1.5f)
            {
                Top = new(-40, 0),
                Width = new(0, 1),
                Height = new(30, 0),

                TextOriginX = .5f
            });

            LeftBox = new()
            {
                Top = new(30, 0),
                Left = new(4, 0),
                Width = new(0, .6f),
                Height = new(-30, 1),
            };

            InitNameInput(LeftBox);
            InitSettings(LeftBox);
            LeftBox.Append(Description = new()
            {
                Top = new(105, 0),
                Width = new(0, 1),
            });

            MainPanel.Append(LeftBox);
            InitMPVisibility(LeftBox);
            InitMPPermissions(LeftBox);

            InitItemSearch();
            UpdateSearch();

            InitDescriptions();

            InitOkBtn();

            MultiplayerLimitedLabel = new(MapMarkers.GetLangValue("EditUI.MPLimited"))
            {
                Top = new(190, 0),
                Width = new(0, 1),
                Height = new(20, 0),

                TextOriginX = .5f
            };

            Initialized = true;
        }

        static void InitNameInput(UIElement box)
        {
            box.Append(new UIText(MapMarkers.GetLangValue("EditUI.Name"))
            {
                Top = new(0, 0),
                Left = new(4, 0),
                Width = new(-4, 1f),
                Height = new(20, 0),

                TextOriginX = 0,
            });

            box.Append(NameInput = new()
            {
                Top = new(20, 0),
                Left = new(0, 0),
                Width = new(0, 1),
                Height = new(30, 0),

                BackgroundColor = new(20, 20, 48, 200)
            });

            NameInput.OnTextChange += () =>
            {
                if (Marker is not null)
                    Marker.DisplayName = NameInput.CurrentString;
            };
        }
        static void InitSettings(UIElement box)
        {
            box.Append(EnabledSwitch = new()
            {
                Top = new(65, 0),
                Left = new(0, 0),
                Width = new(0, .5f),

                Text = MapMarkers.GetLangValue("EditUI.Enabled"),
                BackColor = new(32, 32, 64, 200),
                State = true
            });

            box.Append(PinnedSwitch = new()
            {
                Top = new(65, 0),
                Left = new(0, .5f),
                Width = new(0, .5f),

                Text = MapMarkers.GetLangValue("EditUI.Pinned"),
                BackColor = new(32, 32, 64, 200)
            });

            PinnedSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                Marker.PlayerData.Pinned = PinnedSwitch?.State ?? false;
            };

            EnabledSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                Marker.PlayerData.Enabled = EnabledSwitch?.State ?? false;
            };
        }
        static void InitMPVisibility(UIElement box)
        {
            box.Append(MultiplayerLabel = new(MapMarkers.GetLangValue("EditUI.MPVisibility.Title"))
            {
                Top = new(190, 0),
                Width = new(0, 1),
                Height = new(20, 0),

                TextOriginX = 0
            });

            box.Append(LocalSwitch = new()
            {
                Top = new(210, 0),
                Left = new(0, 0),
                Width = new(0, .5f),

                BackColor = new(32, 32, 64, 200),
                DotColor = Color.Yellow,
                Text = MapMarkers.GetLangValue("EditUI.MPVisibility.Local"),
                State = true,
                RadioGroup = "mpv"
            });

            //box.Append(TeamSwitch = new()
            //{
            //    Top = new(210, 0),
            //    Left = new(0, .33f),
            //    Width = new(0, .33f),
            //
            //    BackColor = new(32, 32, 64, 200),
            //    DotColor = Color.Yellow,
            //    Text = MapMarkers.GetLangValue("EditUI.MPVisibility.Team"),
            //    RadioGroup = "mpv"
            //});

            box.Append(GlobalSwitch = new()
            {
                Top = new(210, 0),
                Left = new(0, .5f),
                Width = new(0, .5f),

                BackColor = new(32, 32, 64, 200),
                DotColor = Color.Yellow,
                Text = MapMarkers.GetLangValue("EditUI.MPVisibility.Global"),
                RadioGroup = "mpv"
            });

            GlobalSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                Marker.ServerSide = GlobalSwitch?.State ?? false;
            };
        }
        static void InitMPPermissions(UIElement box)
        {
            box.Append(PermissionsLabel = new(MapMarkers.GetLangValue("EditUI.MPPermissions.Title"))
            {
                Top = new(260, 0),
                Width = new(0, 1),
                Height = new(20, 0),

                TextOriginX = 0
            });

            box.Append(EditSwitch = new()
            {
                Top = new(280, 0),
                Left = new(0, 0),
                Width = new(0, .5f),

                BackColor = new(32, 32, 64, 200),
                Text = MapMarkers.GetLangValue("EditUI.MPPermissions.Edit"),
                State = true
            });

            box.Append(DeleteSwitch = new()
            {
                Top = new(280, 0),
                Left = new(0, .5f),
                Width = new(0, .5f),

                BackColor = new(32, 32, 64, 200),
                Text = MapMarkers.GetLangValue("EditUI.MPPermissions.Delete"),
                State = true
            });

            DeleteSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                Marker.AnyoneCanRemove = DeleteSwitch.State;
            };

            EditSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                Marker.AnyoneCanEdit = EditSwitch.State;
            };
        }

        static void InitOkBtn()
        {
            if (State is null)
                return;

            UITextPanel<string> button = new(MapMarkers.GetLangValue("EditUI.Ok"))
            {
                Top = new(210, .5f),
                Left = new(-50, .5f),
                Width = new(100, 0),
                Height = new(35, 0)
            };

            button.OnMouseOver += (ev, el) => { (el as UIPanel)!.BackgroundColor = new Color(80, 90, 160) * 0.7f; SoundEngine.PlaySound(SoundID.MenuTick); };
            button.OnMouseOut += (ev, el) => (el as UIPanel)!.BackgroundColor = new Color(63, 82, 151) * 0.7f;

            button.OnClick += (ev, el) => Hide();

            State.Append(button);
        }

        static void InitItemSearch()
        {
            if (MainPanel is null)
                return;

            UIElement box = new()
            {
                Top = new(20, 0),
                Left = new(40, .6f),
                Height = new(-30, 1),
                Width = new(-40, .4f)
            };

            box.Append(new UIText(MapMarkers.GetLangValue("EditUI.Item"))
            {
                Top = new(0, 0),
                Left = new(4, 0),
                Width = new(-4, 1),
                Height = new(20, 0),

                TextOriginX = 0,
            });

            Item i = new();
            i.SetDefaults(ItemID.SoulofLight);

            box.Append(ItemDisplay = new UIItemDisplay
            {
                Top = new(21, 0),
                Item = i,
                BackColor = new(100, 100, 100),
            });

            box.Append(new UIText(MapMarkers.GetLangValue("EditUI.ItemSearch"))
            {
                Top = new(10, 0),
                Left = new(56, 0),
                Width = new(-56, 1),
                Height = new(20, 0),

                TextOriginX = 0,
            });
            box.Append(ItemSearchInput = new UIFocusInputTextField
            {
                Top = new(30, 0),
                Left = new(52, 0),
                Width = new(-52, 1),
                Height = new(30, 0),
                BackgroundColor = new(20, 20, 48, 200)
            });
            ItemSearchInput.OnTextChange += UpdateSearch;

            box.Append(SearchResults = new()
            {
                Top = new(71, 0),
                Width = new(0, 1),
                Height = new(-71, 1),
            });

            MainPanel.Append(box);
        }
        static void UpdateSearch()
        {
            if (SearchResults is null || ItemSearchInput is null || ItemDisplay is null)
                return;

            Vector2 size = SearchResults.GetInnerDimensions().ToRectangle().Size();

            Vector2 slotSize = new(40);
            Vector2 slotPad = new(4);
            Vector2 align = new(.5f, 0);

            Vector2 pos = new();

            int maxCols = (int)((size.X + slotPad.X) / (slotSize.X + slotPad.X));
            int maxRows = (int)((size.Y + slotPad.Y) / (slotSize.Y + slotPad.Y));

            Vector2 start = (size - new Vector2(maxCols, maxRows) * slotSize - new Vector2(maxCols - 1, maxRows - 1) * slotPad) * align;

            SearchResults.RemoveAllChildren();

            pos = start;

            for (int i = 1; i < ItemLoader.ItemCount; i++)
            {
                if (!Lang.GetItemNameValue(i).Contains(ItemSearchInput.CurrentString, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                UIItemDisplay slot = new();
                slot.Item = new();
                slot.Item.SetDefaults(i);

                slot.Top = new(pos.Y, 0);
                slot.Left = new(pos.X, 0);

                slot.Width = new(slotSize.X, 0);
                slot.Height = new(slotSize.Y, 0);

                slot.OnClick += (ev, el) => ItemPicked((el as UIItemDisplay)!.Item!);
                slot.BackColor = new(120, 120, 120);

                SearchResults.Append(slot);

                pos.X += slotSize.X + slotPad.X;
                if (pos.X + slotSize.X > size.X)
                {
                    pos.Y += slotSize.Y + slotPad.Y;
                    pos.X = start.X;

                    if (pos.Y + slotSize.Y > size.Y)
                        break;
                }
            }
        }
        static void ItemPicked(Item item)
        {
            if (Marker is not null)
                Marker.DisplayItem = item.Clone();

            if (ItemDisplay is null)
                return;

            ItemDisplay.Item = item.Clone();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        static void InitDescriptions()
        {
            BindDescription(NameInput, "EditUI.Descriptions.NameInput");

            BindDescription(ItemDisplay, "EditUI.Descriptions.ItemDisplay");
            BindDescription(ItemSearchInput, "EditUI.Descriptions.ItemSearchInput");

            BindDescription(EnabledSwitch, "EditUI.Descriptions.EnabledSwitch");
            BindDescription(PinnedSwitch, "EditUI.Descriptions.PinnedSwitch");

            BindDescription(LocalSwitch, "EditUI.Descriptions.LocalSwitch");
            //BindDescription(TeamSwitch, "EditUI.Descriptions.TeamSwitch");
            BindDescription(GlobalSwitch, "EditUI.Descriptions.GlobalSwitch");

            BindDescription(EditSwitch, "EditUI.Descriptions.EditSwitch");
            BindDescription(DeleteSwitch, "EditUI.Descriptions.DeleteSwitch");
        }
        static void BindDescription(UIElement? element, string description)
        {
            if (Description is null || element is null)
                return;

            element.OnMouseOver += (ev, el) => Description.Text = MapMarkers.GetLangValue(description);
            element.OnMouseOut += (ev, el) => Description.Text = "";
        }

        static void UpdateMarkerInterface()
        {
            NameInput?.SetText(Marker?.DisplayName!);

            if (ItemDisplay is not null)
                ItemDisplay.Item = Marker?.DisplayItem;

            if (EnabledSwitch is not null)
                EnabledSwitch.State = Marker is not null && Marker.PlayerData.Enabled;

            if (PinnedSwitch is not null)
                PinnedSwitch.State = Marker is not null && Marker.PlayerData.Pinned;

            if (Networking.IsSingleplayer || LeftBox is null || Marker is null || !Networking.OtherSideMod || !Marker.CheckOwnerPermission(Main.myPlayer))
            {
                MultiplayerLabel?.Remove();
                LocalSwitch?.Remove();
                GlobalSwitch?.Remove();

                PermissionsLabel?.Remove();
                EditSwitch?.Remove();
                DeleteSwitch?.Remove();
                MultiplayerLimitedLabel?.Remove();
            }

            else if (!Marker.ServerSide && !Networking.CheckMarkerCap(Main.myPlayer))
            {
                MultiplayerLabel?.Remove();
                LocalSwitch?.Remove();
                GlobalSwitch?.Remove();

                PermissionsLabel?.Remove();
                EditSwitch?.Remove();
                DeleteSwitch?.Remove();

                if (MultiplayerLimitedLabel is not null)
                    LeftBox.Append(MultiplayerLimitedLabel);
            }

            else
            {
                MultiplayerLimitedLabel?.Remove();

                if (MultiplayerLabel is not null)
                    LeftBox.Append(MultiplayerLabel);

                if (LocalSwitch is not null)
                {
                    LeftBox.Append(LocalSwitch);
                    LocalSwitch.State = !Marker.ServerSide;
                }

                if (GlobalSwitch is not null)
                {
                    LeftBox.Append(GlobalSwitch);
                    GlobalSwitch.State = Marker.ServerSide;
                }

                if (PermissionsLabel is not null)
                    LeftBox.Append(PermissionsLabel);

                if (EditSwitch is not null)
                {
                    LeftBox.Append(EditSwitch);
                    EditSwitch.State = Marker.AnyoneCanEdit;
                }

                if (DeleteSwitch is not null)
                {
                    LeftBox.Append(DeleteSwitch);
                    DeleteSwitch.State = Marker.AnyoneCanRemove;
                }
            }
        }

        public static void Show(PlacedMarker marker)
        {
            Marker = marker;
            UI.IsVisible = true;
            Main.mapFullscreen = false;
            Main.playerInventory = false;

            if (!Initialized)
                InitInterface();
            UpdateMarkerInterface();
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }
        public static void Hide()
        {
            Marker = null;
            UI.IsVisible = false;
            SoundEngine.PlaySound(SoundID.MenuClose);
        }

        public static bool Draw()
        {
            if (!Initialized)
                InitInterface();

            if (!UI.IsVisible)
                return true;

            UI.Draw(Main.spriteBatch, Main.gameTimeCache);

            if (DrawDebugHitboxes && UI.CurrentState is not null)
                DrawDebug(UI.CurrentState);

            return true;
        }
        public static void Update(GameTime gameTime)
        {
            if (Debug_ToggleVisibility.State == KeybindState.JustPressed)
            {
                if (!UI.IsVisible)
                    UI.IsVisible = true;
                else if (!DrawDebugHitboxes)
                    DrawDebugHitboxes = true;
                else
                {
                    UI.IsVisible = false;
                    DrawDebugHitboxes = false;
                }
            }

            if (!UI.IsVisible)
                return;

            if (Helper.IsFullscreenMap || Helper.IsOverlayMap || Main.playerInventory)
                Hide();

            if (Keybinds.DebugReloadInterfaceKeybind.State == KeybindState.JustPressed)
            {
                InitInterface();
                if (Marker is not null)
                    UpdateMarkerInterface();
            }

            PrevHovering = Hovering;
            UIElement? e = UI.IsVisible ? State?.GetElementAt(Main.MouseScreen) : null;
            Hovering = e is not null and not UIState;

            UI.Update(gameTime);
        }

        static void DrawDebug(UIElement element)
        {
            Main.spriteBatch.DrawRectangle(element.GetOuterDimensions().ToRectangle(), Color.Yellow * .1f);
            Main.spriteBatch.DrawRectangle(element.GetInnerDimensions().ToRectangle(), Color.Lime * .1f);
            Main.spriteBatch.DrawRectangle(element.GetDimensions().ToRectangle(), Color.Red * .1f);

            foreach (UIElement child in element.Children)
                DrawDebug(child);
        }
    }
}
