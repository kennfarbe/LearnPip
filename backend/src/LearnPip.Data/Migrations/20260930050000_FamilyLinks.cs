using LearnPip.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LearnPip.Data.Migrations;

[DbContext(typeof(LearnPipDbContext))]
[Migration("20260930050000_FamilyLinks")]
public sealed class FamilyLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("AgeBand", "Accounts", type: "character varying(8)",
            maxLength: 8, nullable: false, defaultValue: "unknown");
        migrationBuilder.AddColumn<Guid?>("GuardianApprovedByAccountId", "PublicSubmissions",
            type: "uuid", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset?>("GuardianApprovedAtUtc", "PublicSubmissions",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex("IX_PublicSubmissions_GuardianApprovedByAccountId",
            "PublicSubmissions", "GuardianApprovedByAccountId");
        migrationBuilder.AddForeignKey("FK_PublicSubmissions_Accounts_GuardianApprovedByAccountId",
            "PublicSubmissions", "GuardianApprovedByAccountId", "Accounts", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable("FamilyLinks", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            ChildAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
            InviteHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            InviteExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            VerifiedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
            VerificationReference = table.Column<string>(type: "character varying(120)", maxLength: 120,
                nullable: true),
            VerifiedAtUtc = table.Column<DateTimeOffset?>(type: "timestamp with time zone", nullable: true),
            ActivatedAtUtc = table.Column<DateTimeOffset?>(type: "timestamp with time zone", nullable: true),
            RevokedAtUtc = table.Column<DateTimeOffset?>(type: "timestamp with time zone", nullable: true),
            RevokedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_FamilyLinks", x => x.Id);
            table.ForeignKey("FK_FamilyLinks_Accounts_ChildAccountId", x => x.ChildAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_FamilyLinks_Accounts_ParentAccountId", x => x.ParentAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_FamilyLinks_Accounts_VerifiedByAccountId", x => x.VerifiedByAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_FamilyLinks_Accounts_RevokedByAccountId", x => x.RevokedByAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_FamilyLinks_InviteHash", "FamilyLinks", "InviteHash", unique: true);
        migrationBuilder.CreateIndex("IX_FamilyLinks_ChildAccountId_ParentAccountId_Status", "FamilyLinks",
            new[] { "ChildAccountId", "ParentAccountId", "Status" });
        migrationBuilder.CreateIndex("IX_FamilyLinks_ParentAccountId", "FamilyLinks", "ParentAccountId");
        migrationBuilder.CreateIndex("IX_FamilyLinks_VerifiedByAccountId", "FamilyLinks", "VerifiedByAccountId");
        migrationBuilder.CreateIndex("IX_FamilyLinks_RevokedByAccountId", "FamilyLinks", "RevokedByAccountId");

        migrationBuilder.CreateTable("FamilyLinkEvents", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            FamilyLinkId = table.Column<Guid>(type: "uuid", nullable: false),
            ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
            Action = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_FamilyLinkEvents", x => x.Id);
            table.ForeignKey("FK_FamilyLinkEvents_FamilyLinks_FamilyLinkId", x => x.FamilyLinkId,
                "FamilyLinks", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_FamilyLinkEvents_Accounts_ActorAccountId", x => x.ActorAccountId,
                "Accounts", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_FamilyLinkEvents_FamilyLinkId_CreatedAtUtc", "FamilyLinkEvents",
            new[] { "FamilyLinkId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_FamilyLinkEvents_ActorAccountId", "FamilyLinkEvents", "ActorAccountId");

        migrationBuilder.CreateTable("FamilyGoals", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            FamilyLinkId = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
            TargetAtUtc = table.Column<DateTimeOffset?>(type: "timestamp with time zone", nullable: true),
            CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_FamilyGoals", x => x.Id);
            table.ForeignKey("FK_FamilyGoals_FamilyLinks_FamilyLinkId", x => x.FamilyLinkId,
                "FamilyLinks", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_FamilyGoals_FamilyLinkId", "FamilyGoals", "FamilyLinkId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("FamilyGoals");
        migrationBuilder.DropTable("FamilyLinkEvents");
        migrationBuilder.DropTable("FamilyLinks");
        migrationBuilder.DropForeignKey("FK_PublicSubmissions_Accounts_GuardianApprovedByAccountId",
            "PublicSubmissions");
        migrationBuilder.DropIndex("IX_PublicSubmissions_GuardianApprovedByAccountId", "PublicSubmissions");
        migrationBuilder.DropColumn("GuardianApprovedByAccountId", "PublicSubmissions");
        migrationBuilder.DropColumn("GuardianApprovedAtUtc", "PublicSubmissions");
        migrationBuilder.DropColumn("AgeBand", "Accounts");
    }
}
