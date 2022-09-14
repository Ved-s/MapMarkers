using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace MapMarkers
{
    internal class HerosIntegration : ILoadable
    {
        public static HerosIntegration Instance => ModContent.GetInstance<HerosIntegration>();
        internal static MapMarkers MapMarkers => ModContent.GetInstance<MapMarkers>();

        PropertyInfo? TeleporterServiceHasPermissionToUse;
        FieldInfo? TeleporterServiceInstance;

        public bool? TeleporterService_HasPermissionToUse
        {
            get => (bool?)TeleporterServiceHasPermissionToUse?.GetValue(TeleporterServiceInstance?.GetValue(null));
            set
            {
                if (value is null) return;
                TeleporterServiceHasPermissionToUse?.SetValue(TeleporterServiceInstance?.GetValue(null), value.Value);
            }
        }

        public bool AllowTp 
        {
            get => allowTp;
            set
            {
                if (AllowTp == value)
                    return;
                allowTp = value;

                if (!AllowTp)
                    PauseFullscreenMapTP();
                else
                    ResumeFullscreenMapTP();
            }
        }

        public bool TpPermissionOldValue;
        private bool allowTp = true;

        void ILoadable.Load(Mod mod)
        {
            Mod? heros = ModLoader.GetMod("HEROsMod");
            if (heros is null)
                return;

            Type? tp = heros.Code.GetType("HEROsMod.HEROsModServices.Teleporter");
            if (tp is null)
            {
                MapMarkers.Logger.Warn("Cannot find type HEROsMod.HEROsModServices.Teleporter in HEROsMod");
                return;
            }

            TeleporterServiceInstance = tp.GetField("instance", BindingFlags.Static | BindingFlags.Public);
            TeleporterServiceHasPermissionToUse = tp.GetProperty("HasPermissionToUse");
        }

        void ILoadable.Unload()
        {
            TeleporterServiceHasPermissionToUse = null;
            TeleporterServiceInstance = null;
        }



        void PauseFullscreenMapTP()
        {
            if (TpPermissionOldValue)
                return;

            bool? value = TeleporterService_HasPermissionToUse;
            if (value is null or false)
            {
                TpPermissionOldValue = false;
                return;
            }

            TpPermissionOldValue = true;
            TeleporterService_HasPermissionToUse = false;
        }

        void ResumeFullscreenMapTP()
        {
            if (!TpPermissionOldValue)
                return;
            TpPermissionOldValue = false;

            TeleporterService_HasPermissionToUse = true;
        }
    }
}
