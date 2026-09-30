# 更新日志(中文)

> 每次代码变更推送到 `main` 后,CI 会自动编译检查并发布 Release;**本文件最新一节会自动作为该次 Release 的中文更新说明**。格式:每节以 `## ` 开头,最新的写在最上面;只改 README.md 的提交不会触发构建。

## v12.0.0-b1(2026-09-30)首次发布

- 基于上游 [miscusi-peek/cheatengine-mcp-bridge](https://github.com/miscusi-peek/cheatengine-mcp-bridge) v12.0.0 完整功能:约 175 个 MCP 工具,覆盖内存读写、数值/未知值扫描、指针链与指针扫描、反汇编与调试断点、代码注入、符号管理、作弊表操作、内核级操作等
- 新增 GitHub Actions 自动化:代码变更自动触发编译检查(Python 字节码编译 + 依赖安装验证),自动打包 zip 并发布到 Releases,附中文更新说明
- README.md 新增「🇨🇳 中文说明」板块:中文更新内容与中文使用方法(快速开始 + 安全须知)
- 新增本中文更新日志 CHANGELOG_zh.md
- 保留上游 MIT 许可与完整提交历史,便于持续同步上游改进(`git pull upsrc main`)
