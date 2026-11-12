# RazorFolio

用 C# / .NET 10 编写的静态站点生成器，命令名为 `rf`。Markdown 由 Markdig 处理，YAML 由 YamlDotNet 解析，默认主题使用预编译 Razor 模板。

目前支持首页分页、年份/分类/标签归档、相邻文章导航、草稿预览、RSS、sitemap，以及本地监听和整页刷新。项目仍在逐步开发。

## 快速开始

开发需要 .NET 10 SDK。[global.json](global.json) 会选择本机已安装的最新稳定 .NET 10 SDK，允许更新功能带和补丁版本。其中 `10.0.100` 是 SDK 选择的最低基线，不要求单独安装这个版本。

在仓库根目录执行：

```bash
dotnet build
dotnet run --project src/RazorFolio.Cli -- new ~/my-new-blog
dotnet run --project src/RazorFolio.Cli -- dev --site ~/my-new-blog
```

打开 `http://127.0.0.1:8080/`，按 Ctrl+C 停止。`new` 只接受不存在或为空的目录；文章放在独立站点中，不需要放进工具源码仓库。

生成并检查正式产物：

```bash
dotnet run --project src/RazorFolio.Cli -- build --site ~/my-new-blog
dotnet run --project src/RazorFolio.Cli -- preview --site ~/my-new-blog
```

安装工具后，下表中的命令可直接使用；从源码运行时，将 `rf` 换成 `dotnet run --project src/RazorFolio.Cli --`。

| 命令 | 行为 |
| --- | --- |
| `rf new <directory>` | 创建配置、示例文章、静态资源目录和 `.gitignore` |
| `rf dev --site <directory> [--port 8080]` | 包含草稿，监听内容变化并刷新浏览器 |
| `rf build --site <directory>` | 排除草稿，生成正式网站后退出 |
| `rf preview --site <directory> [--directory public] [--port 8080]` | 只提供已有文件，不构建或监听 |
| `rf --help` / `rf --version` | 查看用法或当前程序版本 |

## 站点目录与配置

| 路径 | 用途 |
| --- | --- |
| `content/` | Markdown 文章，支持子目录 |
| `static/` | 原样复制的图片、CSS、附件等 |
| `razorfolio.json` | 站点配置，可省略 |
| `public/` | 正式输出 |
| `.razorfolio/dev/` | 开发输出 |
| `.razorfolio/*.json` | 生成清单，用于清理旧文件 |

`razorfolio.json` 示例：

```json
{
  "title": "我的博客",
  "language": "zh-CN",
  "pageSize": 20,
  "navigation": [
    { "text": "首页", "link": "index.html" },
    { "text": "关于", "link": "about.html" },
    { "text": "GitHub", "link": "https://github.com/" }
  ],
  "description": "记录开发环境、工具使用和日常折腾。",
  "url": "https://example.com/blog/"
}
```

| 字段 | 默认值 | 用途 |
| --- | --- | --- |
| `title` | `RazorFolio` | 站点名称 |
| `language` | `zh-CN` | HTML 的 `lang`，不翻译主题文案 |
| `pageSize` | `20` | 首页每页文章数，必须是正整数 |
| `navigation` | `[]` | 按顺序显示的全站导航，每项包含非空的 `text` 和 `link` |
| `description` | 无 | 首页简介、首页 meta description 和 RSS 频道说明 |
| `url` | 无 | 正式部署地址，支持 `/blog/` 等子路径 |

未知配置字段会报错。`title`、`language` 不能为空；`description` 留空时首页省略简介，RSS 频道说明回退到站点名称。`url` 可省略或设为 null，填写时须为完整 HTTP(S) 地址，不带认证信息、查询参数或片段，末尾 `/` 会自动补齐。

导航中的站内 `link` 按站点根目录解析，使用输出地址（如 `about.html`、`Linux/hello.html`），不自动转换 `.md`。`/` 指向首页，`/about.html` 与 `about.html` 相同；开头的 `/` 在导航配置中表示站点根目录，不是域名根目录。支持查询参数和锚点，中文及空格会编码；生成相对链接以兼容子目录页面和部署子路径。

