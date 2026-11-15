# 发布与分发

本页用于构建和发布 RazorFolio 本身。部署博客站点见 [GitHub Pages](github-pages.md)，运行已下载的程序见[原生包使用](native-package.md)。

## 在 GitHub 发布 Release

1. 推送准备发布的源码提交。
2. 在 **Actions → Native packages → Run workflow** 选择包含该提交的分支，输入版本号，例如 `0.4.0`。省略 `v` 前缀。
3. 等待 Windows 和 Linux 打包及 `release` 任务完成。
4. 在 **Releases** 确认标签指向预期提交，并检查两个原生包及各自的 SHA-256 文件。

工作流会创建对应的标签，例如 `v0.4.0`，并生成发行说明。带后缀的版本会发布为预发布版。每次使用新版本号。包含打包流程和权限设置的定义见 [Native packages 工作流](../.github/workflows/native-packages.yml)。

## 本地生成原生包

安装 `global.json` 指定的 SDK 及目标平台的 NativeAOT 工具链。Windows x64 使用 Visual Studio C++ 工具和 Windows SDK。Linux x64 需要原生链接工具链、zlib 开发文件和 GNU tar。正式 Linux 包在 Ubuntu 24.04 上构建。原生工具链的前置条件见 [.NET NativeAOT 指南](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites)。

在仓库根目录执行以下命令。在 Linux 上将两处 `win-x64` 换成 `linux-x64`。下文的 `0.4.0-preview.1` 仅为示例，请填写本次要发布的版本号。

```text
dotnet publish src/RazorFolio.Cli -c Release -r win-x64 -p:PublishProfile=NativeAot -p:CreateNativePackage=true -p:Version=0.4.0-preview.1 -o artifacts/publish/win-x64
```

压缩包和校验文件位于 `artifacts/releases/`。包名包含版本号和目标平台。内含程序、使用说明、许可证与 `build-info.txt`，调试符号留在 publish 输出目录。

打包流程会解压并执行 `--version`、`new` 和 `build`，检查生成页面和搜索索引。通过后生成 SHA-256 文件。已有同名包或组包工作目录时，可以通过 `-p:NativePackageRoot=<绝对路径>` 指向空目录。

`build-info.txt` 记录版本、源码提交、SDK、运行时和构建基线。`-p:NativeBuildBaseline=...` 可标注本机环境。从源码归档构建时，用 `-p:SourceRevisionId=<对应提交>` 提供提交号。打包定义见 [NativePackage.targets](../build/NativePackage.targets)。

## .NET 工具版

工具版运行需要 .NET 11 和 ASP.NET Core 11。打包与安装使用同一个版本号。

```bash
dotnet pack src/RazorFolio.Cli -c Release -p:Version=0.4.0-preview.1 -o artifacts/packages
dotnet tool install RazorFolio --version 0.4.0-preview.1 --source ./artifacts/packages --tool-path ./artifacts/tools
```
