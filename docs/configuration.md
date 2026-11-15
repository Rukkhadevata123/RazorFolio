# 配置站点和主题

在站点根目录创建 `razorfolio.json`。只填写需要自定义的字段即可。

```json
{
  "title": "我的博客",
  "description": "记录开发与日常。",
  "url": "https://example.com/blog/",
  "navigation": [
    { "text": "首页", "link": "index.html" },
    { "text": "关于", "link": "about.md" }
  ]
}
```

## 站点信息

| 字段 | 用途 |
| --- | --- |
| `title` | 站点名称，默认 `RazorFolio` |
| `description` | 站点简介，用于页面和 RSS 元数据 |
| `language` | 页面语言，默认 `zh-CN` |
| `pageSize` | 每页文章数，默认 `20`，须为正整数 |
| `url` | 正式站点的完整 URL，包含部署子路径 |
| `navigation` | 按配置顺序显示的顶栏链接 |
| `license` | 文章默认许可，包含 `name`、可选的 `url` 和默认为 `true` 的 `enabled` |
| `shareImage` | 默认分享图，支持站点相对路径和完整 HTTP(S) 地址 |

配置中的链接和图片相对于站点根目录，也可使用完整 HTTP(S) 地址。导航链接可以指向 `.md` 文件，构建会解析到其最终页面。省略 `license` 时文章默认不显示许可。单篇配置见[文章许可](writing.md#文章许可)。

可用以下方式设置站点的默认许可。

```json
{
  "license": {
    "name": "CC BY 4.0",
    "url": "https://creativecommons.org/licenses/by/4.0/deed.zh-hans"
  }
}
```

## 默认主题

在配置中加入 `theme` 对象，可自定义站点标识、首页和阅读页。

```json
{
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
    "sidebar": [{ "text": "关于", "link": "about.md" }],
    "footer": "页脚文字"
  }
}
```

- `brand` 设置左上角的文字和图片。省略文字时使用站点名称。
- `hero` 设置首页首屏，`fullScreen` 使首屏占满视口。背景模式为 `none`、`image` 或 `bing`。`image` 需要图片路径，`bing` 加载在线壁纸，失败时回退到指定图片或纯色。
- `profile` 显示作者卡片，`name` 为必填项。省略整个对象即可隐藏卡片。
- `sidebar` 是阅读页分类列表下方的快捷链接。分类和精选由[文章信息](writing.md#分类与相关文章)生成。
- `footer` 设置页脚文字。

例如 `images/logo.png` 对应 `static/images/logo.png`。用 `static/favicon.svg` 设置站点图标，用 `static/custom.css` 补充主题样式。样式内的相对路径从站点根目录解析。完整字段见 [SiteConfiguration](../src/RazorFolio.Core/Configuration/SiteConfiguration.cs) 和 [ThemeOptions](../src/RazorFolio.Core/Configuration/ThemeOptions.cs)。未知 JSON 字段会报错。

## 部署路径与 404

根目录部署使用 `https://example.com/`。子路径部署使用类似 `https://example.com/blog/` 的完整入口。可写入配置的 `url`，也可在正式构建时指定。

```bash
rf build --site ./blog --url https://example.com/blog/
```

`--url` 优先于配置，且只对本次构建生效。两处都接受不含凭据、查询参数和片段的完整 HTTP(S) URL。`rf preview` 从配置读取预览子路径，需检查子路径效果时，将相同 URL 填入 `razorfolio.json`。访问地址见终端输出。

部署时上传完整的 `public/`，其中 `404.html` 是保留的错误页。它从站点入口定位导航和资源，未提供站点 URL 时按域名根目录定位。`dev` 使用本地根路径，`dev` 和 `preview` 在访问缺失页面时都返回 HTTP 404。

正式构建取得站点 URL 后会生成 canonical 地址、RSS 和 sitemap。RSS 收录前 20 篇文章及其摘要，发布时间取 `date` 的 UTC 零点。sitemap 的内容页更新时间优先使用 `updated`，其次使用 `date`，首页和归档等汇总页省略更新时间。日期缺失时省略对应字段。`dev` 不生成 RSS 和 sitemap。

## 分享预览

有站点 URL 时，页面会输出用于分享卡片的 Open Graph 元数据。文章摘要取单篇 `description`，缺省时用站点简介。将图片放在 `static/images/share.png`，再把顶层 `shareImage` 设为 `images/share.png`，即可提供默认分享图。

分享图路径始终从站点根目录解析，开头的 `/` 也表示站点根目录。例如部署到 `https://example.com/blog/` 时，上述图片的分享地址是 `https://example.com/blog/images/share.png`。也可填完整 HTTP(S) 地址。图片地址不可包含凭据或片段，相对路径不可包含 `..`。

文章和普通页可以[单独设置分享图](writing.md#分享预览)。省略图片时不输出 `og:image`，草稿和 404 不输出 Open Graph 元数据。

## 资源与缓存

主题 CSS、JavaScript、字体、自定义样式和页面使用的搜索索引采用内容哈希文件名。内容或主题依赖变化时，构建会更新文件名和页面引用。作者提供的图片和附件保留原路径。

部署后如果仍看到旧页面，检查托管服务或 CDN 的 HTML 缓存。新 HTML 会引用对应版本的资源，旧哈希文件由生成清单清理。
