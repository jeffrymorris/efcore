﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal;

namespace Microsoft.EntityFrameworkCore.Query;

public class AdHocJsonQuerySqlServerTest : AdHocJsonQueryTestBase
{
    protected override ITestStoreFactory TestStoreFactory
        => SqlServerTestStoreFactory.Instance;

    protected override void Seed29219(Context29219 ctx)
    {
        var entity1 = new Context29219.MyEntity
        {
            Id = 1,
            Reference = new Context29219.MyJsonEntity { NonNullableScalar = 10, NullableScalar = 11 },
            Collection =
            [
                new() { NonNullableScalar = 100, NullableScalar = 101 },
                new() { NonNullableScalar = 200, NullableScalar = 201 },
                new() { NonNullableScalar = 300, NullableScalar = null }
            ]
        };

        var entity2 = new Context29219.MyEntity
        {
            Id = 2,
            Reference = new Context29219.MyJsonEntity { NonNullableScalar = 20, NullableScalar = null },
            Collection = [new() { NonNullableScalar = 1001, NullableScalar = null }]
        };

        ctx.Entities.AddRange(entity1, entity2);
        ctx.SaveChanges();

        ctx.Database.ExecuteSql(
            $$"""
INSERT INTO [Entities] ([Id], [Reference], [Collection])
VALUES(3, N'{ "NonNullableScalar" : 30 }', N'[{ "NonNullableScalar" : 10001 }]')
""");
    }

    protected override void Seed30028(Context30028 ctx)
    {
        // complete
        ctx.Database.ExecuteSql(
            $$$$"""
INSERT INTO [Entities] ([Id], [Json])
VALUES(
1,
N'{"RootName":"e1","Collection":[{"BranchName":"e1 c1","Nested":{"LeafName":"e1 c1 l"}},{"BranchName":"e1 c2","Nested":{"LeafName":"e1 c2 l"}}],"OptionalReference":{"BranchName":"e1 or","Nested":{"LeafName":"e1 or l"}},"RequiredReference":{"BranchName":"e1 rr","Nested":{"LeafName":"e1 rr l"}}}')
""");

        // missing collection
        ctx.Database.ExecuteSql(
            $$$$"""
INSERT INTO [Entities] ([Id], [Json])
VALUES(
2,
N'{"RootName":"e2","OptionalReference":{"BranchName":"e2 or","Nested":{"LeafName":"e2 or l"}},"RequiredReference":{"BranchName":"e2 rr","Nested":{"LeafName":"e2 rr l"}}}')
""");

        // missing optional reference
        ctx.Database.ExecuteSql(
            $$$$"""
INSERT INTO [Entities] ([Id], [Json])
VALUES(
3,
N'{"RootName":"e3","Collection":[{"BranchName":"e3 c1","Nested":{"LeafName":"e3 c1 l"}},{"BranchName":"e3 c2","Nested":{"LeafName":"e3 c2 l"}}],"RequiredReference":{"BranchName":"e3 rr","Nested":{"LeafName":"e3 rr l"}}}')
""");

        // missing required reference
        ctx.Database.ExecuteSql(
            $$$$"""
INSERT INTO [Entities] ([Id], [Json])
VALUES(
4,
N'{"RootName":"e4","Collection":[{"BranchName":"e4 c1","Nested":{"LeafName":"e4 c1 l"}},{"BranchName":"e4 c2","Nested":{"LeafName":"e4 c2 l"}}],"OptionalReference":{"BranchName":"e4 or","Nested":{"LeafName":"e4 or l"}}}')
""");
    }

    protected override void Seed33046(Context33046 ctx)
        => ctx.Database.ExecuteSql(
            $$"""
INSERT INTO [Reviews] ([Rounds], [Id])
VALUES(N'[{"RoundNumber":11,"SubRounds":[{"SubRoundNumber":111},{"SubRoundNumber":112}]}]', 1)
""");

