using MapMarkers.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MapMarkers.Items
{
    public class MarkerTPPotion : ModItem
    {
        public static int ItemType => ModContent.ItemType<MarkerTPPotion>();

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault(Language.GetTextValue("Mods.MapMarkes.ItemTooltip.MarkerTPPotion"));
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 30;
            Item.maxStack = 30;
            Item.value = Item.sellPrice(silver: 3);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddConsumeItemCallback(Recipe.ConsumptionRules.Alchemy)

                .AddIngredient(ItemID.BottledWater)
                .AddIngredient(ItemID.SpecularFish)
                .AddIngredient(ItemID.Blinkroot, 2)
                .AddIngredient(ItemID.Moonglow)

                .AddTile(TileID.Bottles)

                .Register();
        }
    }
}
