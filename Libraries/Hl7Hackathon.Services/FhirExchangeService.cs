using Dapper;
using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Services.Interfaces;
using Npgsql;

namespace Hl7Hackathon.Services;

public class FhirExchangeService : IFhirExchangeService
{
    private readonly NpgsqlDataSource _dataSource;

    public FhirExchangeService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<long> InsertFhirExchangeAsync(FhirExchange fhirExchange, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO fhir_exchanges (
                server_name,
                method,
                url,
                status_code,
                request_body,
                response_body,
                duration_ms,
                error,
                occurred_at
            )
            VALUES (
                @ServerName,
                @Method,
                @Url,
                @StatusCode,
                @RequestBody,
                @ResponseBody,
                @DurationMs,
                @Error,
                @OccurredAt
            )
            RETURNING id
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, fhirExchange, cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<long>(command);
    }

    public async Task<IReadOnlyList<FhirExchange>> GetRecentFhirExchangesAsync(int limit, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                server_name,
                method,
                url,
                status_code,
                duration_ms,
                error,
                occurred_at
            FROM fhir_exchanges
            ORDER BY occurred_at DESC, id DESC
            LIMIT @Limit
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Limit = limit }, cancellationToken: cancellationToken);
        var fhirExchanges = await connection.QueryAsync<FhirExchange>(command);

        return fhirExchanges.AsList();
    }

    public async Task<FhirExchange?> GetFhirExchangeByIdAsync(long fhirExchangeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                server_name,
                method,
                url,
                status_code,
                request_body,
                response_body,
                duration_ms,
                error,
                occurred_at
            FROM fhir_exchanges
            WHERE id = @FhirExchangeId
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { FhirExchangeId = fhirExchangeId }, cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<FhirExchange>(command);
    }

    public async Task<int> DeleteAllFhirExchangesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM fhir_exchanges
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }
}
