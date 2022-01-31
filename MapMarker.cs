using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public class MapMarker
    {
        public Point Position;
        public Item Item;
        public string Name;
        public bool BrandNew;

        public MapMarker(string name, Point position, Item item)
        {
            Position = position;
            Item = item;
            Name = name;
        }

        public TagCompound GetData()
        {
            TagCompound tag = new TagCompound();
            tag["x"] = Position.X;
            tag["y"] = Position.Y;
            tag["item"] = ItemIO.Save(Item);
            tag["name"] = Name;
            return tag;
        }

        public static MapMarker FromData(TagCompound data)
        {
            Item item = new Item();
            object i = data["item"];
            if (i is int id)
                item.SetDefaults(id);
            else if (i is TagCompound tag)
                ItemIO.Load(item, tag);

            return new MapMarker(data.GetString("name"), new Point(data.GetInt("x"), data.GetInt("y")), item);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Item.type ^ Name.GetHashCode();
        }
    }
}
