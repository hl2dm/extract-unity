# 極速撤離 Extract — 休閒版（Unity）

一款粉蠟筆風格的「搜刮撤離」小遊戲：潛入阿姨家搜刮零食，用罐頭和肉肉安撫貓狗，在時間內趕上公車撤離回家！

**▶ 線上試玩（WebGL）：https://hl2dm.github.io/extract-unity/**
（第一次載入約 110 MB，建議使用桌機版 Chrome / Edge / Firefox。）

Windows 版下載：見 [Releases](https://github.com/hl2dm/extract-unity/releases)。

## 操作方式
| 按鍵 | 動作 |
|---|---|
| W A S D | 移動 |
| 滑鼠 | 轉動視角 |
| Shift | 跑步 |
| 空白鍵 | 跳躍 / 結算畫面返回基地 / 跳過過場 |
| E | 互動（搜刮、開門、上公車） |
| 1 / 2 或滾輪 | 切換罐頭 / 肉肉 |
| 滑鼠左鍵 | 丟出罐頭或肉肉（安撫、引開貓狗） |
| F | 搜刮時全部拿走 |
| Q | 搜刮時付費跳過等待 |
| Esc | 放開滑鼠 / 跳過過場 |
| F8 | 切換反鋸齒畫質（低 / 中 / 高） |

> 目前可遊玩的地圖：**阿姨家**。在倒數結束前走到公車站上車即可撤離成功。

## 專案說明
- 引擎：Unity 6（6000.4.8f1），URP。
- 開啟方式：用 Unity Hub 加入此資料夾，開啟 `Assets/Scenes/Bootstrap.unity`。
- 場景幾乎全部由程式產生（`Assets/Scripts/SoftKit.cs`、`SoftProps.cs`、`AuntMap.cs`），風格著色器在 `Assets/Resources/Shaders/`。
- `review/`：美術方向、評審與迭代紀錄。

## 授權與素材來源
素材來源請見 [CREDITS.md](CREDITS.md) 與 `Assets/Licenses/`。第三方素材皆為 CC0 或 SIL OFL 1.1（字型）。
