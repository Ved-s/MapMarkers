using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace MapMarkers.Structures
{
    public class UIItemDisplay : UIElement
    {
        public Item? Item { get; set; }
        public Asset<Texture2D> BackTexture = TextureAssets.InventoryBack;

        public Color BackColor = Color.White;
        public Color ItemColor = Color.White;

        public float ItemScale = 1f;

        private bool SetInputBlocking = false;

        public UIItemDisplay()
        {
            Width = new(48, 0);
            Height = new(48, 0);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            if (!SetInputBlocking && !Main.blockInput)
            {
                Main.blockInput = true;
                SetInputBlocking = true;
            }
            base.MouseOver(evt);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            if (SetInputBlocking)
            {
                SetInputBlocking = false;
                Main.blockInput = false;
            }
            base.MouseOut(evt);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dim = GetDimensions();

            Main.spriteBatch.Draw(BackTexture.Value, dim.ToRectangle(), BackColor);

            Vector2 maxitemSize = new(dim.Width - 12, dim.Height - 12);

            Item? item = Item;
            if (item is null || item.IsAir)
                return;

            if (IsMouseHovering)
            {
                Main.hoverItemName = item.HoverName;
                Main.HoverItem = item;
            }

            float inventoryScale = 0.9f;

            Main.instance.LoadItem(item.type);
            Texture2D value6 = TextureAssets.Item[item.type].Value;
            Rectangle frame = ((Main.itemAnimations[item.type] == null) ? value6.Frame() : Main.itemAnimations[item.type].GetFrame(value6));
            Color currentColor = ItemColor;
            float scale = ItemScale * inventoryScale;
            ItemSlot.GetItemLight(ref currentColor, ref scale, item);

            Vector2 scaledSize = frame.Size() * scale;
            if (scaledSize.X > maxitemSize.X || scaledSize.Y > maxitemSize.Y)
            {
                scale *= Math.Min(maxitemSize.X / scaledSize.X, maxitemSize.Y / scaledSize.Y);
                scaledSize = frame.Size() * scale;
            }

            Vector2 drawPosition = dim.Position() + (dim.ToRectangle().Size() - scaledSize) / 2;

            Vector2 origin = Vector2.Zero;

            if (!ItemLoader.PreDrawInInventory(item, spriteBatch, drawPosition, frame, item.GetAlpha(currentColor), item.GetColor(currentColor), origin, scale))
                return;

            spriteBatch.Draw(value6, drawPosition, frame, item.GetAlpha(currentColor), 0f, origin, scale, SpriteEffects.None, 0f);
            if (item.color != Color.Transparent)
            {
                spriteBatch.Draw(value6, drawPosition, frame, item.GetColor(currentColor), 0f, origin, scale, SpriteEffects.None, 0f);
            }

            ItemLoader.PostDrawInInventory(item, spriteBatch, drawPosition, frame, item.GetAlpha(currentColor), item.GetColor(currentColor), origin, scale);

            if (item.stack > 1)
            {
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, item.stack.ToString(), dim.Position() + new Vector2(10f, 26f) * inventoryScale, Color.White, 0f, Vector2.Zero, new Vector2(inventoryScale), -1f, inventoryScale);
            }
        }
    }
}
