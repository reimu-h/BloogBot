using BloogBot.Game.Cache;
using System;
using System.Collections.Generic;

namespace BloogBot.Game.Frames
{
    public class LootFrame
    {
        readonly public IList<LootItem> LootItems = new List<LootItem>();

        public LootFrame()
        {
            var countResult = Functions.LuaCallWithResult("{0} = GetNumLootItems()");
            if (countResult.Length == 0 || !int.TryParse(countResult[0], out var numLootItems))
            {
                return;
            }

            for (var slot = 1; slot <= numLootItems; slot++)
            {
                var result = Functions.LuaCallWithResult($@"
                    if LootSlotIsCoin({slot}) then
                        {{0}} = 'coin'
                    elseif LootSlotIsItem({slot}) then
                        local link = GetLootSlotLink({slot})
                        local id = 0
                        if link then
                            local _, _, parsedId = string.find(link, 'item:(%d+)')
                            id = parsedId or 0
                        end
                        {{0}} = id
                    end
                ");
                if (result.Length == 0 || string.IsNullOrEmpty(result[0]))
                {
                    continue;
                }

                if (result[0] == "coin")
                {
                    LootItems.Add(new LootItem(null, 0, slot - 1, true));
                }
                else if (int.TryParse(result[0], out var itemId))
                {
                    var cacheAddr = itemId > 0 ? Functions.GetItemCacheEntry(itemId) : IntPtr.Zero;
                    var itemCacheEntry = cacheAddr != IntPtr.Zero ? MemoryManager.ReadItemCacheEntry(cacheAddr) : null;
                    var itemCacheInfo = itemCacheEntry != null ? new ItemCacheInfo(itemCacheEntry) : null;
                    LootItems.Add(new LootItem(itemCacheInfo, itemId, slot - 1, false));
                }
            }
        }

        static public void Close() => Functions.LuaCall("CloseLoot()");
    }

    public class LootItem
    {
        internal LootItem(
            ItemCacheInfo info,
            int itemId,
            int lootSlot,
            bool isCoins)
        {
            Info = info;
            ItemId = itemId;
            LootSlot = lootSlot;
            IsCoins = isCoins;
        }

        internal int LootSlot { get; set; }

        public int ItemId { get; set; }
        
        public ItemCacheInfo Info { get; }

        public bool IsCoins { get; }

        public void Loot() => Functions.LootSlot(LootSlot);
    }
}
