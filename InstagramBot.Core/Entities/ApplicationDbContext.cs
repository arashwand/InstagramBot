using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class ApplicationDbContext : IdentityDbContext // از IdentityDbContext ارث‌بری می‌کنیم
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
        //public DbSet<User> Users { get; set; }
    }
}
