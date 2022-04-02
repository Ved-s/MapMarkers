using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace MapMarkers.Buffs
{
    public class TPDisability : ModBuff
    {
        public static int BuffType => ModContent.BuffType<TPDisability>();

        public override void SetDefaults()
        {
            Main.debuff[Type] = true;
        }
    }
}
