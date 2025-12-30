using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class ApplicationDbContext : IdentityDbContext 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Analytic> Analytics { get; set; }
        public DbSet<Interaction> Interactions { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<AutoReplyRule> AutoReplyRules { get; set; }
        public DbSet<AccountAnalytics> AccountAnalytics { get; set; }
        public DbSet<PostAnalytics> PostAnalytics { get; set; }
        public DbSet<AnalyticsSnapshot> AnalyticsSnapshots { get; set; }
        public DbSet<Activity> Activities { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // تنظیمات برای AutoReplyRule Keywords
            modelBuilder.Entity<AutoReplyRule>()
                .Property(r => r.Keywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                    v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>()
                );

           

        }
    }
}
