using FluentMigrator;
using Hl7Hackathon.Core.Common;

namespace Hl7Hackathon.Core.Migrations;

[Migration(202609280003)]
[Tags(DatabaseConstants.SeedTag)]
public class SeedPatients : Migration
{
    public override void Up()
    {
        Insert.IntoTable("patients")
            .Row(new
            {
                cpr = "2512489996",
                given_name = "Nancy Ann",
                family_name = "Berggren",
                gender = "female",
                birth_date = new DateTime(1948, 12, 25)
            })
            .Row(new
            {
                cpr = "1506999990",
                given_name = "Anna",
                family_name = "Testsen",
                gender = "female",
                birth_date = new DateTime(1999, 6, 15)
            })
            .Row(new
            {
                cpr = "1203049991",
                given_name = "Mads",
                family_name = "Prøve",
                gender = "male",
                birth_date = new DateTime(2004, 3, 12)
            })
            .Row(new
            {
                cpr = "0311069994",
                given_name = "Sofie",
                family_name = "Eksempel",
                gender = "female",
                birth_date = new DateTime(2006, 11, 3)
            });
    }

    public override void Down()
    {
        Delete.FromTable("patients")
            .Row(new { cpr = "2512489996" })
            .Row(new { cpr = "1506999990" })
            .Row(new { cpr = "1203049991" })
            .Row(new { cpr = "0311069994" });
    }
}
