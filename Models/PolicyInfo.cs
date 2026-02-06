namespace SupabaseDBManager.Models;

/// <summary>
/// RLS 策略信息
/// </summary>
public class PolicyInfo
{
    /// <summary>
    /// 策略名称
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
    /// 策略类型（PERMISSIVE/RESTRICTIVE）
    /// </summary>
    public string PolicyType { get; set; } = string.Empty;

    /// <summary>
    /// 命令（SELECT/INSERT/UPDATE/DELETE/ALL）
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 角色名
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// USING 表达式
    /// </summary>
    public string? UsingExpression { get; set; }

    /// <summary>
    /// WITH CHECK 表达式
    /// </summary>
    public string? WithCheckExpression { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 完整限定名
    /// </summary>
    public string FullName => $"{Schema}.{TableName}.{Name}";
}
