using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using MapMarkers.Buffs;

namespace MapMarkers.Items
{
    public class MarkerTPPotion : ModItem
    {
        public static int ItemType => ModContent.ItemType<MarkerTPPotion>();

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 30;
            Item.maxStack = 30;
            Item.value = Item.sellPrice(silver: 3);
        }

        public static void UsedOnMarker(AbstractMarker m, Vector2 teleportPos)
        {
            Main.LocalPlayer.Teleport(teleportPos);
            NetMessage.SendData(MessageID.Teleport, -1, -1, null, 0, Main.LocalPlayer.whoAmI, teleportPos.X, teleportPos.Y, 1, 0, 0);

            if (MapPlayer.LocalPlayerHasTPPotion) 
            {
                Item[] bank = Main.LocalPlayer.inventory;

                switch (MapPlayer.LocalPlayerTPPotionBank) 
                {
                    case 1: bank = Main.LocalPlayer.bank?.item; break;
                    case 2: bank = Main.LocalPlayer.bank2?.item; break;
                    case 3: bank = Main.LocalPlayer.bank3?.item; break;
                }

                if (bank != null && bank.Length > MapPlayer.LocalPlayerTPPotionSlot) 
                {
                    Item i = bank[MapPlayer.LocalPlayerTPPotionSlot];
                    if (ItemLoader.ConsumeItem(i, Main.LocalPlayer) && i.stack > 0)
                    {
                        i.stack--;
                    }
                }

#if DEBUG
                int buffTime = 300;
#else
                int buffTime = 1800;
#endif

                Main.LocalPlayer.AddBuff(TPDisability.BuffType, buffTime);
            }
        }

        public override void AddRecipes()
        {
            Recipe r = CreateRecipe();

            r.AddIngredient(ItemID.BottledWater);
            r.AddIngredient(ItemID.SpecularFish);
            r.AddIngredient(ItemID.Blinkroot, 2);
            r.AddIngredient(ItemID.Moonglow);

            r.AddTile(TileID.Bottles);
            r.Register();
        }
    }
}
