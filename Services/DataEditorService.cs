using Npgsql;
using System;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomDataRow = SupabaseDBManager.Models.DataRow;

namespace SupabaseDBManager.Services;

/// <summary>
/// 数据编辑服务
/// </summary>
public class DataEditorService
{
    private readonly SupabaseConnectionService _connectionService;

    public DataEditorService(SupabaseConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    private NpgsqlConnection GetConnection()
    {
        var conn = _connectionService.GetConnection();
        // 打开连接（Npgsql 会自动从连接池中获取）
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }
        return conn;
    }

    /// <summary>
    /// 获取表数据
    /// </summary>
    public async Task<DataTable> GetTableDataAsync(string schema, string tableName, int limit = 100, int offset = 0)
    {
        using var conn = GetConnection();
        var dataTable = new DataTable();

        try
        {
            var query = $"SELECT * FROM {schema}.{tableName} ORDER BY 1 LIMIT {limit} OFFSET {offset}";

            using var command = new NpgsqlCommand(query, conn);
            using var reader = await command.ExecuteReaderAsync();

                // 加载架构（列信息）
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);

                    // 使用 object 类型以支持 null 值和任何数据类型
                    var dataColumn = new DataColumn(columnName, typeof(object));
                    dataTable.Columns.Add(dataColumn);
                }

                // 加载数据
                while (await reader.ReadAsync())
                {
                    var dataRow = dataTable.NewRow();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                        dataRow[i] = value;
                    }

