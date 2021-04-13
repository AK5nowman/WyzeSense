using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class ConfigurationModel
    {
        [Key]
        [MaxLength(128)]
        public string Setting { get; set; }

        [MaxLength(256)]
        public string Value { get; set; }
    }
}
