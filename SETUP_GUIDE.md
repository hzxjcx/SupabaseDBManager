# Supabase 数据库管理工具 - 配置指南

## 开源项目配置说明

本项目为开源工具，用户需要根据自己的 Supabase 项目配置相关参数。

## 快速开始

### 1. 获取 Supabase 连接信息

登录 [Supabase Dashboard](https://supabase.com/dashboard) 并获取以下信息：

#### 方式一：使用 Pooler - Session mode（推荐用于数据库管理工具）

Pooler 模式提供更好的连接管理，特别适合数据库管理工具等长连接应用。

1. 进入你的项目
2. 导航到 **Settings** → **Database**
3. 找到 **Connection string** → **Session mode**（注意：不是 Transaction mode！）
4. 选择 **URI** 格式，你会看到类似以下信息：
   ```
   postgres://postgres.[PROJECT_REF]:[PASSWORD]@aws-0-[REGION].pooler.supabase.com:5432/postgres
   ```

从上面的连接字符串中提取以下信息：
- **Host**: `aws-0-[REGION].pooler.supabase.com`（例如：`aws-0-ap-southeast-1.pooler.supabase.com`）
- **Port**: `5432`（Session mode 端口）
- **Username**: `postgres.[PROJECT_REF]`（例如：`postgres.your-project-id`）
- **Password**: 你的数据库密码
- **Database**: `postgres`
- **Project URL**: 从 Dashboard 首页获取（例如：`https://your-project-id.supabase.co`）
- **PoolMode**: `Session`（在配置中指定）

### 2. 配置 appsettings.json

复制示例配置文件并修改：

```bash
# Windows PowerShell
Copy-Item appsettings.example.json appsettings.json

# Linux/macOS
cp appsettings.example.json appsettings.json
```

然后编辑 `appsettings.json`，填入你的信息：

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
  },
  "ApplicationSettings": {
    "Theme": "Light",
    "Language": "zh-CN",
    "AutoLoadTables": true,
    "DefaultQueryLimit": 100
  }
}
```

### 3. 配置参数说明

#### SupabaseSettings

| 参数 | 说明 | 示例 |
|------|------|------|
| `ProjectUrl` | Supabase 项目 URL | `https://your-project-id.supabase.co` |
| `MaxPoolSize` | 连接池最大大小 | `10` |
| `ConnectionTimeout` | 连接超时时间（秒） | `30` |

#### PoolerSettings

| 参数 | 说明 | 示例 |
|------|------|------|
| `Host` | Pooler 主机地址 | `aws-0-ap-southeast-1.pooler.supabase.com` |
| `Port` | Pooler 端口（事务模式） | `6543` |
| `Database` | 数据库名称 | `postgres` |
| `Username` | 数据库用户名 | `postgres.your-project-id` |
| `Password` | 数据库密码 | `your-password` |

**重要提示**：
- **Transaction mode**（端口 6543）：不适合数据库管理工具，不支持 PREPARE 语句
- **Session mode**（端口 5432）：✅ 推荐，支持所有 PostgreSQL 特性，适合数据库管理工具

#### DirectSettings（可选，用于长时间操作）

| 参数 | 说明 | 示例 |
|------|------|------|
| `Host` | 数据库主机地址 | `db.your-project-id.supabase.co` |
| `Port` | 数据库端口 | `5432` |
| `Database` | 数据库名称 | `postgres` |
| `Username` | 数据库用户名 | `postgres` |
| `Password` | 数据库密码 | `your-password` |

#### ApplicationSettings

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `Theme` | 界面主题 | `Light` |
| `Language` | 界面语言 | `zh-CN` |
| `AutoLoadTables` | 启动时自动加载表列表 | `true` |
| `DefaultQueryLimit` | 默认查询返回行数 | `100` |

### 4. 安全建议

#### 保护配置文件

⚠️ **重要**：`appsettings.json` 包含敏感信息（数据库密码），请妥善保管！

