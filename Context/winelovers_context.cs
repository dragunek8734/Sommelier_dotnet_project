// Context/WineLoversContext.cs
using Microsoft.EntityFrameworkCore;
using dotnetprojekt.Models;
using System;

namespace dotnetprojekt.Context
{
    public class WineLoversContext : DbContext
    {
        public WineLoversContext(DbContextOptions<WineLoversContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Wine> Wines { get; set; }
        public DbSet<Grape> Grapes { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<WineType> WineTypes { get; set; }
        public DbSet<WineAcidity> WineAcidities { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Winery> Wineries { get; set; }
        public DbSet<Admin> Admins { get; set; } // Add this line to include Admin entity
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<LoginHistory> LoginHistory { get; set; }
        public DbSet<TopRatedWinesView> TopRatedWines { get; set; }

        public DbSet<ExperienceLevel> ExperienceLevels { get; set; }
  

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure tables
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Rating>().ToTable("Ratings", schema: "public");
            modelBuilder.Entity<Wine>().ToTable("Wines");
            modelBuilder.Entity<Grape>().ToTable("Grapes");
            modelBuilder.Entity<Dish>().ToTable("Dishes");
            modelBuilder.Entity<WineType>().ToTable("Wine_types");
            modelBuilder.Entity<WineAcidity>().ToTable("Wine_acidity");
            modelBuilder.Entity<Country>().ToTable("Country");
            modelBuilder.Entity<Region>().ToTable("Region");
            modelBuilder.Entity<Winery>().ToTable("Winery");
            modelBuilder.Entity<Admin>().ToTable("Admins"); // Add this line to configure Admin entity
            modelBuilder.Entity<LoginHistory>().ToTable("LoginHistory");


            modelBuilder.Entity<User>()
                .HasOne(u => u.ExperienceLevel)
                .WithMany()
                .HasForeignKey(u => u.ExperienceLvlId)  
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Admin>(entity =>
            {
                // Primary key = Foreign key to user id
                entity.HasKey(a => a.UserId);

                // One-one relation with user
                entity.HasOne(a => a.User)
                    .WithOne(u => u.Admin)
                    .HasForeignKey<Admin>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // deleting admin if user is deleted
            });

            modelBuilder.Entity<UserPreference>().ToTable("UserPreferences");
            
            // Configure LoginHistory relationship
            modelBuilder.Entity<LoginHistory>()
                .HasOne(l => l.User)
                .WithMany(u => u.LoginHistory)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationships
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Wine)
                .WithMany(w => w.Ratings)
                .HasForeignKey(r => r.WineId);

            modelBuilder.Entity<Wine>()
                .HasOne(w => w.Type)
                .WithMany(t => t.Wines)
                .HasForeignKey(w => w.TypeId);

            modelBuilder.Entity<Wine>()
                .HasOne(w => w.Acidity)
                .WithMany(a => a.Wines)
                .HasForeignKey(w => w.AcidityId);

            modelBuilder.Entity<Wine>()
                .HasOne(w => w.Country)
                .WithMany(c => c.Wines)
                .HasForeignKey(w => w.CountryId);            // Wine-Region relationship removed as wines are now associated with wineries instead

            // Configure one-to-many relationship between Winery and Wine
            modelBuilder.Entity<Wine>()
                .HasOne(w => w.Winery)
                .WithMany(wn => wn.Wines)
                .HasForeignKey(w => w.WineryId)
                .IsRequired(false)  // A wine can exist without a winery
                .OnDelete(DeleteBehavior.SetNull);  // If a winery is deleted, set WineryId to null in wines

            modelBuilder.Entity<Region>()
                .HasOne(r => r.Country)
                .WithMany(c => c.Regions)
                .HasForeignKey(r => r.CountryId);

            modelBuilder.Entity<Winery>()
                .HasOne(w => w.Region)
                .WithMany(r => r.Wineries)
                .HasForeignKey(w => w.RegionId);

            // Configure PostgreSQL specific types
            modelBuilder.Entity<Wine>()
                .Property(w => w.GrapeIds)
                .HasColumnType("integer[]");

            modelBuilder.Entity<Wine>()
                .Property(w => w.PairWithIds)
                .HasColumnType("integer[]");

            modelBuilder.Entity<Wine>()
                .Property(w => w.Vintages)
                .HasColumnType("varchar[]");

            modelBuilder.Entity<Wine>()
                .Property(w => w.SearchVector)
                .HasColumnType("tsvector")
                .IsRequired(false);

            // Configure UserPreference PostgreSQL Array type
            modelBuilder.Entity<UserPreference>()
                .Property(p => p.PreferredDishIds)
                .HasColumnType("integer[]");

            // Configure UserPreference relationships
            modelBuilder.Entity<UserPreference>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add performance optimization indexes
            
            // Add indexes for login history
            modelBuilder.Entity<LoginHistory>().HasIndex(l => l.UserId).HasDatabaseName("idx_loginhistory_userid");
            modelBuilder.Entity<LoginHistory>().HasIndex(l => l.LoginTime).HasDatabaseName("idx_loginhistory_logintime");
            
            // Add indexes for the commonly filtered columns
            modelBuilder.Entity<Wine>().HasIndex(w => w.TypeId).HasDatabaseName("idx_wines_typeid");
            modelBuilder.Entity<Wine>().HasIndex(w => w.CountryId).HasDatabaseName("idx_wines_countryid");
            // RegionId index removed as wines are now associated with wineries instead
            modelBuilder.Entity<Wine>().HasIndex(w => w.AcidityId).HasDatabaseName("idx_wines_acidityid");
            modelBuilder.Entity<Wine>().HasIndex(w => w.ABV).HasDatabaseName("idx_wines_abv");
            modelBuilder.Entity<Wine>().HasIndex(w => w.Name).HasDatabaseName("idx_wines_name");
            
            // Composite index for country only
            modelBuilder.Entity<Wine>()
                .HasIndex(w => w.CountryId)
                .HasDatabaseName("idx_wines_country");
            
            // For array columns, configure GIN indexes through EF Core annotation
            // Note: We need to use HasMethod("GIN") to specify the GIN index type for array columns
            modelBuilder.Entity<Wine>()
                .HasIndex(w => w.GrapeIds)
                .HasDatabaseName("idx_wines_grapeids")
                .HasMethod("GIN");
                
            modelBuilder.Entity<Wine>()
                .HasIndex(w => w.PairWithIds)
                .HasDatabaseName("idx_wines_pairwithids")
                .HasMethod("GIN");
                
            // For text search performance
            modelBuilder.Entity<Wine>()
                .HasIndex(w => w.SearchVector)
                .HasDatabaseName("idx_wines_searchvector")
                .HasMethod("GIN");
                
            // Configure TopRatedWinesView
            modelBuilder.Entity<TopRatedWinesView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("mv_top_rated_wines");
                
                entity.Property(e => e.WineId).HasColumnName("wine_id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.AverageRating).HasColumnName("average_rating");
                
                // Configure navigation property to Wine
                entity.HasOne(d => d.Wine)
                    .WithMany()
                    .HasForeignKey(d => d.WineId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });


        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");
            }
        }
    }
}
