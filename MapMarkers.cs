using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace MapMarkers
{
	public class MapMarkers : Mod
	{
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            if (Main.dedServ) ModContent.GetInstance<Net.MapServer>().HandlePacket(reader, whoAmI);
            else Net.MapClient.HandlePacket(reader);
        }
    }
}