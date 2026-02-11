using Microsoft.Win32;
using SupabaseDBManager.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SupabaseDBManager.Services;

/// <summary>
/// 数据库导出服务
/// </summary>
public class ExportService
{
    private readonly MetadataQueryService _metadataQueryService;
    private readonly SqlGenerationService _sqlGenerationService;

    public ExportService(MetadataQueryService metadataQueryService, SqlGenerationService sqlGenerationService)
    {
        _metadataQueryService = metadataQueryService;
        _sqlGenerationService = sqlGenerationService;
    }

    /// <summary>
    /// 导出数据库到文件
    /// </summary>
    public async Task<string> ExportToFileAsync(ExportOptions exportOptions, Action<ExportProgress>? progressCallback = null)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = exportOptions.Format == ExportFormat.Sql
                ? "SQL 文件 (*.sql)|*.sql|所有文件 (*.*)|*.*"
                : "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            DefaultExt = exportOptions.Format == ExportFormat.Sql ? "sql" : "json",
            FileName = $"database_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}"
        };

        if (saveFileDialog.ShowDialog() != true)
        {
            return string.Empty;
        }

        var content = exportOptions.Format == ExportFormat.Sql
            ? await ExportAsSqlAsync(exportOptions, progressCallback)
            : await ExportAsJsonAsync(exportOptions, progressCallback);

        await File.WriteAllTextAsync(saveFileDialog.FileName, content, Encoding.UTF8);
        return saveFileDialog.FileName;
    }

    /// <summary>
    /// 导出为 SQL 格式
    /// </summary>
    public async Task<string> ExportAsSqlAsync(ExportOptions options, Action<ExportProgress>? progressCallback = null)
    {
        var progress = new ExportProgress
        {
            CurrentStage = "开始导出",
            TotalSteps = 100
        };

        progressCallback?.Invoke(progress);

        var sb = new StringBuilder();
        var schemas = options.Schemas.Count > 0 ? options.Schemas.Cast<string?>().ToList() : new List<string?> { null };

        // 添加文件头
        sb.AppendLine("-- ================================================");
        sb.AppendLine("-- Supabase 数据库导出");
        sb.AppendLine($"-- 导出时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- ================================================");
        sb.AppendLine();
        sb.AppendLine("-- 开始事务");
        sb.AppendLine("BEGIN;");
        sb.AppendLine();

        int completedSteps = 0;

        // 1. 导出函数（因为触发器依赖函数）
        if (options.IncludeFunctions)
        {
            progress.CurrentStage = "导出函数";
            progressCallback?.Invoke(progress);

            var functions = new List<FunctionInfo>();
            foreach (var schema in schemas)
            {
                var schemaFunctions = await _metadataQueryService.GetFunctionsAsync(schema);
                functions.AddRange(schemaFunctions);
            }

            foreach (var function in functions)
            {
                progress.CurrentStep = $"函数: {function.FullName}";
                progressCallback?.Invoke(progress);

                if (options.IncludeDropStatements)
                {
                    sb.AppendLine(_sqlGenerationService.GenerateDropFunctionDdl(function));
                }
                sb.AppendLine(_sqlGenerationService.GenerateCreateFunctionDdl(function));
                sb.AppendLine();
                completedSteps++;
                progress.CompletedSteps = completedSteps;
            }
        }

        // 2. 导出表
        if (options.IncludeTables)
        {
            progress.CurrentStage = "导出表结构";
            progressCallback?.Invoke(progress);

            foreach (var schema in schemas)
            {
                var tables = await _metadataQueryService.GetTablesAsync(schema);

                foreach (var table in tables)
                {
                    progress.CurrentStep = $"表: {table.FullName}";
                    progressCallback?.Invoke(progress);

                    var columns = await _metadataQueryService.GetTableColumnsAsync(table.Schema, table.Name);

                    if (options.IncludeDropStatements)
                    {
                        sb.AppendLine(_sqlGenerationService.GenerateDropTableDdl(table));
                    }
                    sb.AppendLine(_sqlGenerationService.GenerateCreateTableDdl(table, columns));
                    sb.AppendLine();
                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 3. 导出索引
        if (options.IncludeIndexes)
        {
            progress.CurrentStage = "导出索引";
            progressCallback?.Invoke(progress);

            foreach (var schema in schemas)
            {
                var indexes = await _metadataQueryService.GetIndexesAsync(schema);

                foreach (var index in indexes)
                {
                    progress.CurrentStep = $"索引: {index.FullName}";
                    progressCallback?.Invoke(progress);

                    if (options.IncludeDropStatements)
                    {
                        sb.AppendLine(_sqlGenerationService.GenerateDropIndexDdl(index));
                    }
                    sb.AppendLine(_sqlGenerationService.GenerateCreateIndexDdl(index));
                    sb.AppendLine();
                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 4. 导出策略
        if (options.IncludePolicies)
        {
            progress.CurrentStage = "导出策略";
            progressCallback?.Invoke(progress);

            foreach (var schema in schemas)
            {
                var policies = await _metadataQueryService.GetPoliciesAsync(schema);

                foreach (var policy in policies)
                {
                    progress.CurrentStep = $"策略: {policy.FullName}";
                    progressCallback?.Invoke(progress);

                    if (options.IncludeDropStatements)
                    {
                        sb.AppendLine(_sqlGenerationService.GenerateDropPolicyDdl(policy));
                    }
                    sb.AppendLine(_sqlGenerationService.GenerateCreatePolicyDdl(policy));
                    sb.AppendLine();
                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 5. 导出触发器
        if (options.IncludeTriggers)
        {
            progress.CurrentStage = "导出触发器";
            progressCallback?.Invoke(progress);

            foreach (var schema in schemas)
            {
                var triggers = await _metadataQueryService.GetTriggersAsync(schema);

                foreach (var trigger in triggers)
                {
                    progress.CurrentStep = $"触发器: {trigger.FullName}";
                    progressCallback?.Invoke(progress);

                    if (options.IncludeDropStatements)
                    {
                        sb.AppendLine(_sqlGenerationService.GenerateDropTriggerDdl(trigger));
                    }
                    sb.AppendLine(_sqlGenerationService.GenerateCreateTriggerDdl(trigger));
                    sb.AppendLine();
                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 6. 导出视图
        if (options.IncludeViews)
        {
            progress.CurrentStage = "导出视图";
            progressCallback?.Invoke(progress);

            foreach (var schema in schemas)
            {
                var views = await _metadataQueryService.GetViewsAsync(schema);

                foreach (var view in views)
                {
                    progress.CurrentStep = $"视图: {view.FullName}";
                    progressCallback?.Invoke(progress);

                    if (options.IncludeDropStatements)
                    {
                        sb.AppendLine(_sqlGenerationService.GenerateDropViewDdl(view));
                    }
                    sb.AppendLine(_sqlGenerationService.GenerateCreateViewDdl(view));
                    sb.AppendLine();
                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        sb.AppendLine("-- 提交事务");
        sb.AppendLine("COMMIT;");
        sb.AppendLine();
        sb.AppendLine("-- 导出完成");

        progress.IsCompleted = true;
        progress.CompletedSteps = completedSteps;
        progressCallback?.Invoke(progress);

        return sb.ToString();
    }

    /// <summary>
    /// 导出为 JSON 格式
    /// </summary>
    public async Task<string> ExportAsJsonAsync(ExportOptions options, Action<ExportProgress>? progressCallback = null)
    {
        var progress = new ExportProgress
        {
            CurrentStage = "开始导出",
            TotalSteps = 100
        };

        progressCallback?.Invoke(progress);

        var export = new DatabaseExport
        {
            ExportTime = DateTime.UtcNow
        };

        var schemas = options.Schemas.Count > 0 ? options.Schemas.Cast<string?>().ToList() : await GetAllSchemasAsync();
        var schemaDict = new ConcurrentDictionary<string, SchemaExport>();
        int completedSteps = 0;

        // 1. 导出表（包含外键信息）
        if (options.IncludeTables)
        {
            progress.CurrentStage = "导出表结构";
            foreach (var schema in schemas)
            {
                var tables = await _metadataQueryService.GetTablesAsync(schema);
                var schemaExport = schemaDict.GetOrAdd(schema ?? "public", _ => new SchemaExport { Name = schema ?? "public" });

                foreach (var table in tables)
                {
                    progress.CurrentStep = $"表: {table.FullName}";
                    progressCallback?.Invoke(progress);

                    var columns = await _metadataQueryService.GetTableColumnsAsync(table.Schema, table.Name);

                    // 从列信息中提取外键
                    var foreignKeys = columns
                        .Where(c => c.IsForeignKey && !string.IsNullOrWhiteSpace(c.ForeignTable))
                        .Select(c => new ForeignKeyExport
                        {
                            ColumnName = c.Name,
                            ReferencedTable = c.ForeignTable!,
                            ReferencedColumn = c.ForeignColumn ?? "id"
                        }).ToList();

                    schemaExport.Tables.Add(new TableExport
                    {
                        Name = table.Name,
                        Comment = table.Comment,
                        Columns = columns.Select(c => new ColumnExport
                        {
                            Name = c.Name,
                            DataType = c.DataType,
                            IsNullable = c.IsNullable,
                            DefaultValue = c.DefaultValue,
                            Comment = c.Comment,
                            ArrayDimensions = c.ArrayDimensions,
                            MaxLength = c.MaxLength
                        }).ToList(),
                        PrimaryKeys = columns.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToList(),
                        ForeignKeys = foreignKeys
                    });

                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 2. 导出策略
        if (options.IncludePolicies)
        {
            progress.CurrentStage = "导出策略";
            foreach (var schema in schemas)
            {
                var policies = await _metadataQueryService.GetPoliciesAsync(schema);
                var schemaExport = schemaDict.GetOrAdd(schema ?? "public", _ => new SchemaExport { Name = schema ?? "public" });

                foreach (var policy in policies)
                {
                    progress.CurrentStep = $"策略: {policy.FullName}";
                    progressCallback?.Invoke(progress);

                    schemaExport.Policies.Add(new PolicyExport
                    {
                        Name = policy.Name,
                        TableName = policy.TableName,
                        Command = policy.Command,
                        PolicyType = policy.PolicyType,
                        Role = policy.Role,
                        UsingExpression = policy.UsingExpression,
                        WithCheckExpression = policy.WithCheckExpression
                    });

                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 3. 导出触发器
        if (options.IncludeTriggers)
        {
            progress.CurrentStage = "导出触发器";
            foreach (var schema in schemas)
            {
                var triggers = await _metadataQueryService.GetTriggersAsync(schema);
                var schemaExport = schemaDict.GetOrAdd(schema ?? "public", _ => new SchemaExport { Name = schema ?? "public" });

                foreach (var trigger in triggers)
                {
                    progress.CurrentStep = $"触发器: {trigger.FullName}";
                    progressCallback?.Invoke(progress);

                    schemaExport.Triggers.Add(new TriggerExport
                    {
                        Name = trigger.Name,
                        TableName = trigger.TableName,
                        Timing = trigger.Timing,
                        Event = trigger.Event,
                        IsRowLevel = trigger.IsRowLevel,
                        FunctionName = trigger.FunctionName,
                        IsEnabled = trigger.IsEnabled
                    });

                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 4. 导出索引
        if (options.IncludeIndexes)
        {
            progress.CurrentStage = "导出索引";
            foreach (var schema in schemas)
            {
                var indexes = await _metadataQueryService.GetIndexesAsync(schema);
                var schemaExport = schemaDict.GetOrAdd(schema ?? "public", _ => new SchemaExport { Name = schema ?? "public" });

                foreach (var index in indexes)
                {
                    progress.CurrentStep = $"索引: {index.FullName}";
                    progressCallback?.Invoke(progress);

                    schemaExport.Indexes.Add(new IndexExport
                    {
                        Name = index.Name,
                        TableName = index.TableName,
                        IndexType = index.IndexType,
                        IsUnique = index.IsUnique,
                        Columns = index.Columns,
                        Expression = index.Expression,
                        PartialCondition = index.PartialCondition
                    });

                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        // 5. 导出函数
        if (options.IncludeFunctions)
        {
            progress.CurrentStage = "导出函数";
            var functions = await _metadataQueryService.GetFunctionsAsync(null);

            foreach (var function in functions)
            {
                progress.CurrentStep = $"函数: {function.FullName}";
                progressCallback?.Invoke(progress);

                var schemaExport = schemaDict.GetOrAdd(function.Schema, _ => new SchemaExport { Name = function.Schema });

                schemaExport.Functions.Add(new FunctionExport
                {
                    Name = function.Name,
                    ReturnType = function.ReturnType,
                    FunctionType = function.FunctionType,
                    Language = function.Language,
                    IsSecurityDefiner = function.IsSecurityDefiner,
                    Definition = function.Definition,
                    Parameters = function.Parameters.Select(p => new FunctionParameterExport
                    {
                        Name = p.Name,
                        Mode = p.Mode,
                        Type = p.Type
                    }).ToList()
                });

                completedSteps++;
                progress.CompletedSteps = completedSteps;
            }
        }

        // 6. 导出视图
        if (options.IncludeViews)
        {
            progress.CurrentStage = "导出视图";
            foreach (var schema in schemas)
            {
                var views = await _metadataQueryService.GetViewsAsync(schema);
                var schemaExport = schemaDict.GetOrAdd(schema ?? "public", _ => new SchemaExport { Name = schema ?? "public" });

                foreach (var view in views)
                {
                    progress.CurrentStep = $"视图: {view.FullName}";
                    progressCallback?.Invoke(progress);

                    schemaExport.Views.Add(new ViewExport
                    {
                        Name = view.Name,
                        IsMaterialized = view.IsMaterialized,
                        ColumnCount = view.ColumnCount,
                        Definition = view.Definition
                    });

                    completedSteps++;
                    progress.CompletedSteps = completedSteps;
                }
            }
        }

        export.Schemas = schemaDict.Values.OrderBy(s => s.Name).ToList();

        progress.IsCompleted = true;
        progress.TotalSteps = completedSteps;
        progress.CompletedSteps = completedSteps;
        progressCallback?.Invoke(progress);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(export, jsonOptions);
    }

    /// <summary>
    /// 获取所有用户架构
    /// </summary>
    private async Task<List<string?>> GetAllSchemasAsync()
    {
        var tables = await _metadataQueryService.GetTablesAsync();
        return tables.Select(t => (string?)t.Schema).Distinct().ToList();
    }
}
