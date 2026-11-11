# RazorFolio

基于 C# / .NET 10、Markdig 和预编译 Razor 模板的静态站点生成器。命令名为 `rf`。

当前已实现最小的 `build`：读取站点 `content/` 顶层的 `.md` 文件，解析可选的 YAML front matter，用 Markdig 转换正文，再通过默认 Razor 主题生成完整 HTML，写入 `public/` 下的同名 `.html` 文件。暂不处理子目录和静态资源，也不会清理旧输出。Markdown 暂时使用基础 CommonMark 语法，扩展后续按需启用。

`new`、`dev`、`preview` 和自定义输出目录尚未实现。

## 项目结构

| 项目 | 计划职责 |
| --- | --- |
| `src/RazorFolio.Core` | 文章模型、Markdown 处理、站点组织与生成流程 |
| `src/RazorFolio.Cli` | `rf` 命令、本地开发与预览服务 |
| `src/RazorFolio.Themes.Default` | 随工具预编译的默认 Razor 主题 |

默认主题使用 Razor SDK 预编译 `Views/Article.cshtml`。CLI 通过项目引用携带默认主题，第一版不加载外部主题插件。

## 开发

在仓库根目录编译程序、查看帮助：

```bash
dotnet build
dotnet run --project src/RazorFolio.Cli -- --help
```

CLI 的 `AssemblyName` 为 `rf`，Linux 默认 Debug 构建的可执行入口为 `src/RazorFolio.Cli/bin/Debug/net10.0/rf`，Windows 对应 `rf.exe`。这是依赖框架的构建，运行时需保留同目录的依赖文件。

目前可以对独立的实验站点运行：

```bash
dotnet run --project src/RazorFolio.Cli -- build --site ~/razorfolio-blog
```

示例文章是 `~/razorfolio-blog/content/hello.md`，输出为 `~/razorfolio-blog/public/hello.html`。页面包含 UTF-8 声明、viewport 和正文，暂未添加 CSS。浏览器标签页标题读取 front matter 的 `title`，正文的一级标题仍来自 Markdown，不会自动插入或替换。

```markdown
---
title: 我的第一篇 RazorFolio 文章
---

# 正文标题
```

front matter 必须位于文件开头。当前只读取 `title`，其他字段暂时忽略；没有 `title`、值为 null 或空白时，回退到文件名。YamlDotNet 负责解析 YAML，格式错误及重复键会导致构建失败。文件名和输出 URL 不随标题变化。

阅读代码可以依次看 `Program.cs`、`Commands/BuildCommand.cs`、`SiteBuilder.cs`、`Content/ArticleParser.cs` 和 `Rendering/MarkdownRenderer.cs`。CLI 处理参数与退出码，Core 处理文件生成；`ArticleParser` 管理解析管线、提取元数据，并从语法树中移除 front matter，`MarkdownRenderer` 将剩余语法树渲染为正文 HTML，避免重复解析 Markdown。

页面渲染通过 Core 中的 `ArticlePage` 和 `IPageRenderer` 连接默认主题。`SiteBuilder` 只提交标题和正文，不接触 ASP.NET Core；主题中的 `RazorPageRenderer` 准备 MVC 视图上下文，再执行预编译模板。CLI 使用 ASP.NET Core 的服务容器组装渲染器，但不会启动 HTTP 监听。

模板用 `@Model.Title` 输出普通文本，由 Razor 编码；`@Html.Raw(Model.BodyHtml)` 原样插入作者 Markdown 生成的 HTML。当前按可信本地文章处理，不执行 HTML 清洗。

后续命令实现后，日常调试使用：

```bash
dotnet run --project src/RazorFolio.Cli -- dev --site ~/razorfolio-blog
dotnet run --project src/RazorFolio.Cli -- build --site ~/razorfolio-blog --output public
dotnet run --project src/RazorFolio.Cli -- preview --site ~/razorfolio-blog --directory public
```

`~/razorfolio-blog` 用于示例文章和可清理的实验，不存放需要长期保存的唯一副本。普通用户也使用独立站点目录，不需要把文章放进生成器源码仓库。

## 站点约定

- `content/`：Markdown 文章。
- `static/`：原样复制的图片、附件等资源。
- `razorfolio.json`：站点配置。
- `.razorfolio/`：开发产物和缓存。
- `public/`：正式网站输出，相对路径按站点根目录解析。

`dev` 计划监听文件、生成页面并自动刷新，默认访问地址为 `http://127.0.0.1:8080`。`build` 生成网站后退出。`preview` 只提供正式产物，不重新生成内容。

`dotnet build` 和 `dotnet publish` 处理生成器程序；`rf build` 处理博客站点。预编译 Razor 模板变化需要重新编译并生成页面；后续再接入 `dotnet watch`。