    protected override void SeedArrayOfPrimitives(ContextArrayOfPrimitives ctx)
    {
        var entity1 = new ContextArrayOfPrimitives.MyEntity
        {
            Id = 1,
            Reference = new ContextArrayOfPrimitives.MyJsonEntity
            {
                IntArray = [1, 2, 3],
                ListOfString =
                [
                    "Foo",
                    "Bar",
                    "Baz"
                ]
            },
            Collection =
            [
                new() { IntArray = [111, 112, 113], ListOfString = ["Foo11", "Bar11"] },
                new() { IntArray = [211, 212, 213], ListOfString = ["Foo12", "Bar12"] }
            ]
        };

        var entity2 = new ContextArrayOfPrimitives.MyEntity
        {
            Id = 2,
            Reference = new ContextArrayOfPrimitives.MyJsonEntity
            {
                IntArray = [10, 20, 30],
                ListOfString =
                [
                    "A",
                    "B",
                    "C"
                ]
            },
            Collection =
            [
                new() { IntArray = [110, 120, 130], ListOfString = ["A1", "Z1"] },
                new() { IntArray = [210, 220, 230], ListOfString = ["A2", "Z2"] }
            ]
        };

        ctx.Entities.AddRange(entity1, entity2);
        ctx.SaveChanges();
    }

    protected override void SeedJunkInJson(ContextJunkInJson ctx)
        => ctx.Database.ExecuteSql(
            $$$$"""
INSERT INTO [Entities] ([Collection], [CollectionWithCtor], [Reference], [ReferenceWithCtor], [Id])
VALUES(
N'[{"JunkReference":{"Something":"SomeValue" },"Name":"c11","JunkProperty1":50,"Number":11.5,"JunkCollection1":[],"JunkCollection2":[{"Foo":"junk value"}],"NestedCollection":[{"DoB":"2002-04-01T00:00:00","DummyProp":"Dummy value"},{"DoB":"2002-04-02T00:00:00","DummyReference":{"Foo":5}}],"NestedReference":{"DoB":"2002-03-01T00:00:00"}},{"Name":"c12","Number":12.5,"NestedCollection":[{"DoB":"2002-06-01T00:00:00"},{"DoB":"2002-06-02T00:00:00"}],"NestedDummy":59,"NestedReference":{"DoB":"2002-05-01T00:00:00"}}]',
N'[{"MyBool":true,"Name":"c11 ctor","JunkReference":{"Something":"SomeValue","JunkCollection":[{"Foo":"junk value"}]},"NestedCollection":[{"DoB":"2002-08-01T00:00:00"},{"DoB":"2002-08-02T00:00:00"}],"NestedReference":{"DoB":"2002-07-01T00:00:00"}},{"MyBool":false,"Name":"c12 ctor","NestedCollection":[{"DoB":"2002-10-01T00:00:00"},{"DoB":"2002-10-02T00:00:00"}],"JunkCollection":[{"Foo":"junk value"}],"NestedReference":{"DoB":"2002-09-01T00:00:00"}}]',
N'{"Name":"r1","JunkCollection":[{"Foo":"junk value"}],"JunkReference":{"Something":"SomeValue" },"Number":1.5,"NestedCollection":[{"DoB":"2000-02-01T00:00:00","JunkReference":{"Something":"SomeValue"}},{"DoB":"2000-02-02T00:00:00"}],"NestedReference":{"DoB":"2000-01-01T00:00:00"}}',
N'{"MyBool":true,"JunkCollection":[{"Foo":"junk value"}],"Name":"r1 ctor","JunkReference":{"Something":"SomeValue" },"NestedCollection":[{"DoB":"2001-02-01T00:00:00"},{"DoB":"2001-02-02T00:00:00"}],"NestedReference":{"JunkCollection":[{"Foo":"junk value"}],"DoB":"2001-01-01T00:00:00"}}',
1)
""");

