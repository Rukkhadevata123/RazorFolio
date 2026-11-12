# RazorFolio

基于 C# / .NET 10、Markdig 和预编译 Razor 模板的静态站点生成器。命令名为 `rf`。

当前已实现最小的 `build`：递归读取站点 `content/` 下的 `.md` 文件，解析可选的 YAML front matter，用 Markdig 转换正文，再通过默认 Razor 主题生成完整 HTML，写入 `public/` 并保留相对目录。`static/` 中的文件也递归复制到 `public/`。构建成功后按生成清单清理过期输出。Markdown 支持 CommonMark、管道表格、任务列表和脚注。

例如 `content/Linux/hello.md` 输出为 `public/Linux/hello.html`，不同目录可有同名文章。文章扩展名 `.md` 不区分大小写。子目录文章引用资源时需按页面位置使用 `../images/example.svg` 等相对路径。

`preview` 可通过本地 HTTP 服务查看已有产物，`new` 可初始化独立站点。`build` 的自定义输出目录尚未实现。

## 创建站点

```bash
dotnet run --project src/RazorFolio.Cli -- new ~/my-new-blog
dotnet run --project src/RazorFolio.Cli -- dev --site ~/my-new-blog
```

`new` 接受不存在或已有但为空的目录，生成 `razorfolio.json`、`content/hello.md`、空的 `static/` 和忽略产物的 `.gitignore`。它不创建 Git 仓库，不执行构建，也不复制生成器源码。非空目录会报错，不提供强制覆盖选项。

参数解析位于 `Commands/NewCommand.cs`，站点文件创建位于 `Content/SiteInitializer.cs`。写入文件使用 `CreateNew`，已有同名文件不会被覆盖；若写入过程中失败，可能留下部分文件，需检查后再重试。

## 项目结构

| 项目 | 计划职责 |
| --- | --- |
| `src/RazorFolio.Core` | 文章模型、Markdown 处理、站点组织与生成流程 |
| `src/RazorFolio.Cli` | `rf` 命令、本地开发与预览服务 |
| `src/RazorFolio.Themes.Default` | 随工具预编译的默认 Razor 主题 |

默认主题使用 Razor SDK 预编译 `Views/Article.cshtml`。CLI 通过项目引用携带默认主题，第一版不加载外部主题插件。

`Views/_ViewStart.cshtml` 为页面选择共享的 `Views/Shared/_Layout.cshtml`。布局负责 HTML 外壳、字符集、viewport 和基础排版；文章页与首页只提供正文和 `ViewData["Title"]`，布局通过 `RenderBody()` 插入正文。修改主题后需要重新编译并构建站点。

基础 CSS 位于默认主题的 `Styles/theme.css`，包含正文宽度、行距、图片缩放和代码块横向滚动，并通过 `color-scheme` 跟随系统深浅色偏好。CSS 作为嵌入资源随主题程序集和工具包携带，构建站点时写入 `assets/razorfolio.css`，所有页面通过共享布局的 `<link rel="stylesheet">` 引用。

Core 中的 `IThemeAsset` 只约定资源的相对输出路径和读取流；`DefaultStylesheet` 负责从默认主题程序集打开 CSS，`SiteBuilder` 负责复制、生成相对 URL 并登记到输出清单。顶层页面使用 `./assets/razorfolio.css`，子目录文章自动使用 `../assets/razorfolio.css` 等路径，支持站点部署子路径。主题负责资源内容，Core 不需要知道程序集里的资源名称。

当前只输出这一份主题样式，不执行 Sass 编译、压缩或文件名指纹处理。修改主题 CSS 后需要重新编译工具并构建站点；已安装工具中的 CSS 随工具更新。开发模式同样将样式写入自己的输出目录。静态资源或文章与主题样式的输出路径冲突时会报错，不隐式覆盖主题。

调整单个博客的外观时，可以创建 `static/custom.css`，不需要修改工具或配置文件。例如：

```css
body {
    max-width: 56rem;
}
```

