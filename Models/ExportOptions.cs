using System.Collections.Generic;

namespace SupabaseDBManager.Models;

/// <summary>
/// 导出选项
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// 导出格式
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.Sql;

    /// <summary>
    /// 是否导出表结构
    /// </summary>
    public bool IncludeTables { get; set; } = true;

    /// <summary>
    /// 是否导出策略
    /// </summary>
    public bool IncludePolicies { get; set; } = true;

    /// <summary>
    /// 是否导出触发器
    /// </summary>
    public bool IncludeTriggers { get; set; } = true;

    /// <summary>
    /// 是否导出索引
    /// </summary>
    public bool IncludeIndexes { get; set; } = true;

    /// <summary>
    /// 是否导出函数
    /// </summary>
    public bool IncludeFunctions { get; set; } = true;

    /// <summary>
    /// 是否导出视图
    /// </summary>
    public bool IncludeViews { get; set; } = true;

    /// <summary>
    /// 要导出的架构列表（空则导出所有）
    /// </summary>
    public List<string> Schemas { get; set; } = new();

    /// <summary>
    /// 是否包含 DROP 语句
    /// </summary>
    public bool IncludeDropStatements { get; set; } = false;

    /// <summary>
    /// 是否添加注释
    /// </summary>
    public bool IncludeComments { get; set; } = true;
}

/// <summary>
/// 导出格式
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// SQL 格式
    /// </summary>
    Sql,

    /// <summary>
    /// JSON 格式
    /// </summary>
    Json
}
