using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    internal static class TaskExtension
    {
        public static async void FireAndForget(this Task task, IWyzeSenseLogger logger)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                logger.LogDebug($"[FireAndForget] Task Error: {e.ToString()}");
            }
        }
    }
}
