[Demo](https://github.com/user-attachments/assets/a184a006-f569-4b55-858a-ed80a7139035)

# CE-MCP —— 让 AI 直接驾驭 Cheat Engine

**让大模型帮你分析程序内存**:创建游戏修改器、训练器、做安全审计、写游戏机器人、加速逆向分析——用自然语言下指令,AI 通过 MCP 协议直接操控 Cheat Engine 完成一切。

[![Version](https://img.shields.io/badge/version-12.0.0-blue.svg)](#) [![Python](https://img.shields.io/badge/python-3.10%2B-green.svg)](https://python.org) [![Build & Release](https://github.com/zhengwuji/CE-MCP/actions/workflows/build-release.yml/badge.svg)](https://github.com/zhengwuji/CE-MCP/actions/workflows/build-release.yml)

> 本仓库基于上游 [miscusi-peek/cheatengine-mcp-bridge](https://github.com/miscusi-peek/cheatengine-mcp-bridge) v12.0.0,保留其全部功能与提交历史,并新增:**Windows 一键安装器(自动定位 CE 目录,找不到有兜底方案)**、**每次代码变更自动编译并发布 Release(附中文更新说明)**、**全中文文档**。

## 更新内容

完整中文更新日志见 **[CHANGELOG_zh.md](CHANGELOG_zh.md)**(最新在最上面;每次 Release 的中文说明自动取自该文件最新一节)。

### 本次(v12.0.0-b2)要点

- 新增 **Windows 一键安装器**:双击运行,自动定位 Cheat Engine 真实安装路径,把 Lua 桥、Python MCP 服务器、工具手册装进 CE 并写好启动自动加载脚本
- 安装器四级自动检测:卸载表注册表 → CE 注册表键 → 运行中的 cheatengine 进程 → 常见安装路径
- **兜底保护**:自动检测失败时可手动选择 CE 目录;连手动都放弃时,可把全部文件解压到桌面并生成中文安装说明,由用户手动放入 CE 目录
- 可选一键安装 Python 依赖(检测到 Python 时)
- README 全面中文化;CI 改为同时产出 `CE-MCP-Setup.exe`(安装器)与 zip 源码包,发布到 Releases

## 功能亮点

约 **175 个 MCP 工具**,覆盖逆向分析全流程:

| 类别 | 能力 |
|---|---|
| 系统/连接 | ping、进程附加、模块枚举、线程列表 |
| 内存读/写 | 读写整型、字节、字符串、指针、指针链、校验和 |
| 扫描 | 精确值/未知初值扫描、多结果管理、扫描会话 |
| 符号/地址 | 符号解析、地址信息、RTTI 类名 |
| 反汇编/分析 | 反汇编、代码页分析、高级分析 |
| 调试 | 断点、单步、调试寄存器、高级调试 |
| 代码仿真 | 指令仿真执行 |
| 内存管理 | 分配/释放内存、区段属性 |
| 进程控制 | 进程启动/暂停/恢复/终止 |
| 注入 | 代码注入、DLL 注入、AA 脚本 |
| 文件/窗口 | 文件操作、窗口操作 |
| 内核 | DBVM / 内核级操作(Ring -1 隐强调试) |
| 类型转换/钩子 | 类型转换、函数钩子 |
| 作弊表 | Cheat Table 完整操作 |

**工作原理**:

```
AI 客户端 ──(MCP / stdio JSON-RPC)──▶ mcp_cheatengine.py(Python)
                                            │
                                            ▼(命名管道 \\.\pipe\CE_MCP_Bridge_v99)
                                    ce_mcp_bridge.lua(CE 内 Lua 桥)
                                            │
                                            ▼
                                      目标进程内存
```

## 安装

### 方式一:一键安装器(推荐)

1. 到 [Releases](https://github.com/zhengwuji/CE-MCP/releases) 下载 `CE-MCP-Setup-vX.exe`;
2. 双击运行(写入 Program Files 需管理员权限,会自动弹 UAC);
3. 安装器**自动定位** CE 真实安装路径,把 `ce_mcp_bridge.lua`、`mcp_cheatengine.py`、工具手册装入 CE 目录,并写好 `autorun\ce_mcp_autorun.lua`(CE 每次启动自动加载);
4. 勾选项:检测到 Python 时可同时自动 `pip install` 依赖;
5. **没找到 CE?兜底保护**:
   - 点「浏览…」手动选择 CE 安装目录;
   - 或选兜底方案:安装器把全部文件解压到桌面「CE-MCP-手动安装」文件夹并生成中文安装说明,按说明手动放入 CE 目录即可。

### 方式二:从 Release 的 zip 手动安装

下载 zip,解压后在 CE 里 `File → Execute Script` 执行 `MCP_Server/ce_mcp_bridge.lua`(或用 dofile 方式),再按下面「使用方法」配置客户端。

### 方式三:从源码

```bash
git clone https://github.com/zhengwuji/CE-MCP.git
pip install -r CE-MCP/MCP_Server/requirements.txt
```

## 使用方法

1. **启动 Cheat Engine**:autorun 脚本自动加载桥,CE 输出
   `[MCP v12.0.0] MCP Server Listening on: CE_MCP_Bridge_v99` 即成功;
2. **在 AI 客户端注册 MCP 服务器**(ZCode / Claude Desktop / Cursor 等,配置文件位置各客户端不同):

   ```json
   {
     "mcpServers": {
       "cheatengine": {
         "command": "python",
         "args": ["C:\\Program Files\\Cheat Engine\\ce_mcp\\mcp_cheatengine.py"]
       }
     }
   }
   ```

   重启客户端,它会经 stdio 自动启动 Python MCP 服务器并连接 CE;
3. **验证**:调用 `ping` 工具,返回 `"version": "12.0.0"` 即端到端打通;
4. **每次启动 CE 自动加载**:一键安装器已自动写好;手动安装的用户把下面一行放进
   `C:\Program Files\Cheat Engine\autorun\ce_mcp_autorun.lua`(目录不存在就新建):

   ```lua
   dofile([[C:\Program Files\Cheat Engine\ce_mcp\ce_mcp_bridge.lua]])
   ```

5. **改了脚本要重载**:在 CE 里重新执行 autorun 脚本即可,桥会自动清理旧状态。

### AI 文档指引

桥加载后会在 CE 的 Lua 引擎窗口打印文档指引(仓库路径、工具手册位置、验证方法),把窗口截图给 AI,它就知道去哪读手册。本仓库内的完整工具手册:`AI_Context/MCP_Bridge_Command_Reference.md`(约 175 个工具逐一说明参数、示例与返回格式)。

## 安全须知(务必阅读)

- **必须关闭** Cheat Engine → Settings → Extra → **"Query memory region routines"**,否则配合 DBVM/反作弊扫描可能触发 `CLOCK_WATCHDOG_TIMEOUT` 蓝屏;
- 环境变量 `CE_MCP_ALLOW_SHELL=1` 会启用危险的 shell 执行工具,默认不要设置;
- 原生管道模式仅限 Windows;若需在 WSL/Linux 跑 MCP 服务器,用 TCP 中继(`ce_tcp_relay.py`,务必只绑定受信任网卡);
- 仅用于授权的安全研究、自己的游戏研究或逆向学习,请勿用于作弊或破坏。

## 常见问题

| 问题 | 处理 |
|---|---|
| `ping` 连接失败 | CE 没开或桥没加载;启动 CE 看 Lua 引擎窗口是否有 v12 横幅 |
| 变量过多报错 | 用 dofile 方式加载,不要整段粘贴桥脚本 |
| 想更新桥 | `git pull` 本仓库后,在 CE 里重新执行 autorun 脚本 |
| 非 Windows 跑 AI 客户端 | TCP 中继模式,见 `MCP_Server/ce_tcp_relay.py` 与上游 README |
| 蓝屏 CLOCK_WATCHDOG_TIMEOUT | 关闭 "Query memory region routines"(见安全须知) |

## 测试

`MCP_Server/test_mcp.py` 是端到端脚本:需 CE 正在运行、桥已加载并附加目标进程,然后 `python MCP_Server/test_mcp.py`。

## 致谢与许可

- 上游项目:[miscusi-peek/cheatengine-mcp-bridge](https://github.com/miscusi-peek/cheatengine-mcp-bridge) 及全部贡献者([@libangli218](https://github.com/libangli218)、[@lauralex](https://github.com/lauralex)、[@iamtyroon](https://github.com/iamtyroon)、[@HachiroSan](https://github.com/HachiroSan)、[@Attacktive](https://github.com/Attacktive) 等)
- 本仓库新增:一键安装器、自动编译发布 CI、中文文档与更新日志
- 许可:MIT(见 [LICENSE](LICENSE))
