using System;
using System.Collections.Generic;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Represents a game achievement.
    /// </summary>
    public class GameAchievement : ObservableObject
    {
        private string _id;

        /// <summary>
        /// Gets or sets the unique identifier for the achievement.
        /// </summary>
        public string Id { get => _id; set => _id = value; }

        private string _name;

        /// <summary>
        /// Gets or sets the name of the achievement.
        /// </summary>
        public string Name { get => _name; set => _name = value; }

        private string _description;

        /// <summary>
        /// Gets or sets the description of the achievement.
        /// </summary>
        public string Description { get => _description; set => _description = value; }

        private string _urlUnlocked;

        /// <summary>
        /// Gets or sets the URL for the unlocked achievement.
        /// </summary>
        public string UrlUnlocked { get => _urlUnlocked; set => _urlUnlocked = value; }

        private string _urlLocked;

        /// <summary>
        /// Gets or sets the URL for the locked achievement.
        /// </summary>
        public string UrlLocked { get => _urlLocked; set => _urlLocked = value; }

        private DateTime? _dateUnlocked;

        /// <summary>
        /// Gets or sets the date and time when the achievement was unlocked.
        /// Converts to local time when reading, and ensures UTC storage when setting.
        /// If the provided value is DateTime.MinValue, it is treated as null.
        /// </summary>
        public DateTime? DateUnlocked
        {
            get
            {
                if (_dateUnlocked.HasValue && _dateUnlocked.Value == DateTime.MinValue)
                {
                    return null;
                }
                return _dateUnlocked?.ToLocalTime();
            }
            set
            {
                if (!value.HasValue || value.Value == default)
                {
                    _dateUnlocked = null;
                }
                else
                {
                    _dateUnlocked = value.Value.ToUniversalTime();
                }
            }
        }

        private float _percent;

        /// <summary>
        /// Gets or sets the percentage of gamers who have unlocked this achievement.
        /// </summary>
        public float Percent { get => _percent; set => _percent = value; }

        private bool _isHidden;

        /// <summary>
        /// Gets or sets whether the achievement is hidden from the player's achievement list.
        /// </summary>
        public bool IsHidden { get => _isHidden; set => _isHidden = value; }

        private float _gamerScore;

        /// <summary>
        /// Gets or sets the gamer score for unlocking the achievement.
        /// </summary>
        public float GamerScore { get => _gamerScore; set => _gamerScore = value; }


        private int _categoryOrder;

        /// <summary>
        /// Gets or sets the order of the achievement within its category.
        /// </summary>
        public int CategoryOrder { get => _categoryOrder; set => _categoryOrder = value; }

        private string _category;

        /// <summary>
        /// Gets or sets the category of the achievement (e.g., "Story", "Combat", "Exploration").
        /// </summary>
        public string Category { get => _category; set => _category = value; }

        private string _categoryIcon;

        /// <summary>
        /// Gets or sets the icon associated with the category of the achievement.
        /// </summary>
        public string CategoryIcon { get => _categoryIcon; set => _categoryIcon = value; }


        private bool _categoryDlc;

        /// <summary>
        /// Gets or sets whether the achievement belongs to a DLC (downloadable content) category.
        /// </summary>
        public bool CategoryDlc { get => _categoryDlc; set => _categoryDlc = value; }
    }
}
