using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MapMarkers
{
    public class CommonGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.WormholePotion;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            switch (item.type)
            {
                case ItemID.WormholePotion:
                    tooltips.Add(new(Mod, "MapMarkersTPLine", "Allows teleporting to markers. Will not teleport to unexplored area."));
                    break;
            }
        }
    }
}
