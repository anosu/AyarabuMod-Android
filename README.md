# AyarabuMod Android

あやかしランブル！的 LemonLoader ARM64 翻译 mod。基于 `Tools/ayarabu.apk`（Unity 6000.0.56f1）生成的 Interop 引用，版本 1.0.0。

## 功能

- 在 `App.AdventureTask.createTask` 选择剧情，在 `App.AdvMessage.initialize` 替换人物名和消息。
- 人物名使用独立 `names.json`，对话仅查当前剧情字典；未命中保留原文。
- 内存缓存、磁盘缓存、并发请求合并、30 秒失败冷却、可选清单哈希校验、损坏缓存容错及离线回退。
- 后台加载优先发布本地副本；切换剧情不会误用上一段剧情，退出时取消请求。
- 游戏内 Toast 提示 mod 加载、翻译下载失败、本地缓存回退和同步等待超时。

旧 Frida 的 `App.Adventure` 已不适用于这个 APK，因此使用核实后的 `AdventureTask`。不包含 GCMod 专属的主数据、战斗或字体样式功能。

## 安装

先使用 Tools 中配套的 LemonLoader Patcher 为原始 APK 安装加载器。将发布 ZIP 解压到加载器日志显示的 MelonLoader base 目录：

```text
Mods/AyarabuMod/AyarabuMod.dll
Mods/AyarabuMod/Utility.dll
```

首次运行生成 `UserData/AyarabuMod.cfg`；缓存目录为 `UserData/AyarabuMod/translations/zh-Hans/`。安装时请同时复制发布包内的 `Utility.dll`，它提供 Toast 通知。

```toml
[Ayarabu.Translation]
Enabled = true
CDN = "https://raw.githubusercontent.com/anosu/ayarabu-translation/refs/heads/main/translations"
Language = "zh-Hans"
AsyncMode = false
```

默认同步加载：剧情入口等待人物名和当前剧情字典就绪，最多等待 10 秒；超时继续后台下载并保留原文。设置 `AsyncMode = true` 可选择后台加载，首次下载期间的消息可能保留原文，已显示的消息不会自动刷新。配置修改后重启游戏；已有配置文件若保留 `AsyncMode = true`，需手动改为 `false`。

CDN 对应筹备中的 https://github.com/anosu/ayarabu-translation 。配置值包含 `/translations`，代码只追加语言和资源路径，不推断或补齐该前缀。仓库尚未上线时可预放本地字典或修改 CDN。

## 翻译仓库约定

```text
translations/
└── zh-Hans/
    ├── manifest.json           # 可选
    ├── names.json
    └── adventures/
        └── 0000123.json
```

`names.json` 和剧情 JSON 均为原文到译文的字符串映射。剧情 ID 使用十进制、至少七位、左侧补零。没有旧版 `zh_Hans` 路径兼容逻辑。

```json
{"原文": "译文"}
```

可选清单格式：

```json
{"names": "人物名字典哈希", "adventures": {"0000123": "剧情字典哈希"}}
```

哈希沿用 GCMod：按 Unicode 码点排序键，将每项拼成 `键 + NUL + 值 + NUL`，对 UTF-8 字节计算小写 MD5。清单缺失时仍下载字典；远程失败或哈希不匹配时使用旧缓存。当前会话已成功加载的数据复用，更新译文后重启游戏刷新。

## 构建与验证

```powershell
git submodule update --init --recursive
dotnet test tests/AyarabuMod.Tests/AyarabuMod.Tests.csproj -c Release
python shared/ModEngineering/scripts/project.py package
```

输出：`artifacts/release/v1.0.0/AyarabuMod-Android.zip`。独立测试覆盖 CDN 路径、缓存校验、失败回退、并发合并和剧情隔离。Unity/ARM64 hook 和中文字体显示仍需在设备中验证。

源码位于 `src/AyarabuMod`，测试位于 `tests/AyarabuMod.Tests`。构建和 CI 细节见[公共工程说明](https://github.com/anosu/ModEngineering/blob/main/docs/CONVENTIONS.md)，编译引用见 [dependencies/README.md](dependencies/README.md)。

推送和 PR 自动执行格式检查、测试及打包，普通 push/PR 不上传测试包。推送与项目版本一致的 `v*` 标签自动创建或更新 GitHub Release，附带 ZIP 和 SHA256SUMS.txt。

`dependencies/melonloader/net6` 来自 Tools 的运行时 ZIP；游戏引用由同目录 Patcher 从原始 APK 生成。完整导出放在忽略的 `dependencies/interop-backup/`，`interop/assemblies/` 仅保留编译需要的三个 DLL。游戏更新时重新生成：

```powershell
../Tools/LemonLoader.Patcher/CLI/LemonLoader.Patcher.CLI.exe patch ../Tools/ayarabu.apk --output artifacts/ayarabu-lemonloader-unsigned.apk --release ../Tools/LemonLoader-runtime-android-arm64.zip --interop-output dependencies/interop-backup
pwsh -NoProfile -File shared/ModEngineering/scripts/sync-dependencies.ps1 -RepositoryRoot . -InteropDirectory dependencies/interop-backup -MelonLoaderDirectory <解压后的运行时-loader/net6目录>
```

生成的 APK 未签名且未对齐，不是可直接安装的发布包；需要按本机 Android 工具链完成 zipalign 和签名。mod 发布 ZIP 不携带游戏或加载器程序集。

## 开发

源码位于 `src/`，测试位于 `tests/`。项目配置由 `.csproj` 管理，依赖版本由 Git 子模块记录。构建、VS 联调和发布命令见[公共工程说明](https://github.com/anosu/ModEngineering/blob/main/docs/CONVENTIONS.md)。
