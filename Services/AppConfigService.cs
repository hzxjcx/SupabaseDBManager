using Microsoft.Extensions.Configuration;
using SupabaseDBManager.Models;
using System.IO;
using System;

namespace SupabaseDBManager.Services;

/// <summary>
/// 应用程序配置服务（从 appsettings.json 读取）
/// </summary>
public class AppConfigService
{
    private readonly IConfiguration _configuration;

    public AppConfigService()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var configPath = Path.Combine(baseDirectory, "appsettings.json");
        var exampleConfigPath = Path.Combine(baseDirectory, "appsettings.example.json");

        var builder = new ConfigurationBuilder()
            .SetBasePath(baseDirectory);

        // 优先使用 appsettings.json，如果不存在则使用 appsettings.example.json
        if (File.Exists(configPath))
        {
            // 存在 appsettings.json，使用它
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }
        else if (File.Exists(exampleConfigPath))
        {
            // 不存在 appsettings.json，但存在 example 文件，使用它
            builder.AddJsonFile("appsettings.example.json", optional: false, reloadOnChange: true);
        }
        else
        {
            // 两者都不存在，创建空的配置（不会报错，但后续需要用户手动配置）
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        }

        _configuration = builder.Build();
    }

    /// <summary>
    /// 获取应用程序配置
    /// </summary>
    public AppSettings GetSettings()
    {
        var settings = new AppSettings();

        _configuration.GetSection("SupabaseSettings").Bind(settings.SupabaseSettings);
        _configuration.GetSection("ApplicationSettings").Bind(settings.ApplicationSettings);

        return settings;
    }

    /// <summary>
    /// 获取连接字符串（基于配置文件）
    /// </summary>
    public string GetConnectionString()
    {
        var settings = GetSettings();
        var connSettings = settings.SupabaseSettings.PoolerSettings;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = connSettings.Host,
            Port = connSettings.Port,
            Database = connSettings.Database,
            Username = connSettings.Username,
            Password = connSettings.Password,
            // 连接池设置
            MaxPoolSize = settings.SupabaseSettings.MaxPoolSize,
            MinPoolSize = 1,
            Timeout = settings.SupabaseSettings.ConnectionTimeout,
            Pooling = true,
            KeepAlive = 30,
            // 🔑 关键：确保命令完成后完全清理
            NoResetOnClose = false
        };

        // Session mode 不需要特殊设置
        // Transaction mode 需要禁用 PREPARE
        if (connSettings.PoolMode == "Transaction")
        {
            // Transaction mode: 每个事务后释放连接
            builder.MaxAutoPrepare = 0;
            builder.NoResetOnClose = true;
        }

        return builder.ToString();
    }

    /// <summary>
    /// 获取 Supabase 项目 URL
    /// </summary>
    public string GetProjectUrl()
    {
        var settings = GetSettings();
        return settings.SupabaseSettings.ProjectUrl;
    }

    /// <summary>
    /// 保存用户配置（保存到加密的本地文件）
    /// </summary>
    public void SaveUserConfig(string connectionString, string projectUrl)
    {
        var configService = new ConfigService();
        var config = new DatabaseConfig
        {
            ConnectionString = connectionString,
            SupabaseUrl = projectUrl,
            ConnectionName = "User Config"
        };
        configService.SaveConfig(config);
    }

    /// <summary>
    /// 检查配置文件是否存在
    /// </summary>
    public bool ConfigFileExists()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        return File.Exists(configPath);
    }

    /// <summary>
    /// 创建示例配置文件
    /// </summary>
    public void CreateExampleConfigFile()
    {
        var examplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.example.json");
        if (!File.Exists(examplePath))
        {
            var exampleConfig = new AppSettings
            {
                SupabaseSettings = new SupabaseSettings
                {
                    ProjectUrl = "https://your-project.supabase.co",
                    PoolerSettings = new PoolerSettings
                    {
                        Host = "aws-0-ap-southeast-1.pooler.supabase.com",
                        Port = 5432,
                        Database = "postgres",
                        Username = "postgres.your-project-id",
                        Password = "your-database-password",
                        PoolMode = "Session"
                    }
                },
                ApplicationSettings = new ApplicationSettings
                {
                    Theme = "Light",
                    Language = "zh-CN",
                    AutoLoadTables = true,
                    DefaultQueryLimit = 100
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exampleConfig, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(examplePath, json);
        }
    }
}
