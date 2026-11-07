namespace CommonPluginsControls.Stores.Ea
{
    /// <summary>
    /// View model for the EA store settings panel.
    /// </summary>
    public class PanelViewModel : StorePanelViewModelBase
    {
        protected override string LoginErrorContext => "EA login failed";

        protected override string LogoutErrorContext => "EA logout failed";
    }
}
