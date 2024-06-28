using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces
{
    public interface IConfigurationRepository
    {
        Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync(bool isActiveFilter = false);
        Task<List<ConfigurationItem>> GetAllConfigurationItemsWithPaginationAsync(int pageSize, int page, bool isActiveFilter = false, string searchText = null);
        Task<List<ConfigurationItem>> GetActiveConfigurationItemsByApplicationNameAsync(string applicationName);
        Task<ConfigurationItem> GetActiveConfigurationItemAsync(string applicationName, string key);
        Task InsertConfigItemAsync(ConfigurationItem configurationItem);
        Task<bool> UpdateConfigItemAsync(ConfigurationItem configurationItem);
        Task<bool> DeleteConfigItemAsync(string id);
        Task<long> GetCountAsync(bool isActiveFilter = false, string searchText = null);
    }
}
