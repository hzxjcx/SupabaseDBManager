using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using SupabaseDBManager.Models;

namespace SupabaseDBManager.Services;

/// <summary>
/// 配置服务 - 用于保存和加载应用程序配置
/// </summary>
public class ConfigService
{
    private readonly string _configFilePath;
    private readonly byte[] _entropy;

    public ConfigService()
    {
        // 配置文件保存在程序所在目录
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

        _configFilePath = Path.Combine(appDirectory, "db_config.json");

        // 使用固定的熵值增加加密强度
        _entropy = Encoding.UTF8.GetBytes("SupabaseDBManager_Config_2024");
    }

    /// <summary>
    /// 保存配置（连接字符串使用 DPAPI 加密）
    /// </summary>
    public void SaveConfig(DatabaseConfig config)
    {
        try
        {
            DatabaseConfig configToSave;

            // 如果有连接字符串，先加密
            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                configToSave = new DatabaseConfig
                {
                    SupabaseUrl = config.SupabaseUrl,
                    ConnectionString = EncryptString(config.ConnectionString),
                    ConnectionName = config.ConnectionName,
                    LastConnected = DateTime.Now
                };
            }
            else
            {
                configToSave = new DatabaseConfig
                {
                    SupabaseUrl = config.SupabaseUrl,
                    ConnectionString = config.ConnectionString,
                    ConnectionName = config.ConnectionName,
                    LastConnected = DateTime.Now
                };
            }

            var json = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"保存配置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载配置（连接字符串自动解密）
    /// </summary>
    public DatabaseConfig? LoadConfig()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return null;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<DatabaseConfig>(json);

            if (config == null)
            {
                return null;
            }

            // 如果有加密的连接字符串，解密它
            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                try
                {
                    config.ConnectionString = DecryptString(config.ConnectionString);
                }
                catch
                {
                    // 解密失败，可能配置损坏，返回 null
                    return null;
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new Exception($"加载配置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 使用 DPAPI 加密字符串
    /// </summary>
    private string EncryptString(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// 使用 DPAPI 解密字符串
    /// </summary>
    private string DecryptString(string encryptedText)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedText);
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// 清除配置
    /// </summary>
    public void ClearConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"清除配置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 检查是否已保存配置
    /// </summary>
    public bool HasConfig()
    {
        return File.Exists(_configFilePath);
    }

    /// <summary>
    /// 保存最近使用的连接列表
    /// </summary>
    public void SaveRecentConnection(string connectionString)
    {
        try
        {
            var recentConnectionsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_connections.json");
            var recentConnections = new List<string>();

            // 加载现有的最近连接
            if (File.Exists(recentConnectionsFilePath))
            {
                var json = File.ReadAllText(recentConnectionsFilePath);
                recentConnections = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }

            // 添加新的连接（如果不存在）
            if (!recentConnections.Contains(connectionString))
            {
                recentConnections.Insert(0, connectionString);

                // 只保留最近 10 个
                if (recentConnections.Count > 10)
                {
                    recentConnections = recentConnections.Take(10).ToList();
                }

                var updatedJson = JsonSerializer.Serialize(recentConnections, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(recentConnectionsFilePath, updatedJson);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"保存最近连接失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载最近使用的连接列表
    /// </summary>
    public List<string> LoadRecentConnections()
    {
        try
        {
            var recentConnectionsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_connections.json");

            if (!File.Exists(recentConnectionsFilePath))
            {
                return new List<string>();
            }

            var json = File.ReadAllText(recentConnectionsFilePath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
