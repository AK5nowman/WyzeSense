using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{

    public class WyzeSensorTypeModel
    {

        [Key]
        public int Id { get; set; }

        [MaxLength(16)]
        public string Type { get; set; }

        //Navigational
        public  List<WyzeSensorStateModel>  States { get; set; } = new();
    }
}
