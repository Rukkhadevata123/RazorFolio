# 写文章

把 Markdown 文件放进站点的 `content/`。例如 `content/notes/hello.md` 默认生成 `public/notes/hello.html`。

```markdown
---
title: 第一篇文章
description: 列表中的简短介绍。
date: 2026-11-13
updated: 2026-11-14
tags: [C#, 工具]
category: [开发记录]
---

正文从这里开始。

## 安装 {#installation}

这一节可通过文章地址后的 #installation 访问。
```

## 文章信息

| 字段 | 用途 |
| --- | --- |
| `title` | 文章标题，默认使用文件名 |
| `description` | 列表与分享摘要，按需填写 |
| `date` / `updated` | 发布与更新日期，格式为 `yyyy-MM-dd` |
| `tags` / `category` | 标签与分类列表 |
| `draft: true` | 仅在 `dev` 中生成 |
| `article: false` | 用于关于页等普通页面，排除在文章列表、归档和 RSS 之外 |
| `star: true` | 加入阅读页侧栏的精选文章 |
| `url: /posts/hello.html` | 指定站内输出地址，须以 `.html` 结尾 |
| `aliases: [/old.html]` | 在指定地址生成静态跳转页 |
| `shareImage` / `license` | 设置单篇分享图或许可，见下文 |

日期可省略。`updated` 可用于文章和普通页，显示在页面头部，若同时填写 `date` 则不能早于它。排序依据是 `date`，从新到旧，同日按输出路径排序，无日期排在最后。未来日期的文章也会发布，暂不发布时请标记为草稿。未知 YAML 字段会忽略。

## 分类与相关文章

阅读页侧栏按分类分组，多分类文章出现在各自的组内。没有分类的文章放在末尾的“未分类”组。当前文章所属分类默认展开，手机上通过“浏览文章”打开列表。精选文章单独显示在分类上方，并保留在原有分类里。

文章末尾最多显示 3 篇相关文章。共同标签越多越靠前，标签数量相同时优先同分类，再沿用文章顺序。只有同分类也可入选。没有匹配时隐藏区块。精选、分类和推荐都只收录本次构建中的文章。

## 标题与文章链接

正文开头的 H1 会移到页面头部，保留格式与锚点。其余章节可用 H2–H6，主题会生成目录。用 `{#id}` 为标题指定固定锚点，例如 `## 安装 {#installation}`，修改标题文字后仍可沿用链接。

同页可写 `[安装](#installation)`，跨页可写 `[另一篇的安装说明](other.md#installation)`。相对 Markdown 链接从源文件所在目录解析，构建会改为目标页面的 URL，即使该页面自定义了 `url`。

## 图片与附件

把图片和附件放在 `static/`，引用时省略这个目录名。下表引用的是同一张图片 `static/images/hello/screenshot.png`。

| 文章位置 | 图片路径 |
| --- | --- |
| `content/hello.md` | `images/hello/screenshot.png` |
| `content/posts/hello.md` | `../images/hello/screenshot.png` |
| `content/posts/Linux/hello.md` | `../../images/hello/screenshot.png` |

以下是 `content/posts/hello.md` 中的图片和附件示例。

```markdown
![运行截图](../images/hello/screenshot.png)
[下载示例](../files/example.zip)
```

上例的附件位于 `static/files/example.zip`。

相对路径从 Markdown 文件所在位置解析，在域名根目录和部署子路径下都适用。设置自定义 `url` 后，构建会调整这些链接。以 `/` 开头的路径从域名根目录解析。手写 HTML 的 `src` 和 `href` 由浏览器按最终页面地址解析。

为图片填写替代文本。图片有替代文本且原始宽或高至少为 160 像素时，可以点击预览。已加链接的图片会打开链接。可给手写的 `<img>` 添加 `data-no-preview` 来关闭单张图片的预览。

## 分享预览

分享信息默认取文章标题、摘要和站点分享图。可在 front matter 中为文章或普通页指定独立的 `shareImage`。

