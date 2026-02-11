namespace SupabaseDBManager.Models;

/// <summary>
/// 导出进度信息
/// </summary>
public class ExportProgress
{
    /// <summary>
    /// 当前阶段
    /// </summary>
    public string CurrentStage { get; set; } = string.Empty;

    /// <summary>
    /// 当前步骤
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// 总步骤数
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// 已完成步骤数
    /// </summary>
    public int CompletedSteps { get; set; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int ProgressPercentage => TotalSteps > 0 ? (int)((double)CompletedSteps / TotalSteps * 100) : 0;

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}
