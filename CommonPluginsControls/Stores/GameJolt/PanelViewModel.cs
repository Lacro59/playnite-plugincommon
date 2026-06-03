namespace CommonPluginsControls.Stores.GameJolt
{
    public class PanelViewModel : StorePanelViewModelBase
    {
        protected override string LoginErrorContext => "Game Jolt login failed";

        protected override string LogoutErrorContext => "Game Jolt logout failed";
    }
}
