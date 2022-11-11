using Microsoft.EntityFrameworkCore;
using File = Mp3Storage.Core.Models.File;

namespace Mp3Storage.Core;

public class Mp3StorageContext : DbContext
{
    public DbSet<File>? Files { get; set; }

    public Mp3StorageContext(DbContextOptions options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
}