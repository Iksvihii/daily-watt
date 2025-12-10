using Microsoft.EntityFrameworkCore;
using DailyWatt.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace DailyWatt.Tests.Infrastructure;

/// <summary>
/// Manages the lifecycle of a test SQLite database
/// Creates a fresh database for each test
/// </summary>
public class TestDatabaseFixture : IDisposable
{
  private readonly List<string> _databasePaths = new();
  
  /// <summary>
  /// Creates a fresh context for a test (creates new database each time)
  /// </summary>
  public ApplicationDbContext CreateContext()
  {
    var databasePath = Path.Combine(Path.GetTempPath(), $"test_dailywatt_{Guid.NewGuid()}.db");
    _databasePaths.Add(databasePath);
    
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite($"Data Source={databasePath}")
        .Options;

    var context = new TestApplicationDbContext(options);
    
    // Disable foreign key constraints for seeding
    context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
    context.Database.EnsureCreated();
    context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
    
    return context;
  }

  public void Dispose()
  {
    // Clean up all SQLite database files
    foreach (var path in _databasePaths)
    {
      if (File.Exists(path))
      {
        try
        {
          File.Delete(path);
        }
        catch
        {
          // Ignore errors deleting temp files
        }
      }
    }
  }
}
