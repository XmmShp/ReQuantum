using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Options;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Utilities;

namespace ReQuantum.Infrastructure.Services;

public interface IStorage
{
    JsonSerializerContext JsonContext { get; }

    bool ContainsKey(string key);
    void SetString(string key, string value);
    string GetString(string key);
    void Remove(string key);
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(IStorage), typeof(IDaemonService)])]
public class SqliteStorage : IStorage, IDaemonService, IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteStorage(IOptions<StorageOptions> options)
    {
        _connection = new SqliteConnection($"Data Source={options.Value.StoragePath}");
        _connection.Open();
        InitializeDatabase();
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

    /// <summary>
    /// 检查指定的键是否存在
    /// </summary>
    public bool ContainsKey(string key)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM KeyValueStore WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() != null;
    }

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
}

public static class StorageExtensions
{
    extension(IStorage storage)
    {
        /// <summary>
        /// 获取值（JSON反序列化，使用源生成器）
        /// </summary>
        public T? Get<T>(string key)
        {
            var value = storage.GetString(key);

            // 从上下文中获取 TypeInfo
            return storage.JsonContext.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> typeInfo
                ? JsonSerializer.Deserialize(value, typeInfo)
                : throw new NotSupportedException();
        }

        /// <summary>
        /// 尝试获取值
        /// </summary>
        public bool TryGet<T>(string key, out T? value)
        {
            try
            {
                value = storage.Get<T>(key);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 设置值（JSON序列化，使用源生成器）
        /// </summary>
        public void Set<T>(string key, T? value)
        {
            if (storage.JsonContext.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T?> typeInfo)
            {
                throw new NotSupportedException();
            }

            var json = JsonSerializer.Serialize(value, typeInfo);
            storage.SetString(key, json);
        }

        /// <summary>
        /// 获取值（JSON反序列化，使用源生成器）
        /// </summary>
        public T? GetWithEncryption<T>(string key)
        {
            var value = storage.GetString(key);
            var decryptedValue = Encryption.Decrypt(value);

            // 从上下文中获取 TypeInfo
            return storage.JsonContext.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> typeInfo
                ? JsonSerializer.Deserialize(decryptedValue, typeInfo)
                : throw new NotSupportedException();
        }

        /// <summary>
        /// 尝试获取值
        /// </summary>
        public bool TryGetWithEncryption<T>(string key, out T? value)
        {
            try
            {
                value = storage.GetWithEncryption<T>(key);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 设置加密值（JSON序列化，使用源生成器）
        /// </summary>
        public void SetWithEncryption<T>(string key, T? value)
        {
            if (storage.JsonContext.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T?> typeInfo)
            {
                throw new NotSupportedException();
            }

            var json = JsonSerializer.Serialize(value, typeInfo);
            var encryptedJson = Encryption.Encrypt(json);
            storage.SetString(key, encryptedJson);
        }
    }
}
