using Npgsql;
using SupabaseDBManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SupabaseDBManager.Services;

/// <summary>
/// 数据库元数据查询服务
/// </summary>
public class MetadataQueryService
{
    private readonly SupabaseConnectionService _connectionService;

    public MetadataQueryService(SupabaseConnectionService connectionService)
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
    /// 获取所有用户表
    /// </summary>
    public async Task<List<TableInfo>> GetTablesAsync(string? schema = null)
    {
        using var conn = GetConnection();
        var tables = new List<TableInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        var whereConditions = new List<string>
        {
            "table_type = 'BASE TABLE'",
            "table_schema NOT IN ('pg_catalog', 'information_schema')"
        };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            whereConditions.Add($"table_schema = '{schema.Replace("'", "''")}'");
        }

        var whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";

        string query = $@"
            SELECT
                table_schema as schema,
                table_name as name,
                obj_description((table_schema||'.'||table_name)::regclass) as comment,
                pg_total_relation_size((table_schema||'.'||table_name)::regclass) as size,
                COALESCE(n_live_tup, 0) as row_count
            FROM information_schema.tables
            LEFT JOIN pg_stat_user_tables ON
                schemaname = table_schema AND
                relname = table_name
            {whereClause}
            ORDER BY table_schema, table_name";

        using var command = new NpgsqlCommand(query, conn);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var schemaOrdinal = reader.GetOrdinal("schema");
            var nameOrdinal = reader.GetOrdinal("name");
            var commentOrdinal = reader.GetOrdinal("comment");
            var sizeOrdinal = reader.GetOrdinal("size");
            var rowCountOrdinal = reader.GetOrdinal("row_count");

            tables.Add(new TableInfo
            {
                Schema = reader.GetString(schemaOrdinal),
                Name = reader.GetString(nameOrdinal),
                Comment = reader.IsDBNull(commentOrdinal) ? null : reader.GetString(commentOrdinal),
                Size = reader.IsDBNull(sizeOrdinal) ? null : reader.GetInt64(sizeOrdinal),
                RowCount = reader.IsDBNull(rowCountOrdinal) ? null : (long?)reader.GetInt64(rowCountOrdinal)
            });
        }