构建器从本次复制的静态资源中识别这个文件，在所有页面的默认样式之后添加它的 `<link>`。自定义样式仍遵循 CSS 层叠和选择器优先级规则。首页、文章及标签归档都使用相对 URL，支持多层目录和站点部署子路径。其他位置或文件名的 CSS 仍会照常复制，但不会自动加载。

`dev` 会监听 `static/custom.css` 的创建、修改和删除，重建后整页刷新，无需重新编译工具。删除文件后会移除页面中的链接，并按清单清理旧输出；手动放在输出目录中的同名文件不会被自动引用。各页面模型通过 `StylesheetUrls` 按顺序携带样式链接，共享布局统一输出。

`Views/Home.cshtml` 生成根目录的 `index.html`，列出所有文章标题和链接，按发布日期从新到旧排序，无日期文章在后；同一天或都无日期时按输出 URL 排序。没有文章时显示空状态；构建报告的页面数包含首页和生成的标签归档页。根目录的 `index.html` 保留给自动首页，`content/index.md` 或与首页路径冲突的静态资源会报错。

首页链接按相对路径生成，各路径段分别进行 URL 编码，支持中文、空格和 `#` 等文件名字符。

文章页提供“返回首页”链接：顶层文章使用 `./index.html`，`Linux/hello.html` 使用 `../index.html`，更深目录依次增加 `../`。首页到文章、文章到首页的链接统一由 `Output/PageUrl.cs` 根据输出文件位置计算，再通过页面模型传给主题。因此这两类导航在站点部署到 `/仓库名/` 等子路径时也能使用，不需要写死域名根路径。Markdown 正文中手写的 `/images/example.svg` 等根路径链接仍由作者自行处理。

文章之间可以写 `[示例](Linux/hello.md)`，Markdig 在 HTML 输出阶段将本地 `.md` 链接改成 `.html`，包括引用式链接。路径和文件名保持不变，查询参数与锚点也保留，例如 `hello.md?view=full#section` 变为 `hello.html?view=full#section`。不会重新计算旧锚点，也不会检查目标文章是否存在。

带协议的外部链接、`//example.com/hello.md`、纯锚点及 `.png`、`.txt` 等资源链接不改写。当前把本地 `.md` 目标都视为文章；如果需要提供 Markdown 源文件下载，需要另外设计下载路径或规则。原始 HTML 中的链接和代码块文本不参与这项 Markdown 链接转换。

## 开发

### 打包和安装工具

CLI 可以打包为 .NET 工具。当前包名是 `RazorFolio`，开发版本为 `0.1.0-dev.1`，安装后的命令是 `rf`。在仓库根目录执行：

```bash
dotnet pack src/RazorFolio.Cli -c Release -o artifacts/packages
dotnet tool install RazorFolio --version 0.1.0-dev.1 \
  --source ./artifacts/packages --tool-path ./artifacts/tools
./artifacts/tools/rf --help
./artifacts/tools/rf --version
```

`dotnet pack` 编译工具并生成本地 `.nupkg`；安装命令通过 `--source` 只读取本地包目录，通过 `--tool-path` 将工具安装到指定位置。`artifacts/` 已被 Git 忽略。这里的步骤不发布到 NuGet，也不进行全局安装。

将安装目录加入当前终端的 PATH 后，可以直接使用短命令：

```bash
export PATH="$PWD/artifacts/tools:$PATH"
rf new ~/my-new-blog
rf build --site ~/my-new-blog
rf dev --site ~/my-new-blog
```

工具包包含 Core、预编译默认主题及其嵌入样式、Markdig 和 YamlDotNet，使用时不需要生成器源码、`.cshtml` 或主题 CSS 源文件。它仍依赖 .NET 10 和 ASP.NET Core 10 共享运行时；安装工具需要 .NET SDK，本项目开发环境使用 .NET 10 SDK。安装目录中还有工具依赖文件，不能只复制 `rf` 入口。

