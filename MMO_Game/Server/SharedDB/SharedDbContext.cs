﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedDB
{
    public class SharedDbContext : DbContext
    {
        public DbSet<TokenDb> Tokens { get; set; }
        public DbSet<ServerDb> Servers { get; set; }

        // GameServer
        public SharedDbContext()
        {

        }
        // Asp.Net
        public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
        {

        }
        
        // GameServer
        public static string ConnectionString { get; set; } = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SharedDB;";
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured == false)
            { 
                options
                    //.UseLoggerFactory(_logger)
                    .UseSqlServer(ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TokenDb>()
                .HasIndex(t=>t.AccountDbId)
                .IsUnique();

            builder.Entity<ServerDb>()
               .HasIndex(s => s.Name)
               .IsUnique();
        }
    }
}
