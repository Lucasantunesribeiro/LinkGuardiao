using LinkGuardiao.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkGuardiao.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260112174000_FixPostgresBooleanTypes")]
    public partial class FixPostgresBooleanTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    "ALTER TABLE \"Users\" ALTER COLUMN \"IsAdmin\" TYPE boolean USING (\"IsAdmin\" <> 0);");

                migrationBuilder.Sql(
                    "ALTER TABLE \"ShortenedLinks\" ALTER COLUMN \"IsActive\" TYPE boolean USING (\"IsActive\" <> 0);");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(
                    "ALTER TABLE \"Users\" ALTER COLUMN \"IsAdmin\" TYPE integer USING (CASE WHEN \"IsAdmin\" THEN 1 ELSE 0 END);");

                migrationBuilder.Sql(
                    "ALTER TABLE \"ShortenedLinks\" ALTER COLUMN \"IsActive\" TYPE integer USING (CASE WHEN \"IsActive\" THEN 1 ELSE 0 END);");
            }
        }
    }
}
