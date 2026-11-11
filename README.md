# RazorFolio

基于 C# / .NET 10、Markdig 和预编译 Razor 模板的静态站点生成器。命令名为 `rf`。

当前只有项目骨架和 CLI 帮助信息，尚未实现文章解析、主题、开发服务或站点构建。Markdig 和 YAML 解析依赖将在对应功能实现时加入。

## 项目结构

| 项目 | 计划职责 |
| --- | --- |
| `src/RazorFolio.Core` | 文章模型、Markdown 处理、站点组织与生成流程 |
| `src/RazorFolio.Cli` | `rf` 命令、本地开发与预览服务 |
| `src/RazorFolio.Themes.Default` | 随工具预编译的默认 Razor 主题 |

默认主题项目已经配置 Razor SDK，模板尚未添加。CLI 通过项目引用携带默认主题，第一版不加载外部主题插件。

## 开发

在仓库根目录编译程序、查看帮助：

```bash
dotnet build
dotnet run --project src/RazorFolio.Cli -- --help
```

CLI 的 `AssemblyName` 为 `rf`，Linux 默认 Debug 构建的可执行入口为 `src/RazorFolio.Cli/bin/Debug/net10.0/rf`，Windows 对应 `rf.exe`。这是依赖框架的构建，运行时需保留同目录的依赖文件。

站点命令实现后，日常调试使用独立的实验站点：

```bash
dotnet run --project src/RazorFolio.Cli -- dev --site ~/razorfolio-blog
dotnet run --project src/RazorFolio.Cli -- build --site ~/razorfolio-blog --output public
dotnet run --project src/RazorFolio.Cli -- preview --site ~/razorfolio-blog --directory public
```

`~/razorfolio-blog` 初始为空，用于示例文章和可清理的实验，不存放需要长期保存的唯一副本。普通用户也使用独立站点目录，不需要把文章放进生成器源码仓库。

## 站点约定

- `content/`：Markdown 文章。
- `static/`：原样复制的图片、附件等资源。
- `razorfolio.json`：站点配置。
- `.razorfolio/`：开发产物和缓存。
- `public/`：正式网站输出，相对路径按站点根目录解析。

`dev` 计划监听文件、生成页面并自动刷新，默认访问地址为 `http://127.0.0.1:8080`。`build` 生成网站后退出。`preview` 只提供正式产物，不重新生成内容。

`dotnet build` 和 `dotnet publish` 处理生成器程序；`rf build` 处理博客站点。预编译 Razor 模板变化需要重新编译并生成页面；后续再接入 `dotnet watch`。
