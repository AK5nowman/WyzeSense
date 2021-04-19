using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public interface IMQTTTemplateService
    {
        Task<Template[]> GetTemplatesAsync();
    }
}