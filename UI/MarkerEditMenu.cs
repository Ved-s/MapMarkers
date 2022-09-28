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

        static UIFocusInputTextField? NameInput;

        static UIFocusInputTextField? ItemSearchInput;
        static UIItemDisplay? ItemDisplay;
        static UIElement? SearchResults;

        static UISwitch? EnabledSwitch, PinnedSwitch;
        static UIAutoLabel? Description;

        static UIText? MultiplayerLabel;
        static UISwitch? LocalSwitch, TeamSwitch, GlobalSwitch;

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

            MainPanel.Append(new UIText("Edit marker", 1.5f)
            {
                Top = new(-40, 0),
                Width = new(0, 1),
                Height = new(30, 0),

                TextOriginX = .5f
            });

            UIElement leftBox = new()
            {
                Top = new(30, 0),
                Left = new(4, 0),
                Width = new(0, .6f),
                Height = new(-30, 1),
            };

            InitNameInput(leftBox);
            InitSettings(leftBox);
            leftBox.Append(Description = new()
            {
                Top = new(105, 0),
                Width = new(0, 1),
            });

            MainPanel.Append(leftBox);
            InitMPVisibility(leftBox);
            InitMPPermissions(leftBox);

            InitItemSearch();
            UpdateSearch();

            InitDescriptions();

            InitOkBtn();

            Initialized = true;
        }

        static void InitNameInput(UIElement box)
        {
            box.Append(new UIText("Name")
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

                Text = "Enabled",
                BackColor = new(32, 32, 64, 200),
                State = true
            });

            box.Append(PinnedSwitch = new()
            {
                Top = new(65, 0),
                Left = new(0, .5f),
                Width = new(0, .5f),

                Text = "Pinned",
                BackColor = new(32, 32, 64, 200)
            });

            PinnedSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                if (PinnedSwitch?.State ?? true)
                    MapMarkers.PinnedMarkers.Add(Marker.Id);
                else
                    MapMarkers.PinnedMarkers.Remove(Marker.Id);
            };

            EnabledSwitch.StateChangedByUser += () =>
            {
                if (Marker is null)
                    return;

                MapMarkers.SetMarkerEnabled(Marker, EnabledSwitch?.State ?? false);
            };
        }
        static void InitMPVisibility(UIElement box)
        {
            box.Append(MultiplayerLabel = new("Multiplayer visibility")
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
                Width = new(0, .33f),

                BackColor = new(32, 32, 64, 200),
                DotColor = Color.Yellow,
                Text = "Local",
                State = true,
                RadioGroup = "mpv"
            });

            box.Append(TeamSwitch = new()
            {
                Top = new(210, 0),
                Left = new(0, .33f),
                Width = new(0, .33f),

                BackColor = new(32, 32, 64, 200),
                DotColor = Color.Yellow,
                Text = "Team",
                RadioGroup = "mpv"
            });

            box.Append(GlobalSwitch = new()
            {
                Top = new(210, 0),
                Left = new(0, .66f),
                Width = new(0, .33f),

                BackColor = new(32, 32, 64, 200),
                DotColor = Color.Yellow,
                Text = "Global",
                RadioGroup = "mpv"
            });
        }
        static void InitMPPermissions(UIElement box)
        {
            box.Append(PermissionsLabel = new("Multiplayer permissions")
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
                Text = "Edit",
                State = true
            });

            box.Append(DeleteSwitch = new()
            {
                Top = new(280, 0),
                Left = new(0, .5f),
                Width = new(0, .5f),

                BackColor = new(32, 32, 64, 200),
                Text = "Delete",
                State = true
            });
        }

        static void InitOkBtn()
        {
            if (State is null)
                return;

            UITextPanel<string> button = new("Ok")
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

            box.Append(new UIText("Item")
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

            box.Append(new UIText("Search item")
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
            BindDescription(NameInput, "The name of the marker bering edited");

            BindDescription(ItemDisplay, "Item which will be displayed as marker icon");
            BindDescription(ItemSearchInput, "Search input for item list");

            BindDescription(EnabledSwitch, "Whether the marker is enabled and shown on the map. Hold Shift while fullscreen map is open to see disabled markers");
            BindDescription(PinnedSwitch, "Whether the marker is pinned and stays always visible on map");

            BindDescription(LocalSwitch, "Only you can see the marker");
            BindDescription(TeamSwitch, "Only your team can see the marker");
            BindDescription(GlobalSwitch, "Everyone on server can see the marker");

            BindDescription(EditSwitch, "Everyone can edit this marker");
            BindDescription(DeleteSwitch, "Everyone can delete this marker");
        }
        static void BindDescription(UIElement? element, string description)
        {
            if (Description is null || element is null)
                return;

            element.OnMouseOver += (ev, el) => Description.Text = description;
            element.OnMouseOut += (ev, el) => Description.Text = "";
        }

        static void UpdateMarkerInterface()
        {
            NameInput?.SetText(Marker?.DisplayName!);

            if (ItemDisplay is not null)
                ItemDisplay.Item = Marker?.DisplayItem;

            if (EnabledSwitch is not null)
                EnabledSwitch.State = Marker is not null && Marker.Enabled;

            if (PinnedSwitch is not null)
                PinnedSwitch.State = Marker is not null && MapMarkers.PinnedMarkers.Contains(Marker.Id);
            


            // TODO: Multiplayer
            MultiplayerLabel?.Remove();
            LocalSwitch?.Remove();
            TeamSwitch?.Remove();
            GlobalSwitch?.Remove();
            PermissionsLabel?.Remove();
            EditSwitch?.Remove();
            DeleteSwitch?.Remove();
        }

        public static void Show(PlacedMarker marker)
        {
            Marker = marker;
            UI.IsVisible = true;
            Main.mapFullscreen = false;

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

            if (Helper.IsFullscreenMap || Helper.IsOverlayMap)
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