    protected override void SeedTrickyBuffering(ContextTrickyBuffering ctx)
        => ctx.Database.ExecuteSql(
            $$$"""
INSERT INTO [Entities] ([Reference], [Id])
VALUES(
N'{"Name": "r1", "Number": 7, "JunkReference":{"Something": "SomeValue" }, "JunkCollection": [{"Foo": "junk value"}], "NestedReference": {"DoB": "2000-01-01T00:00:00"}, "NestedCollection": [{"DoB": "2000-02-01T00:00:00", "JunkReference": {"Something": "SomeValue"}}, {"DoB": "2000-02-02T00:00:00"}]}',1)
""");

    protected override void SeedShadowProperties(ContextShadowProperties ctx)
        => ctx.Database.ExecuteSql(
            $$"""
INSERT INTO [Entities] ([Collection], [CollectionWithCtor], [Reference], [ReferenceWithCtor], [Id], [Name])
VALUES(
N'[{"Name":"e1_c1","ShadowDouble":5.5},{"ShadowDouble":20.5,"Name":"e1_c2"}]',
N'[{"Name":"e1_c1 ctor","ShadowNullableByte":6},{"ShadowNullableByte":null,"Name":"e1_c2 ctor"}]',
N'{"Name":"e1_r", "ShadowString":"Foo"}',
N'{"ShadowInt":143,"Name":"e1_r ctor"}',
1,
N'e1')
""");

    protected override void SeedNotICollection(ContextNotICollection ctx)
    {
        ctx.Database.ExecuteSql(
            $$"""
INSERT INTO [Entities] ([Json], [Id])
VALUES(
N'{"Collection":[{"Bar":11,"Foo":"c11"},{"Bar":12,"Foo":"c12"},{"Bar":13,"Foo":"c13"}]}',
1)
""");

        ctx.Database.ExecuteSql(
            $$$"""
INSERT INTO [Entities] ([Json], [Id])
VALUES(
N'{"Collection":[{"Bar":21,"Foo":"c21"},{"Bar":22,"Foo":"c22"}]}',
2)
""");
    }

