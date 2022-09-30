using MapMarkers.Buffs;
using MapMarkers.Items;
using MapMarkers.Structures;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers
{
    public class MarkerPlayer : ModPlayer
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();
        public static MarkerPlayer LocalInstance => Main.LocalPlayer.GetModPlayer<MarkerPlayer>();

        public TagCompound CurrentMarkerData { get; private set; } = new();
        public TagCompound? CurrentPMD { get; private set; }

        public override void SaveData(TagCompound tag)
        {
            CurrentMarkerData[Main.worldID.ToString()] = MapMarkers.Markers.Values
                .Where(m => m.SaveLocation == SaveLocation.Client)
                .Select(m => MapMarkers.SaveMarker(m))
                .ToList();

            tag["markers"] = CurrentMarkerData;
            tag["pmd"] = PlayerMarkerData.Save();

            if (Main.gameMenu) // Player exiting to menu
            {
                MapMarkers.Markers.RemoveWhere(kvp => kvp.Value.SaveLocation != SaveLocation.Server);
                PlayerMarkerData.Clear();
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("markers", out TagCompound markers))
                CurrentMarkerData = markers;
            if (tag.TryGet("pmd", out TagCompound pmd))
                CurrentPMD = pmd;
        }

        public override void OnEnterWorld(Player player)
        {
            if (CurrentMarkerData.TryGet(Main.worldID.ToString(), out List<TagCompound> markers))
            {
                foreach (TagCompound markerData in markers)
                {
                    MapMarker? marker = MapMarkers.LoadMarker(markerData, SaveLocation.Client);
                    if (marker is null)
                        continue;

                    MapMarkers.Markers[marker.Id] = marker;
                }
            }
            PlayerMarkerData.Load(CurrentPMD);
        }

        public bool FindTPPotion(bool consume)
        {
            return FindTPPotion(Main.LocalPlayer.inventory, consume)
            || FindTPPotion(Main.LocalPlayer.bank?.item, consume)
            || FindTPPotion(Main.LocalPlayer.bank2?.item, consume)
            || FindTPPotion(Main.LocalPlayer.bank3?.item, consume)
            || FindTPPotion(Main.LocalPlayer.bank4?.item, consume);
        }
        private bool FindTPPotion(Item[]? container, bool consume)
        {
            if (container is null)
                return false;

            int potionType = MarkerTPPotion.ItemType;

            foreach (Item item in container)
            {
                if (item is null)
                    continue;

                if (item.type == potionType && item.stack > 0)
                {
                    if (consume && ItemLoader.ConsumeItem(item, Main.LocalPlayer))
                    {
                        item.stack--;
                        if (item.stack <= 0)
                            item.TurnToAir();
                    }
                    return true;
                }
            }

            return false;
        }

        public void Teleport(MapMarker m)
        {
            Vector2? tpPos = TryGetTeleportPos(m);
            if (!tpPos.HasValue)
                // TODO: Notify player
                return;

            if (!FindTPPotion(true))
                // TODO: Notify player
                return;

            if (Main.mapFullscreen)
                Main.mapFullscreen = false;

            Player.Teleport(tpPos.Value);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.Teleport, -1, -1, null, 0, Main.LocalPlayer.whoAmI, tpPos.Value.X, tpPos.Value.Y, 1, 0, 0);

#if DEBUG
            int buffTime = 300;
#else
            int buffTime = 1800;
#endif
            Main.LocalPlayer.AddBuff(TPDisability.BuffType, buffTime);
        }
        private Vector2? TryGetTeleportPos(MapMarker m)
        {
            Vector2 tpTarget = (m.Size / 2) + (m.Position * 16) - (Player.Size / 2);

            Vector2 tpRadius = Player.Size + m.Size;

            Vector2 start = tpTarget - tpRadius;
            Vector2 area = tpRadius * 2 - Player.Size;

            Vector2 closestPos = new Vector2();
            float closestDistSQ = float.MaxValue;
            bool anyPos = false;

            for (float y = start.Y; y < start.Y + area.Y; y += 8)
                for (float x = start.X; x < start.X + area.X; x += 8)
                {
                    Vector2 pos = new Vector2(x, y);
                    if (!Collision.SolidCollision(pos, Player.width, Player.height))
                    {
                        Vector2 diff = tpTarget - pos;

                        float distSQ = diff.Y * diff.Y + diff.X * diff.X;
                        if (distSQ < closestDistSQ)
                        {
                            anyPos = true;
                            closestDistSQ = distSQ;
                            closestPos = pos;
                        }
                    }
                }

            if (anyPos)
                return closestPos;
            return null;

        }

        public bool CanTeleport()
        {
            return !Player.HasBuff(TPDisability.BuffType) && FindTPPotion(false);
        }
    }
}
