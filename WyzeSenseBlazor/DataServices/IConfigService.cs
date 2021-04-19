using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public interface IConfigService
    {
        Task<ConfigurationModel[]> GetConfigAsync();
        Task UpdateConfigAsync(ConfigurationModel configurationModel);
    }
}