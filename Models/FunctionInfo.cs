using System.Collections.Generic;

namespace SupabaseDBManager.Models;

/// <summary>
/// 函数信息
/// </summary>
public class FunctionInfo
{
    /// <summary>
    /// 函数名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 函数架构
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// 返回类型
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// 参数列表
    /// </summary>
    public List<FunctionParameter> Parameters { get; set; } = new();

    /// <summary>
    /// 函数类型（FUNCTION/PROCEDURE/AGGREGATE）
    /// </summary>
    public string FunctionType { get; set; } = "FUNCTION";

    /// <summary>
    /// 是否返回集合
    /// </summary>
    public bool ReturnsSet { get; set; }

    /// <summary>
    /// 函数语言（sql/plpgsql/c等）
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 是否为安全定义符
    /// </summary>
    public bool IsSecurityDefiner { get; set; }

    /// <summary>
    /// 是否为稳定函数
    /// </summary>
    public bool IsStable { get; set; }

    /// <summary>
    /// 函数定义
    /// </summary>
    public string? Definition { get; set; }

    /// <summary>
    /// 完整限定名
    /// </summary>
    public string FullName => $"{Schema}.{Name}";
}

/// <summary>
/// 函数参数
/// </summary>
public class FunctionParameter
{
    /// <summary>
    /// 参数名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 参数模式（IN/OUT/INOUT/VARIADIC）
    /// </summary>
    public string Mode { get; set; } = "IN";
}
