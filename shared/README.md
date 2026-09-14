# 共享源码

Utility 来源：https://github.com/anosu/Utility.git

固定提交：`33327bd6d95f4d4e9ddc93cd4823879f31d05c69`，与本次参考的 GCMod-Android/shared/Utility 一致。

`shared/Utility` 是固定在上述提交的 Git submodule，包含其许可证和编译引用。克隆后运行 `git submodule update --init --recursive`；CI 已启用递归子模块检出。

构建入口为 `build/SharedDependencies.props`，公共目标由 Utility/build/Mod.Shared.targets 提供。共享库只从源码构建，不维护 Utility.dll 二进制副本。
