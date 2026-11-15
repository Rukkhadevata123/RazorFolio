# 文档站

站点使用仓库 README 和 `docs/*.md` 中的指南。准备脚本在临时副本里添加标题、分类和标签，为章节设置与 GitHub 链接对应的锚点，将源码链接指向 GitHub，并复制 workflow 示例供下载。

workflow 下载文件使用 `.yml.txt` 后缀，在博客仓库中保存时去掉末尾的 `.txt`。

在仓库根目录执行以下命令，使用 `global.json` 指定的 SDK。

```bash
dotnet run --file build/prepare-docs.cs --artifacts-path artifacts/docs-tools -- --output artifacts/docs-site
dotnet run --project src/RazorFolio.Cli -- build --site artifacts/docs-site --strict-links
dotnet run --project src/RazorFolio.Cli -- preview --site artifacts/docs-site
```

准备脚本使用新建或空目录。再次准备时，可通过 `--output` 选择另一目录。只修改主题或生成器时，直接重新执行构建即可。

`Documentation` 工作流在 PR 中用项目子路径检查构建。在 `main` 上构建成功后，通过 Actions 部署到本仓库的 Pages。首次启用时，在仓库 **Settings → Pages** 将 Source 设为 **GitHub Actions**。部署 URL 从 Pages 配置获取。
