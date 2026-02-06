using Npgsql;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SupabaseDBManager.Services;

/// <summary>
/// Supabase 数据库连接服务（使用连接池模式）
/// </summary>
public class SupabaseConnectionService
{
    private string? _connectionString;
    private readonly System.Threading.SemaphoreSlim _connectionLock = new System.Threading.SemaphoreSlim(1, 1);

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => !string.IsNullOrEmpty(_connectionString);

    /// <summary>
    /// 测试连接
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // 测试简单查询
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT version()";
            var version = await command.ExecuteScalarAsync();

            return (true, $"连接成功！PostgreSQL 版本: {version}");
        }
        catch (Exception ex)
        {
            return (false, $"连接失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开连接（保存连接字符串，使用连接池）
    /// </summary>
    public async Task<NpgsqlConnection?> OpenConnectionAsync(string connectionString)
    {
        try
        {
            _connectionString = connectionString;

            // 测试连接
            using var testConnection = new NpgsqlConnection(connectionString);
            await testConnection.OpenAsync();

            return testConnection;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开连接失败: {ex.Message}");
            _connectionString = null;
            return null;
        }
    }

    /// <summary>
    /// 获取新连接（每次都从连接池中获取）
    /// </summary>
    public NpgsqlConnection GetConnection()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("数据库未连接");
        }

        // 每次都创建新连接，Npgsql 会自动从连接池中获取
        return new NpgsqlConnection(_connectionString);
    }

    /// <summary>
    /// 执行带锁的数据库操作（确保同一时间只有一个查询在执行）
    /// </summary>
    public async Task<T> ExecuteWithLockAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        // 不使用 cancellationToken，避免 WaitAsync 被取消导致锁未获取
        await _connectionLock.WaitAsync();
        try
        {
            // 检查是否在等待期间被取消
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            return await operation();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 执行带锁的数据库操作（无返回值）
    /// </summary>
    public async Task ExecuteWithLockAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        // 不使用 cancellationToken，避免 WaitAsync 被取消导致锁未获取
        await _connectionLock.WaitAsync();
        try
        {
            // 检查是否在等待期间被取消
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            await operation();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public Task CloseConnectionAsync()
    {
        _connectionString = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    public NpgsqlTransaction? BeginTransaction()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("数据库未连接");
        }

        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection.BeginTransaction();
    }
}
