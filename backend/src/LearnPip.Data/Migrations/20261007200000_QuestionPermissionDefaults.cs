// <copyright file="20261007200000_QuestionPermissionDefaults.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LearnPip.Data.Migrations;

/// <summary>Übernimmt die sichere Fragenmatrix für vorhandene Instanzen.</summary>
[DbContext(typeof(LearnPipDbContext))]
[Migration("20261007200000_QuestionPermissionDefaults")]
public sealed class QuestionPermissionDefaults : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "SystemSettings" ("Key", "Value", "UpdatedAtUtc") VALUES
            ('questions.permissions.user.create', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.readOwn', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.editOwn', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.deleteOwn', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.readShared', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.readForeign', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.readPrivate', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.editForeign', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.deleteForeign', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.approve', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.withdraw', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.reports', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.batch', 'false', CURRENT_TIMESTAMP),
            ('questions.permissions.user.import', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.export', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.user.community', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.create', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.readOwn', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.editOwn', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.deleteOwn', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.readShared', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.readForeign', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.readPrivate', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.editForeign', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.deleteForeign', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.approve', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.withdraw', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.reports', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.batch', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.import', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.export', 'true', CURRENT_TIMESTAMP),
            ('questions.permissions.moderator.community', 'true', CURRENT_TIMESTAMP)
            ON CONFLICT ("Key") DO NOTHING;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"SystemSettings\" WHERE \"Key\" LIKE 'questions.permissions.%'");
    }
}
