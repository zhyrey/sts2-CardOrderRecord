# Card Order Record

[English](README_en.md)

一个用于《Slay the Spire 2》的战斗出牌顺序记录 Mod。

本 Mod 会在暂停菜单中加入两个按钮：

- `Quick Restart`：快速重新开始当前战斗。
- `Card Order`：打开本场战斗的出牌顺序记录界面。

`Card Order` 界面会按回合列出当前战斗中发生过的卡牌事件，例如抽牌、打出、消耗、弃牌、生成、保留、附魔、费用变化、移动到手牌等。右侧的 `History` 列表会保存之前几次战斗或快速重开前的出牌记录，方便对比不同打法。

## 功能

- 在暂停菜单中新增 `Quick Restart` 按钮。
- 在暂停菜单中新增 `Card Order` 按钮。
- 按回合记录当前战斗的卡牌事件。
- 使用类似游戏历史记录界面的卡牌展示方式。
- 点击记录中的卡牌可以打开游戏内置卡牌详情界面。
- 支持右侧历史记录列表：`Current`、`History 1`、`History 2` 等。
- Quick Restart 期间会过滤旧战斗迟到事件，避免上一轮的抓牌混进新记录。
- 只包含 DLL 和 manifest JSON，不需要 `.pck` 文件。

## 安装

1. 打开本项目的 GitHub Releases 页面。
2. 下载最新的 `CardOrderRecord-vX.X.X.zip`。
3. 解压后会得到一个 `CardOrderRecord` 文件夹。
4. 将整个 `CardOrderRecord` 文件夹放到游戏目录的 `mods` 文件夹中。

最终目录结构应类似：

```text
Slay the Spire 2/
  mods/
    CardOrderRecord/
      CardOrderRecord.dll
      CardOrderRecord.json
```

如果游戏目录下没有 `mods` 文件夹，可以手动创建。

启动游戏后，如果游戏提示进入 Modded 模式，确认即可。之后可以在游戏设置中的 Mod 相关界面启用或禁用本 Mod。

## 使用方法

1. 进入一场战斗。
2. 打开暂停菜单。
3. 点击 `Card Order` 查看当前战斗的出牌顺序。
4. 在 `Card Order` 界面右侧点击 `History` 展开历史列表。
5. 点击 `Current` 查看当前战斗，点击 `History 1`、`History 2` 等查看之前保存的记录。
6. 点击记录中的卡牌可以查看该卡牌详情。
7. 点击游戏自带返回按钮或按返回键关闭界面。

`Quick Restart` 位于 `Card Order` 上方。点击后会读取游戏自动存档并重新加载当前战斗。该功能只设计用于单人模式。

## 记录内容

当前版本会尽量记录这些具有牌序意义的事件：

- `Played`：打出卡牌。
- `Drew`：抽牌。
- `Discarded`：弃牌。
- `Exhausted`：消耗。
- `Generated`：生成卡牌。
- `Retained`：保留卡牌。
- `Enchanted`：添加附魔。
- `Cost`：费用变化。
- `Moved` / `To Hand` / `To Draw` / `To Discard`：卡牌移动。
- `Upgraded` / `Downgraded`：升级或降级。
- `Keyword+` / `Keyword-`：关键字变化。
- `Afflicted` / `Afflict-`：负面效果变化。
- `Enchant-`：清除附魔。

连续的系统抽牌、连续的同来源事件会尽量合并在同一行中，让界面更紧凑。

## 注意事项

- History 目前保存在内存中，关闭游戏后不会保留。
- 本 Mod 标记为 `affects_gameplay: false`，主要用于记录和查看信息。
- `Quick Restart` 依赖游戏自动存档。如果自动存档不存在，按钮可能不可用或无法完成重开。
- `Quick Restart` 主要参考单人模式流程实现，不建议在多人模式中使用。
- 由于《Slay the Spire 2》仍处于更新中，游戏内部 API 变化可能导致 Mod 需要适配。

## 从源码构建

需要本机能正常编译 Slay the Spire 2 的 C# Mod，并安装 .NET SDK。

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

只构建，不复制到游戏 `mods` 目录：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -SkipModCopy
```

Linux / macOS:

```bash
./build.sh
```

构建脚本会尝试自动寻找 Steam Library 中的 `Slay the Spire 2`。如果自动检测失败，可以手动传入游戏目录或 Steam Library 路径。

## 参考

- [freude916/sts2-quickRestart](https://github.com/freude916/sts2-quickRestart)：本 Mod 的快速重开按钮与读档重开流程参考了该项目。
- [Slay the Spire 2 Modding Tutorials](https://tutorials.sts2modding.com/)：项目结构、Harmony patch、Mod 本地加载与开发流程参考了该教程站。

## 许可证

本项目目前未声明开源许可证。使用、修改或再发布前请先联系作者。
