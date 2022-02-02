using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
        public ServerMarkerData ServerData;

        public bool IsServerSide => ServerData != null;

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
            if (ServerData != null) tag["server"] = ServerData.GetData();
            return tag;
        }

        public static MapMarker Read(BinaryReader reader, Guid id) 
        {
            string name = reader.ReadString();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int itemType = reader.ReadInt32();

            Item item = new Item();
            item.SetDefaults(itemType);

            MapMarker m = new MapMarker(name, new Point(x,y), item);
            m.ServerData = new ServerMarkerData();
            m.ServerData.Id = id;
            m.ServerData.Owner = reader.ReadString();
            m.ServerData.PublicEdit = reader.ReadBoolean();

            return m;
        }
        public void Write(BinaryWriter writer) 
        {
            writer.Write(Name);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Item.type);
            writer.Write(ServerData.Owner);
            writer.Write(ServerData.PublicEdit);
        }

        public static MapMarker FromData(TagCompound data) 
        {
            Item item = new Item();
            object i = data["item"];
            if (i is int id)
                item.SetDefaults(id);
            else if (i is TagCompound tag)
                ItemIO.Load(item, tag);

            TagCompound server = null;
            ServerMarkerData smd = null;
            data.TryLoad("server", ref server);
            if (server != null) 
            {
                smd = ServerMarkerData.FromData(server);
            }

            return new MapMarker(data.GetString("name"), new Point(data.GetInt("x"), data.GetInt("y")), item) 
            {
                ServerData = smd
            };
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Item.type ^ Name.GetHashCode();
        }
    }
    public class ServerMarkerData
    {
        public string Owner;
        public bool PublicEdit;
        public Guid Id;

        public TagCompound GetData()
        {
            TagCompound tag = new TagCompound();
            tag["owner"] = Owner;
            tag["edit"] = PublicEdit;
            tag["id"] = Id.ToString();
            return tag;
        }

        public static ServerMarkerData FromData(TagCompound data)
        {
            ServerMarkerData m = new ServerMarkerData();
            data.TryLoad("owner", ref m.Owner);
            data.TryLoad("edit", ref m.PublicEdit);
            string id = null;
            data.TryLoad("id", ref id);
            if (id != null) m.Id = Guid.Parse(id);

            return m;
        }
    }
}
