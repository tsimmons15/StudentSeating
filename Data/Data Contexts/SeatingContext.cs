using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentSeating.Models;
using System;

namespace StudentSeating.Data
{
    public class SeatingContext: DbContext
    {
        public Boolean Distancing { get; set; }

        // General point: EFC doesn't load information unless it has to.
        // To ensure Exam.Instructor isn't null, work with Professors first, then Exams.
        public string DbPath { get; set; }
        public string DbKey { get; set; }
        private SqliteConnection _connection;

        public DbSet<Exam> Exams { get; set; }

        public DbSet<Seat> Seats { get; set; }

        public DbSet<Section> Sections { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            this._connection ??= InitializeSQLiteConnection(DbPath, DbKey);
            options.UseSqlite(this._connection);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Exam>()
                .HasIndex(e=>e.Instructor);
            builder.Entity<Exam>()
                .HasKey(e => e.Id);

            builder.Entity<Seat>()
                .HasOne(s => s.Exam)
                .WithMany()
                .HasForeignKey(s => s.ExamId);
        }
        private SqliteConnection InitializeSQLiteConnection(string databaseFile, string key)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFile,
                Password = key// PRAGMA key is being sent from EF Core directly after opening the connection
            };
            return new SqliteConnection(connectionString.ToString());
        }
    }
}
