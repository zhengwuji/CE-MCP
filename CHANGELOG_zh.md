# 更新日志(中文)

> 每次代码变更推送到 `main` 后,CI 会自动编译检查并发布 Release;**本文件最新一节会自动作为该次 Release 的中文更新说明**。格式:每节以 `## ` 开头,最新的写在最上面;只改 README.md 的提交不会触发构建。

## 2026-09-30 一键安装器 + 全中文文档(桥版本 v12.0.0)

- 新增 **Windows 一键安装器 `CE-MCP-Setup.exe`**:双击安装,自动定位 Cheat Engine 真实安装路径(四级检测:卸载表注册表 → CE 注册表键 → 运行中的 cheatengine 进程 → 常见安装路径),自动装入 Lua 桥、Python MCP 服务器、工具手册,并写好 `autorun\ce_mcp_autorun.lua` 启动自动加载脚本
- **兜底保护**:自动检测失败时可手动选择 CE 目录;仍未找到则把全部文件解压到桌面「CE-MCP-手动安装」并生成中文安装说明,手动放入 CE 目录即可使用
- 可选自动安装 Python 依赖(检测到 Python 时可勾选,执行 `pip install -r requirements.txt`)
- README.md **全面中文化**:功能亮点、三种安装方式、使用方法、AI 文档指引、安全须知、常见问题
- CI 升级:每次代码变更自动编译安装器 exe 并连同 zip 一并发布到 Releases,附中文更新说明

## v12.0.0-b1(2026-09-30)首次发布

- 基于上游 [miscusi-peek/cheatengine-mcp-bridge](https://github.com/miscusi-peek/cheatengine-mcp-bridge) v12.0.0 完整功能:约 175 个 MCP 工具,覆盖内存读写、数值/未知值扫描、指针链与指针扫描、反汇编与调试断点、代码注入、符号管理、作弊表操作、内核级操作等
- 新增 GitHub Actions 自动化:代码变更自动触发编译检查(Python 字节码编译 + 依赖安装验证),自动打包 zip 并发布到 Releases,附中文更新说明
- README.md 新增「🇨🇳 中文说明」板块:中文更新内容与中文使用方法(快速开始 + 安全须知)
- 新增本中文更新日志 CHANGELOG_zh.md
- 保留上游 MIT 许可与完整提交历史,便于持续同步上游改进(`git pull upsrc main`)
