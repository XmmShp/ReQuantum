using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Client;
using ReQuantum.Options;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ReQuantum.Services;

public interface IStorage
{
    JsonSerializerContext JsonContext { get; }

    void SetString(string key, string value);
    string GetString(string key);
    void Remove(string key);
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(IStorage), typeof(IInitializable)])]
public class SqliteStorage : IStorage, IInitializable, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteStorage(IOptions<StorageOptions> options)
    {
        _connection = new SqliteConnection($"Data Source={options.Value.StoragePath}");
        _connection.Open();
    }

    private void InitializeDatabase()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
                              CREATE TABLE IF NOT EXISTS KeyValueStore (
                                  Key TEXT PRIMARY KEY,
                                  Value TEXT NOT NULL
                              )
                              """;
        command.ExecuteNonQuery();
    }

    public JsonSerializerContext JsonContext => SourceGenerationContext.Default;

    public void SetString(string key, string value)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO KeyValueStore (Key, Value) 
                              VALUES ($key, $value)
                              ON CONFLICT(Key) DO UPDATE SET Value = $value
                              """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    public string GetString(string key)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT Value FROM KeyValueStore WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key);

        using var reader = command.ExecuteReader();

        return reader.Read()
            ? reader.GetString(0)
            : throw new KeyNotFoundException($"Key '{key}' not found");
    }

    public void Remove(string key)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM KeyValueStore WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key);
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    public Task InitializeAsync(IServiceProvider serviceProvider)
    {
        InitializeDatabase();
        return Task.CompletedTask;
    }
}
