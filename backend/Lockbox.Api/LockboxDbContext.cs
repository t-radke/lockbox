using Microsoft.EntityFrameworkCore;

public class LockboxDbContext : DbContext
{
    public LockboxDbContext(DbContextOptions<LockboxDbContext> options) : base(options)
    {
    }

    public DbSet<FileRecord> FileRecords { get; set; }

}