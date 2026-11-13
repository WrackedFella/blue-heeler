using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(BlueHeeler.Api.Tests.Infrastructure.PostgresFixture))]

namespace BlueHeeler.Api.Tests.Infrastructure;

/// <summary>One PostgreSQL container for the test run; each factory gets its own database in it.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionStringFor(string database) =>
        new Npgsql.NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database }.ConnectionString;

    public ValueTask InitializeAsync() => new(_container.StartAsync());

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
