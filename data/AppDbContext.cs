using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ServiceTreeDemo.Models;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.Json;

namespace ServiceTreeDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ServiceNode> Nodes => Set<ServiceNode>();
    public DbSet<ServiceConnection> Connections => Set<ServiceConnection>();
    public DbSet<ServiceProject> Projects => Set<ServiceProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ServiceNode
        modelBuilder.Entity<ServiceNode>(entity =>
        {
            entity.HasKey(n => n.Id);

            // Store List<Guid> ProjectIds as a JSON string column
            var guidListConverter = new ValueConverter<List<Guid>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
            );

            entity.Property(n => n.ProjectIds)
                  .HasConversion(guidListConverter)
                  .HasColumnType("TEXT");
        });

        // ServiceConnection
        modelBuilder.Entity<ServiceConnection>(entity =>
        {
            entity.HasKey(c => c.Id);
        });

        // ServiceProject
        modelBuilder.Entity<ServiceProject>(entity =>
        {
            entity.HasKey(p => p.Id);
        });
    }
}