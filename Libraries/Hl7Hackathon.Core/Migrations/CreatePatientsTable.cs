using FluentMigrator;

namespace Hl7Hackathon.Core.Migrations;

[Migration(202609280001)]
public class CreatePatientsTable : Migration
{
    public override void Up()
    {
        Create.Table("patients")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("cpr").AsString(10).NotNullable().Unique()
            .WithColumn("given_name").AsString(200).NotNullable()
            .WithColumn("family_name").AsString(200).NotNullable()
            .WithColumn("gender").AsString(20).NotNullable()
            .WithColumn("birth_date").AsDate().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);
    }

    public override void Down()
    {
        Delete.Table("patients");
    }
}
