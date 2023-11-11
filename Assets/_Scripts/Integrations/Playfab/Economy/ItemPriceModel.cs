using System;

namespace CosmicShore.Integrations.Playfab.Economy
{
	[Serializable]
    public class ItemPriceModel
    {
        /// <summary>
        /// The Item Id of the price.
        /// </summary>
        public string ItemId;
        /// <summary>
        /// The amount of the price.
        /// </summary>
        public int Amount;
        
    }
}