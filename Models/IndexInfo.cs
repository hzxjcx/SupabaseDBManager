using System.Collections.Generic;

namespace SupabaseDBManager.Models;

/// <summary>
/// 索引信息
/// </summary>
public class IndexInfo
{
    /// <summary>
    /// 索引名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表架构
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 索引类型（btree/hash/gist/gin）
    /// </summary>
    public string IndexType { get; set; } = string.Empty;

    /// <summary>
    /// 是否唯一索引
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// 是否为主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 索引列列表
    /// </summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// 索引表达式（对于表达式索引）
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// 是否部分索引
    /// </summary>
    public bool IsPartial { get; set; }

    /// <summary>
    /// 部分索引条件
    /// </summary>
    public string? PartialCondition { get; set; }

    /// <summary>
    /// 索引大小（字节）
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 列的文本表示（用于 UI 显示）
    /// </summary>
    public string ColumnsText => Expression ?? string.Join(", ", Columns);

    /// <summary>
    /// 完整限定名
    /// </summary>
    public string FullName => $"{Schema}.{TableName}.{Name}";
}
