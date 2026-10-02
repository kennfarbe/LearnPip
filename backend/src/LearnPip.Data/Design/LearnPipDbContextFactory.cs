// <copyright file="LearnPipDbContextFactory.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnPip.Data.Design;

public sealed class LearnPipDbContextFactory : IDesignTimeDbContextFactory<LearnPipDbContext>
{
    public LearnPipDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? "Host=localhost;Database=learnpip;Username=learnpip;Password=design-time-only";

        var options = new DbContextOptionsBuilder<LearnPipDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LearnPipDbContext(options);
    }
}
