using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using ReLogic.Content;
using Terraria.ID;
using Terraria.ModLoader.IO;
using MapMarkers.Structures;
using System.Collections.Generic;
using MapMarkers.UI;

namespace MapMarkers.Markers
{
    public class PlacedMarker : MapMarker
    {
        public Item DisplayItem
        {
            get => displayItem;
            set { displayItem = value; ItemTextureCache = null; }
        }

        public override SaveLocation SaveLocation => SaveLocation.Client;
        public override Vector2 Size => ItemFrame.Size();

        int ItemType => DisplayItem.type <= ItemID.None ? ItemID.TrifoldMap : DisplayItem.type;
        Texture2D ItemTexture
        {
            get
            {
                if (ItemTextureCache is not null)
                    return ItemTextureCache;

                int itemType = ItemType;

                var asset = TextureAssets.Item[itemType];

                if (asset.State == AssetState.NotLoaded)
                {
                    if (itemType > ItemID.Count)
                        asset = ModContent.Request<Texture2D>(asset.Name, AssetRequestMode.ImmediateLoad);
                    else
                        asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.ImmediateLoad);
                }
                else if (asset.State == AssetState.Loading)
                    asset.Wait();

                return ItemTextureCache = asset.Value;
            }
        }

        Rectangle ItemFrame => Main.itemAnimations[ItemType] is null ?
            ItemTexture.Frame(1, 1, 0, 0, 0, 0)
            : Main.itemAnimations[ItemType].GetFrame(ItemTexture, -1);

        Texture2D? ItemTextureCache;
        private Item displayItem = new();

        public override void Draw()
        {
            Main.spriteBatch.Draw(ItemTexture, ScreenRect, ItemFrame, Color.White);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["name"] = DisplayName;
            tag["item"] = ItemIO.Save(DisplayItem);
            tag["pos"] = Position;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("name", out string name))
                DisplayName = name;
            if (tag.TryGet("item", out TagCompound item))
                DisplayItem = ItemIO.Load(item);
            if (tag.TryGet("pos", out Vector2 pos))
                Position = pos;
        }

        public override IEnumerable<MarkerMenu.MenuItemDefinition> GetMenuItems()
        {
            yield return new MarkerMenu.MenuItemDefinition("Edit", "Edit this marker", () => MarkerEditMenu.Show(this));
        }
    }
}
