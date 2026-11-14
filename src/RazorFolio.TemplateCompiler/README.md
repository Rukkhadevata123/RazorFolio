# Razor 模板编译器

这是源码构建依赖，不随 `rf` 工具包分发。默认主题的 MSBuild 目标先构建本项目，再调用它将 `Views/**/*.cshtml` 转为 `obj/.../Razor/RazorFolioTemplate_*.g.cs`，作为普通 C# 编译进主题程序集。运行时没有模板扫描、反射实例化或 MVC 视图引擎。

编译器引用当前 SDK 自带的官方 Razor/Roslyn DLL。`global.json` 限定 SDK 11.0.1xx 功能带，允许更新补丁；升级功能带前须核对编译器 API、源位置诊断和生成结果。构建工具不参与目标程序的 NativeAOT 发布；主题的项目引用显式隔离 RID、自包含和 AOT 属性。

## 模板约定

- 页面继承 `PageTemplate<TModel>`，局部模板继承 `RazorTemplate<TModel>`，通过 `@inherits` 声明。
- `Model` 为强类型且必须初始化；页面通过 `Metadata.Title`、`Description`、`CssClass` 提供布局元数据。
- 用 `@await PartialAsync(new Shared__Sidebar { Model = Model.Context })` 调用局部模板。类型名由相对于 Views 的路径去扩展名、将目录分隔符改为 `_` 得到；例如 `Shared/_Sidebar.cshtml` 对应 `Shared__Sidebar`。文件路径只接受 ASCII 标识符字符，冲突会失败。
- 普通表达式和动态属性值默认 HTML 编码；`Raw(string)` 仅用于可信作者 HTML。模板渲染结果用内部 `HtmlContent` 包装，组合时不重复编码。
- 布局由 `RazorPageRenderer` 显式调用，接收 `LayoutModel`。不使用 `_ViewStart`、字符串查找局部模板、`ViewData`、Tag Helpers 或其他 MVC 服务。
- 每次渲染创建新模板实例；渲染器可并发使用，取消令牌传递给局部模板和布局。

## 生成与验证

每次构建重新核对模板集合，只写入内容发生变化的生成文件，并删除已移除模板对应的生成文件。生成保留 `.cshtml` 源位置，Razor 语法错误和生成后的 C# 类型错误均会阻止构建。

生成的主题代码不依赖 `Microsoft.AspNetCore.App`。完整 CLI 的原生发布方法见[发布说明](../../docs/releasing.md)。

修改编译器时，检查生成结果、源位置诊断、模板新增/移除后的重建，以及 NativeAOT 发布。
