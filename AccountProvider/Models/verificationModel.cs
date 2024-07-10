namespace AccountProvider.Models
{
    public class VerificationModel
    {
        public string Email { get; set; } = null!;
        public string VerificationCode { get; set; } = null!;
    }
}