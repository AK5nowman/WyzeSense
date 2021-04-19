using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class Payload
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [MaxLength(128)]
        public string Name { get; set; }

        [MaxLength(128)]
        public string Property { get; set; }
        public int PayloadPackageId { get; set; }

    }
}
