using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace MapMarkers
{
    internal class MapConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        
        [Label("Autopause on Marker UI")]
        public bool AutopauseOnUI { get; set; }

        [Label("Show locked chests on map")]
        public bool AddChestMarkers { get; set; }

        [Label("Show statues on map")]
        public bool AddStatueMarkers { get; set; }

        [JsonIgnore]
        private bool _chestPrev;

        [JsonIgnore]
        private bool _statuePrev;

        public override void OnLoaded()
        {
            _chestPrev = AddChestMarkers;
            _statuePrev = AddStatueMarkers;
        }

        public override void OnChanged()
        {
            MapMarkers m = mod as MapMarkers;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Net.MapClient.RequestSpecial(AddStatueMarkers && !_statuePrev);
            }

            if (_chestPrev != AddChestMarkers) 
            {
                if (AddChestMarkers)
                {
                    if (Main.netMode == NetmodeID.SinglePlayer)
                        m.AddChestMarkers();
                }
                else m.ResetChestMarkers();
                _chestPrev = AddChestMarkers;
            }

            if (_statuePrev != AddStatueMarkers)
            {
                if (AddStatueMarkers)
                {
                    if (Main.netMode == NetmodeID.SinglePlayer)
                        m.AddStatueMarkers();
                }
                else m.ResetStatueMarkers();
                _statuePrev = AddStatueMarkers;
            }
        }
    }
}
