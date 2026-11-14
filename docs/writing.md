# 写文章

## 文章信息

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
| `date` | 可选，格式为 `yyyy-MM-dd`；文章按日期倒序排列，无日期排在最后 |
| `tags` / `category` | 标签与并列分类列表 |
| `draft: true` | 仅在开发预览中生成 |
| `article: false` | 普通页面，不进入文章列表、归档和 RSS |
| `url: /posts/hello.html` | 自定义站内输出地址，须以 `.html` 结尾 |
| `aliases: [/old.html]` | 为旧地址生成静态跳转页 |

`content/` 中的 `.md` 文件会参与构建，包括子目录。未来日期的文章照常发布；暂不发布的内容使用 `draft: true`。YAML 中未识别的字段会忽略；目前 `updated`、`cover`、`star` 没有对应展示。

## 标题与文章链接

正文开头的 H1 会移到文章头部，保留格式与锚点；没有开头 H1 时使用 `title`，未填标题则使用文件名。主题自动生成 H2–H6 目录，`{#id}` 可固定标题锚点。

链接到章节时可写 `[安装](#installation)` 或 `[另一篇的安装说明](other.md#installation)`。建议为需要长期引用的标题设置 `{#installation}`，修改标题文字后仍可保留这个地址。

相对 `.md` 链接按源文章目录解析，生成时自动转换为目标文章地址，包括自定义 `url`。普通页面使用同样的写法。

## 图片与附件

图片和附件放在 `static/`，构建时按原目录结构复制到网站根目录，引用时不写 `static`。例如：

```text
my-blog/
├── content/
│   └── posts/
│       └── hello.md
└── static/
    └── images/
        └── hello/
            └── screenshot.png
```

在 `hello.md` 中写：

```markdown
![运行截图](../images/hello/screenshot.png)
```

同一张图片在不同目录的文章中这样引用：

| 文章位置 | 图片路径 |
| --- | --- |
| `content/hello.md` | `images/hello/screenshot.png` |
| `content/posts/hello.md` | `../images/hello/screenshot.png` |
| `content/posts/Linux/hello.md` | `../../images/hello/screenshot.png` |

推荐相对路径，适用于域名根目录和 `/blog/` 等部署子路径。路径按源文章所在目录书写；文章设置自定义 `url` 后，生成器会调整输出链接。以 `/` 开头则表示域名根目录，例如 `/images/hello/screenshot.png`，不会自动添加 `/blog/`。

附件遵循相同规则：`static/files/example.zip` 可在 `content/posts/hello.md` 中用 `[下载示例](../files/example.zip)` 引用。手写 HTML 中的 `src`、`href` 按最终页面地址解析，生成器不改写。

填写 `![说明文字](路径)` 中的替代文本，便于图片加载失败或使用屏幕阅读器时理解内容。原始宽或高至少 160 像素且有替代文本的正文图片支持点击预览；已有链接的图片按原链接打开。单张图片可用 `<img src="..." alt="..." data-no-preview>` 关闭预览。

## Markdown、代码与图表

支持表格、任务列表、脚注、`[TOC]` 及 `tip` / `warning` / `details` 容器。代码围栏注明语言后提供高亮、语言标签和复制按钮；未知语言保留纯文本。

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

Mermaid 图表在浏览器中绘制，随明暗模式切换；宽图可横向滚动，源码可展开复制。使用本地 Mermaid Tiny，当前不支持 mindmap、architecture、图内 KaTeX 和 ELK 布局。

## 数学公式

使用 `$...$` 行内公式，或以独占行的 `$$` 包围块公式：

```markdown
能量与质量的关系为 $E = mc^2$。

$$
\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
$$
```

行内公式与周围文字用空格或标点分隔。本地 KaTeX 支持表格内公式、中文 `\text{}` 和 `aligned` 多行对齐，长块公式可横向滚动。代码中的美元符号不解析，正文中的字面美元符号写作 `\$`。当前支持上述美元符号语法，不支持 `\(...\)` / `\[...\]`、跨公式共享宏或完整 LaTeX。

图表与公式的浏览器依赖随生成器分发，只在需要的页面加载；禁用 JavaScript 或渲染失败时保留源码。

## 阅读与搜索

- 目录显示当前章节；手机上展开后可跳转，按 Esc 收起。宽代码和表格可聚焦后用方向键横向滚动。
- 图片预览支持适应窗口、原始尺寸和方向键滚动，按 Esc 关闭并返回阅读位置。
- 顶栏搜索覆盖文章及普通页的标题、标签和正文，正式构建排除草稿。空格分隔的多个词需全部匹配；方向键选择结果、Enter 打开、Esc 关闭。搜索需要 JavaScript 和 HTTP 访问，原始 HTML 块不进入正文索引。

文章顶部的“打印文章”会等待图片、公式和图表加载；也可使用浏览器的打印命令。打印采用浅色排版，隐藏导航和交互控件，长代码换行，宽表格与公式适配纸张。

## 构建检查

`build` / `dev` 检查 Markdown 中的本地链接、图片、附件及文章锚点。缺失、大小写不符或越出部署子路径时，输出带源文件行列号的警告并继续构建；正式构建也会提示指向未发布草稿的链接。

文章锚点检查支持同页、跨文章、自定义 URL 和 aliases，识别标题、脚注和 HTML 元素 ID。锚点区分大小写，支持中文及 URL 编码；缺失时输出 `RF1005`。手写 HTML 链接、配置链接、外部网址，以及静态附件与汇总页的片段需自行核对。

每次构建重新生成站点，并按生成清单清理旧文件。同一输出目录应串行构建；失败后修正问题并重新构建。正文 HTML 按可信作者内容处理。