```yaml
shareImage: images/article-share.png
```

上例使用 `static/images/article-share.png`。省略或留空时继承站点默认图。分享地址与站点级选项见[分享预览配置](configuration.md#分享预览)。

## 文章许可

文章继承站点配置的 `license`。在 front matter 中填写同名对象可覆盖整项，`name` 为必填名称，`url` 可填写完整 HTTP(S) 地址。

```yaml
license:
  name: CC BY-SA 4.0
  url: https://creativecommons.org/licenses/by-sa/4.0/deed.zh-hans
```

只写 `license: { enabled: false }` 可隐藏单篇许可。省略该字段或写 `license: null` 会继承站点默认值。普通页只展示自身明确配置的许可。许可显示在正文末尾，打印时也保留。

## Markdown、代码与图表

支持表格、任务列表、脚注、`[TOC]` 及 `tip`、`warning`、`details` 容器。代码围栏可注明语言，主题会显示语言标签并提供复制按钮。

````markdown
::: tip 提示
这里是补充说明。
:::

```csharp
Console.WriteLine("Hello, RazorFolio!");
```

```mermaid
flowchart LR
    A[编写文章] --> B[构建网站] --> C[发布]
```
````

未注明或不识别的语言保留纯文本。Mermaid Tiny 在浏览器中渲染图表，支持展开源码和横向滚动，不支持 mindmap、architecture、图内 KaTeX 和 ELK 布局。

## 数学公式

用 `$...$` 写行内公式，用独占一行的 `$$` 包围块公式。

```markdown
能量与质量的关系为 $E = mc^2$。

$$
\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
$$
```

行内公式和相邻文字用空格或标点分隔。KaTeX 支持表格内公式、中文 `\text{}` 和 `aligned` 多行对齐。正文中的字面美元符号写作 `\$`，代码中的美元符号会按代码原样显示。图表和公式渲染失败或浏览器关闭 JavaScript 时，页面保留源码。

## 阅读与搜索

- **阅读时间**自动显示在文章头部。按东亚文字每分钟 300 字、英文及代码每分钟 200 词估算，合计向上取整，至少 1 分钟。元数据、链接地址、图片替代文本、图表与公式源码和原始 HTML 块不计入。
- **目录**跟随当前章节。手机上可展开并跳转，按 Esc 收起。宽代码和表格可聚焦后用方向键滚动。
- **图片预览**支持适应窗口、原始尺寸和方向键滚动，按 Esc 关闭并返回阅读位置。
- **搜索**覆盖文章和普通页的标题、标签及正文，正式构建排除草稿。空格分隔的词需全部匹配。方向键选结果，Enter 打开，Esc 关闭。搜索需要 JavaScript 和 HTTP 访问，原始 HTML 块不索引。
- **打印文章**会等待图片、公式和图表加载。打印样式使用浅色并隐藏导航和交互控件，也可直接使用浏览器的打印命令。

## 构建检查

`build` 和 `dev` 会检查 Markdown 中的本地链接、图片和附件，包括路径大小写、部署子路径和文章锚点。默认在终端显示带源文件行列号的警告。缺失锚点的诊断代码是 `RF1005`。锚点区分大小写，支持中文、URL 编码、脚注和页面 HTML 元素的 ID。手写 HTML 链接、配置链接、外部网址及静态附件与汇总页的片段需另行检查。

在 CI 中可用以下命令启用严格链接检查。

```bash
rf build --site ./blog --strict-links
```

`--strict-links` 可与 `--url` 一起使用。它会报告所有发现的问题，有问题时返回状态码 `1`，CI 可据此停止发布。该次构建的站点与生成清单可用于排查。

构建按生成清单清理旧文件，更新时逐个替换输出文件。同一输出目录应串行构建，输出路径须使用普通目录和文件。修正构建错误后重新运行即可。正文 HTML 按可信作者内容处理。
