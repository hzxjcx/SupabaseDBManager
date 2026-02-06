namespace SupabaseDBManager.Models;

/// <summary>
/// 列信息
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// 列名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// 是否可为空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 列默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 是否为主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否为外键
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// 外键引用表
    /// </summary>
    public string? ForeignTable { get; set; }

    /// <summary>
    /// 外键引用列
    /// </summary>
    public string? ForeignColumn { get; set; }

    /// <summary>
    /// 列注释
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 字符最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 数组维度
    /// </summary>
    public int? ArrayDimensions { get; set; }
}