外部导航支持完整 HTTP(S) 地址，默认在当前标签页打开。不支持 `//` 地址、其他协议、反斜杠或 `.`/`..` 路径段。省略 `navigation` 或设置 `[]` 时不显示主导航；`null`、空项和未知字段会报错。当前不校验导航目标是否存在，链接到草稿也不会使草稿在正式构建中发布。

## 写文章

`content/Linux/hello.md` 会生成 `public/Linux/hello.html`。文件开头可写 YAML front matter：

```markdown
---
title: 用 Podman 搭建开发环境
description: 记录容器创建、目录挂载和日常使用的命令。
date: 2026-11-12
draft: false
tags: [Linux, Podman]
category: [Linux Daily Use]
---

# 正文标题

[TOC]

## 安装

::: tip 小提示
正文可以包含 **强调**、列表和代码块。
:::
```

- `title` 未填写时使用文件名；正文标题由 Markdown 自己提供。
- `description` 是文章的纯文本简介，与站点简介分开；不自动截取正文。
- `date` 可省略，填写时必须是 `yyyy-MM-dd`。它不含时间、时区，未来日期也照常发布。
- `draft: true` 仅在 `dev` 中生成。
- `article` 默认为 `true`；设为 `false` 时生成普通页面，适合关于页、收藏夹等。
- `tags` 使用列表，去掉空项和重复项，区分大小写。
- `category` 使用列表，如 `category: [Linux, 开发环境]`；首尾空白会去除，忽略空项并去重，区分大小写。多个分类是并列关系，不表示层级；省略或为 `null` 时无分类。

普通页面的输出路径和 Markdown 能力与文章相同，但不进入首页分页、年份/分类/标签归档、RSS 或上一篇/下一篇，也不显示日期、分类和标签。配置 `url` 后仍输出 canonical 并收录到正式 sitemap。`draft: true` 对普通页面同样生效；普通页面不是私密页面。

例如创建 `content/about.md`：

```markdown
---
title: 关于
article: false
---

# 关于

这里介绍作者和网站。
```

通过 `about.html`、其他页面中的 `[关于](about.md)` 或配置站点导航访问。

支持表格、任务列表、脚注，以及 `::: tip`、`::: warning`、`::: details` 容器。`[TOC]` 单独成段时列出二至六级标题；嵌套容器的外层使用更多冒号。

二至六级标题旁提供 `#` 锚点链接，方便分享某一节。鼠标悬停或键盘聚焦时显示，触屏设备默认显示；链接沿用目录中的标题 ID。

代码块提供“复制代码”按钮，保留原文的缩进和换行，并提示复制结果。需要启用 JavaScript 且浏览器支持剪贴板写入（通常为 HTTPS 或本机预览）；接口不可用时不显示按钮，仍可手动选择复制。

文章按日期从新到旧排列，无日期的在后，同日期按输出 URL 排序。首页依次生成 `index.html`、`page/2.html` 等；“上一篇 / 下一篇”沿用全站顺序，标签归档集中在 `tags.html`。

每个已收录文章的标签还生成独立页面 `tags/<hash>.html`，文章标签及汇总页的标签标题链接到该页。文件名为清理后标签名的 UTF-8 SHA-256 小写十六进制值，长度固定且区分大小写；页面仍显示原标签名。独立页沿用全站文章顺序，不受首页分页限制，提供返回首页和全部标签入口。原 `tags.html` 的分组、文章列表及 `tag-...` 锚点继续保留。

普通页面和正式构建排除的草稿不产生标签页；开发预览可包含草稿标签。正式配置 `url` 时，标签页有 canonical 并加入 sitemap。标签改名或不再有收录文章时，清单清理旧标签页；改名不生成跳转。只有实际生成的标签页路径受保护，冲突的内容或静态文件会导致构建失败。

分类归档集中在 `categories.html`：分类名按 Ordinal 顺序排列，分类内文章沿用全站日期顺序，不受首页分页大小限制。首页和文章分类链接可进入归档；没有已收录文章的分类时不生成归档或入口。草稿分类仅在开发预览中出现，普通页面不参与分类。正式归档收录到 sitemap；删除最后一个分类后，清单会清理旧归档。

