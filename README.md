# Supabase 数据库管理工具

<div align="center">

**一个开源的、企业级的 Supabase PostgreSQL 数据库管理工具**

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

功能特性 · 快速上手 · 配置简单 · 开源免费

</div>

---

## 📖 项目简介

这是一个专为 Supabase 设计的数据库管理工具，提供直观的界面来浏览和管理 Supabase PostgreSQL 数据库的所有对象，并导出 DDL 语句供 AI 分析。

### ✨ 核心特性

- 🔍 **完整的元数据浏览** - 查看所有表、策略、触发器、索引、函数、视图
- 📋 **一键复制 DDL** - 快速获取 CREATE 语句，方便 AI 分析
- 🚀 **SQL 查询器** - 直接执行自定义 SQL 查询
- 📊 **数据编辑器** - 可视化浏览和编辑表数据
- 🔐 **安全配置存储** - 使用 DPAPI 加密敏感信息
- 🌍 **支持 Pooler 模式** - 完美支持 Supabase Pooler（事务模式）

### 🎯 适用场景

- 🤖 **AI 辅助开发** - 导出 DDL 让 AI 理解数据库结构
- 🔧 **数据库管理** - 快速查看和管理数据库对象
- 📝 **数据分析** - 执行查询分析数据
- 🎓 **学习研究** - 了解 Supabase 数据库结构

---

## 🚀 快速开始

### 前置要求

- .NET 8.0 Runtime 或 SDK
- Windows 操作系统
- Supabase 项目账号

### 1️⃣ 克隆或下载项目

```bash
git clone https://github.com/hzxjcx/SupabaseDBManager.git
cd AS.Tools.SupabaseDBManager
```

### 2️⃣ 配置数据库连接

复制示例配置文件并修改：

```bash
# Windows PowerShell
Copy-Item appsettings.example.json appsettings.json

# 或手动创建 appsettings.json
```

编辑 `appsettings.json`，填入你的 Supabase 连接信息：

```json
{
  "SupabaseSettings": {
    "ProjectUrl": "https://your-project.supabase.co",
    "PoolerSettings": {
      "Host": "aws-0-ap-southeast-1.pooler.supabase.com",
      "Port": 5432,
      "Database": "postgres",
      "Username": "postgres.your-project-id",
      "Password": "your-database-password",
      "PoolMode": "Session"
    },
    "MaxPoolSize": 10,
    "ConnectionTimeout": 30
  }
}
```

📖 **详细配置说明**：请查看 [配置指南](SETUP_GUIDE.md)

### 3️⃣ 编译并运行

```bash
# 编译项目
dotnet build SupabaseDBManager.csproj -c Release

# 运行程序
.\bin\Release\net8.0-windows\SupabaseDBManager.exe
```

或直接从 Visual Studio 运行。

---

## 📚 功能模块

### Tables（数据表）

- 📁 按架构分组的表树
- 📋 查看表结构和列信息
- 📄 一键复制 CREATE TABLE DDL
- 🔍 搜索和筛选表

### Policies（RLS 策略）

- 📋 查看所有 Row Level Security 策略
- 📄 复制 CREATE POLICY DDL
- 🔍 按表和命令筛选

### Triggers（触发器）

- 📋 查看所有触发器
- 📄 复制 CREATE TRIGGER DDL
- 🔍 查看触发时机和事件

### Indexes（索引）

- 📋 查看所有索引
- 📄 复制 CREATE INDEX DDL
- 🔍 查看索引类型和列

### Functions（函数）

- 📋 查看所有函数
- 📄 复制 CREATE FUNCTION DDL
- 🔍 查看函数定义和参数

### Views（视图）

- 📋 查看所有视图
- 📄 复制 CREATE VIEW DDL
- 🔍 查看视图定义

### SQL Query（查询器）

- ✍️ SQL 编辑器
- ▶️ 执行查询
- 📊 结果网格显示
- 📋 导出查询结果

### Data Editor（数据编辑器）

- 📊 浏览表数据（支持分页）
- ✏️ 编辑数据（支持 UPDATE）
- ➕ 添加行（支持 INSERT）
- 🗑️ 删除行（支持 DELETE）
- 🔒 自动区分 INSERT/UPDATE/DELETE 操作
- 💫 支持将字段设置为 NULL（删除内容后保存）

---

## 📂 项目结构

