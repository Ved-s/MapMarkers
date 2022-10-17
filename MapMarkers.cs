using MapMarkers.Structures;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static Terraria.ModLoader.PlayerDrawLayer;

namespace MapMarkers
{
    /// <summary>
    /// Main mod class
    /// </summary>
    public class MapMarkers : Mod
    {
        public static MapMarkers Instance => ModContent.GetInstance<MapMarkers>();

        internal Dictionary<Type, MapMarker> MarkerInstances = new();
        internal Dictionary<(string mod, string name), int> MarkerNetIds = new();

        /// <summary>
        /// List of current player+world markers
        /// </summary>
        public Dictionary<Guid, MapMarker> Markers { get; } = new();

        public ShortGuids MarkerGuids { get; } = new(2);

        public static TagCompound SaveMarker(MapMarker marker)
        {
            TagCompound tag = new();
            tag["id"] = marker.Id.ToByteArray();
            tag["name"] = marker.Name;
            tag["mod"] = marker.SaveModName;
            tag["pos"] = marker.Position;
            TagCompound data = new();
            marker.SaveData(data);
            tag["data"] = data;
            return tag;
        }
        public static MapMarker? LoadMarker(TagCompound markerData, SaveLocation currentLocation)
        {
            if (!markerData.TryGet("name", out string name) || !markerData.TryGet("mod", out string mod))
                return null;

            MapMarker? marker;

            Mod? modInst = ModLoader.GetMod(mod);
            if (modInst is null)
            {
                marker = new UnloadedMarker(name, mod, currentLocation);
            }
            else 
            {
                marker = modInst.GetContent().FirstOrDefault(c => c is MapMarker m && m.Name == name) as MapMarker;
                if (marker is null)
                    marker = new UnloadedMarker(name, mod, currentLocation);
                else marker = marker.CreateInstance();
            }

            if (markerData.TryGet("id", out byte[] id))
                marker.Id = new(id);

            if (markerData.TryGet("pos", out Vector2 pos))
                marker.Position = pos;

            marker.SaveLocation = currentLocation;

            if (markerData.TryGet("data", out TagCompound data))
                marker.LoadData(data);

            return marker;
        }

        public static void SendMarker(MapMarker marker, BinaryWriter writer)
        {
            if (marker.NetId < 0)
                throw new ArgumentException("Marker must be netsynced to send it", nameof(marker));

            writer.Write(marker.NetId);
            writer.Write(marker.Id.ToByteArray());
            writer.Write(marker.Position.X);
            writer.Write(marker.Position.Y);
            SafeIO io = SafeIO.SafeWrite(writer);
            marker.SendData(writer);
            io.EndWrite();
        }
        public static MapMarker? ReceiveMarker(BinaryReader reader)
        {
            int netId = reader.ReadInt32();
            Guid id = new(reader.ReadBytes(16));
            Vector2 pos = new(reader.ReadSingle(), reader.ReadSingle());
            SafeIO io = SafeIO.SafeRead(reader);

            if (netId < 0 || Instance.MarkerInstances.Values.FirstOrDefault(m => m.NetId == netId) is not MapMarker marker)
            {
                io.EndRead(out _);
                return null;
            }

            marker = marker.CreateInstance();
            marker.Id = id;
            marker.Position = pos;
            marker.SaveLocation = Structures.SaveLocation.Server;
            marker.ReceiveData(reader);
            io.EndRead(out int readError);

            if (readError != 0)
                Instance.Logger.WarnFormat("Read length mismatch while receiving marker {0}: {1} bytes", marker.Name, readError);
            
            return marker;
        }

        public void AddMarker(MapMarker marker, bool syncToOthers)
        {
            Markers[marker.Id] = marker;
            MarkerGuids.AddToDictionary(marker.Id);

            if (syncToOthers && marker.NeedsSync())
                Networking.AddMarker(marker);
        }
        // TODO when Netcode: add net handling

        public void RemoveMarker(MapMarker marker, bool syncToOthers)
        {
            Markers.Remove(marker.Id);

            if (syncToOthers && marker.NeedsSync())
                Networking.RemoveMarker(marker);
        }

        public void MoveMarker(MapMarker marker, Vector2 pos, bool syncToOthers)
        {
            if (marker.Position == pos)
                return;

            marker.Position = pos;

            if (syncToOthers && marker.NeedsSync())
                Networking.MoveMarker(marker);
        }

        internal static string GetLangValue(string key)
        {
            string fullKey = "Mods.MapMarkers." + key;
            if (!Language.Exists(fullKey))
                return key;
            return Language.GetTextValue(fullKey);
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            Networking.HandlePacket(reader, whoAmI);
        }
    }
}
