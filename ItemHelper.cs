using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ID;
using Terraria.ModLoader;

namespace MapMarkers
{
    public static class ItemHelper 
    {
        static IReadOnlyDictionary<int, string>? ItemIds;

        static void InitIds()
        {
            if (ItemIds is not null)
                return;

            ItemIds = new Dictionary<int, string>(typeof(ItemID)
                .GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(short) && f.IsLiteral)
                .Select(f => new KeyValuePair<int, string>((int)(short)f.GetValue(null)!, f.Name)));
        }

        public static bool IsValidId(int id)
        {
            if (id < ItemID.Count)
            {
                InitIds();
                return ItemIds!.ContainsKey(id);
            }
            return id < ItemLoader.ItemCount;
        }

        public static int GetByName(string name)
        {
            string? mod = null;

            if (name.Contains('.'))
            {
                string[] split = name.Split('.', count: 2);
                mod = split[0];
                name = split[1];
            }

            if (mod is null)
            {
                if (TryGetFirstVanillaItemId(name, out int id))
                    return id;

                for (int i = ItemID.Count; i < ItemLoader.ItemCount; i++)
                {
                    ModItem item = ItemLoader.GetItem(i);
                    if (item.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                        return i;
                }

                throw new Exception($"\"{name}\" matches no items");
            }

            Mod? modInstance = ModLoader.Mods.FirstOrDefault(m => m.Name.StartsWith(mod, StringComparison.InvariantCultureIgnoreCase));
            if (modInstance is null)
                throw new Exception($"\"{mod}\" matches no mods");

            foreach (ModItem item in modInstance.GetContent<ModItem>())
                if (item.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
                    return item.Type;

            throw new Exception($"\"{name}\" matches no items in {modInstance.Name}");
        }

        static bool TryGetFirstVanillaItemId(string startingWith, out int id)
        {
            InitIds();
            id = 0;
            foreach (var kvp in ItemIds!)
                if (kvp.Value.StartsWith(startingWith, StringComparison.InvariantCultureIgnoreCase))
                {
                    id = kvp.Key;
                    return true;
                }

            return false;
        }
    }
}
