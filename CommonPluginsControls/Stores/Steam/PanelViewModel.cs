using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonPluginsStores;
using CommonPluginsStores.Steam;
using CommonPluginsStores.Steam.Models;
using Playnite.SDK;

namespace CommonPluginsControls.Stores.Steam
{
    public class PanelViewModel : ObservableObject
    {
        internal SteamApi SteamApi { get; set; }
        public SteamUser User => SteamApi.CurrentUser;

        public bool UseApi { get; set; } = true;
        public bool UseAuth { get; set; } = true;


        public AuthStatus AuthStatus => SteamApi.IsUserLoggedIn ? AuthStatus.Ok : AuthStatus.AuthRequired;


        public RelayCommand<object> LoginCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                SteamApi.Login();
                OnPropertyChanged(nameof(AuthStatus));
            });
        }
    }
}
