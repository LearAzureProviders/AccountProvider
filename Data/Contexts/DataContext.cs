

using Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Data.Contexts;

public class DataContext(DbContextOptions options) : IdentityDbContext<UserAccount>(options)
{
    public DbSet<UserAddress> UserAddresses { get; set; }

}
