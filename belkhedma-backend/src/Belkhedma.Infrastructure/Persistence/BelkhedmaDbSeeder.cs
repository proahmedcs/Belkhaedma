using Belkhedma.Domain;
using Microsoft.EntityFrameworkCore;

namespace Belkhedma.Infrastructure.Persistence;

public static class BelkhedmaDbSeeder
{
    public static async Task SeedAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Providers.AnyAsync(cancellationToken))
        {
            return;
        }

        var providers = new List<Provider>
        {
            new()
            {
                Code = "enaya",
                NameAr = "شركة عناية",
                NameEn = "Enaya",
                HasApiAccess = true
            },
            new()
            {
                Code = "fawran",
                NameAr = "فوراً",
                NameEn = "Fawran",
                HasApiAccess = true
            },
            new()
            {
                Code = "tamkeen",
                NameAr = "تمكين",
                NameEn = "Tamkeen",
                HasApiAccess = false
            },
            new()
            {
                Code = "mueen",
                NameAr = "معين",
                NameEn = "Mueen",
                HasApiAccess = false
            }
        };

        await dbContext.Providers.AddRangeAsync(providers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
