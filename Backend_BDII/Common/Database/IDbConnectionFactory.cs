using Npgsql;

namespace Backend_BDII.Common.Database;

public interface IDbConnectionFactory
{
    Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}