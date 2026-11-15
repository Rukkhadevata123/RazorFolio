# 部署到 GitHub Pages

博客仓库根目录放置 `content/`，以及可选的 `static/` 和 `razorfolio.json`。选一个模板复制到博客仓库的 `.github/workflows/pages.yml`，并将其提交到默认分支。

| 需求 | 模板 |
| --- | --- |
| 通过 Actions 部署当前仓库的 Pages | [github-pages.yml](workflows/github-pages.yml) |
| 将生成站点推送到当前或另一个仓库的分支 | [github-pages-branch.yml](workflows/github-pages-branch.yml) |

模板下载 [Releases](https://github.com/Rukkhadevata123/RazorFolio/releases) 中的 Linux 原生包，校验 SHA-256 后构建。默认版本由 `RAZORFOLIO_DEFAULT_VERSION` 指定。手动运行时可通过 `razorfolio_version` 覆盖，填写不带 `v` 的版本号。

## Actions 直接部署

1. 在博客仓库 **Settings → Pages** 将发布源设为 **GitHub Actions**。使用自定义域名时也在此设置。
2. 在 **Actions → Deploy blog to GitHub Pages → Run workflow** 选择源码分支并运行。
3. 部署成功后，从 `github-pages` 环境打开站点。

模板从 Pages 配置读取部署 URL，使用 Actions artifact 发布，对应仓库自带的 `GITHUB_TOKEN` 即可完成部署。

## Git 分支发布

1. 选定存放产物的目标仓库和分支。同仓库发布时，目标分支必须与源码分支及默认分支分开。
2. 在源码仓库 **Settings → Secrets and variables → Actions** 中添加 `PAGES_DEPLOY_TOKEN`。令牌须能写入目标仓库。使用 fine-grained token 时，选中目标仓库并授予 **Contents: Read and write**。
3. 在 **Actions → Publish blog to a Git branch → Run workflow** 填写下表参数。目标分支可以由工作流创建。
4. 在目标仓库 **Settings → Pages** 选择 **Deploy from a branch**，再选定产物分支和 **/(root)**。

| 输入 | 用途 | 示例 |
| --- | --- | --- |
| `target_repository` | 目标 `owner/repo`，留空使用当前仓库 | `alice/site-output` |
| `publish_branch` | 产物分支，默认 `gh-pages` | `gh-pages` |
| `site_url` | 访客打开的完整 URL，含部署子路径 | `https://alice.github.io/site-output/` |

目标仓库使用自定义域名时，`site_url` 填写该域名的公开地址。模板会生成 `CNAME`，仍需在 Pages 中设置域名并配置 DNS。每次发布会同步 `public/` 到分支根目录，包括清理过期产物，并保留提交历史。令牌推送用于触发目标仓库的 Pages 构建。参见 [Pages 发布源说明](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)。

## 检查与更新

模板用 `rf build --url` 传入部署 URL，在临时目录生成本次发布的 `public/`。如果希望在链接诊断出现时中止发布，在这条命令中加上 `--strict-links`。检查范围见[构建检查](writing.md#构建检查)。

内容修改后再次运行工作流。发布后可检查文章、搜索、静态图片以及一个缺失的深层地址。若新部署仍显示旧页面，检查 CDN 的 HTML 缓存。详见[资源与缓存](configuration.md#资源与缓存)。
