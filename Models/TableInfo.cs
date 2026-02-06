namespace SupabaseDBManager.Models;

/// <summary>
/// 数据表信息
/// </summary>
public class TableInfo
{
    /// <summary>
    /// 表架构名称
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// 表名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表注释
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 表大小（字节）
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 行数
    /// </summary>
    public long? RowCount { get; set; }

    /// <summary>
    /// 完整限定名
    /// </summary>
    public string FullName => $"{Schema}.{Name}";
}
