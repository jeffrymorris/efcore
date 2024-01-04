// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable InconsistentNaming

namespace Microsoft.EntityFrameworkCore.Query;

public class RawSqlServerTest : NonSharedModelTestBase
{
    // Issue #13346, #24623
    [ConditionalFact]
    public virtual async Task ToQuery_can_use_FromSqlRaw()
    {
        var contextFactory = await InitializeAsync<Context13346>(seed: c => c.Seed());
        using var context = contextFactory.CreateContext();
        var query = context.Set<Context13346.OrderSummary>().ToList();

        Assert.Equal(4, query.Count);

        AssertSql(
            """
SELECT o.Amount From Orders AS o -- RAW
""");
    }

    public class Context13346(DbContextOptions options) : DbContext(options)
    {
        public virtual DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            modelBuilder.Entity<OrderSummary>()
                .HasNoKey()
                .ToQuery(() => Set<OrderSummary>().FromSqlRaw("SELECT o.Amount From Orders AS o -- RAW"));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public void Seed()
        {
            AddRange(
                new Order { Amount = 1 },
                new Order { Amount = 2 },
                new Order { Amount = 3 },
                new Order { Amount = 4 }
            );

            SaveChanges();
        }

        public class Order
        {
            public int Id { get; set; }
            public int Amount { get; set; }
        }

        public class OrderSummary
        {
            public int Amount { get; set; }
        }
    }

    protected override string StoreName
        => "RawSqlServerTest";

    protected TestSqlLoggerFactory TestSqlLoggerFactory
        => (TestSqlLoggerFactory)ListLoggerFactory;

    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

    protected void AssertSql(params string[] expected)
        => TestSqlLoggerFactory.AssertBaseline(expected);

    protected void ClearLog()
        => TestSqlLoggerFactory.Clear();
}
