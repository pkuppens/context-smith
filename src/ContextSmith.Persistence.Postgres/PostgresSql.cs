using System.Globalization;

namespace ContextSmith.Persistence.Postgres;

/// <summary>SQL text and value formatting shared by the PostgreSQL store and retrieval service.</summary>
public static class PostgresSql
{
    /// <summary>Script that creates the <c>documents</c> table.</summary>
    internal const string DocumentsSchemaScript = """
        CREATE TABLE IF NOT EXISTS documents (
            id text PRIMARY KEY,
            content jsonb NOT NULL,
            updated_at timestamptz NOT NULL DEFAULT now()
        );
        """;

    /// <summary>Builds the script that creates the extension, tables, and index.</summary>
    /// <param name="vectorDimension">Dimension of the embedding vectors. Fixed for the table once created.</param>
    /// <returns>An idempotent SQL script.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="vectorDimension"/> is not positive.</exception>
    public static string CreateSchemaScript(int vectorDimension)
        => "CREATE EXTENSION IF NOT EXISTS vector;\n" + DocumentsSchemaScript + "\n" + ChunksSchemaScript(vectorDimension);

    /// <summary>Builds the script that creates the extension, the <c>chunks</c> table, and its index.</summary>
    /// <param name="vectorDimension">Dimension of the embedding vectors.</param>
    /// <returns>An idempotent SQL script.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="vectorDimension"/> is not positive.</exception>
    internal static string ChunksSchemaScript(int vectorDimension)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(vectorDimension, 0);

        return $"""
            CREATE EXTENSION IF NOT EXISTS vector;
            CREATE TABLE IF NOT EXISTS chunks (
                document_id text NOT NULL,
                id text NOT NULL,
                content jsonb NOT NULL,
                embedding vector({vectorDimension}) NOT NULL,
                PRIMARY KEY (document_id, id)
            );
            """;
    }

    /// <summary>Formats a vector as a pgvector text literal such as <c>[1,0.5]</c>, always with the invariant culture.</summary>
    /// <param name="vector">Vector to format.</param>
    /// <returns>The literal, to be cast with <c>::vector</c> in SQL.</returns>
    public static string FormatVector(IEnumerable<float> vector)
        => "[" + string.Join(',', vector.Select(value => value.ToString("R", CultureInfo.InvariantCulture))) + "]";
}
