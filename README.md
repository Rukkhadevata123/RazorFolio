# RazorFolio

RazorFolio 是用 C# 和 .NET 11 编写的静态博客生成器。命令行程序 `rf` 将 Markdown 文章构建为 HTML。默认主题提供分类、全文搜索、目录、相关文章、代码高亮、Mermaid 和数学公式。Windows 与 Linux 的原生版解压即可运行。

## 开始写博客

从 [Releases](https://github.com/Rukkhadevata123/RazorFolio/releases) 下载适用的原生包，解压后在程序所在目录运行以下命令。

```bash
./rf new ../my-blog
./rf dev --site ../my-blog
```

在 Windows 上将 `./rf` 换成 `.\rf.exe`。`new` 接受尚不存在或为空的目录。打开 `http://127.0.0.1:8080/`，编辑 `my-blog/content/hello.md` 即可看到更新。

```text
my-blog/
├── content/          # Markdown 内容
├── static/           # 图片、附件和自定义样式
├── razorfolio.json   # 站点与主题配置，可省略
├── public/           # 正式构建产物
└── .razorfolio/       # 开发产物和生成清单
```

## 常用命令

下表中的 `rf` 表示原生程序。按 Ctrl+C 退出 `dev` 或 `preview`，两者都支持 `--port`。

| 命令 | 用途 |
| --- | --- |
| `rf new <directory>` | 创建站点 |
| `rf dev --site <directory>` | 预览草稿并随修改刷新 |
| `rf build --site <directory> [--url <site-url>] [--strict-links]` | 生成正式站点，可指定部署 URL 或启用严格链接检查 |
| `rf preview --site <directory> [--directory <output>]` | 查看已有产物，默认使用 `public/` |
| `rf --help` / `rf --version` | 查看帮助或版本 |

正式构建排除草稿，部署时上传整个 `public/`。有关写作、配置与发布的说明如下。

- [写文章](docs/writing.md)
- [配置站点和主题](docs/configuration.md)
- [部署到 GitHub Pages](docs/github-pages.md)
- [使用原生包](docs/native-package.md)

## 从源码开发

安装 [global.json](global.json) 指定的 .NET SDK。允许的补丁更新范围也由该文件控制。以下命令在仓库根目录运行。

```bash
dotnet run --project src/RazorFolio.Cli -- new ../my-blog
dotnet run --project src/RazorFolio.Cli -- dev --site ../my-blog
```

从源码执行其他命令时，同样将 `rf` 换成 `dotnet run --project src/RazorFolio.Cli --`。修改 C#、Razor 模板或内置资源后，重启该命令即可重新编译并预览。

Windows 和 Linux 的 push 与 PR 检查使用以下命令。测试站点保存在 `artifacts/regression/`。

```bash
dotnet build -c Release --artifacts-path artifacts/build
dotnet run --project tests/RazorFolio.RegressionTests -c Release --no-build --artifacts-path artifacts/build
```

| 项目 | 职责 |
| --- | --- |
| [Cli](src/RazorFolio.Cli) | 命令、预览和文件监听 |
| [Core](src/RazorFolio.Core) | 配置、内容解析和站点生成 |
| [Themes.Default](src/RazorFolio.Themes.Default) | 模板、样式和脚本 |
| [TemplateCompiler](src/RazorFolio.TemplateCompiler/README.md) | 构建时将 Razor 模板编译为 C# |

原生版和 .NET 工具版的打包与发布见[发布说明](docs/releasing.md)。

本项目也使用 RazorFolio 构建自己的文档站，本地运行方法见[文档站说明](docs-site/README.md)。

## License

[MIT](LICENSE)。依赖许可证见 [licenses](licenses/README.md)，浏览器依赖见 [vendor](src/RazorFolio.Themes.Default/Scripts/vendor/README.md)。
