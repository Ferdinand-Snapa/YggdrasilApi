using Microsoft.EntityFrameworkCore;
using YggdrasilApi.Models;

namespace YggdrasilApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Decleration> Declerations => Set<Decleration>();
    public DbSet<Graph> Graphs => Set<Graph>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Connection> Connections => Set<Connection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Node.Id is a client-generated GUID string — tell EF not to use the DB to generate it.
        modelBuilder.Entity<Node>()
            .Property(n => n.Id)
            .ValueGeneratedNever();
    }
}
