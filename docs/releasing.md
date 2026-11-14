# 发布与分发

原生包由 [NativeAot 配置](../src/RazorFolio.Cli/Properties/PublishProfiles/NativeAot.pubxml)发布，[NativePackage.targets](../build/NativePackage.targets)负责组包和解包验证。Windows、Linux 和 CI 使用同一入口；当前仓库不自动创建 GitHub Release 或推送 NuGet 包。

## 构建环境

SDK 基线为 global.json 中的 .NET 11.0.100，允许同功能带补丁。构建机还需要 [NativeAOT 原生工具链](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites)：

| 目标 | 构建机 | 工具链 |
| --- | --- | --- |
| win-x64 | Windows x64 | Visual Studio C++ 工具与 Windows SDK |
| linux-x64 | Linux x64，glibc | Clang 或 GCC、链接器、zlib 开发文件、GNU tar |

按目标系统分别构建，不从 Windows 交叉发布 Linux。Linux 的 libc 等系统依赖不会被消除；需要覆盖较老系统时，应选择对应构建基线并实际运行验证，不能仅凭 `linux-x64` 文件名宣称兼容。

## 生成原生分发包

正式候选包从干净的 Git 提交构建，两个平台使用相同版本。例如在 Windows：

```bash
dotnet publish src/RazorFolio.Cli -c Release -r win-x64 -p:PublishProfile=NativeAot -p:CreateNativePackage=true -p:Version=0.1.0-preview.1 -p:NativeBuildBaseline=windows-local -o artifacts/publish/win-x64
```

在 Linux：

```bash
dotnet publish src/RazorFolio.Cli -c Release -r linux-x64 -p:PublishProfile=NativeAot -p:CreateNativePackage=true -p:Version=0.1.0-preview.1 -p:NativeBuildBaseline=linux-local -o artifacts/publish/linux-x64
```

结果位于 `artifacts/releases/`：

```text
razorfolio-0.1.0-preview.1-win-x64.zip
razorfolio-0.1.0-preview.1-win-x64.zip.sha256
razorfolio-0.1.0-preview.1-linux-x64.tar.gz
razorfolio-0.1.0-preview.1-linux-x64.tar.gz.sha256
```

压缩包包含程序、独立使用说明、项目许可证、依赖许可证与 `build-info.txt`；不含 SDK、编译器、源码、样例、验证脚本或调试符号。原生程序的 PDB/DBG 保留在 publish 目录，按需单独保存。

打包后会解开压缩包，校验实际报告的版本并执行 `new`、`build`，确认生成页面与搜索索引。Linux 的执行权限也通过这一步验证。只有检查成功才写出 SHA-256 文件；同名包或工作目录存在时会拒绝覆盖，失败后可使用新的 `NativePackageRoot` 重试。

`-p:NativePackageRoot=<绝对路径>` 可改变包与临时组包目录，WSL 临时验证时可指向 `/tmp`。构建自源码归档而非 Git checkout 时，需显式传入 `-p:SourceRevisionId=<对应提交>`；实际改动尚未提交的本地包只用于验证，不作为正式发布来源。

## 版本与核验

`Version` 同时控制程序版本和包名。`build-info.txt` 记录源码提交、实际 SDK/运行时版本和指定的构建基线；基线标签用于追踪来源，不能代替环境验收。

Linux 校验下载：

```bash
sha256sum --check razorfolio-0.1.0-preview.1-linux-x64.tar.gz.sha256
```

Windows 用 `Get-FileHash <压缩包> -Algorithm SHA256` 与对应 `.sha256` 比较。校验和用于发现下载损坏，不提供签名认证。

这里的“可重复”指明确的构建、打包和验证流程。编译工具链、SDK 补丁及归档时间戳都可能影响字节结果，当前不承诺两次构建的压缩包哈希完全相同。

## CI

[Native packages 工作流](../.github/workflows/native-packages.yml)只手动触发，输入包版本，分别使用 `windows-2025` 和 `ubuntu-24.04`。Action 引用固定提交；SDK 来自 global.json，实际版本进入包内记录。成功后上传压缩包和校验文件为 workflow artifacts，保留 14 天，不自动对外发布。

当前 CI 定义尚未在 GitHub 上实际运行；Ubuntu 24.04 是所选 CI 构建基线，不是已完成的本机发行版兼容性结论。源码推送后应先运行一次工作流、检查上传包，再决定是否正式分发。

## .NET 工具版

保留原有工具分发方式，运行需要 .NET 11 与 ASP.NET Core 11：

```bash
dotnet pack src/RazorFolio.Cli -c Release -p:Version=0.1.0-preview.1 -o artifacts/packages
dotnet tool install RazorFolio --version 0.1.0-preview.1 --source ./artifacts/packages --tool-path ./artifacts/tools
```

更新包时递增版本，避免使用旧缓存。不要把 NativeAot 发布配置传给 `dotnet pack`。

## 已有验证

完整 CLI 已通过 Windows Sandbox（无现代 .NET 安装）和 Arch Linux WSL2（实际删除临时 SDK 后）的 new/build/dev/preview、HTTP、SSE、监听与正常退出检查。新分发包的解包冒烟检查不代替这些完整回归；修改模板、序列化或主机逻辑后应重跑对应检查。

本机详细记录在 `artifacts/windows-sandbox/`、`artifacts/wsl-aot/`、`artifacts/release-check/`；不随源码或分发包发布。其他系统版本和发行版、正式 CI 与发布签名仍需按交付目标验收。
