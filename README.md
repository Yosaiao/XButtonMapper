# XButtonMapper 2.0

XButtonMapper 是一个适用于 Windows 10 21H2 的轻量托盘工具，用鼠标前进、后退侧键触发复制和粘贴。
<img width="476" height="246" alt="image" src="https://github.com/user-attachments/assets/c2bc0868-0bf5-4d02-9c8f-809aebe3b4e1" />


## 2.0 更新

新增可单独为侧键选择的“兼容复制 (Ctrl+Insert)”。如果 Coremail 4.0 中手动按 Ctrl+Insert 可以复制，可在托盘菜单中选择“后退键 → 兼容复制 (Ctrl+Insert)”并验证。升级时先从托盘退出旧版，再运行新版。已有设置会保留。

复制和粘贴现在按前台窗口的键盘布局发送扫描码。

## 直接运行

双击 `dist\XButtonMapper.exe`。程序不会显示主窗口，启动后会出现在系统托盘中。

默认设置：

- 鼠标后退键（XBUTTON1）：复制，等同于 `Ctrl+C`
- 鼠标前进键（XBUTTON2）：粘贴，等同于 `Ctrl+V`
- 已映射的侧键会拦截原有浏览器前进/后退动作

右键托盘图标可以：

- 临时启用或暂停映射
- 为两个侧键分别选择“复制”“兼容复制 (Ctrl+Insert)”“粘贴”或“保持原功能”
- 启用或关闭当前用户的开机自动启动
- 退出程序

暂停状态不会保存，下次启动时映射会恢复启用。两个侧键的动作会保存在：

`%LOCALAPPDATA%\XButtonMapper\settings.ini`

配置文件使用简单的 `键=值` 格式：

- `BackAction`：后退键动作
- `ForwardAction`：前进键动作
- 可用值：`Copy`、`CopyWithInsert`、`Paste`、`PassThrough`

启用“开机自动启动”后会写入：

- 注册表路径：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- 值名：`XButtonMapper`

## 自行构建

在 PowerShell 中进入项目目录并执行：

```powershell
.\build.ps1
```

脚本使用 Windows 自带的 .NET Framework C# 编译器，不下载依赖，输出：

`dist\XButtonMapper.exe`

构建结束前会自动运行结构布局自检，并短暂启动托盘和鼠标钩子完成冒烟测试。

也可指定目标平台：

```powershell
.\build.ps1 -Platform x86
.\build.ps1 -Platform x64
```

默认 `AnyCPU` 构建可在 32 位和 64 位 Windows 10 上运行。

## 兼容性与限制

- 目标运行时为 Windows 10 21H2 自带的 .NET Framework 4.8，无需安装 .NET 6/7/8 或 VC++ 运行库。
- 程序默认以普通用户权限运行，不会弹出 UAC。
- Windows 安全机制不允许普通权限程序向管理员权限窗口注入按键；需要控制管理员窗口时，请手动以管理员身份运行本程序。
- 已设置为复制或粘贴的侧键始终会拦截原前进/后退事件；若目标程序因权限或自身策略拒绝模拟快捷键，也不会退回执行页面导航。
- UAC 安全桌面、部分游戏或反作弊程序、远程控制软件以及禁止粘贴的密码控件可能忽略模拟输入。
- 若鼠标厂商驱动已经重新定义侧键，需要先在厂商软件中恢复为“前进/后退”功能。

## 手动验收建议

1. 在记事本中选择文字，按后退侧键复制，再用前进侧键粘贴。
2. 在浏览器中确认映射启用时不会同时发生页面前进或后退。
3. 将某个侧键设为“保持原功能”，确认浏览器导航恢复。
4. 修改映射后退出并重新运行，确认设置被保留。
5. 连续快速点击、长按侧键，并分别在按住左 Ctrl、右 Ctrl 时测试。
6. 重复启动 EXE，确认只保留一个进程并由现有实例显示托盘提示。
7. 启用开机自动启动后检查当前用户注册表启动项，再关闭并确认启动项被移除。
