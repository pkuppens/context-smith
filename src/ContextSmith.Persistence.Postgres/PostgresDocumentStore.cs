using System.Text.Json;
using ContextSmith.Application;
using ContextSmith.Domain;
using Npgsql;
using NpgsqlTypes;

namespace ContextSmith.Persistence.Postgres;

/// <summary><see cref="IDocumentStore"/> that keeps each document as a <c>jsonb</c> row in PostgreSQL. It uses the same JSON as the file store.</summary>
public sealed class PostgresDocumentStore : IDocumentStore
{
    // jsonb does not keep key order, so the polymorphic "$type" discriminator may no longer come first.
    private static readonly JsonSerializerOptions JsonOptions = new() { AllowOutOfOrderMetadataProperties = true };

    private readonly string _connectionString;
    private readonly Lazy<bool> _schema;

    /// <summary>Creates a store. The schema is created on first use, so the constructor does not connect.</summary>
    /// <param name="connectionString">Npgsql connection string.</param>
    public PostgresDocumentStore(string connectionString)
    {
        _connectionString = connectionString;
        _schema = new Lazy<bool>(() =>
        {
            using var connection = Open();
            using var command = new NpgsqlCommand(PostgresSql.DocumentsSchemaScript, connection);
            command.ExecuteNonQuery();
            return true;
        });
    }

    /// <inheritdoc/>
    public string Store(Document document, string? documentId = null)
    {
        _ = _schema.Value;
        var id = documentId ?? Guid.NewGuid().ToString("n");

        using var connection = Open();
        using var command = new NpgsqlCommand(
            "INSERT INTO documents (id, content) VALUES (@id, @content) " +
            "ON CONFLICT (id) DO UPDATE SET content = EXCLUDED.content, updated_at = now()",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.Add(new NpgsqlParameter("content", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(document) });
        command.ExecuteNonQuery();
        return id;
    }

    /// <inheritdoc/>
    public Document? Get(string documentId)
    {
        _ = _schema.Value;

        using var connection = Open();
        using var command = new NpgsqlCommand("SELECT content::text FROM documents WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", documentId);
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<Document>(json, JsonOptions) : null;
    }

    private NpgsqlConnection Open()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
