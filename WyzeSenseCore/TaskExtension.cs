using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    internal static class TaskExtension
    {
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Logger.Debug($"[FireAndForget] Task Error: {e.ToString()}");
            }
        }
    }
}
