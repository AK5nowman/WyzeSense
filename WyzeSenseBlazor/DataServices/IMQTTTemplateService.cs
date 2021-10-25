using System.Threading.Tasks;
using WyzeSenseBlazor.DataStorage.Models;

namespace WyzeSenseBlazor.DataServices
{
    public interface IMQTTTemplateService
    {
        Task<Template[]> GetTemplatesAsync();
    }
}