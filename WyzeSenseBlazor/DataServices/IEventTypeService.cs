using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public interface IEventTypeService
    {
        Task<WyzeEventTypeModel[]> GetEventTypesAsync();
        Task UpdateEventyTypeAsync(WyzeEventTypeModel wyzeEventTypeModel);
    }
}