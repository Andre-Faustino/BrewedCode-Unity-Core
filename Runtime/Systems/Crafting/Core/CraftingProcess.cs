using System;
using UnityEngine;

namespace BrewedCode.Crafting
{
    /// <summary>
    /// Legacy class for backward compatibility.
    /// DO NOT USE for new code. Use ICraftingService and CraftingStationInfo instead.
    /// This class is only maintained to support existing code that depends on it.
    /// </summary>
    [Obsolete("Legacy class. Use ICraftingService and CraftingStationInfo instead. This class exists only for backward compatibility.")]
    public class CraftingProcess
    {
        public readonly Guid Id = Guid.NewGuid();

        public CraftingStation m_CraftingStation;

        public ICraftable CurrentCraftable { get; private set; }

        public float craftTimeProgress { get; set; } = 0f;
        public float craftTimeProgressRemaining { get; set; } = 0f;
        public float craftTimeProgressElapsed { get; set; } = 0f;
        public float craftTimeProgressTotal { get; set; } = 0f;

        private CraftingProcess(CraftingStation craftingStation, ICraftable craftable)
        {
            m_CraftingStation = craftingStation;
            CurrentCraftable = craftable;
        }

        public static CraftingProcess Create(CraftingStation craftingStation, ICraftable craftable)
        {
            return new CraftingProcess(craftingStation, craftable);
        }

        /// <summary>
        /// Legacy method. Does nothing. Left for source compatibility only.
        /// </summary>
        [Obsolete]
        public void StartProcess()
        {
        }

        /// <summary>
        /// Legacy method. Does nothing. Left for source compatibility only.
        /// </summary>
        [Obsolete]
        public void StopProcess()
        {
        }
    }
}
