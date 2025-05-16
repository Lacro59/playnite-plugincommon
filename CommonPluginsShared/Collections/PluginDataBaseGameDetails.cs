namespace CommonPluginsShared.Collections
{
    public abstract class PluginDataBaseGameDetails<T, Y> : PluginDataBaseGame<T>
    {
        public abstract Y ItemsDetails { get; set; }
    }
}