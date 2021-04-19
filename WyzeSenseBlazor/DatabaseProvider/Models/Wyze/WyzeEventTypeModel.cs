using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class WyzeEventTypeModel
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(16)]
        public string Type { get; set; }

    }
}
