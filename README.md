# RazorFolio

基于 C# / .NET 10、Markdig 和预编译 Razor 模板的静态站点生成器。命令名为 `rf`。

当前已实现最小的 `build`：递归读取站点 `content/` 下的 `.md` 文件，解析可选的 YAML front matter，用 Markdig 转换正文，再通过默认 Razor 主题生成完整 HTML，写入 `public/` 并保留相对目录。`static/` 中的文件也递归复制到 `public/`。构建成功后按生成清单清理过期输出。Markdown 暂时使用基础 CommonMark 语法，扩展后续按需启用。

例如 `content/Linux/hello.md` 输出为 `public/Linux/hello.html`，不同目录可有同名文章。普通 Markdown 链接暂不改写：链接文章时写发布后的 `.html` 路径；子目录文章引用资源时需按页面位置使用 `../images/example.svg` 等相对路径。

`preview` 可通过本地 HTTP 服务查看已有产物。`new` 和 `build` 的自定义输出目录尚未实现。

## 项目结构

| 项目 | 计划职责 |
| --- | --- |
| `src/RazorFolio.Core` | 文章模型、Markdown 处理、站点组织与生成流程 |
| `src/RazorFolio.Cli` | `rf` 命令、本地开发与预览服务 |
| `src/RazorFolio.Themes.Default` | 随工具预编译的默认 Razor 主题 |

默认主题使用 Razor SDK 预编译 `Views/Article.cshtml`。CLI 通过项目引用携带默认主题，第一版不加载外部主题插件。

`Views/_ViewStart.cshtml` 为页面选择共享的 `Views/Shared/_Layout.cshtml`。布局负责 HTML 外壳、字符集、viewport 和基础排版；文章页与首页只提供正文和 `ViewData["Title"]`，布局通过 `RenderBody()` 插入正文。修改主题后需要重新编译并构建站点。

基础 CSS 暂时内联在共享布局中，包含正文宽度、行距、图片缩放和代码块横向滚动，并通过 `color-scheme` 跟随系统深浅色偏好。后续需要独立样式资源时，再接入主题资源输出。

`Views/Home.cshtml` 生成根目录的 `index.html`，列出所有文章标题和链接，暂按源文件路径排序。没有文章时显示空状态；构建报告的页面数包含首页。根目录的 `index.html` 保留给自动首页，`content/index.md` 或与首页路径冲突的静态资源会报错。

首页链接按相对路径生成，各路径段分别进行 URL 编码，支持中文、空格和 `#` 等文件名字符。文章中的手写链接仍不自动改写。

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

示例文章是 `~/razorfolio-blog/content/hello.md`，输出为 `~/razorfolio-blog/public/hello.html`。页面包含 UTF-8 声明、viewport、基础 CSS 和正文。浏览器标签页标题读取 front matter 的 `title`，正文的一级标题仍来自 Markdown，不会自动插入或替换。

```markdown
---
title: 我的第一篇 RazorFolio 文章
---

# 正文标题
```

front matter 必须位于文件开头。当前只读取 `title`，其他字段暂时忽略；没有 `title`、值为 null 或空白时，回退到文件名。YamlDotNet 负责解析 YAML，格式错误及重复键会导致构建失败。文件名和输出 URL 不随标题变化。

阅读代码可以依次看 `Program.cs`、`Commands/SiteCommand.cs`、`SiteBuilder.cs`、`Content/ArticleParser.cs` 和 `Rendering/MarkdownRenderer.cs`。CLI 处理参数与退出码，Core 处理文件生成；`ArticleParser` 管理解析管线、提取元数据，并从语法树中移除 front matter，`MarkdownRenderer` 将剩余语法树渲染为正文 HTML，避免重复解析 Markdown。

页面渲染通过 Core 中的 `ArticlePage` 和 `IPageRenderer` 连接默认主题。`SiteBuilder` 只提交标题和正文，不接触 ASP.NET Core；主题中的 `RazorPageRenderer` 准备 MVC 视图上下文，再执行预编译模板。CLI 使用 ASP.NET Core 的服务容器组装渲染器，但不会启动 HTTP 监听。

模板用 `@Model.Title` 输出普通文本，由 Razor 编码；`@Html.Raw(Model.BodyHtml)` 原样插入作者 Markdown 生成的 HTML。当前按可信本地文章处理，不执行 HTML 清洗。

## 预览已有产物

图片和附件放在 `static/`，例如 `static/images/example.svg` 会复制到 `public/images/example.svg`。顶层文章可写 `![示例](images/example.svg)`；复制不会改写文件内容，也不会压缩或编译 CSS、JS。

`static/` 可以不存在。已有资源在再次构建时覆盖更新；静态资源与本次生成的页面路径冲突时会报错。删除源资源后，下次成功构建会清理生成清单中对应的旧输出。复制逻辑位于 `Assets/StaticAssetCopier.cs`。

