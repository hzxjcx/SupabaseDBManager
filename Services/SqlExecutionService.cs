using Npgsql;
using SupabaseDBManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SupabaseDBManager.Services;

/// <summary>
/// SQL 执行服务
/// </summary>
public class SqlExecutionService
{
    private readonly SupabaseConnectionService _connectionService;

    public SqlExecutionService(SupabaseConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    private NpgsqlConnection? GetConnection()
    {
        var conn = _connectionService.GetConnection();
        if (conn == null || conn.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("数据库未连接");
        }
        return conn;
    }

    /// <summary>
    /// 执行 SELECT 查询
    /// </summary>
    public async Task<QueryResult> ExecuteQueryAsync(string sql)
    {
        var result = new QueryResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var conn = GetConnection();

            using var command = new NpgsqlCommand(sql, conn);
            using var reader = await command.ExecuteReaderAsync();

            // 获取列名
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.ColumnNames.Add(reader.GetName(i));
            }

            // 读取数据行
            var rowIndex = 0;
            while (await reader.ReadAsync())
            {
                var row = new DataRow { Index = rowIndex++ };

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row.SetValue(columnName, value);
                }

                result.Rows.Add(row);
            }

            result.Success = true;
            result.RowsAffected = rowIndex;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Debug.WriteLine($"查询执行失败: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 执行非查询语句（INSERT/UPDATE/DELETE）
    /// </summary>
    public async Task<QueryResult> ExecuteNonQueryAsync(string sql)
    {
        var result = new QueryResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var conn = GetConnection();

            using var command = new NpgsqlCommand(sql, conn);
            var rowsAffected = await command.ExecuteNonQueryAsync();

            result.Success = true;
            result.RowsAffected = rowsAffected;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            Debug.WriteLine($"执行失败: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 获取查询结果集（DataTable 格式）
    /// </summary>
    public async Task<QueryResult> GetDataTableAsync(string sql)
    {
        return await ExecuteQueryAsync(sql);
    }

    /// <summary>
    /// 执行批量 SQL 语句（用分号分隔）
    /// </summary>
    public async Task<List<QueryResult>> ExecuteBatchAsync(string batchSql)
    {
        var results = new List<QueryResult>();

        // 分割 SQL 语句
        var statements = SplitSqlStatements(batchSql);

        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement))
            {
                continue;
            }

            var result = await ExecuteNonQueryAsync(statement);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// 分割 SQL 语句
    /// </summary>
    private List<string> SplitSqlStatements(string sql)
    {
        var statements = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';

        foreach (var ch in sql)
        {
            if ((ch == '\'' || ch == '"') && !inQuote)
            {
                inQuote = true;
                quoteChar = ch;
                current.Append(ch);
            }
            else if (ch == quoteChar && inQuote)
            {
                inQuote = false;
                current.Append(ch);
            }
            else if (ch == ';' && !inQuote)
            {
                var stmt = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(stmt))
                {
                    statements.Add(stmt);
                }
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        // 添加最后一个语句
        var lastStmt = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastStmt))
        {
            statements.Add(lastStmt);
        }

        return statements;
    }

    /// <summary>
    /// 执行标量查询
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(string sql)
    {
        try
        {
            var conn = GetConnection();

            using var command = new NpgsqlCommand(sql, conn);
            var result = await command.ExecuteScalarAsync();

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"标量查询失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 验证 SQL 语句
    /// </summary>
    public bool ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return false;
        }

        var trimmed = sql.Trim().ToUpperInvariant();

        // 检查是否为危险操作
        var dangerousKeywords = new[] {
            "DROP DATABASE",
            "DROP TABLE",
            "TRUNCATE",
            "ALTER DATABASE",
            "DELETE FROM",  // 需要更严格的检查
            "UPDATE",       // 需要更严格的检查
            "INSERT INTO"   // 需要更严格的检查
        };

        // 基本检查
        if (trimmed.Contains("DROP DATABASE"))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取查询的列类型信息
    /// </summary>
    public async Task<Dictionary<string, string>> GetColumnTypesAsync(string sql)
    {
        var columnTypes = new Dictionary<string, string>();

        try
        {
            var conn = GetConnection();

            using var command = new NpgsqlCommand(sql, conn);
            using var reader = await command.ExecuteReaderAsync();
            var schema = reader.GetColumnSchema();

            foreach (var column in schema)
            {
                if (column.ColumnName != null)
                {
                    var dataType = column.DataTypeName ?? "unknown";
                    columnTypes[column.ColumnName] = dataType;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取列类型失败: {ex.Message}");
        }

        return columnTypes;
    }
}
