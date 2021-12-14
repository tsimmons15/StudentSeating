using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentSeating.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StudentSeating.Data
{
    public class OptionsContext : DbContext
    {
        private SqliteConnection _connection;
        public string DbPath { get; set; }
        public string DbKey { get; set; }

        public DbSet<Option> Options { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            this._connection ??= InitializeSQLiteConnection(DbPath, DbKey);
            options.UseSqlite(this._connection);
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
