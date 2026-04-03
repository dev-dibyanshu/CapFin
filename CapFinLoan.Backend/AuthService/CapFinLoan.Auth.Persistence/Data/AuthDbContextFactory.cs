using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CapFinLoan.Auth.Persistence.Data;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=CapFinLoanDb;User Id=sa;Password=YourStrong@Pass123;TrustServerCertificate=True");
        return new AuthDbContext(optionsBuilder.Options);
    }
}
