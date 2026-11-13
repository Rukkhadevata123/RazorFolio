# RazorFolio

C# / .NET 11 静态博客生成器，命令名为 `rf`。使用 Markdig、YamlDotNet 和构建时编译的 Razor 模板。

支持卡片主题、明暗配色、站内搜索、代码高亮与复制、Mermaid 图表、数学公式、文章目录、分页、分类/标签/年份归档、草稿预览、RSS 和 sitemap。仍在开发中。

## 快速开始

安装 .NET SDK 11.0.100 或同一 11.0.1xx 功能带的更新补丁（见 `global.json`），在仓库根目录运行：

```bash
dotnet build
dotnet run --project src/RazorFolio.Cli -- new ~/my-blog
dotnet run --project src/RazorFolio.Cli -- dev --site ~/my-blog
```

打开 `http://127.0.0.1:8080/`。`new` 的目标目录须不存在或为空；博客内容独立于生成器源码。

以下命令从源码运行时，将 `rf` 换成 `dotnet run --project src/RazorFolio.Cli --`：

| 命令 | 用途 |
| --- | --- |
| `rf new <directory>` | 创建站点 |
| `rf dev --site <directory>` | 包含草稿，监听修改并刷新 |
| `rf build --site <directory>` | 生成正式网站，排除草稿 |
| `rf preview --site <directory>` | 预览已有产物 |
| `rf --help` / `rf --version` | 帮助与版本 |

`dev` 和 `preview` 可用 `--port` 指定端口。

## 站点配置

```text
my-blog/
├── razorfolio.json   # 配置，可省略
├── content/          # Markdown，支持子目录
├── static/           # 图片、附件、custom.css、favicon.svg
├── public/           # 正式输出
└── .razorfolio/       # 开发输出与生成清单
```

`razorfolio.json` 示例：

```json
{
  "title": "我的博客",
  "description": "记录开发与日常。",
  "language": "zh-CN",
  "pageSize": 20,
  "url": "https://example.com/blog/",
  "navigation": [
    { "text": "首页", "link": "index.html" },
    { "text": "关于", "link": "about.md" }
  ],
  "theme": {
    "brand": { "text": "我的博客", "image": "images/logo.png" },
    "hero": {
      "title": "欢迎来访",
      "description": "首页介绍。",
      "image": "images/hero.png",
      "fullScreen": true,
      "background": { "mode": "bing", "image": "images/background.jpg" }
    },
    "profile": {
      "name": "作者名",
      "avatar": "images/avatar.png",
      "description": "作者短句",
      "bio": "自我介绍。",
      "links": [{ "text": "关于", "link": "about.md" }]
    },
    "sidebar": [{ "text": "第一篇文章", "link": "hello.md" }],
    "footer": "页脚文字"
  }
}
```

图片放在 `static/images/`，不需要的配置可省略。

- `brand`、`hero`、`profile` 分别控制左上角标识、首页首屏和作者卡片，文字与头像互相独立。
- 省略 `hero` 显示简洁标题区；省略 `profile` 隐藏作者卡片。首屏仅在首页第一页显示。
- `background.mode` 支持 `none`、`image`、`bing`。固定图片模式须填写 `image`；Bing 使用第三方壁纸服务，失败时保留指定图片或纯色。
- 顶栏读取 `navigation`；文章侧栏只显示 `theme.sidebar`，未配置时隐藏。
- 配置中的站内链接和图片按站点根目录解析，也支持 HTTP(S) 地址；`.md` 链接跟随文章的自定义 URL。
- `static/custom.css` 覆盖默认样式，`static/favicon.svg` 设置图标。配置 `url` 后，正式构建生成 canonical、RSS 和 sitemap。

完整字段见 [SiteConfiguration](src/RazorFolio.Core/Configuration/SiteConfiguration.cs) 和 [ThemeOptions](src/RazorFolio.Core/Configuration/ThemeOptions.cs)。未知配置字段会报错。

## 写文章

`content/notes/hello.md` 默认生成 `public/notes/hello.html`：

```markdown
---
title: 第一篇文章
description: 列表中的简短介绍。
date: 2026-11-13
tags: [C#, 工具]
category: [开发记录]
draft: false
---

正文从这里开始。

## 安装 {#installation}

这一节可通过文章地址后的 #installation 访问。
```

