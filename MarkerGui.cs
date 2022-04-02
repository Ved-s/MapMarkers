using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace MapMarkers
{
    public class MarkerGui
    {
        UIPanel Main;
        UIFocusInputTextField Name, Search;

        UIAutoScaleTextTextPanel<string> Ok, Cancel, Reload, Global, Edit, Delete;

        List<Item> Items = new List<Item>();

        public UserInterface UI;

        public MapMarker Marker;

        private MapSystem MapSystem;

        public MarkerGui(MapSystem system)
        {
            MapSystem = system;

            UI = new UserInterface();
            UI.IsVisible = false;

            UI.SetState(new UIState());
        }

        private Color Inactive = new Color(64, 64, 100) * 0.9f;
        private Color InactiveHover = new Color(72, 72, 110) * 0.9f;

        private Color ActiveTextColor = new(100, 180, 100);

        public void InitUI()
        {
            UI.CurrentState.RemoveAllChildren();
            UI.CurrentState.Append(Main = new UIPanel());

            Main.Width = new StyleDimension(620, 0);
            Main.Height = new StyleDimension(340, 0);
            Main.Top = new StyleDimension((Terraria.Main.screenHeight - Main.Height.Pixels) / 2, 0);
            Main.Left = new StyleDimension((Terraria.Main.screenWidth - Main.Width.Pixels) / 2, 0);
            Main.BackgroundColor = new Color(32,32,48) * .7f;

            Main.Append(Name = new UIFocusInputTextField(Language.GetTextValue("Mods.MapMarkers.GUI.MarkerName")));
            Name.Top.Set(20, 0);
            Name.Left.Set(20, 0);
            Name.Width.Set(-40, 0.5f);
            Name.Height.Set(30, 0);
            Name.BackgroundColor = InactiveHover;
            
            Main.Append(Search = new UIFocusInputTextField(Language.GetTextValue("Mods.MapMarkers.GUI.Search")));
            Search.Top.Set(20, 0);
            Search.Left.Set(-203, 1);
            Search.Width.Set(189, 0);
            Search.Height.Set(30, 0);
            Search.BackgroundColor = InactiveHover;
            
            Main.Append(Ok = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.MapMarkers.GUI.Ok")));
            Ok.Top.Set(-54, 1);
            Ok.Left.Set(20, 0);
            Ok.Width.Set(100, 0);
            Ok.Height.Set(40, 0);
            Ok.BackgroundColor = Inactive;

            Main.Append(Cancel = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.MapMarkers.GUI.Cancel")));
            Cancel.Top.Set(-54, 1);
            Cancel.Left.Set(-120, 0.5f);
            Cancel.Width.Set(100, 0);
            Cancel.Height.Set(40, 0);
            Cancel.BackgroundColor = Inactive;

#if DEBUG
            UI.CurrentState.Append(Reload = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.MapMarkers.GUI.Reload")));
            Reload.Top.Set(Main.Top.Pixels - 32, 0);
            Reload.Left.Set(Main.Left.Pixels + Main.Width.Pixels - 100, 0);
            Reload.Width.Set(100, 0);
            Reload.Height.Set(30, 0);
            Reload.BackgroundColor = Color.Yellow;
            Reload.OnClick += (ev, ui) =>
            {
                InitUI();
            };
#endif
            Main.OnClick += (ev, ui) =>
            {
                foreach (Item item in Items)
                {
                    Rectangle rect = new Rectangle((int)item.position.X, (int)item.position.Y, (int)(TextureAssets.InventoryBack.Value.Width * 0.8f), (int)(TextureAssets.InventoryBack.Value.Height * 0.8f));
                    if (rect.Contains(Terraria.Main.MouseScreen.ToPoint()))
                    {
                        Net.MapClient.SetItem(Marker, item);
                    }
                }
            };

            Name.OnTextChange += (s, e) =>
            {
                Net.MapClient.SetName(Marker, Name.CurrentString);
            };
            Search.OnTextChange += (s, e) =>
            {
                ReloadItems();
            };

            Ok.OnClick += (ev, ui) =>
            {
                Marker.BrandNew = false;
                Marker = null;
                Terraria.Main.blockInput = false;
            };
            Cancel.OnClick += (ev, ui) =>
            {
                if (Marker.BrandNew)
                    MapSystem.CurrentPlayerWorldData.Markers.Remove(Marker);
                Marker = null;
                Terraria.Main.blockInput = false;
            };

            InitNetUI();

            Hover(Ok);
            Hover(Cancel);

            ReloadItems();
        }

        private void InitNetUI()
        {
            if (Global != null) Main.RemoveChild(Global);
            if (Edit != null) Main.RemoveChild(Edit);
            if (Delete != null) Main.RemoveChild(Delete);

            if ((Terraria.Main.netMode == NetmodeID.MultiplayerClient || Net.MapClient.CanMakeGlobal)
                && Marker.ServerData != null
                && Marker.ServerData.Owner != Terraria.Main.LocalPlayer.name) return;

            Main.Append(Global = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.MapMarkers.GUI.Global")));
            Global.Top.Set(80, 0);
            Global.Left.Set(20, 0);
            Global.Width.Set(100, 0);
            Global.Height.Set(25, 0);
            Global.BackgroundColor = Inactive;
            Global.TextColor = Marker.IsServerSide ? ActiveTextColor : Color.White;
            Global.OnClick += (ev, ui) =>
            {
                Marker.BrandNew = false;
                Net.MapClient.SetGlobal(Marker, !Marker.IsServerSide);
                InitNetUI();
            };
            Hover(Global);

            if (!Marker.IsServerSide) return;

            bool edit = Marker.ServerData.PublicPerms.HasFlag(MarkerPerms.Edit);
            Main.Append(Edit = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.MapMarkers.GUI.Edit")));
            Edit.Top.Set(80, 0);
            Edit.Left.Set(130, 0);
            Edit.Width.Set(-150, 0.5f);
            Edit.Height.Set(25, 0);
            Edit.BackgroundColor = Inactive;
            Edit.TextColor = edit ? ActiveTextColor : Color.White;

            bool delete = Marker.ServerData.PublicPerms.HasFlag(MarkerPerms.Delete);
            Main.Append(Delete = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.MapMarkers.GUI.Delete")));
            Delete.Top.Set(110, 0);
            Delete.Left.Set(130, 0);
            Delete.Width.Set(-150, 0.5f);
            Delete.Height.Set(25, 0);
            Delete.BackgroundColor = Inactive;
            Delete.TextColor = delete ? ActiveTextColor : Color.White;

            Edit.OnClick += (ev, ui) =>
            {
                MarkerPerms perms = Marker.ServerData.PublicPerms;
                perms ^= MarkerPerms.Edit;

                Net.MapClient.SetPublicPerms(Marker, perms);
                InitNetUI();
            };
            Delete.OnClick += (ev, ui) =>
            {
                MarkerPerms perms = Marker.ServerData.PublicPerms;
                perms ^= MarkerPerms.Delete;

                Net.MapClient.SetPublicPerms(Marker, perms);
                InitNetUI();
            };

            Hover(Edit);
            Hover(Delete);
        }

        private void Hover(UIPanel e, Color? hovered = null, Color? normal = null)
        {
            hovered = hovered ?? InactiveHover;
            normal = normal ?? Inactive;

            e.OnMouseOver += (a, b) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                e.BackgroundColor = hovered.Value;
            };

            e.OnMouseOut += (a, b) =>
            {
                e.BackgroundColor = normal.Value;
            };
        }

        private void ReloadItems()
        {
            Items.Clear();

            string search = Search.CurrentString.ToLower();

            for (int i = 1; i < ItemLoader.ItemCount; i++)
            {
                if (Items.Count >= 25) break;

                if (ItemID.Sets.Deprecated[i]) continue;

                Item item = new Item();
                item.SetDefaults(i);

                if (item.Name.ToLower().Contains(search))
                    Items.Add(item);
            }
        }

        public void SetMarker(MapMarker m)
        {
            Marker = m;

            if (m == null)
            {
                Terraria.Main.blockInput = false;
                return;
            }

            if (Main == null) 
                InitUI();

            Main.Top = new StyleDimension(Terraria.Main.screenHeight / Terraria.Main.UIScale / 2 - Main.Height.Pixels / 2, 0);
            Main.Left = new StyleDimension(Terraria.Main.screenWidth / Terraria.Main.UIScale / 2 - Main.Width.Pixels / 2, 0);

            //Ok.Top.Set(Main.Top.Pixels + 465, 0);
            //Ok.Left.Set(Main.Left.Pixels, 0);
            //
            //Cancel.Top.Set(Main.Top.Pixels + 465, 0);
            //Cancel.Left.Set(Main.Left.Pixels + 200, 0);

            Terraria.Main.blockInput = true;

            UpdateData();
        }

        public void UpdateData()
        {
            Name.SetText(Marker.Name);
            InitNetUI();
        }

        internal bool Draw()
        {
            if (Marker is not null)
            {
                if (Main is null) InitUI();

                UI.Draw(Terraria.Main.spriteBatch, null);

                CalculatedStyle cs = Main.GetDimensions();

                string header = Language.GetTextValue("Mods.MapMarkers.GUI.Header");
                Vector2 headerSize = FontAssets.MouseText.Value.MeasureString(header) * 1.3f;

                Vector2 headerPos = new(cs.X, cs.Y);
                headerPos.X += cs.Width / 2 - headerSize.X / 2;
                headerPos.Y -= headerSize.Y;

                Utils.DrawBorderString(Terraria.Main.spriteBatch, header, headerPos, Color.White, 1.3f);

                Vector2 hintPos = new(cs.X + 30, cs.Y + 160);

                if (Global?.IsMouseHovering ?? false) 
                    Utils.DrawBorderString(Terraria.Main.spriteBatch, Language.GetTextValue("Mods.MapMarkers.GUI.GlobalHint"), hintPos, Color.White);
                else if (Edit?.IsMouseHovering ?? false) 
                    Utils.DrawBorderString(Terraria.Main.spriteBatch, Language.GetTextValue("Mods.MapMarkers.GUI.EditHint"), hintPos, Color.White);
                else if (Delete?.IsMouseHovering ?? false) 
                    Utils.DrawBorderString(Terraria.Main.spriteBatch, Language.GetTextValue("Mods.MapMarkers.GUI.DeleteHint"), hintPos, Color.White);

                float scale = Terraria.Main.inventoryScale;
                Terraria.Main.inventoryScale = 0.8f;

                Vector2 pos = new Vector2(cs.X, cs.Y);
                pos += new Vector2(cs.Width - 264, 26);
                ItemSlot.Draw(Terraria.Main.spriteBatch, ref Marker.Item, ItemSlot.Context.InventoryCoin, pos);

                for (int i = 0; i < Items.Count; i++)
                {
                    Item item = Items[i];
                    Vector2 itemPos = pos + new Vector2((i % 5) * 49, (i / 5) * 49 + 50);
                    item.position = itemPos;
                    ItemSlot.Draw(Terraria.Main.spriteBatch, ref item, ItemSlot.Context.InventoryCoin, itemPos);
                }

                Terraria.Main.inventoryScale = scale;
            }
            return true;
        }

        internal void Update(GameTime gameTime)
        {
            if (Marker is not null)
            {
                UI.Update(gameTime);
            }
        }
    }
}