文本附件约定使用 UTF-8。预览服务为 `.txt` 返回 `text/plain; charset=utf-8`，避免浏览器猜错编码；复制过程不转码。部署后的响应头由实际静态托管服务决定。

```bash
dotnet run --project src/RazorFolio.Cli -- preview --site ~/razorfolio-blog
```

打开 `http://127.0.0.1:8080/` 查看文章列表，或访问 `/hello.html` 查看示例文章，按 Ctrl+C 停止服务。`preview` 默认提供站点的 `public/` 目录，支持 `--directory` 和 `--port`；相对目录按站点根目录解析，例如 `--directory public --port 8081`。

预览不调用生成器、不监听 Markdown，也不注入刷新脚本。修改文章后需要重新执行 `build` 并刷新浏览器。服务只监听 IPv4 回环地址，不开放目录浏览；没有 `index.html` 等默认页时，根地址返回 404，直接访问文章路径即可。不存在的文件也返回 404，不回退到其他页面。

`Commands/PreviewCommand.cs` 负责参数检查，`Hosting/PreviewServer.cs` 负责静态文件服务，Core 不依赖预览服务。

## 开发模式

```bash
dotnet run --project src/RazorFolio.Cli -- dev --site ~/razorfolio-blog
```

默认访问 `http://127.0.0.1:8080/`，可在站点参数后加 `--port 8081`。启动时先构建，随后监听 `content/`、`static/` 和 `razorfolio.json`，变化后串行全站重建。文件事件会合并，写入 `public/` 不触发再次构建。监听器启动后会再同步构建一次，覆盖启动间隙可能发生的修改。

构建成功后浏览器自动整页刷新；改 `.cshtml` 或 C# 仍需要重新编译启动。构建错误会报告，但监听继续，修正文件后能再次生成。构建直接写入 `public/`，中途失败时可能留下部分更新；删除或重命名源文件后会在构建成功时清理清单中的旧产物。开发输出隔离后续再做。

`Hosting/LiveReload.cs` 仅在开发服务的 HTML 响应中插入刷新脚本，通过 `/_rf/events` 的 SSE 连接通知浏览器。每次成功构建更新版本号，失败时不通知刷新；连接恢复后也会比较版本号。开发响应禁用缓存，磁盘上的 HTML 不包含刷新脚本，`build` 和 `preview` 不启用该端点。当前使用整页刷新，不保留页面内交互状态。

`Hosting/SiteWatcher.cs` 通过 `FileSystemWatcher` 收集事件，用有界队列串行处理；作为后台服务随预览服务器一起停止。Ctrl+C 同时停止服务和监听。

`~/razorfolio-blog` 用于示例文章和可清理的实验，不存放需要长期保存的唯一副本。普通用户也使用独立站点目录，不需要把文章放进生成器源码仓库。

## 站点约定

站点根目录可放置 `razorfolio.json`：

```json
{
  "title": "我的博客",
  "language": "zh-CN"
}
```

`title` 用于首页标题和文章标签页标题的后缀；`language` 写入 HTML 的 `lang` 属性，不自动翻译主题文案。配置文件不存在或字段省略时，分别使用 `RazorFolio` 和 `zh-CN`。显式的 null、空白字段、未知字段及无效 JSON 会报错，错误包含配置文件路径。配置由 `Configuration/SiteConfigurationLoader.cs` 每次构建读取一次，再通过页面模型传给主题。

- `content/`：Markdown 文章。
- `static/`：原样复制的图片、附件等资源。
- `razorfolio.json`：站点配置。
- `.razorfolio/`：开发产物和缓存。
- `public/`：正式网站输出，相对路径按站点根目录解析。

`dev` 监听文件、重新生成页面并自动刷新浏览器，默认访问地址为 `http://127.0.0.1:8080`。`build` 生成网站后退出。`preview` 只提供正式产物，不重新生成内容。

`dotnet build` 和 `dotnet publish` 处理生成器程序；`rf build` 处理博客站点。预编译 Razor 模板变化需要重新编译并生成页面；后续再接入 `dotnet watch`。

## 生成清单与清理

每次成功构建会记录 `.razorfolio/outputs.json`，其中保存本次文章、首页和静态资源的相对输出路径。下一次成功生成全部页面后，只删除旧清单中不再出现的文件，再替换清单。手动放在 `public/` 中、从未被生成器记录的文件不会被清理；如果文件路径曾经由生成器管理，之后仍按生成产物处理。

首次启用清单时不会猜测历史文件归属，清单丢失后也不会扫描删除旧文件。空目录暂时保留。清单损坏或包含越界路径会报错；清理不会沿输出中的符号链接删除文件。构建中途失败不执行末尾清理、不更新清单，但已经写入的文件暂不回滚。

`Output/OutputManifest.cs` 负责清单和清理，`StaticAssetCopier` 返回复制过的输出路径，`SiteBuilder` 汇总所有产物。`.razorfolio/` 的变化不触发开发模式重新构建。
