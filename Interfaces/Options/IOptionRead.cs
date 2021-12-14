using Microsoft.Xaml.Behaviors.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StudentSeating.Interfaces
{
    interface IOptionRead
    {
        public Task<Dictionary<string, string>> LoadGeneralConfigurations(Dictionary<string, string> lastUpdated);

        public Task<bool> SaveGeneralConfigurations(Dictionary<string, string> configs);

        public Task<bool> SaveGeneralConfigurationSubset(Dictionary<string, string> configs);

        public Task<bool> SaveGeneralConfiguration(string key, string value);
        public Task<Dictionary<string, string>> LoadPrivateConfigurations(Dictionary<string, string> lastUpdated);

        public Task<bool> SavePrivateConfigurations(Dictionary<string, string> configs);

        public Task<bool> SavePrivateConfigurationSubset(Dictionary<string, string> configs);

        public Task<bool> SavePrivateConfiguration(string key, string value);
    }
}
