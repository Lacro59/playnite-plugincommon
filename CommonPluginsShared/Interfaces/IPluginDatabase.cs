using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommonPluginsShared.Interfaces
{
    public interface IPluginDatabase
    {
        Task<bool> InitializeDatabase();
    }
}
