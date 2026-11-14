# 发布与分发

原生包由 [NativeAot 配置](../src/RazorFolio.Cli/Properties/PublishProfiles/NativeAot.pubxml)发布，[NativePackage.targets](../build/NativePackage.targets)负责组包和解包验证。Windows、Linux 和 CI 使用同一入口。

## 构建环境

SDK 基线为 global.json 中的 .NET 11.0.100，允许同功能带补丁。构建机还需要 [NativeAOT 原生工具链](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites)：

| 目标 | 构建机 | 工具链 |
| --- | --- | --- |
| win-x64 | Windows x64 | Visual Studio C++ 工具与 Windows SDK |
| linux-x64 | Linux x64，glibc | Clang 或 GCC、链接器、zlib 开发文件、GNU tar |

Windows 包在 Windows 上构建，Linux 包在 Linux 上构建。Linux 包使用系统提供的 glibc 等原生库，CI 构建基线为 Ubuntu 24.04；面向较老系统发布时，选择相应的构建环境并验证运行结果。

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

压缩包包含程序、独立使用说明、项目许可证、依赖许可证与 `build-info.txt`。原生程序的 PDB/DBG 保留在 publish 目录，按需单独保存。

打包后会解开压缩包，校验实际报告的版本并执行 `new`、`build`，确认生成页面与搜索索引。Linux 的执行权限也通过这一步验证。只有检查成功才写出 SHA-256 文件；同名包或工作目录存在时会拒绝覆盖，失败后可使用新的 `NativePackageRoot` 重试。

`-p:NativePackageRoot=<绝对路径>` 可改变包与临时组包目录，WSL 临时验证时可指向 `/tmp`。从源码归档构建时，需显式传入 `-p:SourceRevisionId=<对应提交>`。

## 版本与核验

`Version` 同时控制程序版本和包名。`build-info.txt` 记录源码提交、实际 SDK/运行时版本和指定的构建基线，便于追踪包的来源。

Linux 校验下载：

```bash
sha256sum --check razorfolio-0.1.0-preview.1-linux-x64.tar.gz.sha256
```

Windows 用 `Get-FileHash <压缩包> -Algorithm SHA256` 与对应 `.sha256` 比较，确认下载完整。

重建时使用相同的源码提交、版本和构建环境。编译工具链、SDK 补丁及归档时间戳会影响最终压缩包的哈希。

## CI

[Native packages 工作流](../.github/workflows/native-packages.yml)手动触发，分别使用 `windows-2025` 和 `ubuntu-24.04`。Action 引用固定提交；SDK 来自 global.json，实际版本进入包内记录。

在 GitHub 的 **Actions → Native packages → Run workflow** 中选择分支、填写版本并运行。两个平台打包成功后，工作流核验下载的包并创建 GitHub Release，附上 Windows ZIP、Linux tar.gz 和各自的 SHA-256 文件。运行页面也保留 `razorfolio-win-x64` 和 `razorfolio-linux-x64` artifacts，保留期为 14 天。

版本填写 `0.1.0-preview.1` 时，标签为 `v0.1.0-preview.1`，指向本次构建提交。带 `-` 后缀的版本标记为预发布，`0.1.0` 这类版本创建正式 Release；发布说明由 GitHub 自动生成。每次新发布使用新版本号，已有 Release 保持原样；已有同名标签须指向本次构建提交。上传中断留下草稿时，可在 Releases 页面检查并补齐发布。

工作流仅通过 **Run workflow** 手动运行，提交代码和推送标签均不会触发。发布任务使用仓库自带的 `GITHUB_TOKEN`，具有 `contents: write` 权限；NuGet 包按下节单独构建和发布。

## .NET 工具版

工具版运行需要 .NET 11 与 ASP.NET Core 11：

```bash
dotnet pack src/RazorFolio.Cli -c Release -p:Version=0.1.0-preview.1 -o artifacts/packages
dotnet tool install RazorFolio --version 0.1.0-preview.1 --source ./artifacts/packages --tool-path ./artifacts/tools
```

工具包使用默认构建配置。更新时递增版本，避免使用旧缓存。

## 已有验证

完整 CLI 已通过 Windows Sandbox（无现代 .NET 安装）和 Arch Linux WSL2（删除临时 SDK 后）的 new/build/dev/preview、HTTP、SSE、监听与正常退出检查。每次打包会执行解包冒烟检查；修改模板、序列化或主机逻辑后，重跑对应功能回归。

本机详细记录在 `artifacts/windows-sandbox/`、`artifacts/wsl-aot/`、`artifacts/release-check/`，CI 记录见工作流运行页面。
