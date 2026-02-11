using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SupabaseDBManager.Models;

/// <summary>
/// 数据库完整导出模型
/// </summary>
public class DatabaseExport
{
    /// <summary>
    /// 导出时间
    /// </summary>
    [JsonPropertyName("exportTime")]
    public DateTime ExportTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 数据库名称
    /// </summary>
    [JsonPropertyName("databaseName")]
    public string? DatabaseName { get; set; }

    /// <summary>
    /// 数据库版本
    /// </summary>
    [JsonPropertyName("databaseVersion")]
    public string? DatabaseVersion { get; set; }

    /// <summary>
    /// 架构信息
    /// </summary>
    [JsonPropertyName("schemas")]
    public List<SchemaExport> Schemas { get; set; } = new();
}

/// <summary>
/// 架构导出模型
/// </summary>
public class SchemaExport
{
    /// <summary>
    /// 架构名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表列表
    /// </summary>
    [JsonPropertyName("tables")]
    public List<TableExport> Tables { get; set; } = new();

    /// <summary>
    /// 策略列表
    /// </summary>
    [JsonPropertyName("policies")]
    public List<PolicyExport> Policies { get; set; } = new();

    /// <summary>
    /// 触发器列表
    /// </summary>
    [JsonPropertyName("triggers")]
    public List<TriggerExport> Triggers { get; set; } = new();

    /// <summary>
    /// 索引列表
    /// </summary>
    [JsonPropertyName("indexes")]
    public List<IndexExport> Indexes { get; set; } = new();

    /// <summary>
    /// 函数列表
    /// </summary>
    [JsonPropertyName("functions")]
    public List<FunctionExport> Functions { get; set; } = new();

    /// <summary>
    /// 视图列表
    /// </summary>
    [JsonPropertyName("views")]
    public List<ViewExport> Views { get; set; } = new();
}

/// <summary>
/// 表导出模型
/// </summary>
public class TableExport
{
    /// <summary>
    /// 表名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 注释
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// 列信息
    /// </summary>
    [JsonPropertyName("columns")]
    public List<ColumnExport> Columns { get; set; } = new();

    /// <summary>
    /// 主键列
    /// </summary>
    [JsonPropertyName("primaryKeys")]
    public List<string> PrimaryKeys { get; set; } = new();

    /// <summary>
    /// 外键关系
    /// </summary>
    [JsonPropertyName("foreignKeys")]
    public List<ForeignKeyExport> ForeignKeys { get; set; } = new();
}

/// <summary>
/// 列导出模型
/// </summary>
public class ColumnExport
{
    /// <summary>
    /// 列名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型
    /// </summary>
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 是否可空
    /// </summary>
    [JsonPropertyName("isNullable")]
    public bool IsNullable { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 注释
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// 数组维度
    /// </summary>
    [JsonPropertyName("arrayDimensions")]
    public int? ArrayDimensions { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }
}

/// <summary>
/// 外键导出模型
/// </summary>
public class ForeignKeyExport
{
    /// <summary>
    /// 列名
    /// </summary>
    [JsonPropertyName("columnName")]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 引用表
    /// </summary>
    [JsonPropertyName("referencedTable")]
    public string ReferencedTable { get; set; } = string.Empty;

    /// <summary>
    /// 引用列
    /// </summary>
    [JsonPropertyName("referencedColumn")]
    public string ReferencedColumn { get; set; } = string.Empty;
}

/// <summary>
/// 策略导出模型
/// </summary>
public class PolicyExport
{
    /// <summary>
    /// 策略名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表名
    /// </summary>
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 命令
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 策略类型
    /// </summary>
    [JsonPropertyName("policyType")]
    public string PolicyType { get; set; } = string.Empty;

    /// <summary>
    /// 角色
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// USING 表达式
    /// </summary>
    [JsonPropertyName("usingExpression")]
    public string? UsingExpression { get; set; }

    /// <summary>
    /// WITH CHECK 表达式
    /// </summary>
    [JsonPropertyName("withCheckExpression")]
    public string? WithCheckExpression { get; set; }
}

/// <summary>
/// 触发器导出模型
/// </summary>
public class TriggerExport
{
    /// <summary>
    /// 触发器名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表名
    /// </summary>
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 时机
    /// </summary>
    [JsonPropertyName("timing")]
    public string Timing { get; set; } = string.Empty;

    /// <summary>
    /// 事件
    /// </summary>
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// 是否行级
    /// </summary>
    [JsonPropertyName("isRowLevel")]
    public bool IsRowLevel { get; set; }

    /// <summary>
    /// 函数名
    /// </summary>
    [JsonPropertyName("functionName")]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 索引导出模型
/// </summary>
public class IndexExport
{
    /// <summary>
    /// 索引名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表名
    /// </summary>
    [JsonPropertyName("tableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 索引类型
    /// </summary>
    [JsonPropertyName("indexType")]
    public string? IndexType { get; set; }

    /// <summary>
    /// 是否唯一
    /// </summary>
    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; set; }

    /// <summary>
    /// 列
    /// </summary>
    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// 表达式
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    /// <summary>
    /// 部分条件
    /// </summary>
    [JsonPropertyName("partialCondition")]
    public string? PartialCondition { get; set; }
}

/// <summary>
/// 函数导出模型
/// </summary>
public class FunctionExport
{
    /// <summary>
    /// 函数名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 返回类型
    /// </summary>
    [JsonPropertyName("returnType")]
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// 函数类型
    /// </summary>
    [JsonPropertyName("functionType")]
    public string FunctionType { get; set; } = string.Empty;

    /// <summary>
    /// 语言
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 参数
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<FunctionParameterExport> Parameters { get; set; } = new();

    /// <summary>
    /// 是否 SECURITY DEFINER
    /// </summary>
    [JsonPropertyName("isSecurityDefiner")]
    public bool IsSecurityDefiner { get; set; }

    /// <summary>
    /// 完整定义
    /// </summary>
    [JsonPropertyName("definition")]
    public string? Definition { get; set; }
}

/// <summary>
/// 函数参数导出模型
/// </summary>
public class FunctionParameterExport
{
    /// <summary>
    /// 参数名
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 模式
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// 视图导出模型
/// </summary>
public class ViewExport
{
    /// <summary>
    /// 视图名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否物化视图
    /// </summary>
    [JsonPropertyName("isMaterialized")]
    public bool IsMaterialized { get; set; }

    /// <summary>
    /// 列数
    /// </summary>
    [JsonPropertyName("columnCount")]
    public int ColumnCount { get; set; }

    /// <summary>
    /// 视图定义
    /// </summary>
    [JsonPropertyName("definition")]
    public string? Definition { get; set; }
}
