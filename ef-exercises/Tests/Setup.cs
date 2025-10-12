using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using tests.Data;
using tests.Exercises;
using tests.Interfaces;

namespace tests.Tests;

public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<CompanyDbContext>(factory =>
        {
            var postgreSqlContainer = new PostgreSqlBuilder().Build();
            postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
            var connectionString = postgreSqlContainer.GetConnectionString();
            var options = new DbContextOptionsBuilder<CompanyDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            var ctx = new CompanyDbContext(options);
            ctx.Database.EnsureCreated();
            SeedData.SeedAsync(ctx).GetAwaiter().GetResult();
            return ctx;
        });
        services.AddScoped<IEfExercises, EfExercises>();
        services.AddScoped<IEfExercisesIdempotentUpdates, EfExercisesIdempotentUpdates>();
    }
}