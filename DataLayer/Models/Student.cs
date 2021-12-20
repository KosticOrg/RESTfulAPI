using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public int StudentNumber { get; set; }
        public string CreatedBy { get; set; }
        public string Avatar { get; set; }
        public int AddressId { get; set; }
        public Address Address { get; set; }      
    }
}
