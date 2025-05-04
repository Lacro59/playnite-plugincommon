using CommonPluginsShared.Extensions;
using Playnite.SDK.Data;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents downloadable content (DLC) information for a game.
    /// Inherits from <see cref="BasicGameInfos"/> to include basic game details.
    /// </summary>
    public class DlcInfos : BasicGameInfos
    {
        private bool _isOwned;

        /// <summary>
        /// Gets or sets whether the DLC is owned by the user.
        /// </summary>
        public bool IsOwned { get => _isOwned; set => _isOwned = value; }

        private string _price;

        /// <summary>
        /// Gets or sets the price of the DLC as a string (e.g., "19,99 USD").
        /// </summary>
        public string Price { get => _price; set => _price = value; }

        private string _priceBase;

        /// <summary>
        /// Gets or sets the base price of the DLC before any discounts, as a string (e.g., "29,99 USD").
        /// </summary>
        public string PriceBase { get => _priceBase; set => _priceBase = value; }

        /// <summary>
        /// Gets the price of the DLC as a numeric value, parsed from the <see cref="Price"/> property.
        /// </summary>
        [DontSerialize]
        public double PriceNumeric
        {
            get
            {
                if (Price.IsNullOrEmpty())
                {
                    return 0;
                }

                string temp = Price.Replace(",--", string.Empty).Replace(".--", string.Empty).Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "--", string.Empty);
                temp = Regex.Split(temp, @"\s+").Where(s => s != string.Empty).First();
                temp = temp.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                temp = Regex.Replace(temp, @"[^\d" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "-]", "");

                double.TryParse(temp, out double dPrice);
                return dPrice;
            }
        }

        /// <summary>
        /// Gets the base price of the DLC as a numeric value, parsed from the <see cref="PriceBase"/> property.
        /// </summary>
        [DontSerialize]
        public double PriceBaseNumeric
        {
            get
            {
                if (PriceBase.IsNullOrEmpty())
                {
                    return 0;
                }

                string temp = PriceBase.Replace(",--", string.Empty).Replace(".--", string.Empty).Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "--", string.Empty);
                temp = Regex.Split(temp, @"\s+").Where(s => s != string.Empty).First();
                temp = temp.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                temp = Regex.Replace(temp, @"[^\d" + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "-]", "");

                double.TryParse(temp, out double dPrice);
                return dPrice;
            }
        }

        /// <summary>
        /// Gets whether the DLC is free (i.e., its price is zero).
        /// </summary>
        [DontSerialize]
        public bool IsFree => !Price.IsNullOrEmpty() && PriceNumeric == 0;


        /// <summary>
        /// Gets whether the DLC is discounted compared to the base price.
        /// </summary>
        [DontSerialize]
        public bool IsDiscount => !Price.IsEqual(PriceBase);
    }
}