修改 C# 或主题后，已安装的工具不会自动更新。日常开发继续使用下文的 `dotnet run`；验证新包时可用 `dotnet pack ... -p:Version=0.1.0-dev.2` 打出不同版本，再用 `dotnet tool update` 配合相同的 `--source`、`--tool-path` 和新的 `--version` 更新，避免反复复用同一版本的缓存。

`rf --version` 显示当前程序的版本，例如 `RazorFolio 0.1.0-dev.1`，不需要指定站点。版本来自 SDK 写入程序集的 `AssemblyInformationalVersion`，保留开发版本后缀；构建时若包含 Git 提交信息，还会带上 `+提交哈希`。读取的是运行程序内的版本，与当前工作目录无关。

### 从源码运行

在仓库根目录编译程序、查看帮助：

```bash
dotnet build
dotnet run --project src/RazorFolio.Cli -- --help
dotnet run --project src/RazorFolio.Cli -- --version
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

front matter 必须位于文件开头。当前读取 `title`、`description`、`date`、`draft` 和 `tags`，其他字段暂时忽略；没有 `title`、值为 null 或空白时，回退到文件名。YamlDotNet 负责解析 YAML，格式错误及重复键会导致构建失败。文件名和输出 URL 不随标题变化。

可选的 `description` 用作首页文章简介，并写入文章页的 `<meta name="description">`，不额外插入文章正文。省略、null 或纯空白时不输出简介和对应的 meta 标签，也不自动截取正文。首尾空白会去掉，内容按纯文本处理，由 Razor 编码，不解析 Markdown 或 HTML。例如：

```yaml
---
title: 用 Podman 搭建开发环境
description: >-
  记录容器创建、目录挂载和日常使用的命令。
---
```

`>-` 是 YAML 的折叠块语法，可以分行书写简介；YamlDotNet 负责读取，`ArticleParser` 统一处理空值，再通过页面模型传给主题。

可选的 `date: 2026-11-12` 表示发布日期，使用 `DateOnly` 存储，不包含时间和时区。省略或设为 null 时不显示日期；显式填写时必须是有效的 `yyyy-MM-dd`，空字符串、非法日期和完整时间戳会报错并指出文章路径。日期在首页及文章页通过 `<time datetime="...">` 输出；未来日期目前照常发布，不表示定时发布。

`draft: true` 将文章标为草稿，省略或设为 `false` 则正常发布。正式 `build` 不生成草稿及其首页链接；`dev` 包含草稿，并在首页和文章页标注。草稿仍需使用有效的 YAML 和日期。

`tags` 接受 YAML 列表，例如 `tags: [Linux, Podman]`，也可以逐行书写：

```yaml
tags:
  - Linux
  - Podman
```

标签会去掉首尾空白，忽略 null 和空白项，重复名称只保留第一次出现的位置。去重区分大小写，因此 `Linux` 和 `linux` 是两个标签。省略 `tags`、设为 null 或 `[]` 表示没有标签；`tags: Linux` 这样的单个字符串会报错，不自动按逗号拆分。

文章页在正文前显示标签链接；没有标签时不输出列表。标签名称由 Razor 按纯文本编码。解析器将整理后的名称作为只读集合存入 `ArticleInfo.Tags`，构建时再根据文章所在目录生成 `ArticlePage.Tags` 中的链接。

当前参与构建的文章只要有标签，就生成根目录的 `tags.html`，首页也会显示“按标签浏览”。归档将每个标签列为一节，按名称排序（使用区分大小写的 Ordinal 比较，不按拼音排序）；每节中的文章沿用首页的日期排序、草稿标记和简介。`build` 排除草稿及其独有标签，`dev` 包含草稿。没有标签时不生成归档，下次成功构建会按生成清单清理旧归档。

文章里的标签链接跳到 `tags.html` 的对应标题。锚点由标签名的 UTF-8 字节编码为十六进制生成，支持中文、空格、`C#`、斜杠等名称，也能区分大小写；添加其他标签不会改变已有标签的锚点。链接使用相对路径，支持多层文章目录和站点部署子路径。当前只有一个归档页，不分页，也不生成每个标签的独立页面。

