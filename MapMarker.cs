using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public abstract class AbstractMarker
    {
        public virtual Point Position { get; set; }
        public virtual string Name { get; set; }
        public virtual float MinZoom => 0f;
        public virtual bool Active => true;

        public abstract Vector2 Size { get; }

        public virtual bool CanDrag => false;
        public virtual bool CanTeleport => CanTeleportDefault();
        public virtual bool ShowPos => true;

        public abstract void Draw(Vector2 screenPos);
        public virtual void Hover(StringBuilder mouseText) { }

        protected bool CanTeleportDefault() 
        {
            if (!Active) 
                return false;

            return Main.Map[Position.X, Position.Y].Light > 40;
        }
    }

    public class StatueMarker : AbstractMarker
    {
        private int Item;

        public override float MinZoom => 1f;
        public override string Name => Lang.GetItemNameValue(Item);
        public override bool Active => Main.Map[Position.X, Position.Y].Light > 40;

        public override Vector2 Size => Main.itemTexture[Item].Size();

        public StatueMarker(int item, int x, int y)
        {
            Item = item;
            Position = new Point(x + 1, y + 2);
        }

        public override void Draw(Vector2 screenPos)
        {
            if (Main.tile[Position.X, Position.Y].type != TileID.Statues)
            {
                ModContent.GetInstance<MapMarkers>().CurrentMarkers.Remove(this);
                return;
            }

            screenPos.Y -= 8;
            Main.spriteBatch.Draw(Main.itemTexture[Item], screenPos, Color.White * MapHelper.MapAlpha);
        }
    }
    public class LockedChestMarker : AbstractMarker
    {
        public override float MinZoom => 1f;

        public override bool Active
        {
            get
            {
                Chest ch = Main.chest[Chest];
                return Main.Map[ch.x, ch.y].Light > 40;
            }
        }

        public override string Name
        {
            get
            {
                Chest ch = Main.chest[Chest];

                int type = Main.Map[ch.x, ch.y].Type;
                int chest1lookup = Terraria.Map.MapHelper.tileLookup[21];
                int chest1count = Terraria.Map.MapHelper.tileOptionCounts[21];
                int chest2lookup = Terraria.Map.MapHelper.tileLookup[467];
                int chest2count = Terraria.Map.MapHelper.tileOptionCounts[467];

                Tile tile = Main.tile[ch.x, ch.y];
                if (tile is null) return "";

                LocalizedText[] chestType = null;

                if (type >= chest1lookup && type < chest1lookup + chest1count)
                {
                    chestType = Lang.chestType;
                }
                else if (type >= chest2lookup && type < chest2lookup + chest2count)
                {
                    chestType = Lang.chestType2;
                }
                else return "";

                if (Chest < 0)
                {
                    return chestType[0].Value;
                }
                return chestType[tile.frameX / 36].Value;
            }
        }
        public override Point Position
        {
            get
            {
                Chest ch = Main.chest[Chest];
                return new Point(ch.x + 1, ch.y + 1);
            }
        }

        private int Chest;

        public LockedChestMarker(int chest)
        {
            Chest = chest;
            Chest ch = Main.chest[Chest];
        }

        public override Vector2 Size => new Vector2(32, 32);

        public override void Draw(Vector2 screenPos)
        {
            if (!Terraria.Chest.isLocked(Position.X, Position.Y))
            {
                ModContent.GetInstance<MapMarkers>().CurrentMarkers.Remove(this);
                return;
            }

            Chest ch = Main.chest[Chest];
            if (Main.tileTexture[Main.tile[ch.x, ch.y].type] is null) return;

            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                {
                    Tile t = Main.tile[ch.x + x, ch.y + y];

                    Vector2 pos = screenPos + new Vector2(x * 16, y * 16);
                    Rectangle source = new Rectangle(t.frameX, t.frameY, 16, 16);

                    Main.spriteBatch.Draw(Main.tileTexture[t.type], pos, source, Color.White * MapHelper.MapAlpha);
                }
        }
    }

    public class SpawnMarker : AbstractMarker
    {
        public override string Name => "Spawn";
        public override Point Position => new Point(Main.spawnTileX, Main.spawnTileY);

        public override Vector2 Size => Main.itemTexture[Terraria.ID.ItemID.Acorn].Size();

        public override void Draw(Vector2 screenPos)
        {
            Main.spriteBatch.Draw(Main.itemTexture[Terraria.ID.ItemID.Acorn], screenPos, Color.White * MapHelper.MapAlpha);
        }
    }

    public class MapMarker : AbstractMarker
    {
        public Item Item;
        public bool BrandNew;
        public ServerMarkerData ServerData;

        public bool IsServerSide => ServerData != null;

        public override Vector2 Size => Main.itemTexture[Item.type].Size();

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

            MapMarker m = new MapMarker(name, new Point(x, y), item);
            m.ServerData = new ServerMarkerData();
            m.ServerData.Id = id;
            m.ServerData.Owner = reader.ReadString();
            m.ServerData.PublicPerms = (MarkerPerms)reader.ReadInt32();

            return m;
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Item.type);
            writer.Write(ServerData.Owner);
            writer.Write((int)ServerData.PublicPerms);
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

        public override void Draw(Vector2 screenPos)
        {
            Main.spriteBatch.Draw(Main.itemTexture[Item.type], screenPos, Color.White * MapHelper.MapAlpha);
        }
    }
    public class ServerMarkerData
    {
        private const string OwnerDataKey = "owner";
        private const string PubEditDataKey = "edit";
        private const string PubPermDataKey = "perms";
        private const string IdDataKey = "id";

        public string Owner;
        public MarkerPerms PublicPerms = MarkerPerms.None;
        public Guid Id;

        public TagCompound GetData()
        {
            TagCompound tag = new TagCompound();
            tag[OwnerDataKey]   = Owner;
            tag[PubPermDataKey] = (int)PublicPerms;
            tag[IdDataKey]      = Id.ToString();
            return tag;
        }

        public static ServerMarkerData FromData(TagCompound data)
        {
            ServerMarkerData m = new ServerMarkerData();
            data.TryLoad(OwnerDataKey, ref m.Owner);

            if (data.ContainsKey(PubEditDataKey) && data.GetBool(PubEditDataKey))
            {
                m.PublicPerms = MarkerPerms.Edit;
            }
            else if (data.ContainsKey(PubPermDataKey)) 
            {
                m.PublicPerms = (MarkerPerms)data.GetInt(PubPermDataKey);
            }

            string id = null;
            data.TryLoad(IdDataKey, ref id);
            if (id != null) m.Id = Guid.Parse(id);

            return m;
        }
    }

    [Flags]
    public enum MarkerPerms 
    {
        None = 0, 
        Edit = 1, 
        Delete = 2
    }
}
