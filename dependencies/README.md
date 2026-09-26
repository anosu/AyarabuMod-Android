# AyarabuMod Android 编译依赖

游戏引用由 `Tools/ayarabu.apk`（Unity 6000.0.56f1）和配套 LemonLoader Patcher 生成。加载器引用来自 `Tools/LemonLoader-runtime-android-arm64.zip` 的 `assets/LemonLoader/runtime/loader/net6/`。

当前 Mod 不直接使用 CoreModule 类型，因此最小编译目录不包含有问题的 `UnityEngine.CoreModule.dll`。完整 Interop 导出放在 `interop-backup/`，编译只使用 `interop/assemblies/` 中的全部 DLL。

更新和同步方法见 [公共工程规范](https://github.com/anosu/ModEngineering/blob/main/docs/CONVENTIONS.md)。
