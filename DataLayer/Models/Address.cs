using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataLayer.Models
{
    public class Address
    {
        public int Id { get; set; }
        [MinLength(3)]
        public string Street { get; set; }       
        public string City { get; set; }
        public string Country { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
        public ICollection<Student> Students { get; set; }      
    }
    public class AdditionalInfo
    {
        public string StreetNumber { get; set; }
        public string AdditionalNumber { get; set; }
        public string Zip { get; set; }
    }
}
