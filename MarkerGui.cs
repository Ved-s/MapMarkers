using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
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
        UIAutoScaleTextTextPanel<string> Ok, Cancel, Reload;

        List<Item> Items = new List<Item>();

        public UserInterface UI;

        public MapMarker Marker;

        private MapMarkers MapMarkers;

        public MarkerGui(MapMarkers mod) 
        {
            MapMarkers = mod;

            UI = new UserInterface();
            UI.IsVisible = false;

            UI.SetState(new UIState());
        }

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
                foreach (Item i in Items)
                {
                    Rectangle rect = new Rectangle((int)i.position.X, (int)i.position.Y, Terraria.Main.inventoryBackTexture.Width, Terraria.Main.inventoryBackTexture.Height);
                    if (rect.Contains(Terraria.Main.MouseScreen.ToPoint()))
                    {
                        Marker.Item = i;
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
                Marker.Name = Name.CurrentString;
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
            };

            //UI.CurrentState.Append(Reload = new UIAutoScaleTextTextPanel<string>("Reload UI"));
            //Reload.Top.Set(Main.Top.Pixels + 420, 0);
            //Reload.Left.Set(Main.Left.Pixels + 320, 0);
            //Reload.Width.Set(100, 0);
            //Reload.Height.Set(30, 0);
            //Reload.BackgroundColor = Color.Yellow;
            //Reload.OnClick += (ev, ui) =>
            //{
            //    InitUI();
            //};

            UI.CurrentState.Append(Cancel = new UIAutoScaleTextTextPanel<string>("Cancel"));
            Cancel.Top.Set(Main.Top.Pixels + 465, 0);
            Cancel.Left.Set(Main.Left.Pixels + 200, 0);
            Cancel.Width.Set(100, 0);
            Cancel.Height.Set(40, 0);
            Cancel.OnClick += (ev, ui) =>
            {
                if (Marker.BrandNew)
                    MapMarkers.CurrentMarkers.Remove(Marker);
                Marker = null;
            };

            Hover(Ok);
            Hover(Cancel);

            ReloadItems();
        }

        private void Hover(UIPanel e, Color? hovered = null, Color? normal = null) 
        {
            hovered = hovered ?? new Color(63, 82, 151);
            normal = normal ?? new Color(63, 82, 151) * 0.7f;

            e.OnMouseOver += (a, b) =>
            {
                Terraria.Main.PlaySound(SoundID.MenuTick);
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

            for (int i = 1; i < Terraria.Main.itemTexture.Length; i++)
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

            Name.SetText(m.Name);
        }

        internal bool Draw()
        {
            Terraria.Main.blockInput = Marker != null;

            if (Marker != null)
            {
                if (Main == null) InitUI();

                UI.Draw(Terraria.Main.spriteBatch, null);

                CalculatedStyle cs =  Main.GetDimensions();

                Vector2 pos = new Vector2(cs.X, cs.Y);
                pos += new Vector2(32, 134) * Terraria.Main.UIScale;
                ItemSlot.Draw(Terraria.Main.spriteBatch, ref Marker.Item, ItemSlot.Context.InventoryCoin, pos);

                for (int i = 0; i < Items.Count; i++) 
                {
                    Item item = Items[i];
                    Vector2 itemPos = pos + new Vector2((i % 5) * 49, (i / 5) * 49 + 50);
                    item.position = itemPos;
                    ItemSlot.Draw(Terraria.Main.spriteBatch, ref item, ItemSlot.Context.InventoryCoin, itemPos);
                }
            }
            return true;
        }

        internal void Update(GameTime gameTime)
        {
            if (Marker != null)
            {
                UI.Update(gameTime);
            }


        }
    }
}
