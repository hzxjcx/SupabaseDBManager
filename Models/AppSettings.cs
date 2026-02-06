namespace SupabaseDBManager.Models;

/// <summary>
/// 应用程序配置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Supabase 设置
    /// </summary>
    public SupabaseSettings SupabaseSettings { get; set; } = new();

    /// <summary>
    /// 应用程序设置
    /// </summary>
    public ApplicationSettings ApplicationSettings { get; set; } = new();
}

/// <summary>
/// Supabase 连接设置
/// </summary>
public class SupabaseSettings
{
    /// <summary>
    /// Supabase 项目 URL
    /// </summary>
    public string ProjectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Pooler 连接设置
    /// </summary>
    public PoolerSettings PoolerSettings { get; set; } = new();

    /// <summary>
    /// 连接池最大大小
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
}

/// <summary>
/// Pooler 连接设置
/// </summary>
public class PoolerSettings
{
    /// <summary>
    /// Pooler 主机地址
    /// 示例：aws-0-ap-southeast-1.pooler.supabase.com
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Pooler 端口（Session mode: 5432, Transaction mode: 6543）
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string Database { get; set; } = "postgres";

    /// <summary>
    /// 用户名（格式：postgres.project-id）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 数据库密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Pooler 模式：Session 或 Transaction
    /// Session: 适合长连接应用（数据库管理工具）
    /// Transaction: 适合 serverless 函数
    /// </summary>
    public string PoolMode { get; set; } = "Session";
}

/// <summary>
/// 应用程序设置
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// 主题（Light/Dark）
    /// </summary>
    public string Theme { get; set; } = "Light";

    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 启动时自动加载表列表
    /// </summary>
    public bool AutoLoadTables { get; set; } = true;

    /// <summary>
    /// 默认查询返回行数限制
    /// </summary>
    public int DefaultQueryLimit { get; set; } = 100;
}
