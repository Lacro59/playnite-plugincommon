namespace CommonPluginsShared.UI
{
    /// <summary>
    /// Represents a key-value pair for application resource injection.
    /// Used by <see cref="UIHelper.AddResources"/> to add or update global WPF resources.
    /// </summary>
    public class ResourcesList
    {
        /// <summary>Gets or sets the resource key.</summary>
        public string Key { get; set; }

        /// <summary>Gets or sets the resource value.</summary>
        public object Value { get; set; }
    }
}