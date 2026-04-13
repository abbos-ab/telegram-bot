using Microsoft.EntityFrameworkCore;
using CargoBot.Models;

namespace CargoBot.Data
{
    public class KargoDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Parcel> Parcels { get; set; }
        public DbSet<UserParcel> UserParcels { get; set; }

        public KargoDbContext(DbContextOptions<KargoDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserParcel>()
                .HasKey(up => new { up.ChatId, up.TrackCode });

            modelBuilder.Entity<UserParcel>()
                .HasOne(up => up.User)
                .WithMany(u => u.SavedParcels)
                .HasForeignKey(up => up.ChatId);

            modelBuilder.Entity<UserParcel>()
                .HasOne(up => up.Parcel)
                .WithMany(p => p.SavedByUsers)
                .HasForeignKey(up => up.TrackCode);
        }
    }
}