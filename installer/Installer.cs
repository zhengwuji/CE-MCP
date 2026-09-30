// CE MCP 安装器(单文件 WinForms,双击运行)
// 功能:自动定位 Cheat Engine 安装目录 -> 把 Lua 桥/Python MCP 服务器安装进去并写 autorun
//       找不到时兜底:允许手动选择目录,或解压到桌面由用户手动放入 CE 目录
// 编译:csc -codepage:65001 -target:winexe -win32manifest:app.manifest
//       -r:System.Windows.Forms.dll -r:System.Drawing.dll
//       -resource:<lua>,ce_mcp_bridge.lua -resource:<py>,mcp_cheatengine.py
//       -resource:<req>,requirements.txt -resource:<md>,MCP_Bridge_Command_Reference.md
//       Installer.cs
// 命令行:--test-detect 仅打印检测到的 CE 路径(供 CI/调试)
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CeMcpInstaller
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0] == "--test-detect")
            {
                string p = InstallerLogic.DetectCePath();
                Console.WriteLine(p == null ? "(CE not found)" : p);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public static class InstallerLogic
    {
        public static string DetectCePath()
        {
            string p = DetectFromUninstallRegistry();
            if (p == null) p = DetectFromDirectRegistry();
            if (p == null) p = DetectFromRunningProcess();
            if (p == null) p = DetectFromCommonPaths();
            return p;
        }

        // 1) Inno Setup 卸载表:Cheat Engine_is1 -> InstallLocation / Inno Setup: App Path
        static string DetectFromUninstallRegistry()
        {
            string[] roots = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            RegistryKey[] hives = { Registry.LocalMachine, Registry.CurrentUser };
            foreach (RegistryKey hive in hives)
            {
                foreach (string root in roots)
                {
                    try
                    {
                        using (RegistryKey k = hive.OpenSubKey(root))
                        {
                            if (k == null) continue;
                            foreach (string sub in k.GetSubKeyNames())
                            {
                                if (sub.IndexOf("Cheat Engine", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                using (RegistryKey sk = k.OpenSubKey(sub))
                                {
                                    if (sk == null) continue;
                                    string cand = AsString(sk.GetValue("InstallLocation"));
                                    if (!LooksLikeCe(cand)) cand = AsString(sk.GetValue("Inno Setup: App Path"));
                                    if (LooksLikeCe(cand)) return cand;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        // 2) CE 自己的注册表键(部分安装方式会写入)
        static string DetectFromDirectRegistry()
        {
            string[] keys = {
                @"SOFTWARE\WOW6432Node\Cheat Engine",
                @"SOFTWARE\Cheat Engine",
                @"Software\Cheat Engine"
            };
            RegistryKey[] hives = { Registry.LocalMachine, Registry.CurrentUser };
            foreach (RegistryKey hive in hives)
            {
                foreach (string key in keys)
                {
                    try
                    {
                        using (RegistryKey k = hive.OpenSubKey(key))
                        {
                            if (k == null) continue;
                            string[] names = { "InstallPath", "InstallLocation", "Path", "CEPath", "" };
                            foreach (string n in names)
                            {
                                string v = (n.Length == 0) ? AsString(k.GetValue(null)) : AsString(k.GetValue(n));
                                if (LooksLikeCe(v)) return v;
                            }
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        // 3) 正在运行的 cheatengine 进程
        static string DetectFromRunningProcess()
        {
            try
            {
                foreach (Process proc in Process.GetProcesses())
                {
                    try
                    {
                        string n = proc.ProcessName;
                        if (n != null && n.ToLower().StartsWith("cheatengine"))
                        {
                            string exe = null;
                            try { exe = proc.MainModule.FileName; } catch { }
                            if (exe != null && LooksLikeCe(Path.GetDirectoryName(exe)))
                                return Path.GetDirectoryName(exe);
                        }
                    }
                    catch { }
                    finally { try { proc.Dispose(); } catch { } }
                }
            }
            catch { }
            return null;
        }

        // 4) 常见安装路径(含多盘符)
        static string DetectFromCommonPaths()
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string[] candidates = {
                Path.Combine(pf, "Cheat Engine"),
                Path.Combine(pf86, "Cheat Engine"),
                @"C:\Program Files\Cheat Engine",
                @"C:\Program Files (x86)\Cheat Engine",
                @"D:\Program Files\Cheat Engine", @"D:\Program Files (x86)\Cheat Engine",
                @"D:\Cheat Engine", @"E:\Cheat Engine", @"F:\Cheat Engine", @"G:\Cheat Engine"
            };
            foreach (string c in candidates)
                if (LooksLikeCe(c)) return c;
            return null;
        }

        public static bool LooksLikeCe(string dir)
        {
            if (dir == null || dir.Length == 0 || !Directory.Exists(dir)) return false;
            string[] exes = { "cheatengine-x86_64.exe", "cheatengine-i386.exe", "Cheat Engine.exe", "cheatengine-x86_64-SSE4-AVX2.exe" };
            foreach (string e in exes)
                if (File.Exists(Path.Combine(dir, e))) return true;
            return false;
        }

        static string AsString(object o)
        {
            string s = o as string;
            if (s == null) return null;
            s = s.Trim();
            if (s.Length == 0) return null;
            if (s.EndsWith("\\")) s = s.Substring(0, s.Length - 1); // InstallLocation 常带尾斜杠
            return s;
        }

        public static byte[] ReadResource(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) throw new FileNotFoundException("missing embedded resource: " + name);
                byte[] buf = new byte[s.Length];
                s.Read(buf, 0, buf.Length);
                return buf;
            }
        }

        public static void WriteFile(string path, byte[] data)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, data);
        }

        public static string AutorunContent(string bridgePath, string manualPath)
        {
            return "-- CE MCP Bridge 自动加载脚本(由 CE-MCP 安装器写入)\r\n" +
                   "-- 重载方式:在 CE 里重新执行本脚本即可(桥会自动清理旧状态)\r\n" +
                   "dofile([[" + bridgePath + "]])\r\n" +
                   "\r\n" +
                   "print([[\\n==========[ CE MCP - 文档指引(供 AI 阅读) ]==========\\n" +
                   "本机安装目录 : " + bridgePath + "\\n" +
                   "工具手册     : " + manualPath + "\\n" +
                   "在线手册     : https://github.com/zhengwuji/CE-MCP\\n" +
                   "Python 服务器: 同目录 mcp_cheatengine.py(由 AI 客户端自动启动)\\n" +
                   "验证连接     : 调用 MCP 工具 ping -> 应返回 version 12.0.0\\n" +
                   "重载桥       : 在 CE 中重新执行本 autorun 脚本\\n" +
                   "===========================================================]])\r\n";
        }

        public static string ReadmeText(string cePath, string ceMcpDir)
        {
            return "CE MCP 安装结果与使用说明\r\n" +
                   "================================\r\n\r\n" +
                   "安装目录:" + ceMcpDir + "\r\n" +
                   "Cheat Engine:" + cePath + "\r\n\r\n" +
                   "已安装内容:\r\n" +
                   "  ce_mcp_bridge.lua                  Lua 桥(CE 启动时经 autorun 自动加载)\r\n" +
                   "  mcp_cheatengine.py                 Python MCP 服务器(AI 客户端经 stdio 启动)\r\n" +
                   "  requirements.txt                   Python 依赖清单\r\n" +
                   "  AI_Context\\MCP_Bridge_Command_Reference.md   全部工具的详细手册\r\n" +
                   "  ..\\autorun\\ce_mcp_autorun.lua      CE 启动自动加载脚本\r\n\r\n" +
                   "AI 客户端(如 ZCode / Claude Desktop / Cursor)MCP 配置:\r\n" +
                   "{\r\n" +
                   "  \"mcpServers\": {\r\n" +
                   "    \"cheatengine\": {\r\n" +
                   "      \"command\": \"python\",\r\n" +
                   "      \"args\": [\"" + ceMcpDir + "\\mcp_cheatengine.py\"]\r\n" +
                   "    }\r\n" +
                   "  }\r\n" +
                   "}\r\n\r\n" +
                   "Python 依赖安装:python -m pip install -r \"" + ceMcpDir + "\\requirements.txt\"\r\n" +
                   "验证:重启 CE 后,在 AI 客户端调用 ping 工具,应返回 version 12.0.0。\r\n" +
                   "卸载:删除 CE 目录下的 ce_mcp 文件夹和 autorun\\ce_mcp_autorun.lua 即可。\r\n";
        }
    }

    public class MainForm : Form
    {
        TextBox pathBox;
        Button browseBtn;
        Button installBtn;
        CheckBox pipCheck;
        TextBox log;

        public MainForm()
        {
            Text = "CE MCP 安装器(Cheat Engine MCP Bridge)";
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new System.Drawing.Size(640, 380);

            Label title = new Label();
            title.Text = "将 CE MCP 桥安装进 Cheat Engine(自动定位安装目录)";
            title.Location = new System.Drawing.Point(16, 14);
            title.AutoSize = true;
            Controls.Add(title);

            Label lbl = new Label();
            lbl.Text = "Cheat Engine 安装目录:";
            lbl.Location = new System.Drawing.Point(16, 48);
            lbl.AutoSize = true;
            Controls.Add(lbl);

            pathBox = new TextBox();
            pathBox.Location = new System.Drawing.Point(16, 72);
            pathBox.Width = 480;
            Controls.Add(pathBox);

            browseBtn = new Button();
            browseBtn.Text = "浏览…";
            browseBtn.Location = new System.Drawing.Point(508, 70);
            browseBtn.Click += OnBrowse;
            Controls.Add(browseBtn);

            pipCheck = new CheckBox();
            pipCheck.Text = "安装 Python 依赖(检测到 Python 后可选)";
            pipCheck.Location = new System.Drawing.Point(16, 106);
            pipCheck.AutoSize = true;
            Controls.Add(pipCheck);

            installBtn = new Button();
            installBtn.Text = "开始安装";
            installBtn.Location = new System.Drawing.Point(16, 138);
            installBtn.Width = 140;
            installBtn.Height = 34;
            installBtn.Click += OnInstall;
            Controls.Add(installBtn);

            log = new TextBox();
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.Location = new System.Drawing.Point(16, 184);
            log.Width = 600;
            log.Height = 180;
            Controls.Add(log);

            string found = InstallerLogic.DetectCePath();
            if (found != null)
            {
                pathBox.Text = found;
                Log("已自动定位 Cheat Engine:" + found);
                Log("点击「开始安装」即可。安装内容:Lua 桥 + Python MCP 服务器 + 工具手册 + 启动自动加载脚本。");
            }
            else
            {
                Log("未自动找到 Cheat Engine。");
                Log("可点击「浏览…」手动选择 CE 安装目录;若 CE 是绿色版/路径特殊,也可以直接把安装器生成的文件手动放进 CE 目录(兜底方案,安装时会提示)。");
            }
            pipCheck.Checked = IsPythonAvailable();
            if (!pipCheck.Checked)
            {
                pipCheck.Enabled = false;
                pipCheck.Text = "未检测到 Python(可稍后手动:python -m pip install -r requirements.txt)";
            }
        }

        void Log(string s)
        {
            log.AppendText(s + "\r\n");
        }

        void OnBrowse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择 Cheat Engine 安装目录(含 cheatengine-x86_64.exe 的目录)";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    pathBox.Text = dlg.SelectedPath;
                    Log("已手动选择目录:" + dlg.SelectedPath +
                        (InstallerLogic.LooksLikeCe(dlg.SelectedPath) ? "" : "(警告:该目录下未发现 cheatengine 主程序,请确认)"));
                }
            }
        }

        void OnInstall(object sender, EventArgs e)
        {
            string ce = pathBox.Text == null ? "" : pathBox.Text.Trim().TrimEnd('\\');
            if (!InstallerLogic.LooksLikeCe(ce))
            {
                DialogResult r = MessageBox.Show(
                    "目录下未找到 Cheat Engine 主程序(cheatengine-x86_64.exe 等)。\r\n\r\n" +
                    "「是」:重新手动选择 CE 安装目录\r\n" +
                    "「否」:使用兜底方案——把文件解压到桌面,由您手动放入 CE 目录\r\n" +
                    "「取消」:退出",
                    "未找到 Cheat Engine", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (r == DialogResult.Yes) { OnBrowse(sender, e); return; }
                if (r == DialogResult.No) { InstallToDesktopFallback(); return; }
                return;
            }

            try
            {
                string ceMcp = Path.Combine(ce, "ce_mcp");
                string autorunDir = Path.Combine(ce, "autorun");
                string bridgePath = Path.Combine(ceMcp, "ce_mcp_bridge.lua");
                string manualPath = Path.Combine(ceMcp, "AI_Context", "MCP_Bridge_Command_Reference.md");

                InstallerLogic.WriteFile(bridgePath, InstallerLogic.ReadResource("ce_mcp_bridge.lua"));
                InstallerLogic.WriteFile(Path.Combine(ceMcp, "mcp_cheatengine.py"), InstallerLogic.ReadResource("mcp_cheatengine.py"));
                InstallerLogic.WriteFile(Path.Combine(ceMcp, "requirements.txt"), InstallerLogic.ReadResource("requirements.txt"));
                InstallerLogic.WriteFile(manualPath, InstallerLogic.ReadResource("MCP_Bridge_Command_Reference.md"));
                InstallerLogic.WriteFile(Path.Combine(ceMcp, "安装与使用说明.txt"),
                    System.Text.Encoding.UTF8.GetBytes(InstallerLogic.ReadmeText(ce, ceMcp)));

                string autorun = Path.Combine(autorunDir, "ce_mcp_autorun.lua");
                if (File.Exists(autorun))
                {
                    string bak = autorun + ".bak";
                    File.Copy(autorun, bak, true);
                    Log("已备份原有 autorun 脚本 -> " + bak);
                }
                InstallerLogic.WriteFile(autorun,
                    System.Text.Encoding.UTF8.GetBytes(InstallerLogic.AutorunContent(bridgePath, manualPath)));

                Log("已安装 Lua 桥      -> " + bridgePath);
                Log("已安装 Python 服务器 -> " + Path.Combine(ceMcp, "mcp_cheatengine.py"));
                Log("已安装工具手册     -> " + manualPath);
                Log("已写启动自动加载   -> " + autorun);

                if (pipCheck.Checked && pipCheck.Enabled)
                {
                    Log("正在安装 Python 依赖(mcp, pywin32)…");
                    string outp;
                    bool ok = RunPip(Path.Combine(ceMcp, "requirements.txt"), out outp);
                    Log(ok ? "Python 依赖安装完成。" : "Python 依赖安装失败,请手动执行:");
                    if (!ok) Log("  python -m pip install -r \"" + Path.Combine(ceMcp, "requirements.txt") + "\"");
                    if (outp != null && outp.Length > 0) Log(outp);
                }

                Log("");
                Log("安装完成!重启 Cheat Engine 后,在 AI 客户端调用 ping 工具验证(应返回 version 12.0.0)。");
                MessageBox.Show("安装完成!\r\n\r\n请重启 Cheat Engine,然后按「安装与使用说明.txt」里的 JSON 把 MCP 服务器注册到你的 AI 客户端。",
                    "CE MCP 安装器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("安装失败:" + ex.Message);
                Log("兜底方案:把文件解压到桌面手动放入 CE 目录。");
                MessageBox.Show("安装失败:" + ex.Message + "\r\n\r\n可以使用兜底方案:手动把文件复制到 CE 目录。",
                    "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void InstallToDesktopFallback()
        {
            try
            {
                string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CE-MCP-手动安装");
                string bridgePath = Path.Combine(dest, "ce_mcp_bridge.lua");
                InstallerLogic.WriteFile(bridgePath, InstallerLogic.ReadResource("ce_mcp_bridge.lua"));
                InstallerLogic.WriteFile(Path.Combine(dest, "mcp_cheatengine.py"), InstallerLogic.ReadResource("mcp_cheatengine.py"));
                InstallerLogic.WriteFile(Path.Combine(dest, "requirements.txt"), InstallerLogic.ReadResource("requirements.txt"));
                InstallerLogic.WriteFile(Path.Combine(dest, "AI_Context", "MCP_Bridge_Command_Reference.md"),
                    InstallerLogic.ReadResource("MCP_Bridge_Command_Reference.md"));

                string guide = "CE MCP 手动安装说明(兜底方案)\r\n================================\r\n\r\n" +
                    "安装器没有找到 Cheat Engine,已把文件解压到本文件夹。请手动完成两步:\r\n\r\n" +
                    "1. 把整个本文件夹复制到 Cheat Engine 安装目录下,并重命名为 ce_mcp\r\n" +
                    "   (例如:C:\\Program Files\\Cheat Engine\\ce_mcp\\)\r\n\r\n" +
                    "2. 在 CE 目录的 autorun 文件夹里新建 ce_mcp_autorun.lua,内容一行:\r\n\r\n" +
                   "   dofile([[<CE安装目录>\\ce_mcp\\ce_mcp_bridge.lua]])\r\n\r\n" +
                    "   把 <CE安装目录> 换成真实路径。autorun 文件夹不存在就新建一个。\r\n\r\n" +
                    "3. AI 客户端 MCP 配置(command/args 指向实际路径):\r\n" +
                    "   { \"mcpServers\": { \"cheatengine\": { \"command\": \"python\",\r\n" +
                    "     \"args\": [\"<CE安装目录>\\\\ce_mcp\\\\mcp_cheatengine.py\"] } } }\r\n\r\n" +
                    "4. Python 依赖:python -m pip install -r requirements.txt\r\n" +
                    "   验证:重启 CE,调用 ping 工具应返回 version 12.0.0。\r\n";
                InstallerLogic.WriteFile(Path.Combine(dest, "安装说明.txt"), System.Text.Encoding.UTF8.GetBytes(guide));

                Log("已解压到:" + dest);
                Log("请按其中的「安装说明.txt」手动放入 CE 目录。");
                MessageBox.Show("文件已解压到桌面:「CE-MCP-手动安装」文件夹。\r\n请按里面的「安装说明.txt」手动放入 CE 目录。",
                    "兜底方案", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("解压失败:" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static bool IsPythonAvailable()
        {
            string o; return RunCommand("python", "--version", out o) || RunCommand("py", "-3 --version", out o);
        }

        static bool RunCommand(string exe, string args, out string output)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + exe + " " + args + " 2>&1";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(60000);
                    return p.ExitCode == 0;
                }
            }
            catch { output = null; return false; }
        }

        bool RunPip(string reqPath, out string output)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c python -m pip install -r \"" + reqPath + "\" 2>&1";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    output = p.StandardOutput.ReadToEnd().Trim();
                    if (output.Length > 800) output = "…" + output.Substring(output.Length - 800);
                    p.WaitForExit(300000);
                    return p.ExitCode == 0;
                }
            }
            catch { output = null; return false; }
        }
    }
}
