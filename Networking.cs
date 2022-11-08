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
using MapMarkers.Markers;
using log4net.Repository.Hierarchy;

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

        // Limit marker count per player in MP
        public static int PlayerMarkerCap = 10;

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
                case PacketType.Sync:
                    OnSync(reader, whoAmI);
                    break;

                case PacketType.RequestAllMarkers:
                    OnRequestAccessibleMarkers(reader, whoAmI);
                    break;

                case PacketType.MarkerMessage:
                    OnMarkerMessage(reader, whoAmI, ref broadcast);
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

                case PacketType.SyncMarkerCap:
                    OnSyncMarkerCap(reader);
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
                CreatePacket(PacketType.Sync).Send();
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

        public static ModPacket CreateMarkerInstancePacket(int netId, ushort type)
        {
            ModPacket packet = CreatePacket(PacketType.MarkerMessage);
            packet.Write(type);
            packet.Write(true);
            packet.Write(netId);
            return packet;
        }
        public static ModPacket CreateMarkerIdPacket(Guid id, ushort type)
        {
            ModPacket packet = CreatePacket(PacketType.MarkerMessage);
            packet.Write(type);
            packet.Write(false);
            packet.Write(id.ToByteArray());
            
            return packet;
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
            if (!OtherSideMod)
                return;

            ModPacket packet = CreatePacket(PacketType.AddMarker);
            MapMarkers.SendMarker(marker, packet);
            packet.Send(toClient);
        }
        public static void MoveMarker(MapMarker marker, int toClient = -1)
        {
            if (!OtherSideMod)
                return;

            ModPacket packet = CreatePacket(PacketType.MoveMarker);
            packet.Write(marker.Id.ToByteArray());
            packet.Write(marker.Position.X);
            packet.Write(marker.Position.Y);
            packet.Send(toClient);
        }
        public static void RemoveMarker(MapMarker marker, int toClient = -1)
        {
            if (!OtherSideMod)
                return;

            ModPacket packet = CreatePacket(PacketType.RemoveMarker);
            packet.Write(marker.Id.ToByteArray());
            packet.Send(toClient);
        }
        public static void SyncMarkerCap(int toClient = -1)
        {
            ModPacket packet = CreatePacket(PacketType.SyncMarkerCap);
            packet.Write(PlayerMarkerCap);
            packet.Send(toClient);
        }

        public static bool CheckMarkerCap(int whoAmI)
        {
            if (IsSingleplayer || whoAmI < 0 || whoAmI >= 255 || PlayerMarkerCap < 0)
                return true;
            return MapMarkers.Markers.Values.Count(m => m is ClientServerMarker csm && csm.ServerSide) < PlayerMarkerCap;
        }

        static void OnSync(BinaryReader reader, int whoAmI)
        {
            if (IsServer)
            {
                ModPacket packet = CreatePacket(PacketType.Sync);

                packet.Write(MapMarkers.MarkerNetIds.Count);

                foreach (var kvp in MapMarkers.MarkerNetIds)
                {
                    packet.Write(kvp.Key.mod);
                    packet.Write(kvp.Key.name);
                    packet.Write(kvp.Value);
                }
                packet.Write(PlayerMarkerCap);
                packet.Send(whoAmI);
            }
            else if (IsClient)
            {
                MapMarkers.MarkerNetIds.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    MapMarkers.MarkerNetIds[(reader.ReadString(), reader.ReadString())] = reader.ReadInt32();

                PlayerMarkerCap = reader.ReadInt32();

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
        static void OnMarkerMessage(BinaryReader reader, int whoAmI, ref bool broadcast)
        {
            ushort type = reader.ReadUInt16();
            bool instanced = reader.ReadBoolean();
            MapMarker? marker = null;
            broadcast = false;

            if (instanced)
            {
                int netId = reader.ReadInt32();
                if (netId < 0)
                {
                    MapMarkers.Logger.WarnFormat("Received message for unknown marker witn NetId {0}", netId);
                    return;
                }
                marker = MapMarkers.MarkerInstances.Values.FirstOrDefault(m => m.NetId == netId);
            }
            else 
            {
                Guid id = new(reader.ReadBytes(16));
                if (!MapMarkers.Markers.TryGetValue(id, out marker))
                {
                    MapMarkers.Logger.WarnFormat("Received message for unknown marker with id {0}", id);
                    return;
                }
            }

            if (marker is null)
            {
                MapMarkers.Logger.WarnFormat("Received message for unknown marker");
                return;
            }

            marker.HandlePacket(reader, type, whoAmI, ref broadcast);
        }
        static void OnAddMarker(BinaryReader reader, int whoAmI, out bool broadcast)
        {
            MapMarker? marker = MapMarkers.ReceiveMarker(reader);
            broadcast = marker is not null;

            if (marker is not null)
            {
                if (IsServer && !CheckMarkerCap(whoAmI))
                {
                    RemoveMarker(marker, whoAmI);
                    broadcast = false;
                    return;
                }

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
        static void OnSyncMarkerCap(BinaryReader reader)
        {
            if (!IsClient)
                return;

            PlayerMarkerCap = reader.ReadInt32();
        }

        public enum PacketType : byte
        {
            Sync,
            RequestAllMarkers,
            MarkerMessage,

            AddMarker,
            MoveMarker,
            RemoveMarker,

            SyncMarkerCap
        }
    }
}
