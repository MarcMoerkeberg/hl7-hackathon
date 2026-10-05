using FluentMigrator;

namespace Hl7Hackathon.Core.Migrations;

[Migration(202609280002)]
public class CreateFhirExchangesTable : Migration
{
    public override void Up()
    {
        Create.Table("fhir_exchanges")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("server_name").AsString(100).NotNullable()
            .WithColumn("method").AsString(10).NotNullable()
            .WithColumn("url").AsString(int.MaxValue).NotNullable()
            .WithColumn("status_code").AsInt32().Nullable()
            .WithColumn("request_body").AsString(int.MaxValue).Nullable()
            .WithColumn("response_body").AsString(int.MaxValue).Nullable()
            .WithColumn("duration_ms").AsInt32().NotNullable()
            .WithColumn("error").AsString(int.MaxValue).Nullable()
            .WithColumn("occurred_at").AsDateTimeOffset().NotNullable();

        Create.Index("ix_fhir_exchanges_occurred_at")
            .OnTable("fhir_exchanges")
            .OnColumn("occurred_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("fhir_exchanges");
    }
}
