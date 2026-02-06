# 贡献指南

感谢你有兴趣为 SupabaseDBManager 项目做贡献！我们欢迎任何形式的贡献。

## 🤝 如何贡献

### 报告问题

如果你发现了 bug 或有功能建议：

1. 搜索 [已有 Issues](https://github.com/hzxjcx/SupabaseDBManager/issues) 确保问题未被报告
2. 如果没有找到相关问题，[创建新的 Issue](https://github.com/hzxjcx/SupabaseDBManager/issues/new)
3. 填写 Issue 模板，提供详细的信息

### 提交代码

#### 开发环境设置

1. **Fork 项目**
   ```bash
   # 在 GitHub 上 Fork 项目
   git clone https://github.com/hzxjcx/SupabaseDBManager.git
   cd SupabaseDBManager
   ```

2. **配置开发环境**
   - 安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - 推荐使用 Visual Studio 2022 或 Visual Studio Code

3. **构建项目**
   ```bash
   dotnet build -c Debug
   ```

#### 创建分支

```bash
git checkout -b feature/your-feature-name
# 或
git checkout -b fix/your-bug-fix
```

#### 编码规范

- 遵循 C# 编码规范
- 使用有意义的变量和函数名
- 添加必要的 XML 注释
- 保持代码简洁和可读性

#### 提交代码

1. 确保代码通过编译
   ```bash
   dotnet build -c Release
   ```

2. 编写清晰的提交信息
   ```bash
   git commit -m "feat: 添加批量导出 DDL 功能"
   ```

   **提交类型**：
   - `feat:` 新功能
   - `fix:` Bug 修复
   - `docs:` 文档更新
   - `style:` 代码格式化
   - `refactor:` 代码重构
   - `test:` 测试相关
   - `chore:` 构建/工具相关

3. 推送到你的 Fork
   ```bash
   git push origin feature/your-feature-name
   ```

#### 创建 Pull Request

1. 访问 [原始仓库](https://github.com/hzxjcx/SupabaseDBManager)
2. 点击 "New Pull Request"
3. 填写 PR 模板，详细描述你的更改
4. 等待代码审查

### 开发指南

#### 项目结构

```
tools/SupabaseDBManager/
├── Models/          # 数据模型
├── Services/        # 业务服务
├── Views/           # WPF 视图
└── Services/        # 后台服务
```

#### 添加新功能

1. **数据模型**：在 `Models/` 中定义数据结构
2. **服务层**：在 `Services/` 中实现业务逻辑
3. **视图层**：在 `Views/MainWindow.xaml` 中添加 UI
4. **依赖注入**：在 `App.xaml.cs` 中注册服务

#### 示例：添加新的数据库对象类型

```csharp
// 1. Models/YourObjectInfo.cs
public class YourObjectInfo
{
    public string Name { get; set; }
    public string Schema { get; set; }
    // ... 其他属性
}

// 2. Services/MetadataQueryService.cs
public async Task<List<YourObjectInfo>> GetYourObjectsAsync()
{
    var query = @"SELECT * FROM information_schema.your_objects";
    // ... 实现查询逻辑
}

// 3. 注册到依赖注入
// App.xaml.cs
services.AddSingleton<YourObjectService>();
```

#### 代码审查要点

- [ ] 代码符合项目编码规范
- [ ] 添加了必要的注释
- [ ] 错误处理完善
- [ ] 没有引入编译警告
- [ ] 更新了相关文档
- [ ] 测试了所有修改的功能

## 📋 Issue 模板

### Bug 报告

```markdown
**问题描述**
简要描述遇到的问题

**复现步骤**
1. 步骤一
2. 步骤二
3. ...

**期望行为**
描述你期望的正确行为

**实际行为**
描述实际发生的情况

**环境信息**
- OS 版本：
- .NET 版本：
- 应用版本：

**日志**
如果有的话，附上相关日志
```

### 功能建议

```markdown
**功能描述**
清晰描述你建议的新功能

**使用场景**
描述这个功能的使用场景和价值

**可能的实现方案**
如果有想法，可以描述可能的实现方式

**替代方案**
描述你考虑过的替代方案
```

## 🎯 开发路线图

我们欢迎社区贡献，以下是特别需要帮助的领域：

### 高优先级
- [ ] 添加单元测试和集成测试
- [ ] 改进错误处理和用户提示
- [ ] 性能优化（大数据量查询）
- [ ] 国际化支持

### 中优先级
- [ ] SQL 语法高亮
- [ ] 查询结果导出
- [ ] 批量操作
- [ ] 暗色主题

### 低优先级
- [ ] 跨平台支持
- [ ] 插件系统
- [ ] 可视化查询构建器

## 📜 行为准则

- 尊重所有贡献者
- 建设性地讨论问题
- 关注解决问题而不是个人
- 接受反馈并持续改进

## 📞 联系方式

- 💬 [GitHub Discussions](https://github.com/hzxjcx/SupabaseDBManager/discussions)
- 📧 Email: hzxjcx@gmail.com

---

再次感谢你的贡献！🎉
