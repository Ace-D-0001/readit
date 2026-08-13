namespace Read_It.Models
{
    /// <summary>
    /// Join table: Course ↔ Department (many-to-many).
    /// General courses have no rows here — they're visible to everyone.
    /// DepartmentSpecific courses have one or more rows.
    /// </summary>
    public class CourseDepartment
    {
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }
    }
}
