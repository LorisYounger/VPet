# MOD 目录说明

除 `0000_core` 外，这里以前存放的 `1101_EdgeTTS`、`1110_ChatGPT`、`1100_DemoClock` 等文件只是指向开发者本机路径的文本占位符，并不是可用的插件，对源码构建没有作用。

官方插件的源码与编译产物在独立仓库维护：

https://github.com/LorisYounger/VPet.Plugin.Demo

## 安装方法

1. 从上面的仓库下载或编译出插件文件夹（例如 `VPet.Plugin.EdgeTTS/1101_EdgeTTS`）
2. 将整个文件夹复制到本目录下
3. 重启桌宠，或在 设置面板 → MOD 管理 中启用后重启

注意：MOD 的启用标识使用 info.lps 里 `vupmod#名字` 的名字（小写），与文件夹名可能不同。
