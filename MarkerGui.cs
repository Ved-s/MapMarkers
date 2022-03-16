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
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace MapMarkers
{
    public class MarkerGui
    {
        UIPanel Main;
        UIFocusInputTextField Name, Search;

        UIAutoScaleTextTextPanel<string> Header;
        UIAutoScaleTextTextPanel<string> Ok, Cancel, Reload, Global, Edit;

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

        private Color Inactive = new Color(63, 82, 151) * 0.7f;
        private Color InactiveHover = new Color(63, 82, 151);
        private Color Active = new Color(0, 0xDD, 0);
        private Color ActiveHover = new Color(0, 0xFF, 0);

        internal void InitUI()
        {
            UI.CurrentState.RemoveAllChildren();
            UI.CurrentState.Append(Main = new UIPanel());

            Main.Width = new StyleDimension(300, 0);
            Main.Height = new StyleDimension(450, 0);
            Main.Top = new StyleDimension(Terraria.Main.screenHeight / 2 - 250, 0);
            Main.Left = new StyleDimension(Terraria.Main.screenWidth / 2 - 150, 0);

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

            Main.Append(Header = new UIAutoScaleTextTextPanel<string>("Edit marker"));
            Header.Top.Set(-30, 0);
            Header.Left.Set(60, 0);
            Header.Width.Set(160, 0);
            Header.Height.Set(40, 0);
            Header.TextScale = 2f;
            Header.BackgroundColor = new Color(63, 82, 151);

            Main.Append(Name = new UIFocusInputTextField("Marker name"));
            Name.Top.Set(20, 0);
            Name.Left.Set(20, 0);
            Name.Width.Set(-40, 1);
            Name.Height.Set(30, 0);
            Name.OnTextChange += (s, e) =>
            {
                Net.MapClient.SetName(Marker, Name.CurrentString);
            };

            Main.Append(Search = new UIFocusInputTextField("Search items"));
            Search.Top.Set(130, 0);
            Search.Left.Set(70, 0);
            Search.Width.Set(185, 0);
            Search.Height.Set(30, 0);
            Search.OnTextChange += (s, e) =>
            {
                ReloadItems();
            };

            UI.CurrentState.Append(Ok = new UIAutoScaleTextTextPanel<string>("Ok"));
            Ok.Top.Set(Main.Top.Pixels + 465, 0);
            Ok.Left.Set(Main.Left.Pixels, 0);
            Ok.Width.Set(100, 0);
            Ok.Height.Set(40, 0);
            Ok.OnClick += (ev, ui) =>
            {
                Marker.BrandNew = false;
                Marker = null;
                Terraria.Main.blockInput = false;
            };

#if DEBUG
            UI.CurrentState.Append(Reload = new UIAutoScaleTextTextPanel<string>("Reload UI"));
            Reload.Top.Set(Main.Top.Pixels + 420, 0);
            Reload.Left.Set(Main.Left.Pixels + 320, 0);
            Reload.Width.Set(100, 0);
            Reload.Height.Set(30, 0);
            Reload.BackgroundColor = Color.Yellow;
            Reload.OnClick += (ev, ui) =>
            {
                InitUI();
            };
#endif

            UI.CurrentState.Append(Cancel = new UIAutoScaleTextTextPanel<string>("Cancel"));
            Cancel.Top.Set(Main.Top.Pixels + 465, 0);
            Cancel.Left.Set(Main.Left.Pixels + 200, 0);
            Cancel.Width.Set(100, 0);
            Cancel.Height.Set(40, 0);
            Cancel.OnClick += (ev, ui) =>
            {
                if (Marker.BrandNew)
                    MapSystem.CurrentMarkers.Remove(Marker);
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
            if (Global != null)
                Main.RemoveChild(Global);
            if (Edit != null)
                Main.RemoveChild(Edit);

            if ((Terraria.Main.netMode == NetmodeID.MultiplayerClient || Net.MapClient.CanMakeGlobal)
                && Marker.ServerData != null
                && Marker.ServerData.Owner != Terraria.Main.LocalPlayer.name) return;

            Main.Append(Global = new UIAutoScaleTextTextPanel<string>("Global"));
            Global.Top.Set(60, 0);
            Global.Left.Set(20, 0);
            Global.Width.Set(100, 0);
            Global.Height.Set(30, 0);
            Global.BackgroundColor = Marker.IsServerSide ? Active : Inactive;
            Global.OnClick += (ev, ui) =>
            {
                Marker.BrandNew = false;
                Net.MapClient.SetGlobal(Marker, !Marker.IsServerSide);
                InitNetUI();
            };
            Hover(Global, Marker.IsServerSide ? ActiveHover : InactiveHover, Marker.IsServerSide ? Active : Inactive);

            if (!Marker.IsServerSide) return;

            Main.Append(Edit = new UIAutoScaleTextTextPanel<string>("Public edit"));
            Edit.Top.Set(60, 0);
            Edit.Left.Set(130, 0);
            Edit.Width.Set(127, 0);
            Edit.Height.Set(30, 0);
            Edit.BackgroundColor = Marker.ServerData.PublicEdit ? Active : Inactive;
            Edit.OnClick += (ev, ui) =>
            {
                Net.MapClient.SetGlobalEdit(Marker, !Marker.ServerData.PublicEdit);
                InitNetUI();
            };
            Hover(Edit, Marker.ServerData.PublicEdit ? ActiveHover : InactiveHover, Marker.ServerData.PublicEdit ? Active : Inactive);
        }

        private void Hover(UIPanel e, Color? hovered = null, Color? normal = null)
        {
            hovered = hovered ?? new Color(63, 82, 151);
            normal = normal ?? new Color(63, 82, 151) * 0.7f;

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

            if (Main == null) InitUI();

            Main.Top = new StyleDimension(Terraria.Main.screenHeight / Terraria.Main.UIScale / 2 - 250, 0);
            Main.Left = new StyleDimension(Terraria.Main.screenWidth / Terraria.Main.UIScale / 2 - 150, 0);

            Ok.Top.Set(Main.Top.Pixels + 465, 0);
            Ok.Left.Set(Main.Left.Pixels, 0);

            Cancel.Top.Set(Main.Top.Pixels + 465, 0);
            Cancel.Left.Set(Main.Left.Pixels + 200, 0);

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

                float scale = Terraria.Main.inventoryScale;
                Terraria.Main.inventoryScale = 0.8f;

                Vector2 pos = new Vector2(cs.X, cs.Y);
                pos += new Vector2(32, 134);
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
