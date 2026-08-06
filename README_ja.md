<!-- hy-mt2-i18n:start -->
[English](./README.md) | [中文](./README_zh-CN.md) | **日本語** | [Español](./README_es.md)
<!-- hy-mt2-i18n:end -->

<div class="header" align="center">
<img alt="Frontier Station" height="300" src="https://github.com/new-frontiers-14/frontier-station-14/blob/master/Resources/Textures/_NF/Logo/logo.png?raw=true" />
</div>

Frontier Stationは、C#で記述された[Robust Toolbox](https://github.com/space-wizards/RobustToolbox)エンジン上で動作する[Space Station 14](https://github.com/space-wizards/space-station-14)のフォークです。

これがFrontier Stationのメインリポジトリです。

Frontier Stationのコンテンツをホストしたり作成したりしたい場合、必要なのがこのリポジトリです。ここにはRobustToolboxに加え、新たなコンテンツパックを開発するためのコンテンツパックも含まれています。

## リンク集

<div class="header" align="center">

[Discord](https://discord.gg/rKNHDAGPvd) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Patreon](https://www.patreon.com/frontierstation14) | [Wiki](https://frontierstation.wiki.gg/)

</div>

## ドキュメント／ウィキ

私たちの[Wiki](https://frontierstation.wiki.gg/)には、Frontier Stationのコンテンツに関するドキュメントが掲載されています。

## 貢献の仕方

どなたからの貢献も心より歓迎いたします。協力を希望する方はDiscordにご連絡ください。実施可能なアイデアの[一覧](https://discord.com/channels/1123826877245694004/1127017858833068114)も用意しており、誰でもそれらに取り組むことができます。助けを求めることも遠慮しないでください！

現在、メインリポジトリではゲームの翻訳を受け付けていません。別の言語にゲームを翻訳したい場合は、フォークを作成するか、フォークへの寄稿を検討してください。

何か貢献を行う場合は、上流プロジェクトに属するファイルに加えられた変更は必ずコメントで適切にマークするようにしてください（[CONTRIBUTING.md](https://github.com/new-frontiers-14/frontier-station-14/blob/master/CONTRIBUTING.md)の「Changes to upstream files」セクションを参照してください）。

## ビルド

# 基本的な手順
1. このリポジトリをクローンします：
```shell
git clone https://github.com/new-frontiers-14/frontier-station-14.git
```
2. プロジェクトフォルダに移動し、サブモジュールを初期化してエンジンを読み込むために `RUN_THIS.py` を実行します：
```shell
cd frontier-station-14
python RUN_THIS.py
```
3. ソリューションをコンパイルします：

`dotnet build`を使用してサーバーをビルドします。

# プロジェクトのビルドに関するより詳細な手順はこちらです。[More detailed instructions on building the project.](https://docs.spacestation14.com/en/general-development/setup.html)

## ライセンス

コードのライセンスに関する法的な情報、およびコードベース内の各ネームスペースの出典一覧表については、[LEGAL.md](https://github.com/new-frontiers-14/frontier-station-14/blob/master/LEGAL.md) をご覧ください。

特に明記がない限り、ほとんどのアセットはCC-BY-SA 3.0ライセンスの下で提供されています。アセットのライセンス情報および著作権情報はメタデータファイルに記載されています。例：

Emberfallから取得したコードは、[MilonPLの許可](https://github.com/new-frontiers-14/frontier-station-14/pull/3607)により、MITライセンスの条件で特別に再ライセンスされています。

[2fca06eaba205ae6fe3aceb8ae2a0594f0effee0](https://github.com/new-frontiers-14/frontier-station-14/commit/2fca06eaba205ae6fe3aceb8ae2a0594f0effee0) は、2024年7月1日 16:04 UTCにプッシュされました。

特に明記がない限り、ほとんどのアセットは[CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/)のライセンスで提供されています。アセットのライセンスおよび著作権情報はメタデータファイルに記載されています。例として、[crowbarのメタデータ](https://github.com/new-frontiers-14/frontier-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json)をご覧ください。

一部のアセットは非営利目的向けの[CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/)またはそれに類する非営利ライセンスの下で提供されているため、本プロジェクトを営利目的で利用したい場合はこれらを削除する必要があります。
