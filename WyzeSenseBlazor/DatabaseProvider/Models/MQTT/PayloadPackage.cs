using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DatabaseProvider.Models
{
    public class PayloadPackage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(128)]
        public string Topic { get; set; }
        public int TemplateId { get; set; }

        //Navigational
        public Template Template { get; set; }
        public List<Payload> Payloads { get; set; }
    }

}
