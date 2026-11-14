# 原生包许可证

这些许可证用于原生分发包；依赖升级时同步检查原文和版本。

| 文件 | 依赖版本与来源 |
| --- | --- |
| Markdig.txt | [Markdig 1.6.0，BSD-2-Clause](https://github.com/xoofx/markdig/blob/1.6.0/license.txt) |
| YamlDotNet.txt | [YamlDotNet 18.1.0，MIT](https://github.com/aaubry/YamlDotNet/blob/v18.1.0/LICENSE.txt) |

.NET 与 ASP.NET Core 的许可证和第三方声明在打包时从实际解析的运行时包复制，避免记录与运行时版本脱节。浏览器依赖复用 [vendor](../src/RazorFolio.Themes.Default/Scripts/vendor/README.md) 下已有的许可证；源码生成器与 Razor 编译器不进入分发包。
