# 更新日志

本项目的所有重要更改都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [1.0.0] - 2026-02-26

### 新增
- 🎉 首个开源版本发布
- ✨ 完整的数据库元数据浏览功能（表、策略、触发器、索引、函数、视图）
- 📋 一键复制 DDL 语句功能
- 🚀 SQL 查询器，支持自定义查询
- 📊 数据编辑器，支持浏览、编辑、添加、删除数据
- 🔐 使用 Windows DPAPI 加密存储敏感配置
- 🌍 完整支持 Supabase Pooler 模式（事务模式）
- 🎨 现代化的 WPF 用户界面
- 📝 完善的文档和配置指南

### 支持的功能
- **Tables 标签页**
  - 按架构分组显示表树
  - 查看表结构、列信息、主键、外键
  - 显示完整的 CREATE TABLE DDL
  - 搜索和筛选表

- **Policies 标签页**
  - 查看所有 Row Level Security 策略
  - 显示策略命令、类型、角色
  - 复制 CREATE POLICY DDL

- **Triggers 标签页**
  - 查看所有触发器
  - 显示触发时机、事件、函数
  - 复制 CREATE TRIGGER DDL

- **Indexes 标签页**
  - 查看所有索引
  - 显示索引类型、唯一性、列
  - 复制 CREATE INDEX DDL

- **Functions 标签页**
  - 查看所有函数
  - 显示函数签名和定义
  - 复制 CREATE FUNCTION DDL

- **Views 标签页**
  - 查看所有视图
  - 显示视图定义
  - 复制 CREATE VIEW DDL

- **SQL Query 标签页**
  - SQL 编辑器
  - 执行查询并显示结果
  - 复制查询结果

- **Data Editor 标签页**
  - 浏览表数据
  - 编辑数据（支持 UPDATE）
  - 添加新行（支持 INSERT）
  - 删除行（支持 DELETE）
  - 自动区分 INSERT/UPDATE 操作
  - 支持将字段设置为 NULL

### 技术特性
- ✅ 自动禁用 PREPARE 语句以支持 Pooler
- ✅ 使用依赖注入管理服务
- ✅ 所有数据库操作异步化
- ✅ 完善的错误处理和日志
- ✅ 配置文件加密存储

### 文档
- README.md - 项目说明
- SETUP_GUIDE.md - 详细配置指南
- LICENSE - MIT 许可证
- CHANGELOG.md - 更新日志
- CONTRIBUTING.md - 贡献指南

---

## [未来版本]

### v1.1（计划中）
- [ ] 批量导出 DDL 到文件
- [ ] 查询历史记录
- [ ] 高级搜索和筛选
- [ ] 数据编辑器增强（批量编辑）
- [ ] 支持导入 CSV/Excel 数据

### v1.2（计划中）
- [ ] 支持多个项目配置切换
- [ ] SQL 语法高亮
- [ ] 查询结果导出（CSV/Excel/JSON）
- [ ] 暗色主题
- [ ] 自定义主题

### v2.0（未来）
- [ ] 支持 Supabase Storage 管理
- [ ] 支持 Edge Functions
- [ ] 跨平台支持（Linux/macOS）
- [ ] 插件系统
- [ ] 可视化查询构建器

---

## 版本说明

- **主版本号**：不兼容的 API 变更
- **次版本号**：向下兼容的功能新增
- **修订号**：向下兼容的问题修正

## 链接

- [当前版本](https://github.com/hzxjcx/SupabaseDBManager/releases/latest)
- [所有版本](https://github.com/hzxjcx/SupabaseDBManager/releases)