    #region EnumLegacyValues

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Read_enum_property_with_legacy_values(bool async)
    {
        var contextFactory = await InitializeAsync<ContextEnumLegacyValues>(
            seed: SeedEnumLegacyValues);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Select(
                x => new
                {
                    x.Reference.IntEnum,
                    x.Reference.ByteEnum,
                    x.Reference.LongEnum,
                    x.Reference.NullableEnum
                });

            var exception = async
                ? await (Assert.ThrowsAsync<SqlException>(() => query.ToListAsync()))
                : Assert.Throws<SqlException>(() => query.ToList());

            // Conversion failed when converting the nvarchar value '...' to data type int
            Assert.Equal(245, exception.Number);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Read_json_entity_with_enum_properties_with_legacy_values(bool async)
    {
        var contextFactory = await InitializeAsync<ContextEnumLegacyValues>(
            seed: SeedEnumLegacyValues,
            shouldLogCategory: c => c == DbLoggerCategory.Query.Name);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Select(x => x.Reference).AsNoTracking();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(ContextEnumLegacyValues.ByteEnum.Redmond, result[0].ByteEnum);
            Assert.Equal(ContextEnumLegacyValues.IntEnum.Foo, result[0].IntEnum);
            Assert.Equal(ContextEnumLegacyValues.LongEnum.Three, result[0].LongEnum);
            Assert.Equal(ContextEnumLegacyValues.ULongEnum.Three, result[0].ULongEnum);
            Assert.Equal(ContextEnumLegacyValues.IntEnum.Bar, result[0].NullableEnum);
        }

        var testLogger = new TestLogger<SqlServerLoggingDefinitions>();
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.ByteEnum))));
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.IntEnum))));
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.LongEnum))));
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.ULongEnum))));
    }

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public virtual async Task Read_json_entity_collection_with_enum_properties_with_legacy_values(bool async)
    {
        var contextFactory = await InitializeAsync<ContextEnumLegacyValues>(
            seed: SeedEnumLegacyValues,
            shouldLogCategory: c => c == DbLoggerCategory.Query.Name);

        using (var context = contextFactory.CreateContext())
        {
            var query = context.Entities.Select(x => x.Collection).AsNoTracking();

            var result = async
                ? await query.ToListAsync()
                : query.ToList();

            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Count);
            Assert.Equal(ContextEnumLegacyValues.ByteEnum.Bellevue, result[0][0].ByteEnum);
            Assert.Equal(ContextEnumLegacyValues.IntEnum.Foo, result[0][0].IntEnum);
            Assert.Equal(ContextEnumLegacyValues.LongEnum.One, result[0][0].LongEnum);
            Assert.Equal(ContextEnumLegacyValues.ULongEnum.One, result[0][0].ULongEnum);
            Assert.Equal(ContextEnumLegacyValues.IntEnum.Bar, result[0][0].NullableEnum);
            Assert.Equal(ContextEnumLegacyValues.ByteEnum.Seattle, result[0][1].ByteEnum);
            Assert.Equal(ContextEnumLegacyValues.IntEnum.Baz, result[0][1].IntEnum);
            Assert.Equal(ContextEnumLegacyValues.LongEnum.Two, result[0][1].LongEnum);
            Assert.Equal(ContextEnumLegacyValues.ULongEnum.Two, result[0][1].ULongEnum);
            Assert.Null(result[0][1].NullableEnum);
        }

        var testLogger = new TestLogger<SqlServerLoggingDefinitions>();
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.ByteEnum))));
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.IntEnum))));
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.LongEnum))));
        Assert.Single(
            ListLoggerFactory.Log.Where(
                l => l.Message == CoreResources.LogStringEnumValueInJson(testLogger).GenerateMessage(nameof(ContextEnumLegacyValues.ULongEnum))));
    }

    private void SeedEnumLegacyValues(ContextEnumLegacyValues ctx)
        => ctx.Database.ExecuteSql(
            $$"""
INSERT INTO [Entities] ([Collection], [Reference], [Id], [Name])
VALUES(
N'[{"ByteEnum":"Bellevue","IntEnum":"Foo","LongEnum":"One","ULongEnum":"One","Name":"e1_c1","NullableEnum":"Bar"},{"ByteEnum":"Seattle","IntEnum":"Baz","LongEnum":"Two","ULongEnum":"Two","Name":"e1_c2","NullableEnum":null}]',
N'{"ByteEnum":"Redmond","IntEnum":"Foo","LongEnum":"Three","ULongEnum":"Three","Name":"e1_r","NullableEnum":"Bar"}',
1,
N'e1')
""");

    public class ContextEnumLegacyValues(DbContextOptions options) : DbContext((new DbContextOptionsBuilder(options)).ConfigureWarnings(b => b.Log(CoreEventId.StringEnumValueInJson)).Options)
    {

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public DbSet<MyEntity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyEntity>().Property(x => x.Id).ValueGeneratedNever();
            modelBuilder.Entity<MyEntity>().OwnsOne(x => x.Reference, b => b.ToJson());
            modelBuilder.Entity<MyEntity>().OwnsMany(x => x.Collection, b => b.ToJson());
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public MyJsonEntity Reference { get; set; }
            public List<MyJsonEntity> Collection { get; set; }
        }

        public class MyJsonEntity
        {
            public string Name { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public IntEnum IntEnum { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public ByteEnum ByteEnum { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public LongEnum LongEnum { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public ULongEnum ULongEnum { get; set; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public IntEnum? NullableEnum { get; set; }
        }

        public enum IntEnum
        {
            Foo = int.MinValue,
            Bar,
            Baz = int.MaxValue,
        }

        public enum ByteEnum : byte
        {
            Seattle,
            Redmond,
            Bellevue = 255,
        }

        public enum LongEnum : long
        {
            One = long.MinValue,
            Two = 1,
            Three = long.MaxValue,
        }

        public enum ULongEnum : ulong
        {
            One = ulong.MinValue,
            Two = 1,
            Three = ulong.MaxValue,
        }
    }

    #endregion
}
