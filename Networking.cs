using Terraria;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using System.IO;
using Terraria.ModLoader;
using System.Buffers;
using Terraria.GameContent;
using Microsoft.Xna.Framework;

namespace MapMarkers
{
    public static class Networking
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        public static bool IsSingleplayer => Main.netMode == NetmodeID.SinglePlayer;
        public static bool IsClient => Main.netMode == NetmodeID.MultiplayerClient;
        public static bool IsServer => Main.netMode == NetmodeID.Server;

        public static bool IsPlayer => IsSingleplayer || IsClient;
        public static bool IsWorld => IsSingleplayer || IsServer;

        public static bool OtherSideMod => IsClient && MapMarkers.IsNetSynced || IsServer;

        public static ModPacket CreatePacket(PacketType type)
        {
            ModPacket packet = MapMarkers.GetPacket();
            packet.Write((byte)type);
            return packet;
        }
        internal static void HandlePacket(BinaryReader reader, int whoAmI)
        {
            PacketType type = (PacketType)reader.ReadByte();
            
            long dataStart = reader.BaseStream.Position;
            bool broadcast = false;

            ProcessPacket(type, reader, whoAmI, ref broadcast);

            if (broadcast && IsServer)
            {
                int size = (int)(reader.BaseStream.Position - dataStart);
                reader.BaseStream.Seek(dataStart, SeekOrigin.Begin);
                ModPacket packet = CreatePacket(type);
                CopyToStream(reader.BaseStream, packet.BaseStream, size);

                packet.Send(-1, whoAmI);
            }
        }

        internal static void ProcessPacket(PacketType type, BinaryReader reader, int whoAmI, ref bool broadcast)
        {
            broadcast = false;
            switch (type)
            {
                case PacketType.RequestNetIds:
                    OnRequestNetIds(reader, whoAmI);
                    break;

                case PacketType.RequestAllMarkers:
                    OnRequestAccessibleMarkers(reader, whoAmI);
                    break;

                case PacketType.AddMarker:
                    OnAddMarker(reader, whoAmI, out broadcast);
                    break;

                case PacketType.MoveMarker:
                    OnMoveMarker(reader, whoAmI, out broadcast);
                    break;

                case PacketType.RemoveMarker:
                    OnRemoveMarker(reader, whoAmI, out broadcast);
                    break;

                default:
                    MapMarkers.Logger.WarnFormat("Unknown netmessage {type} from {player}", type, Main.player[whoAmI].name);
                    break;
            }
        }

        internal static void OnJoinWorld()
        {
            MapMarkers.MarkerNetIds.Clear();
            if (IsServer)
            {
                int id = 0;

                foreach (var marker in MapMarkers.MarkerInstances.Values)
                {
                    marker.NetId = id;
                    id++;
                }
            }
            else if (IsClient)
                CreatePacket(PacketType.RequestNetIds).Send();
        }
        internal static void CopyToStream(Stream from, Stream to, int length)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(length, 1024));

            while (length > 0)
            {
                int count = Math.Min(length, buffer.Length);
                int read = from.Read(buffer, 0, count);
                length -= read;
                to.Write(buffer, 0, read);
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }

        static MapMarker? GetMarkerByReadId(BinaryReader reader)
        {
            Guid id = new(reader.ReadBytes(16));
            if (!MapMarkers.Markers.TryGetValue(id, out MapMarker? marker))
                marker = null;

            return marker;
        }

        public static void AddMarker(MapMarker marker, int toClient = -1)
        {
            ModPacket packet = CreatePacket(PacketType.AddMarker);
            MapMarkers.SendMarker(marker, packet);
            packet.Send(toClient);
        }
        public static void MoveMarker(MapMarker marker, int toClient = -1)
        {
            ModPacket packet = CreatePacket(PacketType.MoveMarker);
            packet.Write(marker.Id.ToByteArray());
            packet.Write(marker.Position.X);
            packet.Write(marker.Position.Y);
            packet.Send(toClient);
        }
        public static void RemoveMarker(MapMarker marker, int toClient = -1)
        {
            ModPacket packet = CreatePacket(PacketType.RemoveMarker);
            packet.Write(marker.Id.ToByteArray());
            packet.Send(toClient);
        }

        static void OnRequestNetIds(BinaryReader reader, int whoAmI)
        {
            if (IsServer)
            {
                ModPacket packet = CreatePacket(PacketType.RequestNetIds);

                packet.Write(MapMarkers.MarkerNetIds.Count);

                foreach (var kvp in MapMarkers.MarkerNetIds)
                {
                    packet.Write(kvp.Key.mod);
                    packet.Write(kvp.Key.name);
                    packet.Write(kvp.Value);
                }
                packet.Send(whoAmI);
            }
            else if (IsClient)
            {
                MapMarkers.MarkerNetIds.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    MapMarkers.MarkerNetIds[(reader.ReadString(), reader.ReadString())] = reader.ReadInt32();

                CreatePacket(PacketType.RequestAllMarkers).Send();
            }
        }
        static void OnRequestAccessibleMarkers(BinaryReader reader, int whoAmI)
        {
            if (IsServer)
            {
                ModPacket packet = CreatePacket(PacketType.RequestAllMarkers);

                MapMarker[] markersToSend = MapMarkers.Markers.Values.Where(m => m.ShouldBeSentTo(whoAmI)).ToArray();
                packet.Write(markersToSend.Length);

                for (int i = 0; i < markersToSend.Length; i++)
                    MapMarkers.SendMarker(markersToSend[i], packet);

                packet.Send(whoAmI);
            }
            else if (IsClient)
            {
                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    MapMarker? marker = MapMarkers.ReceiveMarker(reader);
                    if (marker is not null)
                        MapMarkers.AddMarker(marker, false);
                }
            }
        }
        static void OnAddMarker(BinaryReader reader, int whoAmI, out bool broadcast)
        {
            MapMarker? marker = MapMarkers.ReceiveMarker(reader);
            broadcast = marker is not null;

            if (marker is not null)
            {
                if (IsClient)
                    marker.SaveLocation = Structures.SaveLocation.Server;

                MapMarkers.AddMarker(marker, false);
            }
        }
        static void OnMoveMarker(BinaryReader reader, int whoAmI, out bool broadcast)
        {
            MapMarker? marker = GetMarkerByReadId(reader);
            Vector2 pos = new(reader.ReadSingle(), reader.ReadSingle());

            broadcast = false;
            if (marker is null)
                return;
            
            if (IsServer && !marker.CanMove(whoAmI))
            {
                broadcast = false;
                MoveMarker(marker, whoAmI);
                return;
            }
            broadcast = true;

            MapMarkers.MoveMarker(marker, pos, false);
        }
        static void OnRemoveMarker(BinaryReader reader, int whoAmI, out bool broadcast)
        {
            MapMarker? marker = GetMarkerByReadId(reader);

            broadcast = false;
            if (marker is null)
                return;

            if (IsServer && !marker.CanDelete(whoAmI))
            {
                broadcast = false;
                AddMarker(marker, whoAmI);
                return;
            }
            broadcast = true;

            MapMarkers.RemoveMarker(marker, false);
        }

        public enum PacketType : byte
        {
            RequestNetIds,
            RequestAllMarkers,

            AddMarker,
            MoveMarker,
            RemoveMarker
        }
    }
}
