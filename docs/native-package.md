# 使用 RazorFolio 原生包

从 [Releases](https://github.com/Rukkhadevata123/RazorFolio/releases) 下载 Windows x64 的 `.zip` 或 Linux x64 的 `.tar.gz`，将整个包解压到独立目录。原生程序可直接运行，无需安装 .NET。Linux 包使用系统的 glibc，构建基线见包内的 `build-info.txt`。

## 创建和预览

在解压目录中运行以下命令。Windows 将 `./rf` 换成 `.\rf.exe`。

```text
./rf new ../my-blog
./rf dev --site ../my-blog
```

`new` 接受尚不存在或为空的目录。打开 `http://127.0.0.1:8080/`，编辑 `my-blog/content/hello.md`。图片和附件放在 `static/`，站点设置写在 `razorfolio.json`。

按 Ctrl+C 结束开发预览。用以下命令生成正式站点并查看结果。

```text
./rf build --site ../my-blog
./rf preview --site ../my-blog
```

正式构建会排除草稿。预览入口见终端输出，部署时上传站点的整个 `public/`。两种预览模式都支持 `--port`。

## 校验与升级

用 `./rf --version` 查看版本。Linux 可用 `sha256sum --check <校验文件>` 核对下载，Windows 可用 `Get-FileHash <压缩包> -Algorithm SHA256` 与对应的 `.sha256` 文件比较。

升级时将新包解压到另一目录，用新程序构建原有站点即可。

如需配置、写作或部署示例，请查阅 [RazorFolio 仓库](https://github.com/Rukkhadevata123/RazorFolio)。本项目使用 MIT 许可证，随程序分发的依赖许可证见包内的 `licenses/`。
