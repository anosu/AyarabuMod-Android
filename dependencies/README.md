# AyarabuMod Android 编译依赖

游戏引用由 `Tools/ayarabu.apk`（Unity 6000.0.56f1）和配套 LemonLoader Patcher 生成。加载器引用来自 `Tools/LemonLoader-runtime-android-arm64.zip` 的 `assets/LemonLoader/runtime/loader/net6/`。

`UnityEngine.CoreModule.dll` 使用 `UnityCore` 别名，隔离生成的不完整 `NullableAttribute`。完整 Interop 导出放在 `interop-backup/`，编译只使用 `interop/assemblies/` 中项目声明的 DLL。

更新和同步方法见 [公共工程规范](https://github.com/anosu/ModEngineering/blob/main/docs/CONVENTIONS.md)。
