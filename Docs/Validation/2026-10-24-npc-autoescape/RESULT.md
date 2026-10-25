# NPC路通自動逃生：驗收結果

日期：2026-10-24。Unity 6000.5.0b9，Windows，Built-in。
狀態：本次功能完成；未做Player build或實體鍵鼠手感測試。

## 行為

不需玩家靠近或按E。NPC從WaitingForSafeRoute開始，自主尋找完整安全路線；有路即走，火／固定障礙阻路則等待，路線恢復後繼續。開場路通就直接逃生，不要求其他房間的火全滅。

移除TryCalm／IsCalmed／WaitingForHelp／安撫距離與牆阻互動；移除E鍵讀取及LocalPlayerInput的NPC清單。歷史命令calm參數、Initialize的survivors參數僅保留呼叫相容並忽略，沒有安撫行為。場景舊序列化npcs資料不再被使用，未為清除未用資料重存場景。

## 實際執行

| 入口 | 證據 | 結果 |
| --- | --- | --- |
| `FireRescue.Editor.NpcAutoEscapeValidation.BatchRun` | `playmode.txt`；`Logs/npc-autoescape-2026-10-24.log` | 18項PASS，Unity exit 0 |
| `FireRescue.Editor.HandlingValidation.Run` | `handling-regression.txt`；`Logs/npc-autoescape-handling-2026-10-24.log` | 25項PASS，Unity exit 0 |

合計43項Play Mode檢查。C#實際編譯通過，無新增編譯／Runtime錯誤；既有驗收檔CS0618警告保留。原`Logs/handling-results.txt`已備份並恢復，本輪另存報告，不覆寫歷史結果。

### 核心證據

- 真正當前TutorialGreybox／Step4 User Layout，而非停用的舊組裝：保持NPC的原Autonomous=true與火場模擬，輸入裝置讀取關閉，未送安撫命令，NPC透過實際Update抵達原出口。
- 玩家僅在戶外出生點向旁邊走30幀，避免身體堵出口，未靠近NPC；未傳送NPC、移動出口、改火種或保存場景。
- 獨立fixture無附近玩家：安全通路自行逃生；初始普通火或Layer8實牆阻路時等待，施水／移牆後實際走到出口。
- 移動途中在剩餘路線起火會停下；清火後恢復並完成救援。Visual停用也不影響尋路／逃生。
- 救援計數與完成事件各一次，重複Tick不重複計算。
- Handling回歸：WASD方向、輸入失焦／停用、噴射生命週期、接管牆阻、油地慣性與碰撞；玩家CharacterController阻路時NPC不穿人，玩家離開後繼續完成路線。

## 修改與保護

- Runtime只改`RescueNpc.cs`、`LocalPlayerInput.cs`；沿用原安全判定、尋路演算法及碰撞。
- 7份舊驗收同步移除安撫API／前置條件；原本需固定測試時序的案例仍由Autonomous開關控制。未假設所有歷史場景驗收均已重跑：本輪只執行上表兩套，其餘通過C#編譯及針對性diff檢查。
- 新增`NpcAutoEscapeValidation.cs`與Unity產生的meta；無新增套件或架構系統。
- `asset-audit.json`：430檔基線中421檔不變、9個既有腳本改動、新增2檔、無刪除；Missing Meta=0。Scene、Prefab、模型、既有GUID、Packages、ProjectSettings均不變。
- 備份`Archive/NpcAutoEscapeBaseline-2026-10-24`；工作紀錄`Docs/WORKLOG.md`。未commit／push／清理工作樹。

## 未擴張的範圍

不做消防斧破門、3m來源／窗口重新配置、教學、NPC傷害、多人或正式UI。安全路線沿用FireField與固定障礙判定；獨立油鍋ObjectFire與NPC安全路線的額外整合仍是原有後續工作，不能宣稱本輪已完成。
