using IdentityService.Api.Models.ApiClient;
using IdentityService.Api.Models.RefreshToken;
using IdentityService.Api.Models.Role;
using IdentityService.Api.Models.User;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace IdentityService.Api.Data
{
    public class ApplicationDbContext :DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ApiClient> ApiClients { get; set; }
        // MassTransit
        public DbSet<InboxState> InboxState { get; set; }
        public DbSet<OutboxState> OutboxState { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(x => x.Email).IsUnique();
                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasIndex(x => x.Name).IsUnique();
                entity.HasMany(r => r.Users)
                      .WithOne(x => x.Role)
                      .HasForeignKey(x => x.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ApiClient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ClientId).IsUnique();
                entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ClientSecretHash).IsRequired();
            });
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
        }
    }
}
