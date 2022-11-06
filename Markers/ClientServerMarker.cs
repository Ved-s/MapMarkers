using MapMarkers.Structures;
using System.IO;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MapMarkers.Markers
{
    public abstract class ClientServerMarker : MapMarker
    {
        internal protected bool IgnoreSetChecks { get; internal set; } = false;

        public string? Owner;

        public bool ServerSide
        {
            get => serverSide;
            set
            {
                if (IgnoreSetChecks)
                {
                    serverSide = value;
                    return;
                }

                if (value == serverSide || !CheckOwnerPermission(Main.myPlayer))
                    return;

                serverSide = value;

                if (value)
                {
                    Owner = Networking.IsServer ? null : Main.LocalPlayer.name;
                    Networking.AddMarker(this);
                }
                else
                {
                    Owner = null;
                    Networking.RemoveMarker(this);
                }
            }
        }
        public bool AnyoneCanRemove
        {
            get => anyoneCanRemove;
            set
            {
                if (!ServerSide || IgnoreSetChecks)
                {
                    anyoneCanRemove = value;
                    return;
                }

                if (anyoneCanRemove == value || !CheckOwnerPermission(Main.myPlayer))
                    return;

                anyoneCanRemove = value;

                ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetRemovePermission);
                packet.Write(value);
                packet.Send();
            }
        }

        private bool serverSide;
        private bool anyoneCanRemove;

        public override SaveLocation SaveLocation => ServerSide ? SaveLocation.Server : SaveLocation.Client;

        public bool CheckOwnerPermission(int whoAmI)
        {
            return !ServerSide
            || Networking.IsSingleplayer
            || Owner == Main.player[whoAmI].name
            || Networking.IsServer && Owner is null;
        }

        public override bool CanDelete(int whoAmI)
        {
            return AnyoneCanRemove || CheckOwnerPermission(whoAmI);
        }

        public override bool CanMove(int whoAmI)
        {
            return CheckOwnerPermission(whoAmI);
        }

        public override void SendData(BinaryWriter writer)
        {
            writer.Write(ServerSide);
            if (ServerSide)
            {
                writer.Write(Owner is null);
                if (Owner is not null)
                    writer.Write(Owner);
                writer.Write(AnyoneCanRemove);
            }
        }

        public override void ReceiveData(BinaryReader reader)
        {
            serverSide = reader.ReadBoolean();
            if (serverSide)
            {
                if (reader.ReadBoolean())
                    Owner = null;
                else
                    Owner = reader.ReadString();
                anyoneCanRemove = reader.ReadBoolean();
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["owner"] = Owner;
            tag["server"] = ServerSide;
            tag["anyRemove"] = AnyoneCanRemove;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.TryGet("owner", out string? owner))
                Owner = owner;
            if (tag.TryGet("server", out bool server))
                serverSide = server;
            else
                serverSide = Owner is not null || Networking.IsServer;
            if (tag.TryGet("anyRemove", out bool anyRemove))
                anyoneCanRemove = anyRemove;
        }

        public override void HandlePacket(BinaryReader reader, ushort type, int whoAmI, ref bool broadcast)
        {
            MessageType mt = (MessageType)type;

            switch (mt)
            {
                case MessageType.SetRemovePermission:
                    bool value = reader.ReadBoolean();
                    if (Networking.IsServer && !CheckOwnerPermission(whoAmI))
                    {
                        ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetRemovePermission);
                        packet.Write(anyoneCanRemove);
                        packet.Send(whoAmI);
                        return;
                    }

                    broadcast = true;
                    anyoneCanRemove = value;
                    break;
            }
        }

        public override void AddDebugInfo(StringBuilder builder)
        {
            if (ServerSide)
            {
                builder.AppendLine($"Owner: {Owner ?? "[Server]"}");
                if (AnyoneCanRemove)
                    builder.AppendLine($"Anyone can remove");
            }
        }

        enum MessageType : ushort 
        {
            SetRemovePermission = 0xf000
        }
    }
}
