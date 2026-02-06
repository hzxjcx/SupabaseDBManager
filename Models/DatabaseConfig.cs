using System;

namespace SupabaseDBManager.Models;

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// Supabase URL（用于显示）
    /// </summary>
    public string? SupabaseUrl { get; set; }

    /// <summary>
    /// PostgreSQL 连接字符串（加密存储）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 连接名称（可选，用于标识）
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// 最后连接时间
    /// </summary>
    public DateTime? LastConnected { get; set; }
}
