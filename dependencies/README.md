# AyarabuMod Android 编译引用

| 路径 | 内容 | Git 跟踪 |
| --- | --- | --- |
| interop/assemblies/ | Assembly-CSharp.dll、Il2Cppmscorlib.dll、UnityEngine.CoreModule.dll | 是 |
| melonloader/net6/ | MelonLoader.dll、0Harmony.dll、Il2CppInterop.Runtime.dll | 是 |
| interop-backup/ | 当前 APK 的完整 Interop 导出 | 否 |

游戏引用来自 `Tools/ayarabu.apk`，Unity 6000.0.56f1，由 Tools 中配套 LemonLoader Patcher 生成。加载器引用来自 `Tools/LemonLoader-runtime-android-arm64.zip` 的 `assets/LemonLoader/runtime/loader/net6/`。

Toast 的 Utility.dll 通过 `shared/Utility` 源码构建，使用该共享项目自带的编译引用；不在此目录保存公共库 DLL 副本。发布脚本自动打包项目输出中的 Utility.dll。

更新引用时先保留完整导出，再运行：

```powershell
pwsh -NoProfile -File scripts/sync-dependencies.ps1 -InteropDirectory dependencies/interop-backup -MelonLoaderDirectory <运行时-loader/net6目录>
```

脚本根据 csproj 显式 Reference 列表同步必要 DLL；源目录必须不同于目标目录，目标中多余文件会移除。因此完整导出必须放在 interop-backup，而非目标 assemblies 目录。

`DependencyRoot`、`GameInteropReferenceDirectory`、`MelonLoaderReferenceDirectory` 支持通过 MSBuild 属性覆盖。所有引用 `Private=false`，不复制到 mod 发布目录。Unity CoreModule 使用 UnityCore 别名，避免生成的 NullableAttribute 干扰 C# 编译器。
