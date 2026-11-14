# 站点配置

站点根目录的 `razorfolio.json` 控制站点信息与默认主题，不需要的字段可省略。示例：

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

## 站点信息

| 字段 | 用途与默认值 |
| --- | --- |
| `title` | 站点名称，默认 `RazorFolio` |
| `description` | 可选站点简介，用于页面与 RSS 元数据 |
| `language` | 页面语言，默认 `zh-CN` |
| `pageSize` | 每页文章数，默认 `20`，须为正整数 |
| `url` | 正式网站的完整入口，含部署子路径，见下节 |
| `navigation` | 顶栏链接，按配置顺序显示 |
| `license` | 文章默认许可，含 `name`、可选 `url` 和 `enabled`（默认 `true`）；省略时不显示 |
| `shareImage` | 默认分享图，填写站点相对路径或完整 HTTP(S) 图片地址 |

例如，在 `razorfolio.json` 顶层设置所有文章的默认协议：

```json
"license": {
  "name": "CC BY 4.0",
  "url": "https://creativecommons.org/licenses/by/4.0/deed.zh-hans"
}
```

许可显示在正文末尾右下角，打印时保留名称并附上链接地址。启用时 `name` 必填，`url` 可省略或填写完整 HTTP(S) 地址。单篇覆盖和隐藏见[文章许可](writing.md#文章许可)。

## 分享预览

设置站点 `url` 后，页面会输出 Open Graph 标题、类型、规范地址和站点名称，供分享平台读取。摘要优先使用单篇 `description`，否则使用站点 `description`；没有摘要时省略，不自动截取正文。

例如将图片放在 `static/images/share.png`，在 `razorfolio.json` 顶层设置：

```json
"shareImage": "images/share.png"
```

图片路径从站点根目录解析，开头的 `/` 也表示站点根目录；配置 `url: https://example.com/blog/` 时，上例输出 `https://example.com/blog/images/share.png`。也可使用完整 HTTP(S) 图片地址。路径不接受 `..`，图片地址不能包含凭据或片段。

文章及普通页可用 YAML `shareImage` 覆盖默认图，省略、空值或 `null` 时继承。未设置图片则省略 `og:image`。文章类型为 `article`，普通页、首页与归档为 `website`；草稿和 404 不输出分享信息。图片须随站点部署并能公开访问，分享平台的最终样式和缓存由平台决定。协议见 [Open Graph](https://ogp.me/)。

## 默认主题

示例中的图片放在 `static/images/`。配置里的路径按站点根目录书写，如 `images/logo.png`；正文中的路径规则见[图片与附件](writing.md#图片与附件)。

- `brand`、`hero`、`profile` 分别控制左上角标识、首页首屏和作者卡片，文字与头像互相独立。
- 省略 `hero` 显示简洁标题区；`hero.image` 是首屏插图，`hero.background` 是背景，`fullScreen` 让首屏占满视口高度。省略 `profile` 隐藏作者卡片，配置时须填写 `name`。
- `background.mode` 支持 `none`、`image`、`bing`。固定图片模式须填写 `image`；Bing 使用第三方壁纸服务，失败时保留指定图片或纯色。
- 顶栏读取 `navigation`；阅读页侧栏按 `category` 自动分组全部文章，分类按名称排序，组内按日期从新到旧排列，同日按输出路径排序。多分类文章出现在各自分类中，没有分类的文章放在末尾的“未分类”组；普通页不进入分类列表，草稿仅在开发预览中显示。
- 当前文章所属分类默认展开，其他分类可点击展开或收起；手机上通过“浏览文章”打开列表。`theme.sidebar` 是分类列表下方的快捷链接，可放普通页或常用入口。
- 在文章 YAML 中设置 `star: true`，即可在分类上方独立显示“精选文章”，沿用文章排序；没有精选时隐藏该区域。分类列表仍保留全部文章，普通页不进入精选，草稿仅开发预览显示。
- 配置中的站内链接和图片按站点根目录解析，也支持 HTTP(S) 地址；`.md` 链接跟随文章的自定义 URL。
- `static/custom.css` 覆盖默认样式，`static/favicon.svg` 设置图标。`footer` 为页脚文字。

默认主题适配桌面与手机，目录跟随当前章节；首页分页只更新文章列表并保留介绍区。搜索、图片预览和打印用法见[阅读与搜索](writing.md#阅读与搜索)。动画遵循系统的“减少动态效果”设置。

完整字段见 [SiteConfiguration](../src/RazorFolio.Core/Configuration/SiteConfiguration.cs) 和 [ThemeOptions](../src/RazorFolio.Core/Configuration/ThemeOptions.cs)。未知配置字段会报错。

## 部署路径与 404

部署到域名根目录时，`url` 填写 `https://example.com/`；部署到子目录时，填写完整入口，例如 `https://example.com/blog/`。

配置 `url` 后，正式页面带 canonical 地址，`build` 还会生成 `feed.xml` 和 `sitemap.xml`；`dev` 不生成这两个文件。RSS 收录按文章顺序排列的前 20 篇及其 `description`，不含全文；`pubDate` 使用文章的 `date`，按 UTC 零点输出，没有日期时省略。sitemap 中文章和普通页的 `lastmod` 优先使用 `updated`，否则使用 `date`；没有日期的内容及首页、归档等汇总页省略 `lastmod`。

构建会生成带主题的 `404.html`，提供返回首页、归档和搜索入口。它的资源和站内链接从配置的站点根路径定位，访问深层错误地址时也能加载样式。省略 `url` 时按域名根目录生成。`404.html` 是生成器保留路径，发布时随 `public/` 一起上传；GitHub Pages 会将它用作[自定义 404 页面](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-custom-404-page-for-your-github-pages-site)，其他静态服务器可将错误页指向此文件。

`rf preview` 提供已有产物，配置了子路径时，使用终端显示的入口，例如 `http://127.0.0.1:8080/blog/`；它也支持本地根路径。`rf dev` 使用本地根路径并自动刷新。两种模式访问缺失页面均返回 HTTP 404；错误页带 `noindex`，不进入文章列表、搜索索引、RSS 或 sitemap。

