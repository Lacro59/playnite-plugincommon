using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPluginsStores.Gog.Models
{
    public class PriceResult
    {
        public List<PriceItem> items { get; set; }
    }

    public class PriceSelf
    {
        public string href { get; set; }
    }

    public class PriceLinks
    {
        public PriceSelf self { get; set; }
    }

    public class PriceCurrency
    {
        public string code { get; set; }
    }

    public class Price
    {
        public PriceCurrency currency { get; set; }
        public string basePrice { get; set; }
        public string finalPrice { get; set; }
        public string bonusWalletFunds { get; set; }
    }

    public class PriceProduct
    {
        public int id { get; set; }
    }

    public class PriceEmbedded
    {
        public List<Price> prices { get; set; }
        public PriceProduct product { get; set; }
    }

    public class PriceItem
    {
        public PriceLinks _links { get; set; }
        public PriceEmbedded _embedded { get; set; }
    }
}
