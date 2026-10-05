using Dapper;
using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Services.Interfaces;
using Npgsql;

namespace Hl7Hackathon.Services;

public class PatientService : IPatientService
{
    private readonly NpgsqlDataSource _dataSource;

    public PatientService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Patient>> GetAllPatientsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                cpr,
                given_name,
                family_name,
                gender,
                birth_date,
                created_at
            FROM patients
            ORDER BY family_name, given_name
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var patients = await connection.QueryAsync<Patient>(command);

        return patients.AsList();
    }

    public async Task<Patient?> GetPatientByIdAsync(long patientId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                cpr,
                given_name,
                family_name,
                gender,
                birth_date,
                created_at
            FROM patients
            WHERE id = @PatientId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { PatientId = patientId }, cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<Patient>(command);
    }
}