生成归档时，`tags.html` 路径保留给工具；与 `content/tags.md`、`content/tags.html/` 下文章或静态资源的路径冲突会报错。`Views/Tags.cshtml` 负责归档结构，首页和归档共用 `Views/Shared/_ArticleList.cshtml` 输出文章列表。

Markdown 扩展集中配置在 `Content/MarkdownPipelineFactory.cs`。`ArticleParser` 与 `MarkdownRenderer` 接收同一条管线，分别解析语法树和渲染 HTML；这样表格、任务列表、脚注的解析器与渲染器能一起生效。后续扩展按需添加，不一次启用所有扩展。任务列表是文章中的静态勾选状态，不保存浏览器交互。

支持 `::: tip`、`::: warning` 和 `::: details` 容器，类型后可写纯文本标题；省略时分别显示“提示”“注意”“详情”。正文继续按 Markdown 渲染，标题按普通文本编码，暂不解析标题内的 Markdown。嵌套时外层使用更多冒号，例如外层 `:::: details`、内层 `::: tip`，结束围栏与各自开头对应。

```markdown
::: tip 小提示
正文可以包含 **强调**、列表和代码块。
:::

::: details 展开配置
这里的内容默认折叠。
:::
```

Markdig 负责容器语法，`Rendering/BlogContainerRenderer.cs` 将提示框输出为 `aside`、折叠块输出为原生 `details/summary`，不需要 JavaScript。其他容器名保留 Markdig 的默认通用容器输出。这只是常用语法适配，不代表完整兼容 Hope 的所有容器功能。

标题通过 Markdig 自动生成锚点 ID。需要正文目录时，将 `[TOC]` 单独写成一个段落，并在前后留空行；目录列出二至六级标题，排除文章一级标题，层级由标题级别决定。代码块里的 `[TOC]` 保持原样。重复标题由 Markdig 分配不同的 ID；修改标题可能改变锚点，当前不承诺兼容旧 VuePress 文章的锚点规则。

```markdown
# 文章标题

[TOC]

## 安装

### Linux

## 使用
```

目录是静态 HTML 链接，没有滚动高亮和侧栏逻辑。没有 `[TOC]` 时不输出目录，但标题仍有锚点。

阅读代码可以依次看 `Program.cs`、`Commands/SiteCommand.cs`、`SiteBuilder.cs`、`Content/ArticleParser.cs` 和 `Rendering/MarkdownRenderer.cs`。CLI 处理参数与退出码，Core 处理文件生成；`ArticleParser` 管理解析管线、提取元数据，并从语法树中移除 front matter，`MarkdownRenderer` 将剩余语法树渲染为正文 HTML，避免重复解析 Markdown。

文章信息分为两层：`ArticleMetadata` 接收 YAML 原始值，`ArticleParser` 校验日期、补上默认标题、处理空简介并整理标签后，生成 `ArticleInfo`。这个 record 集中保存标题、日期、草稿状态、简介和标签；`ParsedArticle`、`ArticlePage` 和 `ArticleLink` 通过 `Info` 属性携带同一个实例，不再逐项复制这些字段。Markdown 语法树、正文 HTML 和链接地址仍由各自的模型持有。

页面渲染通过 Core 中的 `ArticlePage` 和 `IPageRenderer` 连接默认主题。`SiteBuilder` 提交文章信息、正文 HTML、站点配置和首页链接，不接触 ASP.NET Core；主题中的 `RazorPageRenderer` 准备 MVC 视图上下文，再执行预编译模板。例如文章模板通过 `Model.Info.Title` 读取标题。CLI 使用 ASP.NET Core 的服务容器组装渲染器，但不会启动 HTTP 监听。

