using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace Centrocdx.Data;

/// <summary>
/// Umbraco/NPoco opens connections through <see cref="DbProviderFactory"/>.
/// This factory uses <see cref="NpgsqlDataSource"/> so Entra tokens from
/// <see cref="NpgsqlDataSourceBuilder.UsePeriodicPasswordProvider"/> are applied.
/// </summary>
public sealed class DataSourceNpgsqlDbProviderFactory : DbProviderFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public DataSourceNpgsqlDbProviderFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public override bool CanCreateDataSourceEnumerator => NpgsqlFactory.Instance.CanCreateDataSourceEnumerator;

    public override DbCommand CreateCommand() => NpgsqlFactory.Instance.CreateCommand()!;

    public override DbCommandBuilder CreateCommandBuilder() => NpgsqlFactory.Instance.CreateCommandBuilder()!;

    public override DbConnection CreateConnection() => new NpgsqlDataSourceDbConnection(_dataSource);

    public override DbConnectionStringBuilder CreateConnectionStringBuilder() =>
        NpgsqlFactory.Instance.CreateConnectionStringBuilder()!;

    public override DbDataAdapter CreateDataAdapter() => NpgsqlFactory.Instance.CreateDataAdapter()!;

    public override DbParameter CreateParameter() => NpgsqlFactory.Instance.CreateParameter()!;
}

/// <summary>
/// NPoco assigns <see cref="DbConnection.ConnectionString"/> after create.
/// Connections from <see cref="NpgsqlDataSource"/> already have the string and password provider;
/// ignore the assignment so Umbraco cannot detach them from the data source.
/// </summary>
internal sealed class NpgsqlDataSourceDbConnection : DbConnection
{
    private readonly NpgsqlConnection _inner;

    public NpgsqlDataSourceDbConnection(NpgsqlDataSource dataSource)
    {
        _inner = dataSource.CreateConnection();
    }

    internal NpgsqlConnection Inner => _inner;

    [AllowNull]
    public override string ConnectionString
    {
        get => _inner.ConnectionString;
        set { }
    }

    public override string Database => _inner.Database;

    public override string DataSource => _inner.DataSource;

    public override string ServerVersion => _inner.ServerVersion;

    public override ConnectionState State => _inner.State;

    public override int ConnectionTimeout => _inner.ConnectionTimeout;

    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    public override void Close() => _inner.Close();

    public override void Open() => _inner.Open();

    public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        _inner.BeginTransaction(isolationLevel);

    protected override DbCommand CreateDbCommand() => new NpgsqlDataSourceDbCommand(_inner.CreateCommand(), this);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal sealed class NpgsqlDataSourceDbCommand : DbCommand
{
    private readonly NpgsqlCommand _inner;
    private DbConnection? _connection;

    public NpgsqlDataSourceDbCommand(NpgsqlCommand inner, DbConnection connection)
    {
        _inner = inner;
        _connection = connection;
        if (connection is NpgsqlDataSourceDbConnection owner)
        {
            _inner.Connection = owner.Inner;
        }
    }

    [AllowNull]
    public override string CommandText
    {
        get => _inner.CommandText;
        set => _inner.CommandText = value ?? string.Empty;
    }

    public override int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _inner.DesignTimeVisible;
        set => _inner.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set
        {
            _connection = value;
            if (value is NpgsqlDataSourceDbConnection owner)
            {
                _inner.Connection = owner.Inner;
            }
            else if (value is null)
            {
                _inner.Connection = null;
            }
        }
    }

    protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = (NpgsqlTransaction?)value;
    }

    public override void Cancel() => _inner.Cancel();

    public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();

    public override object? ExecuteScalar() => _inner.ExecuteScalar();

    public override void Prepare() => _inner.Prepare();

    protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        _inner.ExecuteReader(behavior);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
