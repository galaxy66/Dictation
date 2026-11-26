using Dictation.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Api.Data;

public sealed class DictationDbContext(DbContextOptions<DictationDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Letter> Letters => Set<Letter>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<Sentence> Sentences => Set<Sentence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Letter>(entity =>
        {
            entity.Property(letter => letter.Character).HasMaxLength(32).IsRequired();
            entity.HasIndex(letter => letter.Character).IsUnique();
            entity.HasIndex(letter => letter.SortOrder);
            entity.HasData(
                new Letter { Id = 1, Character = "آ ا", SortOrder = 0 },
                new Letter { Id = 2, Character = "ب", SortOrder = 1 },
                new Letter { Id = 3, Character = "ا", SortOrder = 2 },
                new Letter { Id = 4, Character = "د", SortOrder = 3 },
                new Letter { Id = 5, Character = "س", SortOrder = 4 },
                new Letter { Id = 6, Character = "ت", SortOrder = 5 },
                new Letter { Id = 7, Character = "او", SortOrder = 6 },
                new Letter { Id = 8, Character = "ر", SortOrder = 7 });
        });

        builder.Entity<Word>(entity =>
        {
            entity.Property(word => word.Text).HasMaxLength(200).IsRequired();
            entity.HasOne(word => word.Letter)
                .WithMany(letter => letter.Words)
                .HasForeignKey(word => word.LetterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Sentence>(entity =>
        {
            entity.Property(sentence => sentence.Text).HasMaxLength(1000).IsRequired();
            entity.HasOne(sentence => sentence.Letter)
                .WithMany(letter => letter.Sentences)
                .HasForeignKey(sentence => sentence.LetterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