**推荐做法**：

1. **不要将包含真实密码的 appsettings.json 提交到版本控制**
   ```bash
   # 添加到 .gitignore
   appsettings.json
   ```

2. **仅提交 appsettings.example.json 作为模板**

3. **本地开发时使用测试数据库**

4. **定期更换数据库密码**

#### 文件权限设置

```bash
# Linux/macOS - 设置仅当前用户可读写
chmod 600 appsettings.json

# Windows - 通过文件属性设置安全权限
```

### 5. 连接模式选择

#### Pooler - Session mode（推荐用于数据库管理工具）

**优点**：
- ✅ 支持所有 PostgreSQL 特性（包括 PREPARE 语句）
- ✅ 适合长连接应用（如数据库管理工具）
- ✅ 官方推荐的生产环境配置
- ✅ 连接池管理高效

**缺点**：
- ❌ 不适合事务型短连接（如 serverless 函数）

**适用场景**：
- 数据库管理工具（✅ 当前工具）
- 需要长时间连接的应用
- 复杂的数据库操作

#### Pooler - Transaction mode（用于 serverless 函数）

**优点**：
- ✅ 适合短连接事务操作
- ✅ 自动负载均衡

**缺点**：
- ❌ 不支持 PREPARE 语句
- ❌ 不适合数据库管理工具

**适用场景**：
- Serverless 函数
- 事务型短连接应用
- 无状态 API

### 6. 常见问题

#### Q1: 提示"连接失败"

**解决方法**：
1. 检查 `appsettings.json` 中的连接信息是否正确
2. 确认数据库密码是否正确
3. 验证 Supabase 项目是否暂停
4. 检查网络连接

#### Q2: 提示"PREPARE 语句不支持"

**解决方法**：
1. 推荐使用 **Session mode**（端口 5432），不会有此问题
2. 如果使用 **Transaction mode**（端口 6543），工具会自动禁用 PREPARE 语句
3. 如果仍有问题，检查 `PoolerSettings.PoolMode` 配置是否正确

#### Q3: 连接超时

**解决方法**：
1. 增加 `ConnectionTimeout` 值（默认 30 秒）
2. 检查网络延迟

#### Q4: 如何验证连接是否正常？

运行程序，点击"测试连接"按钮，如果成功会显示：
```
连接成功！PostgreSQL 版本: PostgreSQL 15.x.x
```

### 7. 示例配置

#### 开发环境配置

```json
{
  "SupabaseSettings": {
    "ProjectUrl": "https://dev-project.supabase.co",
    "PoolerSettings": {
      "Host": "aws-0-ap-southeast-1.pooler.supabase.com",
      "Port": 5432,
      "Database": "postgres",
      "Username": "postgres.devproject",
      "Password": "dev-password-123",
      "PoolMode": "Session"
    },
    "MaxPoolSize": 5,
    "ConnectionTimeout": 15
  },
  "ApplicationSettings": {
    "AutoLoadTables": false,
    "DefaultQueryLimit": 50
  }
}
```

#### 生产环境配置

```json
{
  "SupabaseSettings": {
    "ProjectUrl": "https://prod-project.supabase.co",
    "PoolerSettings": {
      "Host": "aws-0-ap-southeast-1.pooler.supabase.com",
      "Port": 5432,
      "Database": "postgres",
      "Username": "postgres.prodproject",
      "Password": "strong-production-password",
      "PoolMode": "Session"
    },
    "MaxPoolSize": 20,
    "ConnectionTimeout": 30
  },
  "ApplicationSettings": {
    "AutoLoadTables": true,
    "DefaultQueryLimit": 100
  }
}
```

### 8. 下一步

配置完成后：

1. ✅ 运行 `SupabaseDBManager.exe`
2. ✅ 点击"测试连接"验证配置
3. ✅ 开始使用工具管理数据库

详细使用说明请参阅 [README.md](README.md)

## 技术支持

如有问题，请在项目仓库提交 Issue。
