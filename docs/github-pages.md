# 部署到 GitHub Pages

博客仓库根目录包含 `content/`，以及可选的 `static/` 和 `razorfolio.json`。两种模板都手动运行，使用 Ubuntu 24.04 下载指定版本的 RazorFolio 原生包，校验 SHA-256 后构建网站。

| 模板 | 发布方式 | 目标 |
| --- | --- | --- |
| [github-pages.yml](workflows/github-pages.yml) | 上传 artifact，由 Actions 直接部署 | 运行工作流的仓库所对应的 Pages |
| [github-pages-branch.yml](workflows/github-pages-branch.yml) | 将生成文件提交并推送到 Git 分支 | 当前或其他任意 GitHub 仓库的指定分支 |

选择一种模板，复制到博客仓库的 `.github/workflows/pages.yml`，提交并推送到默认分支。RazorFolio 版本可在 [Releases](https://github.com/Rukkhadevata123/RazorFolio/releases) 查看，工作流输入版本号时省略 `v` 前缀，例如 `0.1.0`。

## Actions 直接部署

1. 在博客仓库的 **Settings → Pages → Build and deployment** 中，将 **Source** 设为 **GitHub Actions**。自定义域名也在该页面设置。
2. 在 **Actions → Deploy blog to GitHub Pages → Run workflow** 选择源码分支并填写 RazorFolio 版本。
3. 部署完成后，从 `github-pages` deployment 打开网站。

网站 URL 自动读取该仓库的 Pages 配置，支持项目子路径、用户站点和自定义域名。此模式使用仓库自带的 `GITHUB_TOKEN`；构建产物通过 artifact 交付，Git 分支只保存博客源码。

## Git 分支发布

1. 准备目标仓库，并选择专门存放生成文件的分支，例如 `gh-pages`；分支尚不存在时会在发布时创建。
2. 在博客源码仓库的 **Settings → Secrets and variables → Actions** 中添加 `PAGES_DEPLOY_TOKEN`。使用有权写入目标仓库的个人访问令牌；fine-grained token 选择目标仓库并授予 **Contents: Read and write**。
3. 在 **Actions → Publish blog to a Git branch → Run workflow** 填写下表参数并运行。
4. 在**目标仓库**的 **Settings → Pages** 中选择 **Deploy from a branch**，指定发布分支和 **/(root)**。

| 输入 | 含义 | 示例 |
| --- | --- | --- |
| `razorfolio_version` | 原生 CLI 的 Release 版本 | `0.1.0` |
| `target_repository` | 目标 `owner/repo`；留空使用当前仓库 | `alice/site-output` |
| `publish_branch` | 产物分支，默认 `gh-pages` | `gh-pages`、`main`、`deploy/site` |
| `site_url` | 最终网站的完整 URL，含部署子路径 | `https://alice.github.io/site-output/` |

常见配置：

| 场景 | 目标仓库 | 分支 | 网站 URL |
| --- | --- | --- | --- |
| 源码与产物同仓库 | 留空 | `gh-pages` | `https://alice.github.io/blog/` |
| 发布到用户站点仓库 | `alice/alice.github.io` | `main` | `https://alice.github.io/` |
| 发布到其他项目仓库 | `alice/site-output` | `gh-pages` | `https://alice.github.io/site-output/` |
| 使用自定义域名 | `alice/site-output` | `gh-pages` | `https://notes.example.com/` |

模板将 `public/` 的内容放到目标分支根目录，每次同步更新文件、移除过期产物，并保留提交历史。同仓库发布会检查目标与源码分支、默认分支分离。

分支模式统一使用 `PAGES_DEPLOY_TOKEN`，使推送能够触发目标仓库的 Pages 发布。GitHub 官方说明，使用 `GITHUB_TOKEN` 推送的提交不会触发 Pages 构建。[发布源说明](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)

模板添加 `.nojekyll`。使用自定义域名时，按 `site_url` 的主机名写入 `CNAME`；同时在目标仓库的 Pages 设置域名并完成 DNS 配置。`site_url` 用于生成链接和元数据，目标仓库与分支决定产物写到哪里。

## 构建与更新

工作流将 `content/`、`static/` 和配置复制到临时站点，在副本中设置部署 URL，再生成新的 `public/`。源码配置保持原样，仓库内已有的输出和缓存也不会混入此次发布。

生产构建排除草稿，生成主题资源、搜索索引、RSS、sitemap 和 `404.html`。发布后检查首页、文章、搜索、图片和一个不存在的深层地址。本地可填写相同的 `url`，通过 `rf build` 和 `rf preview` 检查路径，见[部署路径与 404](configuration.md#部署路径与-404)。

更新文章或升级生成器后，再次手动运行所选工作流。Action 固定到具体提交，RazorFolio 固定到填写的版本。部署机制参考 [GitHub Pages 自定义工作流](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)和 [Git 分支发布 Action](https://github.com/peaceiris/actions-gh-pages)。
