namespace SupabaseDBManager.Models;

/// <summary>
/// 视图信息
/// </summary>
public class ViewInfo
{
    /// <summary>
    /// 视图名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 视图架构
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// 视图定义
    /// </summary>
    public string? Definition { get; set; }

    /// <summary>
    /// 视图注释
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 视图查询语句
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// 是否为物化视图
    /// </summary>
    public bool IsMaterialized { get; set; }

    /// <summary>
    /// 视图列数
    /// </summary>
    public int ColumnCount { get; set; }

    /// <summary>
    /// 完整限定名
    /// </summary>
    public string FullName => $"{Schema}.{Name}";
}
