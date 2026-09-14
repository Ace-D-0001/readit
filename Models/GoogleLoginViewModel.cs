namespace Read_It.Models
{
    public class GoogleLoginViewModel
    {
        public string? ReturnUrl { get; set; }
        public string Role { get; set; } = "Student";
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}
