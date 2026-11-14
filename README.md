# RazorFolio

C# / .NET 11 静态博客生成器，命令名为 `rf`。使用 Markdig、YamlDotNet 和构建时编译的 Razor 模板，可发布为无需安装 .NET 的原生程序。仍在开发中。

支持明暗主题、全文搜索、代码高亮、Mermaid、数学公式、目录、分页、分类/标签/年份归档、草稿预览、RSS 和 sitemap。

## 快速开始

从源码运行需要 .NET SDK 11.0.100 或同一 11.0.1xx 功能带的更新补丁（见 `global.json`）：

```bash
dotnet build
dotnet run --project src/RazorFolio.Cli -- new ../my-blog
dotnet run --project src/RazorFolio.Cli -- dev --site ../my-blog
```

打开 `http://127.0.0.1:8080/`。`new` 的目标须不存在或为空，博客内容独立于生成器源码。

使用原生包时直接运行 `rf`；从源码运行时，将下表的 `rf` 换成 `dotnet run --project src/RazorFolio.Cli --`。

| 命令 | 用途 |
| --- | --- |
| `rf new <directory>` | 创建站点 |
| `rf dev --site <directory>` | 包含草稿，监听修改并刷新 |
| `rf build --site <directory>` | 生成正式网站，排除草稿 |
| `rf preview --site <directory>` | 预览已有产物，可用 `--directory` 指定目录 |
| `rf --help` / `rf --version` | 帮助与版本 |

`dev` 和 `preview` 可用 `--port` 指定端口，Ctrl+C 退出。

## 站点内容

```text
my-blog/
├── razorfolio.json   # 站点与主题配置，可省略
├── content/          # Markdown 文章
├── static/           # 图片、附件和 custom.css
├── public/           # 正式生成结果
└── .razorfolio/       # 开发结果和生成清单
```

- [站点与主题配置](docs/configuration.md)：导航、首页、作者卡片、部署 URL。
- [写文章](docs/writing.md)：front matter、链接、代码、图表、公式与当前边界。
- [原生包使用](docs/native-package.md)：解压、运行和升级。

部署时上传 `public/`，其中包含带主题的 `404.html`。子路径部署填写配置 `url`，具体用法见[部署路径与 404](docs/configuration.md#部署路径与-404)。主题资源和浏览器库随生成器提供；写作者只需维护站点内容。

## 开发与发布

`dotnet build` 构建生成器，`rf build` 生成网站。修改 C#、Razor 模板或内置资源后，重新编译并重启 `dev`。

Windows/Linux 原生打包、版本、校验和、CI 与 .NET 工具打包见[发布说明](docs/releasing.md)。本地样例、检查程序和产物保存在被忽略的 `artifacts/`，不进入分发包。

| 项目 | 职责 |
| --- | --- |
| [Cli](src/RazorFolio.Cli) | 命令、预览、监听 |
| [Core](src/RazorFolio.Core) | 配置、内容解析和站点生成 |
| [Themes.Default](src/RazorFolio.Themes.Default) | 模板、样式和脚本 |
| [TemplateCompiler](src/RazorFolio.TemplateCompiler/README.md) | 仅构建时使用的 Razor → C# 编译工具 |

## License

[MIT](LICENSE)。依赖许可证见 [licenses](licenses/README.md)，浏览器依赖来源见 [vendor](src/RazorFolio.Themes.Default/Scripts/vendor/README.md)。