| 字段 | 说明 |
| --- | --- |
| `title` / `description` | 标题与可选摘要；不自动截取摘要 |
| `date` | 可选，支持 `yyyy-MM-dd` 和旧站的 `yyyy-MM-dd HH:mm:ss`；按日期倒序排列，时间校验后仅取日期，不作时区转换 |
| `tags` / `category` | 标签与并列分类列表 |
| `draft: true` | 仅在开发预览中生成 |
| `article: false` | 普通页面，不进入文章列表、归档和 RSS |
| `url: /posts/hello.html` | 自定义站内输出地址，须以 `.html` 结尾 |
| `aliases: [/old.html]` | 为旧地址生成静态跳转页 |

正文开头的 H1 会移到文章头部，保留格式与锚点；没有开头 H1 时使用 `title`，未填标题则使用文件名。主题自动生成 H2–H6 目录，`{#id}` 可固定标题锚点。

支持表格、任务列表、脚注、`[TOC]` 及 `tip` / `warning` / `details` 容器。代码围栏注明语言后提供高亮、语言标签和复制按钮；未知语言保留纯文本。

`mermaid` 代码围栏在浏览器中绘制图表，随明暗模式切换；宽图可横向滚动，源码可展开复制。使用本地 Mermaid Tiny，仅含图表的页面加载运行库。禁用 JavaScript 或渲染失败时保留源码；当前不支持 mindmap、architecture、KaTeX 和 ELK 布局。

数学公式使用 `$...$` 行内语法，或以独占行的 `$$` 包围块公式；行内公式与周围文字用空格或标点分隔。通过本地 KaTeX 排版，支持表格内公式、中文 `\text{}` 和 `aligned` 多行对齐；长块公式可横向滚动。运行库、样式和字体按需加载，禁用 JavaScript 或渲染失败时保留 TeX 源码。代码中的美元符号不解析，正文中的字面美元符号写作 `\$`；暂不支持 `\(...\)` / `\[...\]` 输入语法、跨公式共享宏或完整 LaTeX。

正文相对 `.md` 链接按源文件目录解析。图片和附件放在 `static/`；手写 HTML 的链接不改写。

`build` / `dev` 检查 Markdown 中本地链接、图片和附件是否属于本次产物，缺失、大小写不符或越出部署子路径时输出带源文件行列号的警告，仍继续构建。旧产物不会掩盖断链，正式构建会提示指向未发布草稿的链接。以 `/` 开头的地址按域名根目录解析，部署到 `/blog/` 时应带此前缀或使用相对地址。

## 开发与打包

修改 C#、Razor 模板或内置资源后，需要重新编译并重启 `dev`。`dotnet build` 构建生成器，`rf build` 生成网站；发布时上传 `public/`。

`.cshtml` 在构建时生成强类型 C#，运行时直接组合页面、布局和局部模板，无需 MVC 视图引擎。模板编译器只用于源码构建，不进入工具包；SDK 编译器接口与模板约定见 [TemplateCompiler](src/RazorFolio.TemplateCompiler/README.md)。模板层已通过 NativeAOT 验证，完整 CLI 的 JSON/YAML 等适配仍待完成。

```bash
dotnet pack src/RazorFolio.Cli -c Release -o artifacts/packages
dotnet tool install RazorFolio --version 0.1.0-dev.1 --source ./artifacts/packages --tool-path ./artifacts/tools
./artifacts/tools/rf --version
```

运行工具需要 .NET 11 与 ASP.NET Core 11。更新包时递增版本，避免复用旧缓存。

| 项目 | 职责 |
| --- | --- |
| [Cli](src/RazorFolio.Cli) | 命令、预览、监听 |
| [Core](src/RazorFolio.Core) | 配置、解析、生成；入口为 [SiteBuilder](src/RazorFolio.Core/SiteBuilder.cs) |
| [Themes.Default](src/RazorFolio.Themes.Default) | Razor 模板、样式和脚本 |
| [TemplateCompiler](src/RazorFolio.TemplateCompiler) | 仅构建时使用的 Razor → C# 编译工具 |

顶栏搜索覆盖标题、标签和正文，多个关键词用空格分隔，结果需全部匹配。构建输出 `search-index.json`，收录文章和普通页，正式排除草稿；原始 HTML 块暂不索引。搜索需要 JavaScript，并通过 `dev`、`preview` 或网站服务器访问。

构建检查输出冲突，只清理生成清单登记的旧文件；失败不回滚，勿并发构建同一输出目录。正文 HTML 按可信作者内容处理。链接检查暂不涵盖锚点、手写 HTML、配置中的链接及外部网址；尚未实现增量构建或完整 Hope 语法兼容。

## License

[MIT](LICENSE)。浏览器依赖的来源与许可见 [vendor/README.md](src/RazorFolio.Themes.Default/Scripts/vendor/README.md)。
