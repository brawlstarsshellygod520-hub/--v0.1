# 住宅 Greybox 整合驗證（2026-10-25）

狀態：核心流程／Windows 建置通過；Windows 實際鍵鼠遊玩驗收尚未完成。

## 驗收對照

| 項目 | 證據 | 結果 |
| --- | --- | --- |
| 消防斧破門 | 三次實際工具命中；每刀一次；破後玩家與 NPC 穿過；模型／碰撞分離 | 通過 |
| 3m 管線配置 | 從出生步行接水，固定北窗後約2.87m，實際水柱熄滅窗外物件火；長度未增加 | 通過 |
| NPC 整合 | 門開但火未清仍等待；路通自行抵出口；物件火復燃停止再重判；沒有安撫 | 通過 |
| 關卡統計 | 地板已滅但油鍋仍燃燒時不誤報；空／重複引用去重；救援事件恰一次 | 通過 |
| 重新 Play | 退出 Play 後再次進入，門耐久、火點、NPC、來源連接與玩家出生點乾淨 | 通過 |
| 模型替換邊界 | 門 View 替換／停用不影響受擊和碰撞；物件火 View 停用仍阻路並列入統計 | 通過 |
| Windows x64 建置 | Mono Development、指定 TutorialGreybox，0 errors／0 warnings，產物153,892,582 bytes | 通過 |
| Windows 鍵鼠完整試玩 | 曾啟動 Player，但操作工具被使用者 Escape 中斷；無完整操作證據 | 未完成 |

## 執行入口與證據

- `FireRescue.Editor.ResidentialIntegrationBuilder.BatchInstallAndValidate`：場景保存重開、引用、模型邊界、安裝冪等，exit 0；[install.txt](install.txt)。
- `FireRescue.Editor.ResidentialIntegrationValidation.BatchRun`：完整流程39項＋第二次 Play 4項，exit 0；[playmode.txt](playmode.txt)。
- `FireRescue.Editor.BreakableDoorValidation.BatchRun`：21項（含4項短管窗口互動負向／切換測試），exit 0；[破門／統計／路由紀錄](../2026-10-25-breakable-door/playmode.txt)。
- `FireRescue.Editor.ResidentialIntegrationBuilder.BatchBuildWindows`：exit 0；[build.txt](build.txt)。
- 合計 **64 項 Play Mode 檢查**。這些是專案自訂批次驗收，不冒充 NUnit Test Runner 的64個測試案例。
- 整關測試透過共用輸入命令驅動真 CharacterController、工具、噴流與元素反應，主流程沒有傳送或直接刪火。這與實體鍵鼠輸入驗證不同。
- 最後針對 F 的失敗路由分支補修後，重跑21項針對性測試並重建 Windows；成功路由的整關43項未再次重跑，既有報告保留。

## 發現與處理

1. 馬桶碰撞會擋到北側火點的水柱：修正測試步行路線，先繞至側邊，不移除碰撞。
2. 主室靠近沙拉油時按 F，依既有規則是換接而非拔管：修正腳本先回原水箱旁再拔管，不改換接規則。
3. 窗口可互動但折線超過3m時，F原本會被吞掉：改為固定失敗時退回來源／斷管，新增負向檢查。原有失敗紀錄保留在 attempt1／attempt2 檔。

## 變更範圍

與前回合的432檔基線比對：426檔不變、6檔修改、無缺失，新增13個 Assets 檔案（含 meta）。6個修改為 TutorialGreybox、FirefighterTools、RescueNpc、HoseConnection、RescueGameManager、ObjectFire。原戶型網格、既有Prefab、Packages與ProjectSettings相對該基線不變。

基線：`../2026-10-24-residential-integration/baseline.json`。此比較不是與最初 Git v0.1 比較；公開提交會包含之前累積的消防專案進度。

## 限制與下一步

先由使用者完成 Windows 鍵鼠試玩與手感回饋。仍為單人、本機、無限來源、手動單點窗口；沒有整條消防管碰撞／自動繞牆、正式UI、多人或完整美術。不要把建置成功或自動命令通過等同於所有硬體輸入都驗收完成。