                    dataTable.Rows.Add(dataRow);
                }

                // 接受所有更改，将行状态设置为 Unchanged
                // 这样用户修改后，行状态才会变为 Modified
                dataTable.AcceptChanges();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取表数据失败: {ex.Message}");
            throw;
        }

        return dataTable;
    }

    /// <summary>
    /// 获取表的总行数
    /// </summary>
    public async Task<long> GetTableRowCountAsync(string schema, string tableName)
    {
        using var conn = GetConnection();

        try
        {
            var query = $"SELECT COUNT(*) FROM {schema}.{tableName}";

            using var command = new NpgsqlCommand(query, conn);
            var result = await command.ExecuteScalarAsync();

            return result != null ? Convert.ToInt64(result) : 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取表行数失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 插入行
    /// </summary>
    public async Task<bool> InsertRowAsync(string schema, string tableName, Dictionary<string, object?> values)
    {
        using var conn = GetConnection();
        NpgsqlTransaction? transaction = null;

        try
        {
            transaction = await conn.BeginTransactionAsync();

            var columnNames = values.Keys.ToList();
            var columnsStr = string.Join(", ", columnNames.Select(c => $"\"{c}\""));
            var placeholdersStr = string.Join(", ", columnNames.Select((_, i) => $"@p{i}"));

            var insertSql = $"INSERT INTO {schema}.{tableName} ({columnsStr}) VALUES ({placeholdersStr}) RETURNING *";

            using var command = new NpgsqlCommand(insertSql, conn, transaction);
            foreach (var (key, value) in values.Select((v, i) => (v.Key, v.Value)))
            {
                var paramIndex = values.Keys.ToList().IndexOf(key);
                command.Parameters.AddWithValue($"@p{paramIndex}", value ?? DBNull.Value);
            }

            await command.ExecuteScalarAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            Debug.WriteLine($"插入行失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 更新行
    /// </summary>
    public async Task<bool> UpdateRowAsync(
        string schema,
        string tableName,
        Dictionary<string, object?> values,
        Dictionary<string, object?> whereClause)
    {
        using var conn = GetConnection();
        NpgsqlTransaction? transaction = null;

        try
        {
            transaction = await conn.BeginTransactionAsync();

            var setClause = string.Join(", ", values.Keys.Select(k => $"\"{k}\" = @{k}_value"));
            var whereClauseStr = string.Join(" AND ", whereClause.Keys.Select(k => $"\"{k}\" = @{k}_where"));

            var updateSql = $"UPDATE {schema}.{tableName} SET {setClause} WHERE {whereClauseStr}";

            using var command = new NpgsqlCommand(updateSql, conn, transaction);

            // 添加 SET 参数
            foreach (var (key, value) in values)
            {
                command.Parameters.AddWithValue($"@{key}_value", value ?? DBNull.Value);
            }

            // 添加 WHERE 参数
            foreach (var (key, value) in whereClause)
            {
                command.Parameters.AddWithValue($"@{key}_where", value ?? DBNull.Value);
            }

            var rowsAffected = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            Debug.WriteLine($"更新行失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 删除行
    /// </summary>
    public async Task<bool> DeleteRowAsync(string schema, string tableName, Dictionary<string, object?> whereClause)
    {
        using var conn = GetConnection();
        NpgsqlTransaction? transaction = null;

        try
        {
            transaction = await conn.BeginTransactionAsync();

            var whereClauseStr = string.Join(" AND ", whereClause.Keys.Select(k => $"\"{k}\" = @{k}"));

            var deleteSql = $"DELETE FROM {schema}.{tableName} WHERE {whereClauseStr}";

            using var command = new NpgsqlCommand(deleteSql, conn, transaction);

            foreach (var (key, value) in whereClause)
            {
                command.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
            }

            var rowsAffected = await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            Debug.WriteLine($"删除行失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 批量插入行
    /// </summary>
    public async Task<int> BulkInsertAsync(string schema, string tableName, List<Dictionary<string, object?>> rows)
    {
        using var conn = GetConnection();
        var inserted = 0;

        try
        {
            using var transaction = await conn.BeginTransactionAsync();

            foreach (var row in rows)
            {
                var success = await InsertRowAsync(schema, tableName, row);
                if (success)
                {
                    inserted++;
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"批量插入失败: {ex.Message}");
            throw;
        }

        return inserted;
    }

    /// <summary>
    /// 构建主键 WHERE 条件
    /// </summary>
    public Dictionary<string, object?> BuildPrimaryKeyWhereClause(CustomDataRow row, List<string> primaryKeys)
    {
        var whereClause = new Dictionary<string, object?>();

        foreach (var pk in primaryKeys)
        {
            var value = row.GetValue(pk);
            whereClause[pk] = value;
        }

        return whereClause;
    }

    /// <summary>
    /// 获取表的主键列
    /// </summary>
    public async Task<List<string>> GetPrimaryKeyColumnsAsync(string schema, string tableName)
    {
        using var conn = GetConnection();
        var primaryKeys = new List<string>();

        try
        {
            var query = @"
                SELECT a.attname as column_name
                FROM pg_index i
                JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
                WHERE
                    i.indrelid = (@schema||'.'||@tableName)::regclass::oid
                    AND i.indisprimary";

            using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@schema", schema);
            command.Parameters.AddWithValue("@tableName", tableName);

            using var reader = await command.ExecuteReaderAsync();
            var columnNameOrdinal = reader.GetOrdinal("column_name");
            while (await reader.ReadAsync())
            {
                primaryKeys.Add(reader.GetString(columnNameOrdinal));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取主键列失败: {ex.Message}");
        }

        return primaryKeys;
    }

    /// <summary>
    /// 搜索表数据
    /// </summary>
    public async Task<List<CustomDataRow>> SearchTableDataAsync(
        string schema,
        string tableName,
        string searchColumn,
        string searchValue,
        int limit = 100)
    {
        using var conn = GetConnection();
        var rows = new List<CustomDataRow>();

        try
        {
            var query = $"SELECT * FROM {schema}.{tableName} WHERE \"{searchColumn}\" LIKE @searchValue LIMIT {limit}";

            using var command = new NpgsqlCommand(query, conn);
            command.Parameters.AddWithValue("@searchValue", $"%{searchValue}%");

            using var reader = await command.ExecuteReaderAsync();

            var rowIndex = 0;
            while (await reader.ReadAsync())
            {
                var row = new CustomDataRow { Index = rowIndex++ };

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row.SetValue(columnName, value);
                }

                rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"搜索表数据失败: {ex.Message}");
            throw;
        }

        return rows;
    }
}