年份归档在 `archives.html`，首页各分页提供“按年份浏览”入口，也可将它加入站点导航。按 `date` 的年份从新到旧分组，组内沿用全站文章顺序；无日期文章放在最后的“未注明日期”组，不推断文件时间。归档包含全部已收录文章，不受首页分页大小限制；普通页面排除，草稿仅在开发预览中出现。有文章时才生成归档并预留该输出路径；最后一篇文章移除后，清单清理旧归档，首页入口随之消失。正式构建配置 `url` 时，归档有 canonical 并收录到 sitemap。

本地 Markdown 链接会从 `.md` 改成 `.html`，保留查询参数和锚点，但不检查目标是否存在。图片放在 `static/`，按页面位置引用，例如子目录文章使用 `../images/example.svg`。正文中手写的 `/images/...` 仍指向域名根路径。

## 主题与发布文件

默认主题和 CSS 随工具携带。站点可以通过以下文件调整外观，无需重新编译工具：

| 文件 | 效果 |
| --- | --- |
| `static/custom.css` | 在默认样式之后加载 |
| `static/favicon.svg` | 自动作为站点图标引用 |

配置 `url` 后，正式构建还会输出 canonical 链接、`sitemap.xml` 和 `feed.xml`，并显示 RSS 订阅入口。RSS 提供最近 20 篇已发布文章的标题、链接和可选简介；标签归档、RSS 和相邻文章导航不受首页分页大小限制。

`dev` 不自动生成 sitemap 或 RSS，草稿也不输出 canonical。RSS 暂不提供全文或 `pubDate`，sitemap 暂不填写 `lastmod`；当前的发布日期不足以代表精确发布时间或最后修改时间。

## 构建与预览约定

`dev` 监听 `content/`、`static/` 和配置文件，串行全站重建，成功后整页刷新。`dev` 和 `build` 使用独立目录与清单；服务默认只监听 `127.0.0.1`。修改 C#、Razor 模板或内置 CSS 后，需要重新编译并启动工具。

构建会检查输出路径冲突，成功后只清理生成清单中已登记的旧文件。未登记的手动文件不会被删除，空目录保留；构建失败时已写内容暂不回滚。同一输出目录不要并发构建。

Markdown 中的原始 HTML 按可信作者内容处理，不做清洗。目前尚无增量构建、外部主题插件、资源压缩或完整的 VuePress Hope 语法兼容。

## 打包为 `rf` 工具

```bash
dotnet pack src/RazorFolio.Cli -c Release -o artifacts/packages
dotnet tool install RazorFolio --version 0.1.0-dev.1 \
  --source ./artifacts/packages --tool-path ./artifacts/tools
./artifacts/tools/rf --version
```

将 `artifacts/tools` 加入 PATH 后可直接运行 `rf`。工具包包含默认主题和解析库，运行时仍需 .NET 10 与 ASP.NET Core 10；安装需要 .NET SDK，不能只复制 `rf` 入口文件。

修改工具后，已安装版本不会自动更新。试验新包时可用 `-p:Version=0.1.0-dev.2` 指定新版本，再通过 `dotnet tool update` 更新，避免复用旧版本缓存。

## 源码与检查

| 项目 | 职责 |
| --- | --- |
| [RazorFolio.Cli](src/RazorFolio.Cli) | 命令解析、预览服务、文件监听 |
| [RazorFolio.Core](src/RazorFolio.Core) | 配置、文章解析、路径与生成流程 |
| [RazorFolio.Themes.Default](src/RazorFolio.Themes.Default) | 预编译 Razor 视图和内置样式 |

生成流程从 [SiteBuilder.cs](src/RazorFolio.Core/SiteBuilder.cs) 看起。文章信息通过 `ArticleInfo` 传递，公共布局数据由 `PageContext` 提供。

可选的 ReSharper 命令行检查：

```bash
dotnet tool install JetBrains.ReSharper.GlobalTools --tool-path ./artifacts/resharper
./artifacts/resharper/jb inspectcode RazorFolio.slnx \
  --build --severity=INFO --format=Text --format=Sarif \
  --output=artifacts/resharper-report \
  --caches-home=artifacts/resharper-cache --no-updates
```

报告在 `artifacts/resharper-report/`。`INFO` 包含 Hint 级提示；报告不会自动修改源码，反序列化等间接调用需要结合代码判断。

## License

[MIT](LICENSE).