```
tools/SupabaseDBManager/
├── SupabaseDBManager.csproj    # 项目文件
├── appsettings.json                     # 配置文件（用户填写）
├── appsettings.example.json             # 配置示例（模板）
├── App.xaml                              # 应用程序入口
├── App.xaml.cs
├── Views/
│   └── MainWindow.xaml + .xaml.cs        # 主窗口
├── Models/                               # 数据模型
│   ├── TableInfo.cs
│   ├── ColumnInfo.cs
│   ├── PolicyInfo.cs
│   ├── TriggerInfo.cs
│   ├── IndexInfo.cs
│   ├── FunctionInfo.cs
│   ├── ViewInfo.cs
│   ├── QueryResult.cs
│   ├── DataRow.cs
│   ├── DatabaseConfig.cs
│   └── AppSettings.cs
├── Services/                             # 服务层
│   ├── SupabaseConnectionService.cs      # 连接服务
│   ├── MetadataQueryService.cs           # 元数据查询
│   ├── SqlGenerationService.cs           # DDL 生成
│   ├── SqlExecutionService.cs            # SQL 执行
│   ├── DataEditorService.cs              # 数据编辑
│   ├── ConfigService.cs                  # 配置服务（DPAPI）
│   └── AppConfigService.cs               # 配置文件读取
├── README.md                             # 本文件
├── SETUP_GUIDE.md                        # 配置指南
└── IMPLEMENTATION_SUMMARY.md             # 实施总结
```

---

## 🛠️ 技术栈

- **.NET 8.0** - 主框架
- **WPF** - UI 框架
- **Npgsql 8.0.3** - PostgreSQL 驱动（支持 Pooler）
- **Microsoft.Extensions.Configuration** - 配置管理
- **System.Text.Json** - JSON 序列化

### 关键技术点

- ✅ **Pooler 支持** - 自动禁用 PREPARE 语句
- ✅ **依赖注入** - 使用 Microsoft.Extensions.DependencyInjection
- ✅ **异步操作** - 所有数据库操作使用 async/await
- ✅ **安全存储** - Windows DPAPI 加密敏感信息
- ✅ **模块化设计** - 清晰的分层架构

---

## 🔧 配置说明

### 连接模式

#### Pooler - Session mode（推荐用于数据库管理工具）

- ✅ 官方推荐的数据库连接方式
- ✅ 支持长连接，适合数据库管理工具
- ✅ 连接池管理高效
- ✅ 支持所有 PostgreSQL 特性（包括 PREPARE 语句）
- ⚠️ 注意：端口是 5432，不是 6543

**连接字符串格式**：
```
Host=aws-0-ap-southeast-1.pooler.supabase.com;
Port=5432;
Username=postgres.project-id;
Password=your-password;
Database=postgres
```

#### Pooler - Transaction mode（用于 serverless 函数）

- ✅ 适合 serverless 函数
- ✅ 事务完成后自动释放连接
- ⚠️ 不支持 PREPARE 语句（工具会自动禁用）
- ⚠️ 端口是 6543

**连接字符串格式**：
```
Host=aws-0-ap-southeast-1.pooler.supabase.com;
Port=6543;
Username=postgres.project-id;
Password=your-password;
Database=postgres
```

### 获取 Supabase 连接信息

