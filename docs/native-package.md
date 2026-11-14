# RazorFolio 原生版

将整个压缩包解压到独立目录，无需安装 .NET。Windows 使用 `rf.exe`，Linux 使用 `rf`；以下以当前目录运行示例：

```text
./rf new ../my-blog
./rf dev --site ../my-blog
./rf build --site ../my-blog
./rf preview --site ../my-blog
```

Windows 将 `./rf` 换成 `.\rf.exe`。`new` 的目标须不存在或为空。预览地址为 `http://127.0.0.1:8080/`，可用 `--port` 指定端口，Ctrl+C 退出。

文章放在站点 `content/`，资源放在 `static/`，配置文件为 `razorfolio.json`。`dev` 包含草稿并监听修改；`build` 生成正式 `public/`，部署时上传该目录。

运行 `./rf --version` 查看版本。包内 `build-info.txt` 记录版本、源码提交、SDK 和构建基线；`.sha256` 文件用于核对下载完整性。Linux 包需要 x64、glibc 环境，具体构建基线见包内记录。

升级时将新包解压到另一目录，确认版本后使用新程序构建已有站点。文章和配置继续保存在原站点目录。

使用与配置说明：[RazorFolio](https://github.com/Rukkhadevata123/RazorFolio)。本项目使用 MIT 许可证；`licenses/` 包含随程序分发的依赖许可证和声明。默认主题及浏览器库已嵌入程序，生成站点不需要 npm 或联网下载资源。
