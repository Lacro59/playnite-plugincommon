using System;

namespace CommonPluginsShared.Models
{
    public class DataGame
    {
        public Guid Id { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }

        public bool IsDeleted { get; set; }
        public ulong CountData { get; set; }

        /// <summary>
        /// Overridden so that WPF's editable ComboBox displays the game name
        /// in its text box after the user selects an item.
        /// Without this, WPF calls the default Object.ToString() which returns
        /// the fully-qualified type name, breaking the display after selection.
        /// </summary>
        public override string ToString() => Name ?? string.Empty;
    }
}