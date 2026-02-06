using System.Collections.Generic;

namespace SupabaseDBManager.Models;

/// <summary>
/// 数据行
/// </summary>
public class DataRow
{
    /// <summary>
    /// 行序号
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 列数据集合（列名 -> 值）
    /// </summary>
    public Dictionary<string, object?> Values { get; set; } = new();

    /// <summary>
    /// 获取指定列的值
    /// </summary>
    public object? GetValue(string columnName)
    {
        return Values.TryGetValue(columnName, out var value) ? value : null;
    }

    /// <summary>
    /// 设置指定列的值
    /// </summary>
    public void SetValue(string columnName, object? value)
    {
        Values[columnName] = value;
    }

    /// <summary>
    /// 是否为新行
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// 是否已修改
    /// </summary>
    public bool IsModified { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
