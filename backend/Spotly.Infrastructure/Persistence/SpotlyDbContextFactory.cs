using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Spotly.Infrastructure.Persistence;

public sealed class SpotlyDbContextFactory : IDesignTimeDbContextFactory<SpotlyDbContext>
{
    public SpotlyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SpotlyDbContext>()
            .UseSqlServer(
                "Server=tcp:spotly-black-sql-iwrqpc22xsrvm.database.windows.net,1433;Database=SpotlyDB;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;",
                sql => sql.EnableRetryOnFailure(3))
            .Options;
        return new SpotlyDbContext(options);
    }
}
