using SupabaseDBManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupabaseDBManager.Services;

/// <summary>
/// SQL DDL 生成服务
/// </summary>
public class SqlGenerationService
{
    /// <summary>
    /// 生成 CREATE TABLE DDL
    /// </summary>
    public string GenerateCreateTableDdl(TableInfo table, List<ColumnInfo> columns)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- 表: {table.FullName}");
        if (!string.IsNullOrWhiteSpace(table.Comment))
        {
            sb.AppendLine($"-- 注释: {table.Comment}");
        }
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {table.FullName} (");

        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            sb.Append($"    {col.Name} {FormatDataType(col)}");

            if (col.IsPrimaryKey)
            {
                sb.Append(" PRIMARY KEY");
            }

            if (!col.IsNullable && !col.IsPrimaryKey)
            {
                sb.Append(" NOT NULL");
            }

            if (!string.IsNullOrWhiteSpace(col.DefaultValue))
            {
                sb.Append($" DEFAULT {col.DefaultValue}");
            }

            if (i < columns.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine(");");

        // 添加列注释
        foreach (var col in columns.Where(c => !string.IsNullOrWhiteSpace(c.Comment)))
        {
            sb.AppendLine($"COMMENT ON COLUMN {table.FullName}.{col.Name} IS '{col.Comment}';");
        }

        if (!string.IsNullOrWhiteSpace(table.Comment))
        {
            sb.AppendLine($"COMMENT ON TABLE {table.FullName} IS '{table.Comment}';");
        }

        return sb.ToString();
    }

    private string FormatDataType(ColumnInfo col)
    {
        var type = col.DataType.ToUpperInvariant();

        // 处理数组类型
        if (col.ArrayDimensions.HasValue && col.ArrayDimensions.Value > 0)
        {
            type += string.Concat(Enumerable.Repeat("[]", col.ArrayDimensions.Value));
        }

        // 处理有长度的类型
        if (col.MaxLength.HasValue && col.MaxLength.Value > 0)
        {
            var typesWithLength = new[] { "CHARACTER VARYING", "VARCHAR", "CHARACTER", "CHAR", "BIT VARYING", "BIT" };
            if (typesWithLength.Any(t => type.Contains(t)))
            {
                type = $"{type}({col.MaxLength.Value})";
            }
        }

        return type;
    }

    /// <summary>
    /// 生成 CREATE POLICY DDL
    /// </summary>
    public string GenerateCreatePolicyDdl(PolicyInfo policy)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- 策略: {policy.FullName}");
        sb.AppendLine($"CREATE POLICY IF NOT EXISTS \"{policy.Name}\"");
        sb.AppendLine($"ON {policy.Schema}.{policy.TableName}");
        sb.AppendLine($"AS {policy.PolicyType}");

        if (policy.Command != "ALL")
        {
            sb.AppendLine($"FOR {policy.Command}");
        }

        sb.AppendLine($"TO {policy.Role}");

        if (!string.IsNullOrWhiteSpace(policy.UsingExpression))
        {
            sb.AppendLine($"USING ({policy.UsingExpression})");
        }

        if (!string.IsNullOrWhiteSpace(policy.WithCheckExpression))
        {
            sb.AppendLine($"WITH CHECK ({policy.WithCheckExpression})");
        }

        sb.Append(";");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 CREATE TRIGGER DDL
    /// </summary>
    public string GenerateCreateTriggerDdl(TriggerInfo trigger)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- 触发器: {trigger.FullName}");
        sb.AppendLine($"CREATE TRIGGER \"{trigger.Name}\"");
        sb.AppendLine($"{trigger.Timing} {trigger.Event}");

        if (trigger.IsRowLevel)
        {
            sb.AppendLine("FOR EACH ROW");
        }

        sb.AppendLine($"ON {trigger.Schema}.{trigger.TableName}");
        sb.Append($"EXECUTE FUNCTION {trigger.FunctionName}();");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 CREATE INDEX DDL
    /// </summary>
    public string GenerateCreateIndexDdl(IndexInfo index)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- 索引: {index.FullName}");
        sb.Append($"CREATE ");

        if (index.IsUnique)
        {
            sb.Append("UNIQUE ");
        }

        if (!string.IsNullOrWhiteSpace(index.IndexType))
        {
            sb.Append($"{index.IndexType.ToUpperInvariant()} ");
        }

        sb.Append($"INDEX IF NOT EXISTS \"{index.Name}\" ");
        sb.AppendLine($"ON {index.Schema}.{index.TableName}");

        // 索引列或表达式
        if (!string.IsNullOrWhiteSpace(index.Expression))
        {
            sb.AppendLine($"({index.Expression})");
        }
        else
        {
            var columnsStr = string.Join(", ", index.Columns.Select(c => $"\"{c}\""));
            sb.AppendLine($"({columnsStr})");
        }

        if (index.IsPartial && !string.IsNullOrWhiteSpace(index.PartialCondition))
        {
            sb.AppendLine($"WHERE {index.PartialCondition}");
        }

        sb.Append(";");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 CREATE FUNCTION DDL
    /// </summary>
    public string GenerateCreateFunctionDdl(FunctionInfo function)
    {
        // 如果有完整定义，直接使用
        if (!string.IsNullOrWhiteSpace(function.Definition))
        {
            var def = function.Definition;

            // 确保使用 CREATE OR REPLACE
            if (!def.StartsWith("CREATE OR REPLACE", StringComparison.OrdinalIgnoreCase))
            {
                def = def.Replace("CREATE FUNCTION", "CREATE OR REPLACE FUNCTION");
                def = def.Replace("CREATE PROCEDURE", "CREATE OR REPLACE PROCEDURE");
            }

            return def;
        }

        // 否则生成基本定义
        var sb = new StringBuilder();

        sb.AppendLine($"-- 函数: {function.FullName}");
        sb.AppendLine($"CREATE OR REPLACE {function.FunctionType} {function.FullName}(");

        var paramsStr = string.Join(", ", function.Parameters.Select(p =>
        {
            var sb2 = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(p.Name))
            {
                sb2.Append(p.Name);
                if (!string.IsNullOrWhiteSpace(p.Mode))
                {
                    sb2.Append($" {p.Mode}");
                }
                sb2.Append($" {p.Type}");
            }
            return sb2.ToString();
        }));

        sb.AppendLine($"    {paramsStr}");
        sb.AppendLine($")");
        sb.AppendLine($"RETURNS {function.ReturnType}");
        sb.AppendLine($"LANGUAGE {function.Language}");

        if (function.IsSecurityDefiner)
        {
            sb.AppendLine($"SECURITY DEFINER");
        }

        if (function.IsStable)
        {
            sb.AppendLine($"STABLE");
        }

        sb.AppendLine("AS $$");
        sb.AppendLine($"    -- 函数体待实现");
        sb.AppendLine("$$;");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 CREATE VIEW DDL
    /// </summary>
    public string GenerateCreateViewDdl(ViewInfo view)
    {
        var sb = new StringBuilder();

        var viewType = view.IsMaterialized ? "MATERIALIZED VIEW" : "VIEW";

        sb.AppendLine($"-- 视图: {view.FullName}");
        sb.AppendLine($"CREATE OR REPLACE {viewType} IF NOT EXISTS {view.FullName} AS");
        sb.AppendLine(view.Definition?.TrimEnd() ?? "-- 未找到视图定义");
        sb.Append(";");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 DROP TABLE DDL
    /// </summary>
    public string GenerateDropTableDdl(TableInfo table, bool cascade = false)
    {
        var cascadeStr = cascade ? " CASCADE" : "";
        return $"DROP TABLE IF EXISTS {table.FullName}{cascadeStr};";
    }

    /// <summary>
    /// 生成 DROP POLICY DDL
    /// </summary>
    public string GenerateDropPolicyDdl(PolicyInfo policy)
    {
        return $"DROP POLICY IF EXISTS \"{policy.Name}\" ON {policy.Schema}.{policy.TableName};";
    }

    /// <summary>
    /// 生成 DROP TRIGGER DDL
    /// </summary>
    public string GenerateDropTriggerDdl(TriggerInfo trigger)
    {
        return $"DROP TRIGGER IF EXISTS \"{trigger.Name}\" ON {trigger.Schema}.{trigger.TableName};";
    }

    /// <summary>
    /// 生成 DROP INDEX DDL
    /// </summary>
    public string GenerateDropIndexDdl(IndexInfo index)
    {
        return $"DROP INDEX IF EXISTS {index.Schema}.{index.Name};";
    }

    /// <summary>
    /// 生成 DROP FUNCTION DDL
    /// </summary>
    public string GenerateDropFunctionDdl(FunctionInfo function, bool cascade = false)
    {
        var args = string.Join(", ", function.Parameters.Select(p => p.Type));
        var cascadeStr = cascade ? " CASCADE" : "";
        return $"DROP {function.FunctionType} IF EXISTS {function.FullName}({args}){cascadeStr};";
    }

    /// <summary>
    /// 生成 DROP VIEW DDL
    /// </summary>
    public string GenerateDropViewDdl(ViewInfo view, bool cascade = false)
    {
        var viewType = view.IsMaterialized ? "MATERIALIZED VIEW" : "VIEW";
        var cascadeStr = cascade ? " CASCADE" : "";
        return $"DROP {viewType} IF EXISTS {view.FullName}{cascadeStr};";
    }
}
