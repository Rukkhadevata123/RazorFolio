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

图片放在 `static/images/`，不需要的配置可省略。

- `brand`、`hero`、`profile` 分别控制左上角标识、首页首屏和作者卡片，文字与头像互相独立。
- 省略 `hero` 显示简洁标题区；省略 `profile` 隐藏作者卡片。首屏仅在首页第一页显示。
- `background.mode` 支持 `none`、`image`、`bing`。固定图片模式须填写 `image`；Bing 使用第三方壁纸服务，失败时保留指定图片或纯色。
- 顶栏读取 `navigation`；文章侧栏只显示 `theme.sidebar`，未配置时隐藏。
- 配置中的站内链接和图片按站点根目录解析，也支持 HTTP(S) 地址；`.md` 链接跟随文章的自定义 URL。
- `static/custom.css` 覆盖默认样式，`static/favicon.svg` 设置图标。配置 `url` 后，正式构建生成 canonical、RSS 和 sitemap。

完整字段见 [SiteConfiguration](../src/RazorFolio.Core/Configuration/SiteConfiguration.cs) 和 [ThemeOptions](../src/RazorFolio.Core/Configuration/ThemeOptions.cs)。未知配置字段会报错。

## 部署路径与 404

部署到域名根目录时，`url` 填写 `https://example.com/`；部署到子目录时，填写完整入口，例如 `https://example.com/blog/`。

构建会生成带主题的 `404.html`，提供返回首页、归档和搜索入口。它的资源和站内链接从配置的站点根路径定位，访问深层错误地址时也能加载样式。省略 `url` 时按域名根目录生成。`404.html` 是生成器保留路径，发布时随 `public/` 一起上传；GitHub Pages 会将它用作[自定义 404 页面](https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-custom-404-page-for-your-github-pages-site)，其他静态服务器可将错误页指向此文件。

`rf preview` 在本地根路径和配置的子路径提供已有产物，终端会打印子路径入口；例如上述配置对应 `http://127.0.0.1:8080/blog/`。`rf dev` 使用本地根路径，生成的开发版 404 也使用根路径链接。两种模式访问缺失页面时均返回 HTTP 404，开发模式支持自动刷新。404 页面带 `noindex`，文章列表、搜索索引、RSS 和 sitemap 只收录正常内容。

