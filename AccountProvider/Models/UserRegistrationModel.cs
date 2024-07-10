

using Microsoft.Extensions.Logging.Abstractions;

namespace AccountProvider.Models;


public class UserRegistrationModel
{

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string Email { get; set; } = null!;
    public string? Password { get; set; } = null!;
}
