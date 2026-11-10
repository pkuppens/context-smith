using System.Text.Json;
using ContextSmith.Application;
using Npgsql;
using NpgsqlTypes;

namespace ContextSmith.Persistence.Postgres;

/// <summary><see cref="IRetrievalService"/> that keeps one document's chunk embeddings in a <c>pgvector</c> column and ranks them by cosine distance.</summary>
public sealed class PostgresRetrievalService : IRetrievalService
{
    private readonly string _connectionString;
    private readonly string _documentId;
    private readonly string _schemaScript;
    private bool _schemaReady;

    /// <summary>Creates a service for one document. The schema is created on first use, so the constructor does not connect.</summary>
    /// <param name="connectionString">Npgsql connection string.</param>
    /// <param name="documentId">Document whose chunks this service indexes and searches.</param>
    /// <param name="vectorDimension">Dimension of the embedding vectors. It sets the column size when the table is first created.</param>
    public PostgresRetrievalService(string connectionString, string documentId, int vectorDimension)
    {
        _connectionString = connectionString;
        _documentId = documentId;
        _schemaScript = PostgresSql.ChunksSchemaScript(vectorDimension);
    }

    /// <inheritdoc/>
    public async Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "INSERT INTO chunks (document_id, id, content, embedding) VALUES (@document, @id, @content, @embedding::vector) " +
            "ON CONFLICT (document_id, id) DO UPDATE SET content = EXCLUDED.content, embedding = EXCLUDED.embedding",
            connection);
        command.Parameters.AddWithValue("document", _documentId);
        command.Parameters.AddWithValue("id", chunk.Id);
        command.Parameters.Add(new NpgsqlParameter("content", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(chunk) });
        command.Parameters.AddWithValue("embedding", PostgresSql.FormatVector(embedding));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "SELECT content::text FROM chunks WHERE document_id = @document ORDER BY embedding <=> @query::vector LIMIT @top",
            connection);
        command.Parameters.AddWithValue("document", _documentId);
        command.Parameters.AddWithValue("query", PostgresSql.FormatVector(queryEmbedding));
        command.Parameters.AddWithValue("top", topK);

        var chunks = new List<Chunk>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            chunks.Add(JsonSerializer.Deserialize<Chunk>(reader.GetString(0))!);
        }

        return chunks;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!_schemaReady)
        {
            await using var command = new NpgsqlCommand(_schemaScript, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _schemaReady = true;
        }

        return connection;
    }
}
