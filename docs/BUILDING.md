# 构建和发布

安装 .NET 8 SDK 和 PowerShell 7。使用 `AyarabuMod-Android.slnx` 打开项目需要支持 slnx 的 IDE / SDK；命令行和 CI 直接构建 csproj，不依赖 slnx 支持。

```powershell
git submodule update --init --recursive
dotnet tool restore
dotnet tool run csharpier check .
dotnet test AyarabuMod.Tests/AyarabuMod.Tests.csproj -c Release
pwsh -NoProfile -File scripts/build-release.ps1 -Configuration Release -ExpectedVersion v1.0.0
```

修改代码后可用 `dotnet tool run csharpier format .` 统一格式。

Visual Studio 普通开发打开 `AyarabuMod-Android.slnx`，其中包含 Mod、测试和仓库内的 Utility 项目。联合开发共享库时打开 `AyarabuMod-Android.local.slnx`，搭配 `SharedDependencies.local.props` 使用 `../Utility/Utility/Utility.csproj`。这两个本地文件被 Git 忽略，路径必须指向同一份 Utility 源码。

命令行存在本地配置时也会采用该覆盖。若要验证仓库固定源码，传入 `-p:UsePinnedSharedDependencies=true`；CI 始终忽略本地配置。更新解决方案项目列表后，在 VS 重新加载解决方案并生成，使设计时引用指向的 Utility.dll 就绪。

发布脚本从 `AyarabuMod/AyarabuMod.csproj` 读取版本，验证 `Core/ModInfo.cs` 编译后的版本常量和可选的 `ExpectedVersion`。它生成固定 ZIP 时间戳、核验内部部署路径和文件哈希，并写入 SHA256SUMS.txt。构建进程默认超时 600 秒，可通过 `TimeoutSeconds` 调整。

```text
artifacts/release/v<version>/
├── AyarabuMod-Android.zip
└── SHA256SUMS.txt
```

ZIP 包含 `Mods/AyarabuMod/AyarabuMod.dll` 和 `Mods/AyarabuMod/Utility.dll`。Utility 通过 `build/SharedDependencies.props` 和 ProjectReference 从 `shared/Utility` 源码构建，为 Toast 提供支持；不需要 Extension。

`shared/Utility` 使用固定提交的 Git submodule，来源见 `shared/README.md`。CI 递归检出子模块，无需本机外部目录。新克隆可将 `AyarabuMod-Android.local.slnx.example` 和 `SharedDependencies.local.props.example` 复制为去掉 `.example` 后缀的本地文件，并调整共享源码路径；CI 强制使用固定的子模块源码。

`.github/workflows/build.yml` 在 push、pull_request 和手动触发时检查格式、测试和构建，并上传 Actions artifact。`v*` 标签额外创建 GitHub Release；已有同名 Release 时更新附件。标签必须和 csproj / ModInfo 版本一致。发布工作流需仓库授予 `contents: write`，已在 YAML 中声明。

CI 使用仓库中的最小编译引用，不需要原始 APK、Tools 目录或完整 Interop 备份。游戏程序集与加载器程序集不放入发布 ZIP。
