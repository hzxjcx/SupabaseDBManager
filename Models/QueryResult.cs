using System.Collections.Generic;

namespace SupabaseDBManager.Models;

/// <summary>
/// 查询结果
/// </summary>
public class QueryResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 影响的行数
    /// </summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// 列名列表
    /// </summary>
    public List<string> ColumnNames { get; set; } = new();

    /// <summary>
    /// 数据行
    /// </summary>
    public List<DataRow> Rows { get; set; } = new();

    /// <summary>
    /// 是否为查询结果
    /// </summary>
    public bool IsQueryResult => Rows.Count > 0 || ColumnNames.Count > 0;
}
