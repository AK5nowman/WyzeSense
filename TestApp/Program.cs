using System;
using System.Linq;
using WyzeSenseBlazor.DatabaseProvider;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace TestApp
{
    public static class TaskExtensionsss
    {

    }
    class Program
    {
        static void Main(string[] args)
        {
            var options = new DbContextOptionsBuilder().UseSqlite("Data Source=wyzesensor.db");
            using (var db = new WyzeDbContext( options.Options))
            {
                db.Database.EnsureDeleted();
                d
            }
            Console.WriteLine("Hello World!");
        }

    }
}
