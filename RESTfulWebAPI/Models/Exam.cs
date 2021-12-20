using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RESTfulWebAPI.Models
{
    public class Exam : TableEntity
    {
        [Required]
        public int StudentId { get; set; }
        [Required]
        public int CourseId { get; set; }
        [Required]
        public int Points { get; set; }
        public bool Passed { get; set; }       
    }
}