模板用 `@Model.Info.Title` 输出普通文本，由 Razor 编码；`@Html.Raw(Model.BodyHtml)` 原样插入作者 Markdown 生成的 HTML。当前按可信本地文章处理，不执行 HTML 清洗。

## 预览已有产物

图片和附件放在 `static/`，例如 `static/images/example.svg` 会复制到 `public/images/example.svg`。顶层文章可写 `![示例](images/example.svg)`；复制不会改写文件内容，也不会压缩或编译 CSS、JS。

`static/` 可以不存在。已有资源在再次构建时覆盖更新；静态资源与本次生成的页面或主题样式路径冲突时会报错。删除源资源后，下次成功构建会清理生成清单中对应的旧输出。复制逻辑位于 `Assets/StaticAssetCopier.cs`。

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

默认访问 `http://127.0.0.1:8080/`，可在站点参数后加 `--port 8081`。启动时先构建到 `.razorfolio/dev/` 并提供该目录的 HTTP 服务，随后监听 `content/`、`static/` 和 `razorfolio.json`，变化后串行全站重建。文件事件会合并，写入 `.razorfolio/` 和 `public/` 不触发再次构建。监听器启动后会再同步构建一次，覆盖启动间隙可能发生的修改。

构建成功后浏览器自动整页刷新；改 `.cshtml` 或 C# 仍需要重新编译启动。构建错误会报告，但监听继续，修正文件后能再次生成。构建直接写入当前目标目录，中途失败时可能留下部分更新；删除或重命名源文件后会在构建成功时清理对应清单中的旧产物。

`dev` 包含草稿，`build` 排除草稿并写入 `public/`，两者使用独立的输出目录和生成清单，可以在开发服务运行时执行正式构建。仍不支持同时运行多个写入同一目标的构建进程。旧版 `dev` 留在 `public/` 中且已登记的草稿页面，会在下一次正式 `build` 时清理。

`SiteBuildTarget` 集中确定输出目录、清单路径和是否包含草稿；CLI 选择目标后，首次构建和监听重建使用同一个对象，HTTP 服务也读取它的输出目录。`preview` 默认展示 `public/` 中已有的正式产物，不做草稿过滤。静态附件不按文章草稿状态筛选，正文中指向草稿的手写链接也暂不校验。

`Hosting/LiveReload.cs` 仅在开发服务的 HTML 响应中插入刷新脚本，通过 `/_rf/events` 的 SSE 连接通知浏览器。每次成功构建更新版本号，失败时不通知刷新；连接恢复后也会比较版本号。开发响应禁用缓存，磁盘上的 HTML 不包含刷新脚本，`build` 和 `preview` 不启用该端点。当前使用整页刷新，不保留页面内交互状态。

`Hosting/SiteWatcher.cs` 通过 `FileSystemWatcher` 收集事件，用有界队列串行处理；作为后台服务随预览服务器一起停止。Ctrl+C 同时停止服务和监听。

`~/razorfolio-blog` 用于示例文章和可清理的实验，不存放需要长期保存的唯一副本。普通用户也使用独立站点目录，不需要把文章放进生成器源码仓库。

## 站点约定

站点根目录可放置 `razorfolio.json`：

```json
{
  "title": "我的博客",
  "language": "zh-CN",
  "url": "https://example.com/blog/"
}
```

`title` 用于首页标题和文章标签页标题的后缀；`language` 写入 HTML 的 `lang` 属性，不自动翻译主题文案。配置文件不存在或这两个字段省略时，分别使用 `RazorFolio` 和 `zh-CN`；显式设为 null 或空白会报错。未知字段及无效 JSON 也会报错，错误包含配置文件路径。配置由 `Configuration/SiteConfigurationLoader.cs` 每次构建读取一次，再通过页面模型传给主题。

可选的 `url` 表示站点正式部署地址，支持根域名和 `/blog/` 等子路径。省略或设为 null 时不输出 canonical，`rf new` 也不会替作者填写域名。填写时必须是完整的 HTTP(S) URL，不能带用户名密码、查询参数或片段；空字符串和相对路径会报错。首尾空白会去掉，末尾缺少 `/` 时自动补上，将配置路径作为站点目录。

