<!-- hy-mt2-i18n:start -->
[English](./README.md) | **中文** | [日本語](./README_ja.md) | [Español](./README_es.md)
<!-- hy-mt2-i18n:end -->

<div class="header" align="center">
<img alt="Frontier Station" height="300" src="https://github.com/new-frontiers-14/frontier-station-14/blob/master/Resources/Textures/_NF/Logo/logo.png?raw=true" />
</div>

Frontier Station 是基于 [Space Station 14](https://github.com/space-wizards/space-station-14) 分支开发的项目，它运行在用 C# 编写的 [Robust Toolbox](https://github.com/space-wizards/RobustToolbox) 引擎之上。

这是 Frontier Station 的主仓库。

如果您想为 Frontier Station 托管或创建内容，那么这就是您需要的仓库。它既包含了 RobustToolbox，也提供了用于开发新内容包的相关资源。

## 链接

<div class="header" align="center">

[Discord](https://discord.gg/rKNHDAGPvd) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Patreon](https://www.patreon.com/frontierstation14) | [Wiki](https://frontierstation.wiki.gg/)

</div>

## 文档/维基页面

我们的[Wiki](https://frontierstation.wiki.gg/)上有关于Frontier Station内容的文档。

## 贡献方式

我们欢迎任何人的贡献。如果您想帮忙，请加入我们的 Discord 频道。我们有[待完成的任务列表](https://discord.com/channels/1123826877245694004/1127017858833068114)，任何人都可以着手处理。遇到问题也别犹豫，随时寻求帮助！

我们目前不在主仓库中接受游戏翻译工作。如果您希望将游戏翻译成其他语言，可以考虑创建一个分支或为某个分支做出贡献。

如果您要做出任何贡献，请注意，对上游项目中的文件所做的任何修改都应通过注释进行适当标注（详见[CONTRIBUTING.md](https://github.com/new-frontiers-14/frontier-station-14/blob/master/CONTRIBUTING.md)中的“对上游文件的修改”部分）。

## 编译

1. 克隆该仓库：
```shell
git clone https://github.com/new-frontiers-14/frontier-station-14.git
```
2. 进入项目文件夹并运行 `RUN_THIS.py` 以初始化子模块并加载引擎：
```shell
cd frontier-station-14
python RUN_THIS.py
```
3. 编译解决方案：

使用 `dotnet build` 命令构建服务器。

[关于项目构建的更详细说明。](https://docs.spacestation14.com/en/general-development/setup.html)

## 许可证

请阅读[LEGAL.md](https://github.com/new-frontiers-14/frontier-station-14/blob/master/LEGAL.md)，了解有关代码许可的法律信息，其中包含代码库中每个命名空间的归属说明表。

除另有说明外，大多数资源的许可均为 CC-BY-SA 3.0。资源的许可协议及版权信息均存储在元数据文件中。示例。

从Emberfall中摘取的代码已在获得MilonPL的[许可](https://github.com/new-frontiers-14/frontier-station-14/pull/3607)后，根据MIT条款重新授权使用。

[2fca06eaba205ae6fe3aceb8ae2a0594f0effee0](https://github.com/new-frontiers-14/frontier-station-14/commit/2fca06eaba205ae6fe3aceb8ae2a0594f0effee0) 于 2024 年 7 月 1 日世界标准时间 16:04 被推送。

除非另有说明，否则大多数资源的许可均为 [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/)。相关资源的许可协议及版权信息均记载在元数据文件中。例如，可查看[撬棍的元数据](https://github.com/new-frontiers-14/frontier-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json)。

请注意，部分资源的许可协议为非商业用途的[CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/)或类似的非商业许可协议，如果您希望将此项目用于商业用途，则需要移除这些资源。
