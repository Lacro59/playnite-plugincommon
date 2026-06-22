namespace CommonPluginsControls.Stores.Gog
{
    public class PanelViewModel : StorePanelViewModelBase
    {
        protected override string LoginErrorContext => "GOG login failed";

        protected override string LogoutErrorContext => "GOG logout failed";
    }
}