配置后，各页面在 `<head>` 中输出 `<link rel="canonical">`：上述示例的首页为 `https://example.com/blog/`，`Linux/hello.html` 为 `https://example.com/blog/Linux/hello.html`，标签归档为 `https://example.com/blog/tags.html`。中文、空格和文件名中的特殊字符按 URL 编码。canonical 声明页面的首选正式地址，不执行跳转，也不改变输出目录、站内相对链接或本地监听地址。

`dev` 中的已发布文章仍引用配置的正式地址，草稿页不输出 canonical；`preview` 继续原样提供已有 HTML。canonical 和 sitemap 的绝对地址都由 `Output/PageUrl.cs` 生成。当前尚未生成 RSS。

配置 `url` 后，正式 `build` 还会生成 `public/sitemap.xml`，包含首页、已发布文章和存在时的标签归档页。地址与各页面的 canonical 一致，支持部署子路径；不包含草稿、CSS、附件、静态目录中的手写 HTML 或标签锚点，也不计入构建报告的 HTML 页面数。`dev` 不自动生成 sitemap，检查正式产物时使用 `build` 和 `preview`。

`Output/SitemapWriter.cs` 使用 .NET 的 XML API 输出 UTF-8 文件，处理命名空间和 XML 转义。第一版只写必需的 `loc`，不填写 `lastmod`、`changefreq` 或 `priority`；发布日期不能代替页面最后修改时间。格式与单文件限制参照 [Sitemaps 协议](https://www.sitemaps.org/protocol.html)：最多 50,000 条地址，每条短于 2,048 字符，文件不超过 50 MB，超限时报错，暂不拆分文件。

生成 sitemap 时会检查 `sitemap.xml` 的路径冲突，并将文件登记到生成清单。删除 `url` 或设为 null 后，下次正式构建会清理此前生成的 sitemap。未启用自动生成时，可以通过 `static/sitemap.xml` 提供自己的文件。当前不自动生成 `robots.txt`，也不向搜索引擎提交。

- `content/`：Markdown 文章。
- `static/`：原样复制的图片、附件等资源。
- `razorfolio.json`：站点配置。
- `.razorfolio/`：开发产物和缓存。
- `public/`：正式网站输出，相对路径按站点根目录解析。

`dev` 监听文件、重新生成页面并自动刷新浏览器，默认访问地址为 `http://127.0.0.1:8080`。`build` 生成网站后退出。`preview` 只提供正式产物，不重新生成内容。

`dotnet build` 和 `dotnet publish` 处理生成器程序；`rf build` 处理博客站点。预编译 Razor 模板变化需要重新编译并生成页面；后续再接入 `dotnet watch`。

## 生成清单与清理

每次成功构建会记录本次页面、sitemap、主题样式和静态资源的相对输出路径。正式构建使用 `.razorfolio/outputs.json` 管理 `public/`，开发构建使用 `.razorfolio/dev-outputs.json` 管理 `.razorfolio/dev/`，清单放在各自的 HTTP 服务目录之外。

下一次成功生成全部页面后，只删除对应旧清单中不再出现的文件，再替换该清单。手动放在输出目录中、从未被生成器记录的文件不会被清理；如果文件路径曾经由生成器管理，之后仍按生成产物处理。开发构建的清理不会删除正式产物，正式构建也不会清理开发产物。

首次启用清单时不会猜测历史文件归属，清单丢失后也不会扫描删除旧文件。空目录暂时保留。清单损坏或包含越界路径会报错；清理不会沿输出中的符号链接删除文件。构建中途失败不执行末尾清理、不更新清单，但已经写入的文件暂不回滚。

`Output/OutputManifest.cs` 负责清单和清理，`StaticAssetCopier` 返回复制过的输出路径，`SiteBuilder` 汇总所有产物。`.razorfolio/` 的变化不触发开发模式重新构建。
