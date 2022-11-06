using MapMarkers.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Net;

namespace MapMarkers.Markers
{
    public class PlacedMarker : ClientServerMarker
    {
        public bool AnyoneCanEdit
        {
            get => anyoneCanEdit;
            set
            {
                if (IgnoreSetChecks)
                {
                    anyoneCanEdit = value;
                    return;
                }

                if (anyoneCanEdit == value || !CheckOwnerPermission(Main.myPlayer))
                    return;

                anyoneCanEdit = value;

                if (!ServerSide)
                    return;

                ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetEditPermission);
                packet.Write(value);
                packet.Send();
            }
        }

        public override string DisplayName
        {
            get => base.DisplayName;
            set
            {
                if (IgnoreSetChecks)
                {
                    base.DisplayName = value;
                    return;
                }

                if (base.DisplayName == value || !CheckOwnerPermission(Main.myPlayer))
                    return;

                base.DisplayName = value;

                if (!ServerSide)
                    return;

                ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetName);
                packet.Write(value);
                packet.Send();
            }
        }

        public Item DisplayItem
        {
            get => displayItem;
            set 
            {
                if (IgnoreSetChecks)
                {
                    displayItem = value;
                    ItemTextureCache = null;
                    return;
                }

                if (!CheckOwnerPermission(Main.myPlayer))
                    return;

                displayItem = value; 
                ItemTextureCache = null;

                if (!ServerSide)
                    return;

                ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetItem);
                ItemIO.Send(value, packet);
                packet.Send();
            }
        }

        public int DisplayItemType
        {
            get => DisplayItem.type;
            set
            {
                Item i = new();
                i.SetDefaults(value);
                DisplayItem = i;
            }
        }

        public override Vector2 Size => ItemFrame.Size();

        int ItemType => DisplayItem.type <= ItemID.None ? ItemID.TrifoldMap : DisplayItem.type;
        Texture2D ItemTexture
        {
            get
            {
                if (ItemTextureCache is not null)
                    return ItemTextureCache;

                int itemType = ItemType;

                var asset = TextureAssets.Item[itemType];

                if (asset.State == AssetState.NotLoaded)
                {
                    if (itemType > ItemID.Count)
                        asset = ModContent.Request<Texture2D>(asset.Name, AssetRequestMode.ImmediateLoad);
                    else
                        asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.ImmediateLoad);
                }
                else if (asset.State == AssetState.Loading)
                    asset.Wait();

                return ItemTextureCache = asset.Value;
            }
        }

        Rectangle ItemFrame => Main.itemAnimations[ItemType] is null ?
            ItemTexture.Frame(1, 1, 0, 0, 0, 0)
            : Main.itemAnimations[ItemType].GetFrame(ItemTexture, -1);

        Texture2D? ItemTextureCache;
        private Item displayItem = new();
        private bool anyoneCanEdit = false;

        public override void Draw()
        {
            Main.spriteBatch.Draw(ItemTexture, ScreenRect, ItemFrame, Color.White);
        }

        public override void Hover(StringBuilder mouseText)
        {
            if (Main.keyState.PressingShift() && ServerSide)
                mouseText.AppendLine($"Owner: {Owner ?? "Server"}");
        }

        public override void SaveData(TagCompound tag)
        {
            base.SaveData(tag);
            tag["name"] = DisplayName;
            tag["item"] = ItemIO.Save(DisplayItem);
            tag["anyEdit"] = AnyoneCanEdit;
        }

        public override void LoadData(TagCompound tag)
        {
            base.LoadData(tag);
            if (tag.TryGet("name", out string name))
                base.DisplayName = name;
            if (tag.TryGet("item", out TagCompound item))
                displayItem = ItemIO.Load(item);
            if (tag.TryGet("anyEdit", out bool anyEdit))
                anyoneCanEdit = anyEdit;
        }

        public override void SendData(BinaryWriter writer)
        {
            base.SendData(writer);
            writer.Write(DisplayName);
            ItemIO.Send(DisplayItem, writer);
            writer.Write(AnyoneCanEdit);
        }

        public override void ReceiveData(BinaryReader reader)
        {
            base.ReceiveData(reader);
            base.DisplayName = reader.ReadString();
            displayItem = ItemIO.Receive(reader);
            anyoneCanEdit = reader.ReadBoolean();
        }

        public override bool CanMove(int whoAmI)
        {
            return AnyoneCanEdit || base.CanMove(whoAmI);
        }

        public override IEnumerable<MarkerMenu.MenuItemDefinition> GetMenuItems()
        {
            if (AnyoneCanEdit || CheckOwnerPermission(Main.myPlayer))
                yield return new MarkerMenu.MenuItemDefinition("Mods.MapMarkers.MarkerMenuItem.Edit", "Mods.MapMarkers.MarkerMenuItemDesc.Edit", () => MarkerEditMenu.Show(this));
        }

        public override void AddDebugInfo(StringBuilder builder)
        {
            base.AddDebugInfo(builder);
            if (AnyoneCanEdit)
                builder.AppendLine($"Anyone can edit");
            builder.AppendLine($"Item: {Lang.GetItemNameValue(ItemType)}");
        }

        public override void HandlePacket(BinaryReader reader, ushort type, int whoAmI, ref bool broadcast)
        {
            if (type > (ushort)MessageType.Max)
            {
                base.HandlePacket(reader, type, whoAmI, ref broadcast);
                return;
            }

            MessageType mt = (MessageType)type;

            switch (mt)
            {
                case MessageType.SetName:
                    string name = reader.ReadString();
                    if (!AnyoneCanEdit && Networking.IsServer && !CheckOwnerPermission(whoAmI))
                    {
                        ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetName);
                        packet.Write(base.DisplayName);
                        packet.Send(whoAmI);
                        return;
                    }

                    broadcast = true;
                    base.DisplayName = name;
                    break;

                case MessageType.SetItem:
                    Item item = ItemIO.Receive(reader);
                    if (!AnyoneCanEdit && Networking.IsServer && !CheckOwnerPermission(whoAmI))
                    {
                        ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetItem);
                        ItemIO.Send(DisplayItem, packet);
                        packet.Send(whoAmI);
                        return;
                    }

                    broadcast = true;
                    displayItem = item;
                    break;

                case MessageType.SetEditPermission:
                    bool edit = reader.ReadBoolean();
                    if (Networking.IsServer && !CheckOwnerPermission(whoAmI))
                    {
                        ModPacket packet = CreatePacket(Id, (ushort)MessageType.SetEditPermission);
                        packet.Write(AnyoneCanEdit);
                        packet.Send(whoAmI);
                        return;
                    }

                    broadcast = true;
                    anyoneCanEdit = edit;
                    break;
            }
        }

        enum MessageType : ushort
        {
            SetName,
            SetItem,
            SetEditPermission,

            Max = 0x00ff
        }
    }
}
