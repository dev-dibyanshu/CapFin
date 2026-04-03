using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CapFinLoan.Document.Persistence.Data;

public class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost,1433;Database=CapFinLoanDb;User Id=sa;Password=YourStrong@Pass123;TrustServerCertificate=True");
        return new DocumentDbContext(optionsBuilder.Options);
    }
}
