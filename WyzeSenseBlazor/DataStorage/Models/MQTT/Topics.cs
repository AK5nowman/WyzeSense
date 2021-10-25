using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WyzeSenseBlazor.DataStorage.Models
{
    public class Topics
    {
        public string RootTopic { get; set; }
        public string TemplateName { get; set; }
    }
}

