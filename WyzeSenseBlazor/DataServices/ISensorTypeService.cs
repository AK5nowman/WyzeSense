using System.Threading.Tasks;
using WyzeSenseBlazor.DatabaseProvider.Models;

namespace WyzeSenseBlazor.DataServices
{
    public interface ISensorTypeService
    {
        Task<WyzeSensorTypeModel[]> GetSensorTypesAsync();
        Task UpdateSensorTypeAsync(WyzeSensorTypeModel wyzeSensorTypeModel);
        Task UpdateStateAsync(WyzeSensorStateModel wyzeSensorStateModel);
    }
}