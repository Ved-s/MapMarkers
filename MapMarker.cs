﻿using System;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using MapMarkers.Structures;
using Terraria.ModLoader.IO;
using Steamworks;
using System.Text;
using System.Data;
using System.Collections.Generic;
using static MapMarkers.UI.MarkerMenu;
using System.IO;

namespace MapMarkers
{
    /// <summary>
    /// Base class for map markers
    /// </summary>
    public abstract class MapMarker : ILoadable
    {
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        /// <summary>
        /// Internal marker type name
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Mod which loaded this marker type
        /// </summary>
        public Mod Mod => InstanceMod ??= MapMarkers.MarkerInstances[GetType()].InstanceMod;

        internal virtual string SaveModName => Mod.Name;

        /// <summary>
        /// Marker display name
        /// </summary>
        public virtual string DisplayName { get; set; } = "Unnamed marker";

        /// <summary>
        /// Marker id
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Marker position in world tiles coordinates<br/>
        /// <b>Warning!</b> Position is not saved automatically
        /// </summary>
        public virtual Vector2 Position { get; set; }

        /// <summary>
        /// Markre size in pixels
        /// </summary>
        public virtual Vector2 Size { get; set; } = new(20, 20);

        /// <summary>
        /// Hover outline color
        /// </summary>
        public virtual Color OutlineColor { get; set; } = Color.White;

        /// <summary>
        /// Whether this marker is active and can be drawn/hovered<br/>
        /// </summary>
        public virtual bool Active { get; set; } = true;

        /// <summary>
        /// Whether this marker should be drawn topmost, i.e. in front of all markers, map icons and minimap frame
        /// </summary>
        public virtual bool DrawTopMost { get; set; } = false;

        /// <summary>
        /// Whether this marker should be clipped to <see cref="Helper.MapVisibleScreenRect"/>
        /// </summary>
        public virtual bool ClipToMap { get; set; } = true;

        /// <summary>
        /// Marker save location
        /// </summary>
        public virtual SaveLocation SaveLocation { get; set; }

        /// <summary>
        /// Whether this marker is hovered at this moment
        /// </summary>
        public bool Hovered { get; internal set; }

        /// <summary>
        /// Marker screen rectangle to draw in
        /// </summary>
        public Rect ScreenRect { get; internal set; }

        public PlayerMarkerData PlayerData 
        {
            get
            {
                if (playerData is not null && playerData.Id == Id)
                    return playerData;

                return playerData = PlayerMarkerData.GetByMarkerId(Id);
            }
        }

        internal Mod InstanceMod = null!;
        private PlayerMarkerData? playerData = null;

        void ILoadable.Load(Mod mod)
        {
            InstanceMod = mod;
            MapMarkers.MarkerInstances[GetType()] = this;
        }

        void ILoadable.Unload()
        {
            MapMarkers.MarkerInstances.Remove(GetType());
        }

        /// <summary>
        /// Marker drawing method<br/>
        /// Use <see cref="ScreenRect"> to draw marker
        /// </summary>
        public abstract void Draw();

        /// <summary>
        /// Called when marker is hovered on the map
        /// </summary>
        /// <param name="mouseText">Mouse text</param>
        public virtual void Hover(StringBuilder mouseText) { }

        /// <summary>
        /// Called when saving this marker's data
        /// </summary>
        public virtual void SaveData(TagCompound tag) { }

        /// <summary>
        /// Called when loading this marker's data
        /// </summary>
        public virtual void LoadData(TagCompound tag) { }

        public virtual bool ShouldBeSentTo(int whoAmI) => true;

        public virtual void SendData(BinaryWriter writer) { }
        public virtual void ReceiveData(BinaryReader reader) { }

        public virtual IEnumerable<MenuItemDefinition> GetMenuItems() { yield break; }

        public virtual MapMarker CreateInstance() => (MapMarker)Activator.CreateInstance(GetType())!;

        public virtual bool CanMove(int whoAmI) => true;
        public virtual bool CanDelete(int whoAmI) => true;

        public bool NeedsSync() => Networking.IsServer || SaveLocation != SaveLocation.Client;
    }

    [Autoload(false)]
    public class UnloadedMarker : MapMarker
    {
        public override string Name { get; }
        internal override string SaveModName { get; }

        public UnloadedMarker(string name, string mod, SaveLocation saveLocation)
        {
            Name = name;
            SaveModName = mod;
            SaveLocation = saveLocation;
            InstanceMod = ModContent.GetInstance<MapMarkers>();
        }

        public override void Draw() 
        {

        }
    }
}
