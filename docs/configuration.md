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

## 默认主题

示例中的图片放在 `static/images/`。配置里的路径按站点根目录书写，如 `images/logo.png`；正文中的路径规则见[图片与附件](writing.md#图片与附件)。

- `brand`、`hero`、`profile` 分别控制左上角标识、首页首屏和作者卡片，文字与头像互相独立。
- 省略 `hero` 显示简洁标题区；`hero.image` 是首屏插图，`hero.background` 是背景，`fullScreen` 让首屏占满视口高度。省略 `profile` 隐藏作者卡片，配置时须填写 `name`。
- `background.mode` 支持 `none`、`image`、`bing`。固定图片模式须填写 `image`；Bing 使用第三方壁纸服务，失败时保留指定图片或纯色。
- 顶栏读取 `navigation`；文章侧栏只显示 `theme.sidebar`，未配置时隐藏。
- 配置中的站内链接和图片按站点根目录解析，也支持 HTTP(S) 地址；`.md` 链接跟随文章的自定义 URL。
- `static/custom.css` 覆盖默认样式，`static/favicon.svg` 设置图标。`footer` 为页脚文字。

默认主题适配桌面与手机，目录跟随当前章节；首页分页只更新文章列表并保留介绍区。搜索、图片预览和打印用法见[阅读与搜索](writing.md#阅读与搜索)。动画遵循系统的“减少动态效果”设置。

完整字段见 [SiteConfiguration](../src/RazorFolio.Core/Configuration/SiteConfiguration.cs) 和 [ThemeOptions](../src/RazorFolio.Core/Configuration/ThemeOptions.cs)。未知配置字段会报错。

## 部署路径与 404

部署到域名根目录时，`url` 填写 `https://example.com/`；部署到子目录时，填写完整入口，例如 `https://example.com/blog/`。

配置 `url` 后，正式页面带 canonical 地址，`build` 还会生成 `feed.xml` 和 `sitemap.xml`；`dev` 不生成这两个文件。RSS 收录按文章顺序排列的前 20 篇及其 `description`，目前不含全文和发布时间；sitemap 不含更新时间。

构建会生成带主题的 `404.html`，提供返回首页、归档和搜索入口。它的资源和站内链接从配置的站点根路径定位，访问深层错误地址时也能加载样式。省略 `url` 时按域名根目录生成。`404.html` 是生成器保留路径，发布时随 `public/` 一起上传；GitHub Pages 会将它用作[自定义 404 页面](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-custom-404-page-for-your-github-pages-site)，其他静态服务器可将错误页指向此文件。

`rf preview` 提供已有产物，配置了子路径时，使用终端显示的入口，例如 `http://127.0.0.1:8080/blog/`；它也支持本地根路径。`rf dev` 使用本地根路径并自动刷新。两种模式访问缺失页面均返回 HTTP 404；错误页带 `noindex`，不进入文章列表、搜索索引、RSS 或 sitemap。

