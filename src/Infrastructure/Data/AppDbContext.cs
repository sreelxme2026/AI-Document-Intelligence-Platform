using Application.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<Embedding> Embeddings => Set<Embedding>();

    public DbSet<QueryHistory> QueryHistories => Set<QueryHistory>();

    public DbSet<QueryHistorySource> QueryHistorySources => Set<QueryHistorySource>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureDocument(builder);
        ConfigureDocumentChunk(builder);
        ConfigureEmbedding(builder);
        ConfigureQueryHistory(builder);
        ConfigureQueryHistorySource(builder);
    }

    private static void ConfigureDocument(ModelBuilder builder)
    {
        builder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.FileName)
    .IsRequired()
    .HasMaxLength(255);

            entity.Property(d => d.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(d => d.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.StoragePath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(d => d.Status)
                .IsRequired();

            entity.Property(d => d.StatusMessage)
    .HasColumnType("nvarchar(max)");

            entity.Property(d => d.Title)
                .HasMaxLength(500);

            entity.Property(d => d.Description)
                .HasColumnType("nvarchar(max)");

            entity.Property(d => d.Tags)
                .HasMaxLength(1000);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany<DocumentChunk>()
                .WithOne()
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDocumentChunk(ModelBuilder builder)
    {
        builder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Content)
    .IsRequired()
    .HasColumnType("nvarchar(max)");

            entity.HasIndex(c => new { c.DocumentId, c.ChunkIndex })
                .IsUnique();
        });
    }

    private static void ConfigureEmbedding(ModelBuilder builder)
    {
        builder.Entity<Embedding>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VectorJson)
    .IsRequired()
    .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Model)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.DocumentChunkId)
                .IsUnique();

            entity.HasOne<DocumentChunk>()
                .WithOne()
                .HasForeignKey<Embedding>(e => e.DocumentChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureQueryHistory(ModelBuilder builder)
    {
        builder.Entity<QueryHistory>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.QueryText)
    .IsRequired()
    .HasMaxLength(4000);

            entity.Property(q => q.AnswerText)
    .HasColumnType("nvarchar(max)");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<QueryHistorySource>()
                .WithOne()
                .HasForeignKey(s => s.QueryHistoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureQueryHistorySource(ModelBuilder builder)
    {
        builder.Entity<QueryHistorySource>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasOne<DocumentChunk>()
                .WithMany()
                .HasForeignKey(s => s.DocumentChunkId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}