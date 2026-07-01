using Microsoft.EntityFrameworkCore;

public class LockboxDbContext : DbContext
{
    public LockboxDbContext(DbContextOptions<LockboxDbContext> options) : base(options)
    {
    }

    public DbSet<FileRecord> Ids { get; set; }
    public DbSet<FileRecord> OriginalFileNames { get; set; }
    public DbSet<FileRecord> GUIDFileNames { get; set; }
    public DbSet<FileRecord> UploadTimes { get; set; }
}