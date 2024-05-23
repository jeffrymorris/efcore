﻿// <copyright file="Class1.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <summary>Kto to wymyslil</summary>

using Microsoft.EntityFrameworkCore;

namespace MyPrecompiledTest;

public class MyContext : DbContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public DbSet<MyEntity> Entities { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Repro;Trusted_Connection=True;MultipleActiveResultSets=true");
    }
}

public class MyEntity
{
    public int Id { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Name { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}



public class MyTestClass
{
    public void Test()
    {
        using var ctx = new MyContext();
        //ctx.Database.EnsureDeleted();
        //ctx.Database.EnsureCreated();

        var query = ctx.Entities.Where(x => x.Id > 5).ToList();
    }
}