        return tables;
    }

    /// <summary>
    /// 获取表的列信息
    /// </summary>
    public async Task<List<ColumnInfo>> GetTableColumnsAsync(string schema, string tableName, CancellationToken cancellationToken = default)
    {
        // 🔑 使用全局锁，确保同一时间只有一个查询在执行
        return await _connectionService.ExecuteWithLockAsync(async () =>
        {
            return await GetTableColumnsInternalAsync(schema, tableName, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// 内部实现：获取表的列信息（不获取锁，由调用者确保线程安全）
    /// </summary>
    private async Task<List<ColumnInfo>> GetTableColumnsInternalAsync(string schema, string tableName, CancellationToken cancellationToken)
    {
        using var conn = GetConnection();
        var columns = new List<ColumnInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        string escapedSchema = schema.Replace("'", "''");
        string escapedTableName = tableName.Replace("'", "''");

        string query = $@"
            SELECT
                c.column_name as name,
                c.data_type as data_type,
                c.is_nullable as is_nullable,
                c.column_default as default_value,
                COALESCE(c.character_maximum_length, c.numeric_precision) as max_length,
                c.udt_schema || '.' || c.udt_name as udt_name,
                c.ordinal_position,
                pgd.description as comment
            FROM information_schema.columns c
            LEFT JOIN pg_catalog.pg_description pgd ON
                pgd.objoid = (c.table_schema||'.'||c.table_name)::regclass::oid
                AND pgd.objsubid = c.ordinal_position
            WHERE
                c.table_schema = '{escapedSchema}'
                AND c.table_name = '{escapedTableName}'
            ORDER BY c.ordinal_position";

        // 🔑 使用显式的 using 块，确保资源被正确释放
        using (var command = new NpgsqlCommand(query, conn))
        {
            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                var columnList = new List<ColumnInfo>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    var nameOrdinal = reader.GetOrdinal("name");
                    var dataTypeOrdinal = reader.GetOrdinal("data_type");
                    var isNullableOrdinal = reader.GetOrdinal("is_nullable");
                    var defaultValueOrdinal = reader.GetOrdinal("default_value");
                    var maxLengthOrdinal = reader.GetOrdinal("max_length");
                    var commentOrdinal = reader.GetOrdinal("comment");

                    var column = new ColumnInfo
                    {
                        Name = reader.GetString(nameOrdinal),
                        DataType = reader.GetString(dataTypeOrdinal),
                        IsNullable = reader.GetString(isNullableOrdinal) == "YES",
                        DefaultValue = reader.IsDBNull(defaultValueOrdinal) ? null : reader.GetString(defaultValueOrdinal),
                        MaxLength = reader.IsDBNull(maxLengthOrdinal) ? null : reader.GetInt32(maxLengthOrdinal) as int?,
                        Comment = reader.IsDBNull(commentOrdinal) ? null : reader.GetString(commentOrdinal)
                    };
                    columnList.Add(column);
                }

                // 获取主键信息（已经在锁内，直接调用内部方法）
                var pkColumns = await GetPrimaryKeyColumnsInternalAsync(schema, tableName);
                foreach (var col in columnList)
                {
                    col.IsPrimaryKey = pkColumns.Contains(col.Name);
                }

                // 获取外键信息（已经在锁内，直接调用内部方法）
                var fkInfo = await GetForeignKeyInfoInternalAsync(schema, tableName);
                foreach (var col in columnList)
                {
                    if (fkInfo.ContainsKey(col.Name))
                    {
                        col.IsForeignKey = true;
                        col.ForeignTable = fkInfo[col.Name].Table;
                        col.ForeignColumn = fkInfo[col.Name].Column;
                    }
                }

                return columnList;
            }
        }
    }

    /// <summary>
    /// 内部实现：获取主键列（不获取锁，由调用者确保线程安全）
    /// </summary>
    private async Task<HashSet<string>> GetPrimaryKeyColumnsInternalAsync(string schema, string tableName)
    {
        using var conn = GetConnection();
        var columns = new HashSet<string>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        string escapedSchema = schema.Replace("'", "''");
        string escapedTableName = tableName.Replace("'", "''");
        string qualifiedName = $"{escapedSchema}.{escapedTableName}";

        string query = $@"
            SELECT a.attname as column_name
            FROM pg_index i
            JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
            WHERE
                i.indrelid = '{qualifiedName}'::regclass::oid
                AND i.indisprimary";

        using (var command = new NpgsqlCommand(query, conn))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                var columnNameOrdinal = reader.GetOrdinal("column_name");
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(columnNameOrdinal));
                }
            }
        }

        return columns;
    }

    /// <summary>
    /// 内部实现：获取外键信息（不获取锁，由调用者确保线程安全）
    /// </summary>
    private async Task<Dictionary<string, (string Table, string Column)>> GetForeignKeyInfoInternalAsync(string schema, string tableName)
    {
        using var conn = GetConnection();
        var fkInfo = new Dictionary<string, (string Table, string Column)>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        string escapedSchema = schema.Replace("'", "''");
        string escapedTableName = tableName.Replace("'", "''");

        string query = $@"
            SELECT
                kcu.column_name,
                ccu.table_schema || '.' || ccu.table_name as foreign_table,
                ccu.column_name as foreign_column
            FROM information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE
                tc.constraint_type = 'FOREIGN KEY'
                AND tc.table_schema = '{escapedSchema}'
                AND tc.table_name = '{escapedTableName}'";

        using (var command = new NpgsqlCommand(query, conn))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                var columnNameOrdinal = reader.GetOrdinal("column_name");
                var foreignTableOrdinal = reader.GetOrdinal("foreign_table");
                var foreignColumnOrdinal = reader.GetOrdinal("foreign_column");
                while (await reader.ReadAsync())
                {
                    fkInfo[reader.GetString(columnNameOrdinal)] = (
                        reader.GetString(foreignTableOrdinal),
                        reader.GetString(foreignColumnOrdinal)
                    );
                }
            }
        }

        return fkInfo;
    }

    /// <summary>
    /// 获取 RLS 策略
    /// </summary>
    public async Task<List<PolicyInfo>> GetPoliciesAsync(string? schema = null, string? tableName = null)
    {
        using var conn = GetConnection();
        var policies = new List<PolicyInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        var whereConditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            whereConditions.Add($"schemaname = '{schema.Replace("'", "''")}'");
        }
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            whereConditions.Add($"tablename = '{tableName.Replace("'", "''")}'");
        }

        var whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

        string query = $@"
            SELECT
                policyname as name,
                schemaname as schema,
                tablename as table_name,
                permissive::text as policy_type,
                cmd as command,
                array_to_string(roles, ', ') as role,
                qual as using_expression,
                with_check as with_check_expression
            FROM pg_policies
            {whereClause}
            ORDER BY schemaname, tablename, policyname";

        using var command = new NpgsqlCommand(query, conn);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var nameOrdinal = reader.GetOrdinal("name");
            var schemaOrdinal = reader.GetOrdinal("schema");
            var tableNameOrdinal = reader.GetOrdinal("table_name");
            var policyTypeOrdinal = reader.GetOrdinal("policy_type");
            var commandOrdinal = reader.GetOrdinal("command");
            var roleOrdinal = reader.GetOrdinal("role");
            var usingExpressionOrdinal = reader.GetOrdinal("using_expression");
            var withCheckExpressionOrdinal = reader.GetOrdinal("with_check_expression");

            policies.Add(new PolicyInfo
            {
                Name = reader.GetString(nameOrdinal),
                Schema = reader.GetString(schemaOrdinal),
                TableName = reader.GetString(tableNameOrdinal),
                PolicyType = reader.GetString(policyTypeOrdinal) == "PERMISSIVE" ? "PERMISSIVE" : "RESTRICTIVE",
                Command = reader.GetString(commandOrdinal),
                Role = reader.GetString(roleOrdinal),
                UsingExpression = reader.IsDBNull(usingExpressionOrdinal) ? null : reader.GetString(usingExpressionOrdinal),
                WithCheckExpression = reader.IsDBNull(withCheckExpressionOrdinal) ? null : reader.GetString(withCheckExpressionOrdinal),
                IsEnabled = true
            });
        }

        return policies;
    }

    /// <summary>
    /// 获取触发器
    /// </summary>
    public async Task<List<TriggerInfo>> GetTriggersAsync(string? schema = null, string? tableName = null)
    {
        using var conn = GetConnection();
        var triggers = new List<TriggerInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        var whereConditions = new List<string> { "NOT t.tgisinternal" };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            whereConditions.Add($"n.nspname = '{schema.Replace("'", "''")}'");
        }
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            whereConditions.Add($"c.relname = '{tableName.Replace("'", "''")}'");
        }

        var whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";

        string query = $@"
            SELECT
                t.tgname as name,
                c.relname as table_name,
                n.nspname as schema,
                pg_get_triggerdef(t.oid) as definition,
                CASE WHEN t.tgtype::integer & 1 <> 0 THEN 'ROW' ELSE 'STATEMENT' END as level,
                CASE WHEN t.tgtype::integer & 2 <> 0 THEN 'BEFORE' ELSE 'AFTER' END as timing,
                string_agg(
                    CASE
                        WHEN t.tgtype::integer & 4 <> 0 THEN 'INSERT'
                        WHEN t.tgtype::integer & 8 <> 0 THEN 'DELETE'
                        WHEN t.tgtype::integer & 16 <> 0 THEN 'UPDATE'
                        WHEN t.tgtype::integer & 64 <> 0 THEN 'TRUNCATE'
                    END, ', '
                ) as event,
                p.proname as function_name,
                t.tgenabled::text as enabled
            FROM pg_trigger t
            JOIN pg_class c ON t.tgrelid = c.oid
            JOIN pg_namespace n ON c.relnamespace = n.oid
            JOIN pg_proc p ON t.tgfoid = p.oid
            {whereClause}
            GROUP BY t.tgname, c.relname, n.nspname, t.oid, p.proname, t.tgenabled::text
            ORDER BY n.nspname, c.relname, t.tgname";

        using var command = new NpgsqlCommand(query, conn);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var levelOrdinal = reader.GetOrdinal("level");
            var timingOrdinal = reader.GetOrdinal("timing");
            var eventOrdinal = reader.GetOrdinal("event");
            var enabledOrdinal = reader.GetOrdinal("enabled");
            var nameOrdinal = reader.GetOrdinal("name");
            var schemaOrdinal = reader.GetOrdinal("schema");
            var tableNameOrdinal = reader.GetOrdinal("table_name");
            var functionNameOrdinal = reader.GetOrdinal("function_name");
            var definitionOrdinal = reader.GetOrdinal("definition");

            var isRowLevel = reader.GetString(levelOrdinal) == "ROW";
            var timing = reader.GetString(timingOrdinal);
            var @event = reader.GetString(eventOrdinal);
            var enabled = reader.GetString(enabledOrdinal);

            triggers.Add(new TriggerInfo
            {
                Name = reader.GetString(nameOrdinal),
                Schema = reader.GetString(schemaOrdinal),
                TableName = reader.GetString(tableNameOrdinal),
                Timing = timing,
                Event = @event,
                FunctionName = reader.GetString(functionNameOrdinal),
                IsRowLevel = isRowLevel,
                IsEnabled = enabled == "O" || enabled == "A",
                Definition = reader.GetString(definitionOrdinal)
            });
        }

        return triggers;
    }

    /// <summary>
    /// 获取索引
    /// </summary>
    public async Task<List<IndexInfo>> GetIndexesAsync(string? schema = null, string? tableName = null)
    {
        using var conn = GetConnection();
        var indexes = new List<IndexInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        var whereConditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(schema))
        {
            whereConditions.Add($"n.nspname = '{schema.Replace("'", "''")}'");
        }
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            whereConditions.Add($"t.relname = '{tableName.Replace("'", "''")}'");
        }

        var whereClause = whereConditions.Count > 0 ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

        string query = $@"
            SELECT
                i.relname as name,
                n.nspname as schema,
                t.relname as table_name,
                am.amname as index_type,
                ix.indisunique as is_unique,
                ix.indisprimary as is_primary,
                pg_relation_size(i.oid) as size,
                ix.indpred::text as partial_condition,
                array_to_string(array_agg(a.attname ORDER BY array_position(ix.indkey, a.attnum)), ', ') as columns,
                pg_get_expr(ix.indexprs, ix.indrelid)::text as expression
            FROM pg_index ix
            JOIN pg_class t ON ix.indrelid = t.oid
            JOIN pg_class i ON ix.indexrelid = i.oid
            JOIN pg_namespace n ON t.relnamespace = n.oid
            JOIN pg_am am ON i.relam = am.oid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            {whereClause}
            GROUP BY i.relname, n.nspname, t.relname, am.amname,
                     ix.indisunique, ix.indisprimary, ix.indpred, ix.indexprs, i.oid, ix.indrelid
            ORDER BY n.nspname, t.relname, i.relname";

        using var command = new NpgsqlCommand(query, conn);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnsOrdinal = reader.GetOrdinal("columns");
            var nameOrdinal = reader.GetOrdinal("name");
            var schemaOrdinal = reader.GetOrdinal("schema");
            var tableNameOrdinal = reader.GetOrdinal("table_name");
            var indexTypeOrdinal = reader.GetOrdinal("index_type");
            var isUniqueOrdinal = reader.GetOrdinal("is_unique");
            var isPrimaryOrdinal = reader.GetOrdinal("is_primary");
            var partialConditionOrdinal = reader.GetOrdinal("partial_condition");
            var sizeOrdinal = reader.GetOrdinal("size");
            var expressionOrdinal = reader.GetOrdinal("expression");

            // columns 现在是字符串（逗号分隔的列名）
            var columnsStr = reader.IsDBNull(columnsOrdinal) ? "" : reader.GetString(columnsOrdinal);
            var columnList = string.IsNullOrWhiteSpace(columnsStr)
                ? new List<string>()
                : columnsStr.Split(',').Select(c => c.Trim()).ToList();

            indexes.Add(new IndexInfo
            {
                Name = reader.GetString(nameOrdinal),
                Schema = reader.GetString(schemaOrdinal),
                TableName = reader.GetString(tableNameOrdinal),
                IndexType = reader.GetString(indexTypeOrdinal),
                IsUnique = reader.GetBoolean(isUniqueOrdinal),
                IsPrimaryKey = reader.GetBoolean(isPrimaryOrdinal),
                Columns = columnList,
                PartialCondition = reader.IsDBNull(partialConditionOrdinal) ? null : reader.GetString(partialConditionOrdinal),
                IsPartial = !reader.IsDBNull(partialConditionOrdinal),
                Size = reader.GetInt64(sizeOrdinal),
                Expression = reader.IsDBNull(expressionOrdinal) ? null : reader.GetString(expressionOrdinal)
            });
        }

        return indexes;
    }

    /// <summary>
    /// 获取函数
    /// </summary>
    public async Task<List<FunctionInfo>> GetFunctionsAsync(string? schema = null)
    {
        using var conn = GetConnection();
        var functions = new List<FunctionInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        var whereConditions = new List<string>
        {
            "n.nspname NOT IN ('pg_catalog', 'information_schema')"
        };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            whereConditions.Add($"n.nspname = '{schema.Replace("'", "''")}'");
        }

        var whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";

        string query = $@"
            SELECT
                n.nspname as schema,
                p.proname as name,
                pg_get_function_result(p.oid) as return_type,
                pg_get_function_arguments(p.oid) as arguments,
                CASE WHEN p.prokind = 'f' THEN 'FUNCTION'
                     WHEN p.prokind = 'p' THEN 'PROCEDURE'
                     WHEN p.prokind = 'a' THEN 'AGGREGATE'
                     ELSE 'FUNCTION' END as function_type,
                p.proretset as returns_set,
                p.prolang as language_oid,
                l.lanname as language,
                p.prosecdef as is_security_definer,
                pg_get_functiondef(p.oid) as definition
            FROM pg_proc p
            JOIN pg_namespace n ON p.pronamespace = n.oid
            JOIN pg_language l ON p.prolang = l.oid
            {whereClause}
            ORDER BY n.nspname, p.proname";

        using var command = new NpgsqlCommand(query, conn);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var argumentsOrdinal = reader.GetOrdinal("arguments");
            var nameOrdinal = reader.GetOrdinal("name");
            var schemaOrdinal = reader.GetOrdinal("schema");
            var returnTypeOrdinal = reader.GetOrdinal("return_type");
            var functionTypeOrdinal = reader.GetOrdinal("function_type");
            var returnsSetOrdinal = reader.GetOrdinal("returns_set");
            var languageOrdinal = reader.GetOrdinal("language");
            var isSecurityDefinerOrdinal = reader.GetOrdinal("is_security_definer");
            var definitionOrdinal = reader.GetOrdinal("definition");

            var argsStr = reader.GetString(argumentsOrdinal);
            var parameters = ParseFunctionArguments(argsStr);

            functions.Add(new FunctionInfo
            {
                Name = reader.GetString(nameOrdinal),
                Schema = reader.GetString(schemaOrdinal),
                ReturnType = reader.GetString(returnTypeOrdinal),
                Parameters = parameters,
                FunctionType = reader.GetString(functionTypeOrdinal),
                ReturnsSet = reader.GetBoolean(returnsSetOrdinal),
                Language = reader.GetString(languageOrdinal),
                IsSecurityDefiner = reader.GetBoolean(isSecurityDefinerOrdinal),
                Definition = reader.GetString(definitionOrdinal)
            });
        }

        return functions;
    }

    private List<FunctionParameter> ParseFunctionArguments(string argsStr)
    {
        var parameters = new List<FunctionParameter>();
        if (string.IsNullOrWhiteSpace(argsStr) || argsStr == "()")
        {
            return parameters;
        }

        // 简单解析参数
        var args = argsStr.TrimStart('(').TrimEnd(')').Split(',');
        foreach (var arg in args)
        {
            var parts = arg.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var paramName = parts[0];
                var paramType = parts[1];

                parameters.Add(new FunctionParameter
                {
                    Name = paramName,
                    Type = paramType,
                    Mode = "IN"
                });
            }
        }

        return parameters;
    }

    /// <summary>
    /// 获取视图
    /// </summary>
    public async Task<List<ViewInfo>> GetViewsAsync(string? schema = null)
    {
        // 🔑 使用全局锁，确保同一时间只有一个查询在执行
        return await _connectionService.ExecuteWithLockAsync(async () =>
        {
            return await GetViewsInternalAsync(schema);
        });
    }

    /// <summary>
    /// 内部实现：获取视图（不获取锁，由调用者确保线程安全）
    /// </summary>
    private async Task<List<ViewInfo>> GetViewsInternalAsync(string? schema)
    {
        using var conn = GetConnection();
        var views = new List<ViewInfo>();

        // 动态构建 WHERE 子句以避免参数类型推断问题
        var whereConditions = new List<string>
        {
            "v.table_schema NOT IN ('pg_catalog', 'information_schema')"
        };
        if (!string.IsNullOrWhiteSpace(schema))
        {
            whereConditions.Add($"v.table_schema = '{schema.Replace("'", "''")}'");
        }

        var whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";

        // 使用单个查询获取所有信息，包括列数（避免多次查询导致并发问题）
        string query = $@"
            SELECT
                v.table_schema as schema,
                v.table_name as name,
                v.view_definition as definition,
                obj_description((v.table_schema||'.'||v.table_name)::regclass, 'pg_class') as comment,
                COALESCE(c.column_count, 0) as column_count
            FROM information_schema.views v
            LEFT JOIN (
                SELECT
                    table_schema,
                    table_name,
                    COUNT(*) as column_count
                FROM information_schema.columns
                GROUP BY table_schema, table_name
            ) c ON v.table_schema = c.table_schema AND v.table_name = c.table_name
            {whereClause}
            ORDER BY v.table_schema, v.table_name";

        using var command = new NpgsqlCommand(query, conn);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var schemaOrdinal = reader.GetOrdinal("schema");
            var nameOrdinal = reader.GetOrdinal("name");
            var definitionOrdinal = reader.GetOrdinal("definition");
            var commentOrdinal = reader.GetOrdinal("comment");
            var columnCountOrdinal = reader.GetOrdinal("column_count");

            var view = new ViewInfo
            {
                Schema = reader.GetString(schemaOrdinal),
                Name = reader.GetString(nameOrdinal),
                Definition = reader.IsDBNull(definitionOrdinal) ? null : reader.GetString(definitionOrdinal),
                Comment = reader.IsDBNull(commentOrdinal) ? null : reader.GetString(commentOrdinal),
                ColumnCount = reader.GetInt32(columnCountOrdinal),
                IsMaterialized = false
            };

            views.Add(view);
        }

        return views;
    }
}
