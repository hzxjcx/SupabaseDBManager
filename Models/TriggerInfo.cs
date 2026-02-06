namespace SupabaseDBManager.Models;

/// <summary>
/// 触发器信息
/// </summary>
public class TriggerInfo
{
    /// <summary>
    /// 触发器名称
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
    /// 触发时机（BEFORE/AFTER/INSTEAD OF）
    /// </summary>
    public string Timing { get; set; } = string.Empty;

    /// <summary>
    /// 触发事件（INSERT/UPDATE/DELETE/TRUNCATE）
    /// </summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// 触发函数名
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 函数参数
    /// </summary>
    public string? FunctionArguments { get; set; }

    /// <summary>
    /// 是否为行级触发器
    /// </summary>
    public bool IsRowLevel { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 触发器定义
    /// </summary>
    public string? Definition { get; set; }

    /// <summary>
    /// 完整限定名
    /// </summary>
    public string FullName => $"{Schema}.{TableName}.{Name}";
}
