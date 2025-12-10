using Microsoft.EntityFrameworkCore;
using DailyWatt.Infrastructure.Data;

namespace DailyWatt.Tests.Infrastructure;

/// <summary>
/// Test DbContext that allows tests to run against a real SQLite database without migrations
/// </summary>
public class TestApplicationDbContext : ApplicationDbContext
{
  public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
  {
  }
}

