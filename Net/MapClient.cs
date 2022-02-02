using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MapMarkers.Net
{
    public static class MapClient
    {
        public static MapSystem MapSystem => ModContent.GetInstance<MapSystem>();
        public static MapMarkers Mod => ModContent.GetInstance<MapMarkers>();

        public static bool CanMakeGlobal => Mod.IsNetSynced;

        public static void HandlePacket(BinaryReader reader)
        {
            PacketMessageType msgType = (PacketMessageType)reader.ReadByte();
            switch (msgType)
            {
                case PacketMessageType.Sync:
                    Sync(reader);
                    break;
                case PacketMessageType.RequestMarkers:
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        byte[] guid = reader.ReadBytes(16);
                        Guid id = new Guid(guid);
                        MapMarker m = MapMarker.Read(reader, id);

                        int index = MapSystem.CurrentMarkers.FindIndex(m => m.ServerData?.Id == id);
                        if (index != -1) MapSystem.CurrentMarkers.RemoveAt(index);

                        MapSystem.CurrentMarkers.Add(m);
                    }
                    break;
            }
        }

        private static void Sync(BinaryReader reader)
        {
            byte[] guid = reader.ReadBytes(16);
            Guid id = new Guid(guid);

            SyncMessageType type = (SyncMessageType)reader.ReadByte();

            MapMarker marker = null;

            if (type != SyncMessageType.Add)
            {
                marker = MapSystem.CurrentMarkers.FirstOrDefault(m => m.ServerData != null && m.ServerData.Id == id);
                if (marker == null) return;
            }

            bool updateUI = false;

            switch (type)
            {
                case SyncMessageType.Add:
                    marker = MapMarker.Read(reader, id);
                    MapSystem.CurrentMarkers.Add(marker);
                    break;
                case SyncMessageType.Remove:
                    MapSystem.CurrentMarkers.Remove(marker);
                    if (MapSystem.MarkerGui.Marker == marker)
                        MapSystem.MarkerGui.Marker = null;
                    break;
                case SyncMessageType.UpdateName:
                    marker.Name = reader.ReadString();
                    updateUI = true;
                    break;
                case SyncMessageType.UpdatePos:
                    marker.Position.X = reader.ReadInt32();
                    marker.Position.Y = reader.ReadInt32();
                    updateUI = true;
                    break;
                case SyncMessageType.UpdateItem:
                    Item item = new Item();
                    item.SetDefaults(reader.ReadInt32());
                    marker.Item = item;
                    break;
                case SyncMessageType.UpdateEdit:
                    marker.ServerData.PublicEdit = reader.ReadBoolean();
                    if (!marker.ServerData.PublicEdit && MapSystem.MarkerGui.Marker == marker)
                        MapSystem.MarkerGui.Marker = null;
                    break;
            }

            if (MapSystem.MarkerGui.Marker == marker && updateUI) MapSystem.MarkerGui.UpdateData();
        }

        private static ModPacket CreateSyncPacket(MapMarker m, SyncMessageType type)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)PacketMessageType.Sync);
            packet.Write(m.ServerData.Id.ToByteArray());
            packet.Write((byte)type);
            return packet;
        }
        public static bool AllowEdit(MapMarker m)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return true;
            if (m.ServerData == null) return true;

            if (m.ServerData.Owner == Main.LocalPlayer.name) return true;

            return m.ServerData.PublicEdit;
        }

        public static void RequestMarkers()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)PacketMessageType.RequestMarkers);
                packet.Send();
            }
            else MapSystem.CurrentMarkers.AddRange(ModContent.GetInstance<MapServer>().Markers);
        }

        public static void SetName(MapMarker m, string name)
        {
            if (m.Name == name) return;
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    m.Name = name;
                    break;
                case NetmodeID.MultiplayerClient:
                    if (!AllowEdit(m)) return;
                    m.Name = name;
                    if (!m.IsServerSide) return;
                    ModPacket packet = CreateSyncPacket(m, SyncMessageType.UpdateName);
                    packet.Write(name);
                    packet.Send();
                    break;
            }
        }
        public static void SetItem(MapMarker m, Item item)
        {
            if (m.Item.type == item.type) return;
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    m.Item = item;
                    break;
                case NetmodeID.MultiplayerClient:
                    if (!AllowEdit(m)) return;
                    m.Item = item;
                    if (!m.IsServerSide) return;
                    ModPacket packet = CreateSyncPacket(m, SyncMessageType.UpdateItem);
                    packet.Write(item.type);
                    packet.Send();
                    break;
            }
        }
        public static void SetPos(MapMarker m, Point pos)
        {
            if (m.Position == pos) return;
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    m.Position = pos;
                    break;
                case NetmodeID.MultiplayerClient:
                    if (!AllowEdit(m)) return;
                    m.Position = pos;
                    if (!m.IsServerSide) return;
                    ModPacket packet = CreateSyncPacket(m, SyncMessageType.UpdatePos);
                    packet.Write(pos.X);
                    packet.Write(pos.Y);
                    packet.Send();
                    break;
            }
        }
        public static void SetGlobal(MapMarker m, bool global)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient && m.ServerData != null && m.ServerData.Owner != Main.LocalPlayer.name) return;

                if (global)
                {
                    m.ServerData = new ServerMarkerData()
                    {
                        Id = Guid.NewGuid(),
                        Owner = Main.LocalPlayer.name,
                        PublicEdit = false
                    };

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = CreateSyncPacket(m, SyncMessageType.Add);
                        m.Write(packet);
                        packet.Send();
                    }
                    else { ModContent.GetInstance<MapServer>().Markers.Add(m); }
                }
                else
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = CreateSyncPacket(m, SyncMessageType.Remove);
                        m.ServerData = null;
                        packet.Send();
                    }
                    else 
                    {
                        m.ServerData = null;
                        ModContent.GetInstance<MapServer>().Markers.Remove(m);
                    }
                }
            }
            
        }
        public static void SetGlobalEdit(MapMarker m, bool globalEdit)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (m.ServerData != null && m.ServerData.Owner != Main.LocalPlayer.name) return;

                m.ServerData.PublicEdit = globalEdit;

                ModPacket packet = CreateSyncPacket(m, SyncMessageType.UpdateEdit);
                packet.Write(globalEdit);
                packet.Send();
            }
            else m.ServerData.PublicEdit = globalEdit;
        }
    }
}