1. 登录 [Supabase Dashboard](https://supabase.com/dashboard)
2. 选择你的项目
3. 导航到 **Settings** → **Database**
4. 找到 **Connection string** → **Session mode**（注意：不是 Transaction mode！）
5. 选择 **URI** 格式，复制连接字符串

**重要**：数据库管理工具应使用 **Session mode**（端口 5432），而不是 Transaction mode（端口 6543）

📖 **详细配置步骤**：请查看 [配置指南](SETUP_GUIDE.md)

---

## 🔒 安全特性

- ✅ 连接字符串使用 **Windows DPAPI** 加密存储
- ✅ 配置文件仅当前用户可访问
- ✅ 支持环境变量配置
- ✅ 不记录敏感日志

### 安全建议

⚠️ **重要**：
1. 不要将包含真实密码的 `appsettings.json` 提交到版本控制
2. 仅提交 `appsettings.example.json` 作为模板
3. 定期更换数据库密码
4. 使用强密码

---

## 📖 使用示例

### 1. 连接数据库

启动程序后，配置会从 `appsettings.json` 自动加载。

点击 **🔗 测试连接** 按钮验证连接。

### 2. 浏览表结构

1. 在 **Tables** 标签页选择一个表
2. 右侧自动显示列信息
3. 点击 **📋 复制 DDL** 复制 CREATE TABLE 语句

### 3. 执行 SQL 查询

1. 切换到 **SQL Query** 标签页
2. 在编辑器中输入 SQL：
   ```sql
   SELECT * FROM your_table LIMIT 100;
   ```
3. 点击 **▶️ 执行查询**
4. 结果显示在下方网格中

### 4. 查看表数据

1. 在 **Tables** 标签页选择表
2. 点击 **📄 查看数据**
3. 自动跳转到 **Data Editor** 标签页

---

## ❓ 常见问题

### Q: 提示"连接失败"？

**A**: 检查以下项目：
1. `appsettings.json` 配置是否正确
2. 数据库密码是否正确
3. Supabase 项目是否暂停
4. 网络连接是否正常

### Q: 不支持 PREPARE 语句错误？

**A**:
- 如果使用 Session mode（端口 5432），不会有此问题
- 如果使用 Transaction mode（端口 6543），工具会自动禁用 PREPARE 语句
- 推荐：数据库管理工具使用 Session mode

### Q: 如何批量导出 DDL？

**A**: 当前版本支持单个对象的 DDL 复制。批量导出功能计划在 v1.1 中实现。

### Q: 如何将字段设置为 NULL？

**A**:
1. 在 Data Editor 中选中要修改的单元格
2. **删除所有内容**（使单元格变为空）
3. 点击"保存更改"
4. 系统会自动将空字符串转换为数据库 NULL

### Q: 为什么有时候更新会失败？

**A**: 常见原因：
- 主键字段被修改（不允许修改主键）
- 违反数据库约束（如唯一约束、外键约束）
- 数据类型不匹配
- 字段长度超出限制
- 查看错误消息了解具体原因

### Q: 支持哪些 Supabase 功能？

**A**:
- ✅ PostgreSQL 所有系统表查询
- ✅ RLS 策略管理
- ✅ 触发器和函数
- ✅ 自定义 SQL 查询
- ❌ Storage 管理（计划中）
- ❌ Edge Functions（计划中）

---

## 🤝 贡献指南

欢迎贡献代码、报告问题或提出建议！

### 如何贡献

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 开发环境

- Visual Studio 2022 或 VS Code
- .NET 8.0 SDK
- Git

---

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

---

## 🙏 致谢

- [Supabase](https://supabase.com/) - 提供优秀的开源 Firebase 替代方案
- [Npgsql](https://www.npgsql.org/) - 优秀的 .NET PostgreSQL 数据驱动
- [Microsoft](https://www.microsoft.com/net) - .NET 平台

---

## 📞 联系方式

- 🐛 问题反馈: [GitHub Issues](https://github.com/hzxjcx/SupabaseDBManager/issues)
- 💬 功能讨论: [GitHub Discussions](https://github.com/hzxjcx/SupabaseDBManager/discussions)
- 📖 文档: [项目 Wiki](https://github.com/hzxjcx/SupabaseDBManager/wiki)

## ⭐ Star History

如果这个项目对你有帮助，请给个 Star 支持一下！

[![Star History Chart](https://api.star-history.com/svg?repos=hzxjcx/SupabaseDBManager&type=Date)](https://star-history.com/#hzxjcx/SupabaseDBManager&Date)

---

## 🗺️ 路线图

- [ ] v1.1 - 批量操作和查询历史
- [ ] v1.2 - SQL 编辑器增强和结果导出
- [ ] v2.0 - 跨平台支持和插件系统

详细规划请查看 [CHANGELOG.md](CHANGELOG.md)

## 📊 开发状态

| 模块 | 状态 | 说明 |
|------|------|------|
| 元数据查询 | ✅ 完成 | 支持所有主要数据库对象 |
| DDL 生成 | ✅ 完成 | 自动生成 CREATE 语句 |
| SQL 查询器 | ✅ 完成 | 支持自定义查询 |
| 数据编辑器 | ✅ 完成 | 支持增删改查和 NULL 值 |
| 配置管理 | ✅ 完成 | 加密存储配置 |
| Pooler 支持 | ✅ 完成 | 自动禁用 PREPARE 语句 |
| 批量导出 | 🚧 计划中 | v1.1 |
| 查询历史 | 🚧 计划中 | v1.1 |
| 结果导出 | 🚧 计划中 | v1.2 |
| 跨平台 | 🚧 计划中 | v2.0 |

---

<div align="center">

**如果这个项目对你有帮助，请给个 ⭐️ Star！**

Made with ❤️ by hzxjcx

</div>
