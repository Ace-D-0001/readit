using System;
using System.Collections.Generic;

namespace Read_It.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;          // e.g. "Computer Science and Engineering"
        public string Abbreviation { get; set; } = string.Empty;  // e.g. "CSE"

        public virtual ICollection<CourseDepartment> CourseDepartments { get; set; } = new List<CourseDepartment>();
    }
}
