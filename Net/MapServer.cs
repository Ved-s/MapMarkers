using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers.Net
{
    public class MapServer : ModWorld
    {
        public List<MapMarker> Markers = new List<MapMarker>();

        public void HandlePacket(BinaryReader reader, int whoAmI)
        {
            PacketMessageType msgType = (PacketMessageType)reader.ReadByte();
            switch (msgType)
            {
                case PacketMessageType.Sync:
                    Sync(reader, whoAmI);
                    break;
                case PacketMessageType.RequestMarkers:
                    ModPacket packet = mod.GetPacket();
                    packet.Write((byte)PacketMessageType.RequestMarkers);
                    packet.Write(Markers.Count);
                    Console.WriteLine($"[Map Markers] Sending {Markers.Count} markers to {Main.player[whoAmI].name}");
                    foreach (MapMarker m in Markers) 
                    {
                        packet.Write(m.ServerData.Id.ToByteArray());
                        m.Write(packet);
                    }
                    packet.Send(whoAmI);
                    break;
            }
        }

        private void Sync(BinaryReader reader, int whoAmI)
        {
            byte[] guid = reader.ReadBytes(16);
            Guid id = new Guid(guid);

            SyncMessageType type = (SyncMessageType)reader.ReadByte();
            long pos = reader.BaseStream.Position;
            bool redirect = true;

            MapMarker marker = Markers.FirstOrDefault(m => m.ServerData.Id == id);

            Console.WriteLine(string.Format("[Map Markers] Received message {0}, marker {1}", type, marker?.Name ?? "null"));

            switch (type)
            {
                case SyncMessageType.Add:
                    marker = MapMarker.Read(reader, id);
                    Markers.Add(marker);
                    break;
                case SyncMessageType.Remove:
                    if (marker.ServerData.Owner != Main.player[whoAmI].name) return;
                    Markers.Remove(marker);
                    break;
                case SyncMessageType.UpdateName:
                    if (!AllowEdit(marker, whoAmI)) { DisallowEditFor(marker, whoAmI); return; }
                    marker.Name = reader.ReadString();
                    break;
                case SyncMessageType.UpdatePos:
                    if (!AllowEdit(marker, whoAmI)) { DisallowEditFor(marker, whoAmI); return; }
                    marker.Position.X = reader.ReadInt32();
                    marker.Position.Y = reader.ReadInt32();
                    break;
                case SyncMessageType.UpdateItem:
                    if (!AllowEdit(marker, whoAmI)) { DisallowEditFor(marker, whoAmI); return; }
                    Item item = new Item();
                    item.SetDefaults(reader.ReadInt32());
                    marker.Item = item;
                    break;
                case SyncMessageType.UpdateEdit:
                    if (marker.ServerData.Owner != Main.player[whoAmI].name) return;
                    marker.ServerData.PublicEdit = reader.ReadBoolean();
                    break;
            }

            long size = reader.BaseStream.Position - pos;
            if (redirect)
            {
                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
                byte[] data = reader.ReadBytes((int)size);

                ModPacket packet = CreateSyncPacket(marker, type);
                packet.Write(data);
                packet.Send(-1, whoAmI);
            }
        }

        private void DisallowEditFor(MapMarker m, int whoAmI) 
        {
            ModPacket packet = CreateSyncPacket(m, SyncMessageType.UpdateEdit);
            packet.Write(false);
            packet.Send(whoAmI);
        }

        private ModPacket CreateSyncPacket(MapMarker m, SyncMessageType type)
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)PacketMessageType.Sync);
            packet.Write(m.ServerData.Id.ToByteArray());
            packet.Write((byte)type);
            return packet;
        }
        public static bool AllowEdit(MapMarker m, int player)
        {
            if (m.ServerData == null) return true;

            if (m.ServerData.Owner == Main.player[player].name) return true;

            return m.ServerData.PublicEdit;
        }

        public override void Load(TagCompound tag)
        {
            if (tag.ContainsKey("markers")) 
            {
                foreach (TagCompound m in tag.GetList<TagCompound>("markers")) 
                {
                    Markers.Add(MapMarker.FromData(m));
                }
            }
        }

        public override TagCompound Save()
        {
            return new TagCompound()
            {
                ["markers"] = Markers.Select(m => m.GetData()).ToList()
            };
        }
    }

    public enum PacketMessageType : byte 
    {
        Sync,
        RequestMarkers
    }

    public enum SyncMessageType : byte
    {
        Add,
        Remove,
        UpdateName,
        UpdatePos,
        UpdateItem,
        UpdateEdit,
    }
}
