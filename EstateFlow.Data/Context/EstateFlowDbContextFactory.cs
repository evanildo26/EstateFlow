using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using EstateFlow.Data.Context;

namespace EstateFlow.Data;

public class EstateFlowDbContextFactory : IDesignTimeDbContextFactory<EstateFlowDbContext>
{
    // Used only by EF Core tooling (Add-Migration / Update-Database).
    // The connection string is read from the environment so no credentials
    // live in source control. Before running migrations:
    //   PowerShell:  $env:ConnectionStrings__EstateFlowDb = "Server=tcp:..."
    public EstateFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__EstateFlowDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set the 'ConnectionStrings__EstateFlowDb' environment variable " +
                "before running EF Core migrations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<EstateFlowDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new EstateFlowDbContext(optionsBuilder.Options);
    }
}
