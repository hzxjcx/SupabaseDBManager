using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SupabaseDBManager.Models;
using SupabaseDBManager.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SupabaseDBManager.Views;

/// <summary>
/// MainWindow 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private SupabaseConnectionService _connectionService = null!;
    private MetadataQueryService _metadataService = null!;
    private SqlGenerationService _sqlGenerationService = null!;
    private SqlExecutionService _sqlExecutionService = null!;
    private DataEditorService _dataEditorService = null!;
    private ConfigService _configService = null!;
    private AppConfigService _appConfigService = null!;

    private string? _currentConnectionString;
    private TextBox? _connectionStringTextBox; // 用于显示连接字符串的 TextBox

    // 缓存数据
    private List<TableInfo>? _allTables;
    private List<PolicyInfo>? _allPolicies;
    private List<TriggerInfo>? _allTriggers;
    private List<IndexInfo>? _allIndexes;
    private List<FunctionInfo>? _allFunctions;
    private List<ViewInfo>? _allViews;

    // Data Editor 当前状态
    private System.Data.DataTable? _currentDataTable;
    private List<string>? _currentPrimaryKeys;

    // 防止并发加载
    private System.Threading.CancellationTokenSource? _loadColumnsCts;
    private bool _isLoadingTreeView = false;  // 防止 TreeView 加载时触发事件
    private readonly System.Threading.SemaphoreSlim _columnQuerySemaphore = new System.Threading.SemaphoreSlim(1, 1);

    public MainWindow()
    {
        InitializeComponent();
        InitializeServices();
        LoadSavedConfig();
    }

    private void InitializeServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SupabaseConnectionService>();
        services.AddSingleton<MetadataQueryService>();
        services.AddSingleton<SqlGenerationService>();
        services.AddSingleton<SqlExecutionService>();
        services.AddSingleton<DataEditorService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<AppConfigService>();

        var provider = services.BuildServiceProvider();

        _connectionService = provider.GetRequiredService<SupabaseConnectionService>();
        _metadataService = provider.GetRequiredService<MetadataQueryService>();
        _sqlGenerationService = provider.GetRequiredService<SqlGenerationService>();
        _sqlExecutionService = provider.GetRequiredService<SqlExecutionService>();
        _dataEditorService = provider.GetRequiredService<DataEditorService>();
        _configService = provider.GetRequiredService<ConfigService>();
        _appConfigService = provider.GetRequiredService<AppConfigService>();
    }

    private void LoadSavedConfig()
    {
        try
        {
            // 优先从 appsettings.json 加载配置
            if (_appConfigService.ConfigFileExists())
            {
                var connectionString = _appConfigService.GetConnectionString();
                var projectUrl = _appConfigService.GetProjectUrl();

                SupabaseUrlTextBox.Text = projectUrl;
                SetConnectionString(connectionString);

                // 配置已加载，但不自动连接
                UpdateConnectionStatus(false);
                return;
            }

            // 如果配置文件不存在，尝试从加密的本地配置加载
            var config = _configService.LoadConfig();
            if (config != null)
            {
                SupabaseUrlTextBox.Text = config.SupabaseUrl ?? string.Empty;
                SetConnectionString(config.ConnectionString ?? string.Empty);
                UpdateConnectionStatus(false);
            }
            else
            {
                // 显示提示信息
                MessageBox.Show(
                    "未找到配置文件。\n\n请按照以下步骤配置：\n" +
                    "1. 复制 appsettings.example.json 为 appsettings.json\n" +
                    "2. 修改 appsettings.json 中的 Supabase 连接信息\n" +
                    "3. 重启程序",
                    "配置提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetConnectionString(string connectionString)
    {
        if (_connectionStringTextBox != null)
        {
            _connectionStringTextBox.Text = connectionString;
        }
        else
        {
            ConnectionStringPasswordBox.Password = connectionString;
        }
    }

    private string GetConnectionString()
    {
        if (_connectionStringTextBox != null)
        {
            return _connectionStringTextBox.Text;
        }
        else
        {
            return ConnectionStringPasswordBox.Password;
        }
    }

    private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var connectionString = GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("请输入 Connection String", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TestConnectionButton.IsEnabled = false;
            TestConnectionButton.Content = "连接中...";

            var (success, message) = await _connectionService.TestConnectionAsync(connectionString);

            if (success)
            {
                _currentConnectionString = connectionString;
                await _connectionService.OpenConnectionAsync(connectionString);
                UpdateConnectionStatus(true);

                // 自动加载表列表
                await LoadAllTablesAsync();

                // 不再显示成功提示框，避免打断用户操作
                // MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                UpdateConnectionStatus(false);
                MessageBox.Show(message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus(false);
            MessageBox.Show($"测试连接失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            TestConnectionButton.IsEnabled = true;
            TestConnectionButton.Content = "🔗 测试连接";
        }
    }

    private void OnSaveConfigClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = new DatabaseConfig
            {
                SupabaseUrl = SupabaseUrlTextBox.Text,
                ConnectionString = GetConnectionString(),
                ConnectionName = "Default Connection"
            };

            _configService.SaveConfig(config);
            MessageBox.Show("配置已保存！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnToggleConnectionStringVisibilityClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var parent = (Grid)button.Parent;

        if (_connectionStringTextBox == null)
        {
            // 切换到 TextBox 显示
            _connectionStringTextBox = new TextBox
            {
                Text = ConnectionStringPasswordBox.Password,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 0)
            };

            parent.Children.RemoveAt(0);
            parent.Children.Insert(0, _connectionStringTextBox);
            button.Content = "🙈";
        }
        else
        {
            // 切换回 PasswordBox
            ConnectionStringPasswordBox.Password = _connectionStringTextBox.Text;
            parent.Children.RemoveAt(0);
            parent.Children.Insert(0, ConnectionStringPasswordBox);
            _connectionStringTextBox = null;
            button.Content = "👁️";
        }
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        if (isConnected)
        {
            ConnectionStatusTextBlock.Text = "✅ 已连接";
            ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            ConnectionStatusTextBlock.Text = "❌ 未连接";
            ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }

    private void EnsureConnection()
    {
        if (!_connectionService.IsConnected)
        {
            MessageBox.Show("请先连接数据库！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            throw new InvalidOperationException("数据库未连接");
        }
    }

    // Tables 标签页
    private void OnTableSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_allTables == null) return;

        var searchText = TableSearchTextBox.Text.ToLowerInvariant();
        var filteredTables = _allTables
            .Where(t => t.Name.ToLowerInvariant().Contains(searchText) ||
                       t.Schema.ToLowerInvariant().Contains(searchText))
            .ToList();

        LoadTablesToTreeView(filteredTables);
    }

    private async void OnTablesTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // 防止在加载 TreeView 时触发事件
        if (_isLoadingTreeView)
        {
            return;
        }

        // 只处理叶子节点（表），忽略父节点（schema）
        if (TablesTreeView.SelectedItem is TreeViewItem item && item.Tag is TableInfo table)
        {
            // 检查是否已经加载过这个表的列信息
            if (ColumnsDataGrid.Items.Count > 0 && TableNameTextBlock.Text == table.FullName)
            {
                // 已经加载过，跳过
                return;
            }

            TableNameTextBlock.Text = table.FullName;
            TableInfoTextBlock.Text = $"行数: {table.RowCount ?? 0} | 大小: {FormatSize(table.Size)}";
            CurrentObjectTextBlock.Text = $"当前表: {table.FullName}";

            // 自动加载列信息
            await LoadTableColumnsAsync(table);
        }
    }

    private async Task LoadTableColumnsAsync(TableInfo table)
    {
        // 取消之前的加载任务
        _loadColumnsCts?.Cancel();
        _loadColumnsCts?.Dispose();

        var cts = new System.Threading.CancellationTokenSource();
        _loadColumnsCts = cts;

        bool lockAcquired = false;

        try
        {
            // 🔑 关键：等待之前的查询完成或取消
            // 注意：不使用 cts.Token，避免 WaitAsync 被取消导致锁未获取
            await _columnQuerySemaphore.WaitAsync();
            lockAcquired = true;

            // 检查是否在等待期间被取消
            if (cts.Token.IsCancellationRequested)
            {
                return;
            }

            EnsureConnection();

            var columns = await _metadataService.GetTableColumnsAsync(
                table.Schema,
                table.Name,
                cts.Token);

            // 只在未被取消时更新 UI
            if (!cts.Token.IsCancellationRequested)
            {
                ColumnsDataGrid.ItemsSource = columns;
                var ddl = _sqlGenerationService.GenerateCreateTableDdl(table, columns);
                TableDdlTextBox.Text = ddl;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 查询被取消，正常情况，不显示错误
            return;
        }
        catch (Exception ex)
        {
            // 如果不是因为取消而失败，显示错误
            if (!cts.Token.IsCancellationRequested)
            {
                MessageBox.Show($"加载列信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            // 🔑 关键：只释放确实获取的锁
            if (lockAcquired)
            {
                _columnQuerySemaphore.Release();
            }
            if (_loadColumnsCts == cts)
            {
                _loadColumnsCts = null;
            }
            cts.Dispose();
        }
    }

    private async void OnShowTableStructureClick(object sender, RoutedEventArgs e)
    {
        if (TablesTreeView.SelectedItem is TreeViewItem item && item.Tag is TableInfo table)
        {
            await LoadTableColumnsAsync(table);
        }
        else
        {
            MessageBox.Show("请先选择一个表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnCopyTableDdlClick(object sender, RoutedEventArgs e)
    {
        if (TablesTreeView.SelectedItem is TreeViewItem item && item.Tag is TableInfo table)
        {
            try
            {
                EnsureConnection();

                // 🔑 使用统一的加载方法，确保并发控制
                await LoadTableColumnsAsync(table);

                // 获取当前显示的 DDL 并复制
                var ddl = TableDdlTextBox.Text;
                if (!string.IsNullOrWhiteSpace(ddl))
                {
                    Clipboard.SetText(ddl);
                    MessageBox.Show("DDL 已复制到剪贴板！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制 DDL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("请先选择一个表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnViewTableDataClick(object sender, RoutedEventArgs e)
    {
        if (TablesTreeView.SelectedItem is TreeViewItem item && item.Tag is TableInfo table)
        {
            // 切换到 Data Editor 标签页
            MainTabControl.SelectedIndex = 7; // Data Editor 标签页

            // 设置选中的表
            foreach (var comboBoxItem in TableSelectorComboBox.Items)
            {
                if (comboBoxItem is TableInfo t && t.FullName == table.FullName)
                {
                    TableSelectorComboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }

            // 自动加载数据
            OnLoadTableDataClick(sender, e);
        }
        else
        {
            MessageBox.Show("请先选择一个表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // Policies 标签页
    private async void OnRefreshPoliciesClick(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConnection();
            _allPolicies = await _metadataService.GetPoliciesAsync();
            PoliciesDataGrid.ItemsSource = _allPolicies;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载策略失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCopyPolicyDdlClick(object sender, RoutedEventArgs e)
    {
        if (PoliciesDataGrid.SelectedItems.Count == 0)
        {
            MessageBox.Show("请先选择至少一个策略", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ddlList = new List<string>();
            foreach (var item in PoliciesDataGrid.SelectedItems)
            {
                if (item is PolicyInfo policy)
                {
                    ddlList.Add(_sqlGenerationService.GenerateCreatePolicyDdl(policy));
                }
            }

            if (ddlList.Count > 0)
            {
                var allDdl = string.Join(Environment.NewLine + Environment.NewLine, ddlList);
                Clipboard.SetText(allDdl);
                MessageBox.Show($"已复制 {ddlList.Count} 个策略的 DDL 到剪贴板！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制 DDL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Triggers 标签页
    private async void OnRefreshTriggersClick(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConnection();
            _allTriggers = await _metadataService.GetTriggersAsync();
            TriggersDataGrid.ItemsSource = _allTriggers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载触发器失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCopyTriggerDdlClick(object sender, RoutedEventArgs e)
    {
        if (TriggersDataGrid.SelectedItems.Count == 0)
        {
            MessageBox.Show("请先选择至少一个触发器", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ddlList = new List<string>();
            foreach (var item in TriggersDataGrid.SelectedItems)
            {
                if (item is TriggerInfo trigger)
                {
                    ddlList.Add(_sqlGenerationService.GenerateCreateTriggerDdl(trigger));
                }
            }

            if (ddlList.Count > 0)
            {
                var allDdl = string.Join(Environment.NewLine + Environment.NewLine, ddlList);
                Clipboard.SetText(allDdl);
                MessageBox.Show($"已复制 {ddlList.Count} 个触发器的 DDL 到剪贴板！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制 DDL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Indexes 标签页
    private async void OnRefreshIndexesClick(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConnection();
            _allIndexes = await _metadataService.GetIndexesAsync();

            // 为每个索引添加列的文本表示
            foreach (var index in _allIndexes)
            {
                if (index.Columns.Count > 0)
                {
                    var props = index.GetType().GetProperty("ColumnsText");
                    if (props == null)
                    {
                        // 动态添加属性
                        // 这里简化处理，实际可以使用 ViewModel 包装
                    }
                }
            }

            IndexesDataGrid.ItemsSource = _allIndexes;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载索引失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCopyIndexDdlClick(object sender, RoutedEventArgs e)
    {
        if (IndexesDataGrid.SelectedItems.Count == 0)
        {
            MessageBox.Show("请先选择至少一个索引", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ddlList = new List<string>();
            foreach (var item in IndexesDataGrid.SelectedItems)
            {
                if (item is IndexInfo index)
                {
                    ddlList.Add(_sqlGenerationService.GenerateCreateIndexDdl(index));
                }
            }

            if (ddlList.Count > 0)
            {
                var allDdl = string.Join(Environment.NewLine + Environment.NewLine, ddlList);
                Clipboard.SetText(allDdl);
                MessageBox.Show($"已复制 {ddlList.Count} 个索引的 DDL 到剪贴板！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制 DDL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Functions 标签页
    private async void OnRefreshFunctionsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConnection();
            _allFunctions = await _metadataService.GetFunctionsAsync();
            FunctionsDataGrid.ItemsSource = _allFunctions;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载函数失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnFunctionsDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FunctionsDataGrid.SelectedItem is FunctionInfo function)
        {
            var ddl = _sqlGenerationService.GenerateCreateFunctionDdl(function);
            FunctionDefinitionTextBox.Text = ddl;
        }
    }

    private void OnCopyFunctionDdlClick(object sender, RoutedEventArgs e)
    {
        if (FunctionsDataGrid.SelectedItems.Count == 0)
        {
            MessageBox.Show("请先选择至少一个函数", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ddlList = new List<string>();
            foreach (var item in FunctionsDataGrid.SelectedItems)
            {
                if (item is FunctionInfo function)
                {
                    ddlList.Add(_sqlGenerationService.GenerateCreateFunctionDdl(function));
                }
            }

            if (ddlList.Count > 0)
            {
                var allDdl = string.Join(Environment.NewLine + Environment.NewLine, ddlList);
                Clipboard.SetText(allDdl);
                MessageBox.Show($"已复制 {ddlList.Count} 个函数的 DDL 到剪贴板！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制 DDL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Views 标签页
    private async void OnRefreshViewsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConnection();
            _allViews = await _metadataService.GetViewsAsync();
            ViewsDataGrid.ItemsSource = _allViews;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载视图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCopyViewDdlClick(object sender, RoutedEventArgs e)
    {
        if (ViewsDataGrid.SelectedItems.Count == 0)
        {
            MessageBox.Show("请先选择至少一个视图", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ddlList = new List<string>();
            foreach (var item in ViewsDataGrid.SelectedItems)
            {
                if (item is ViewInfo view)
                {
                    ddlList.Add(_sqlGenerationService.GenerateCreateViewDdl(view));
                }
            }

            if (ddlList.Count > 0)
            {
                var allDdl = string.Join(Environment.NewLine + Environment.NewLine, ddlList);
                Clipboard.SetText(allDdl);
                MessageBox.Show($"已复制 {ddlList.Count} 个视图的 DDL 到剪贴板！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制 DDL 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // SQL Query 标签页
    private async void OnExecuteQueryClick(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConnection();
            var sql = SqlEditorTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(sql))
            {
                MessageBox.Show("请输入 SQL 查询", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _sqlExecutionService.ExecuteQueryAsync(sql);

            if (result.Success)
            {
                if (result.IsQueryResult)
                {
                    QueryResultsDataGrid.ItemsSource = result.Rows;
                    QueryResultsDataGrid.AutoGenerateColumns = true;
                    MessageBox.Show($"查询成功！返回 {result.Rows.Count} 行，耗时 {result.ExecutionTimeMs} ms",
                                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    QueryResultsDataGrid.ItemsSource = null;
                    MessageBox.Show($"执行成功！影响 {result.RowsAffected} 行，耗时 {result.ExecutionTimeMs} ms",
                                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show($"查询失败: {result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"执行查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCopyQueryResultsClick(object sender, RoutedEventArgs e)
    {
        // 简化的结果复制
        MessageBox.Show("结果复制功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnClearQueryClick(object sender, RoutedEventArgs e)
    {
        SqlEditorTextBox.Clear();
        QueryResultsDataGrid.ItemsSource = null;
    }

    // Data Editor 标签页
    private void OnTableSelectorSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 表选择改变时的处理
    }

    private async void OnLoadTableDataClick(object sender, RoutedEventArgs e)
    {
        await LoadTableDataAsync();
    }

    private async Task LoadTableDataAsync()
    {
        try
        {
            EnsureConnection();

            if (TableSelectorComboBox.SelectedItem is TableInfo table)
            {
                _currentDataTable = await _dataEditorService.GetTableDataAsync(table.Schema, table.Name);
                _currentPrimaryKeys = await _dataEditorService.GetPrimaryKeyColumnsAsync(table.Schema, table.Name);

                TableDataDataGrid.ItemsSource = _currentDataTable.DefaultView;
                TableDataDataGrid.AutoGenerateColumns = true;

                CurrentObjectTextBlock.Text = $"当前表: {table.FullName} | 行数: {_currentDataTable.Rows.Count}";
            }
            else
            {
                MessageBox.Show("请先选择一个表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnAddRowClick(object sender, RoutedEventArgs e)
    {
        if (_currentDataTable == null)
        {
            MessageBox.Show("请先加载数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // 添加一个新行
            var newRow = _currentDataTable.NewRow();
            _currentDataTable.Rows.Add(newRow);

            // 滚动到新行
            TableDataDataGrid.ScrollIntoView(newRow);

            CurrentObjectTextBlock.Text = CurrentObjectTextBlock.Text?.Replace($"行数: {_currentDataTable.Rows.Count - 1}",
                $"行数: {_currentDataTable.Rows.Count}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"添加行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSaveRowChangesClick(object sender, RoutedEventArgs e)
    {
        if (_currentDataTable == null || TableSelectorComboBox.SelectedItem is not TableInfo table)
        {
            MessageBox.Show("请先加载数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentPrimaryKeys == null || _currentPrimaryKeys.Count == 0)
        {
            MessageBox.Show("无法获取表的主键信息，无法保存更改", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            EnsureConnection();

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            // 处理新增行
            foreach (System.Data.DataRow row in _currentDataTable.Rows)
            {
                if (row.RowState == System.Data.DataRowState.Added)
                {
                    var values = new Dictionary<string, object?>();
                    foreach (System.Data.DataColumn col in _currentDataTable.Columns)
                    {
                        // 如果是 DBNull，跳过（使用数据库默认值）
                        if (row.IsNull(col))
                        {
                            continue;
                        }

                        var value = row[col];

                        // 将空字符串转换为 DBNull.Value（设置为数据库 NULL）
                        if (value is string str && string.IsNullOrWhiteSpace(str))
                        {
                            values[col.ColumnName] = DBNull.Value;
                        }
                        else
                        {
                            values[col.ColumnName] = value;
                        }
                    }

                    var success = await _dataEditorService.InsertRowAsync(table.Schema, table.Name, values);
                    if (success)
                    {
                        successCount++;
                        // 接受更改，避免重复提交
                        row.AcceptChanges();
                    }
                    else
                    {
                        failCount++;
                        errors.Add($"插入行失败: {string.Join(", ", values.Keys)}");
                    }
                }
            }

            // 处理修改行
            foreach (System.Data.DataRow row in _currentDataTable.Rows)
            {
                if (row.RowState == System.Data.DataRowState.Modified)
                {
                    // 构建更新值
                    var values = new Dictionary<string, object?>();
                    foreach (System.Data.DataColumn col in _currentDataTable.Columns)
                    {
                        // 只包含被修改的列
                        if (!row.IsNull(col, System.Data.DataRowVersion.Current) ||
                            row[col, System.Data.DataRowVersion.Current] != row[col, System.Data.DataRowVersion.Original])
                        {
                            var currentValue = row[col, System.Data.DataRowVersion.Current];

                            // 将空字符串转换为 DBNull.Value（设置为数据库 NULL）
                            if (currentValue is string str && string.IsNullOrWhiteSpace(str))
                            {
                                values[col.ColumnName] = DBNull.Value;
                            }
                            else
                            {
                                values[col.ColumnName] = currentValue;
                            }
                        }
                    }

                    // 如果没有实际修改，跳过
                    if (values.Count == 0)
                    {
                        continue;
                    }

                    // 构建主键 WHERE 条件（使用原始值）
                    var whereClause = new Dictionary<string, object?>();
                    foreach (var pk in _currentPrimaryKeys)
                    {
                        whereClause[pk] = row[pk, System.Data.DataRowVersion.Original];
                    }

                    var success = await _dataEditorService.UpdateRowAsync(table.Schema, table.Name, values, whereClause);
                    if (success)
                    {
                        successCount++;
                        row.AcceptChanges();
                    }
                    else
                    {
                        failCount++;
                        errors.Add($"更新行失败: 主键 {string.Join(", ", whereClause.Keys)}");
                    }
                }
            }

            // 显示结果
            var message = $"保存完成！\n成功: {successCount} 行\n失败: {failCount} 行";
            if (errors.Count > 0)
            {
                message += "\n\n错误详情:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                {
                    message += $"\n... 还有 {errors.Count - 5} 个错误";
                }
            }

            MessageBox.Show(message, failCount > 0 ? "部分失败" : "成功",
                MessageBoxButton.OK, failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            // 如果有成功的操作，刷新数据显示
            if (successCount > 0)
            {
                await LoadTableDataAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存更改失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnDeleteRowClick(object sender, RoutedEventArgs e)
    {
        if (_currentDataTable == null || TableSelectorComboBox.SelectedItem is not TableInfo table)
        {
            MessageBox.Show("请先加载数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentPrimaryKeys == null || _currentPrimaryKeys.Count == 0)
        {
            MessageBox.Show("无法获取表的主键信息，无法删除行", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedItems = TableDataDataGrid.SelectedItems;
        if (selectedItems.Count == 0)
        {
            MessageBox.Show("请先选择要删除的行", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"确定要删除选中的 {selectedItems.Count} 行吗？\n\n此操作不可撤销！",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            EnsureConnection();

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            // 转换为列表以避免在遍历时修改集合
            var rowsToDelete = new List<System.Data.DataRowView>();
            foreach (var item in selectedItems)
            {
                if (item is System.Data.DataRowView dataRowView)
                {
                    rowsToDelete.Add(dataRowView);
                }
            }

            foreach (var dataRowView in rowsToDelete)
            {
                var row = dataRowView.Row;

                // 构建主键 WHERE 条件
                var whereClause = new Dictionary<string, object?>();
                foreach (var pk in _currentPrimaryKeys)
                {
                    whereClause[pk] = row[pk];
                }

                var success = await _dataEditorService.DeleteRowAsync(table.Schema, table.Name, whereClause);
                if (success)
                {
                    successCount++;
                    // 从 DataTable 中移除该行
                    row.Delete();
                    row.AcceptChanges();
                }
                else
                {
                    failCount++;
                    errors.Add($"删除行失败: 主键 {string.Join(", ", whereClause.Values)}");
                }
            }

            // 显示结果
            var message = $"删除完成！\n成功: {successCount} 行\n失败: {failCount} 行";
            if (errors.Count > 0)
            {
                message += "\n\n错误详情:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                {
                    message += $"\n... 还有 {errors.Count - 5} 个错误";
                }
            }

            MessageBox.Show(message, failCount > 0 ? "部分失败" : "成功",
                MessageBoxButton.OK, failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            // 刷新数据显示
            await LoadTableDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnTableDataDataGridLoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1).ToString();
    }

    /// <summary>
    /// 自动生成列时的处理 - 设置列样式以支持 null 值
    /// </summary>
    private void OnTableDataDataGridAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is DataGridTextColumn textColumn)
        {
            // 为文本列设置样式，允许空字符串转换为 null
            textColumn.ElementStyle = new Style(typeof(TextBlock));
            textColumn.ElementStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new DynamicResourceExtension(SystemColors.WindowTextBrush)));

            textColumn.EditingElementStyle = new Style(typeof(TextBox));
            textColumn.EditingElementStyle.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        }
    }

    // 辅助方法
    private async Task LoadAllTablesAsync()
    {
        try
        {
            EnsureConnection();
            _allTables = await _metadataService.GetTablesAsync();
            LoadTablesToTreeView(_allTables);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载表列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTablesToTreeView(List<TableInfo> tables)
    {
        // 设置标志位，防止加载时触发选择事件
        _isLoadingTreeView = true;

        try
        {
            TablesTreeView.Items.Clear();

            var groupedTables = tables.GroupBy(t => t.Schema);
            foreach (var group in groupedTables)
            {
                var schemaItem = new TreeViewItem
                {
                    Header = $"📂 {group.Key}",
                    IsExpanded = true
                };

                foreach (var table in group)
                {
                    var tableItem = new TreeViewItem
                    {
                        Header = $"📊 {table.Name}",
                        Tag = table
                    };
                    schemaItem.Items.Add(tableItem);
                }

                TablesTreeView.Items.Add(schemaItem);
            }

            // 同时填充表选择器
            TableSelectorComboBox.ItemsSource = tables;
        }
        finally
        {
            // 直接重置，不使用 Dispatcher 延迟
            _isLoadingTreeView = false;
        }
    }

    private string FormatSize(long? bytes)
    {
        if (!bytes.HasValue || bytes.Value == 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes.Value;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // 仅在已连接时才加载表列表
        if (_connectionService.IsConnected)
        {
            await LoadAllTablesAsync();
        }
        // 移除未连接时的自动加载尝试，避免错误提示
    }
}
