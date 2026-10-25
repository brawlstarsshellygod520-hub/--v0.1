# 窗口導管驗證結果

開始：2026-10-22；完成：2026-10-23（Asia/Taipei）。Unity：6000.5.0b9。

## 結果

| 入口 | 結果 | 證據 |
| --- | --- | --- |
| `WindowHoseBuilder.BatchInstallAndValidate` | exit 0；編譯、保存重開、接點與輸入引用、Visual分離、重跑不覆寫通過 | `install.txt`；`Logs/window-hose-install.log` |
| `WindowHoseValidation.BatchRun` | exit 0；最終50項Play Mode檢查通過 | `playmode.txt`；`Logs/window-hose-playmode.log` |
| `WaterGameplayValidation.BatchRun` | exit 0；27項回歸通過 | `water-regression.txt`；`Logs/window-hose-water-regression.log` |
| `PowderGameplayValidation.BatchRun` | exit 0；36項回歸通過 | `powder-regression.txt`；`Logs/window-hose-powder-regression.log` |

本輪共113項檢查。水與乾粉回歸在最後一次「只限固定路由的視覺下垂限幅」前執行；其後未改來源、輸入、反應、直線管長或直線呈現公式，最後重跑窗口50項驗收該顯示修正。不是完整測試套件、硬體輸入、Player build或多人驗收。

最終四份log無C#編譯錯誤／測試失敗／runtime例外；仍有既有FindObjectsByType等CS0618警告。

## 實際遊玩證據

- 不是直接連接／滅火API取代整個流程：Play Mode中enabled LocalPlayerInput共用命令驅動CharacterController，由出生點進屋、F接水、繞廚房與後門進庭院，再走到窗外F固定。
- 近窗口再F解除僅清路由；關閉Visual仍可互動／限制移動；原實體牆阻止角色穿窗，退開也以實際走路完成，沒有傳送跳過路段。
- 固定點為(-8.07,1.15,2.45)；LineRenderer確實包含該頂點，另檢查窗口附近線段不穿實體牆及窗台。室外移動時折線長度明顯大於來源直線距離。
- 戶外普通火經Game Camera螢幕座標→ResolveAimPoint→右鍵命令→噴射／反應熄滅，鬆鍵停止。
- 往z=12走時，實際停在約(-8.82,0.02,10.63)，符合13m折線餘長圓，不是地圖外圍牆阻擋。
- API夾具另驗無來源／超距拒絕、可互動但路由超13m拒絕並保留原路由、兩名HoseConnection狀態獨立、換來源／斷接、停用／刪除窗口回退。

實際Game Camera圖像：`01-window-fixed.png`、`02-outside-routed.png`、`03-outdoor-fire-extinguished.png`。已檢視固定與移動狀態，沒有以概念圖替代。

## 驗證中修正

1. 前門外立即向西被已開門板擋住，向南繞又超13m：保留失敗報告／圖／log，改走既有後門與庭院，不改門或管長。
2. 舊地圖測試的後門接近點與新乾粉瓶重疊：保留第二份失敗報告，改繞來源東側，不關碰撞、不用傳送。
3. 首次47項通過後，截圖發現短管段套用長管下垂量而穿窗台：保留修前圖，固定路由的下垂／擺幅按每段長度限幅。新增窗口淨空與退離牆的走路檢查後，最終50項通過。

## 變更保護

`change-audit.json`：修改前402個Assets／Packages／ProjectSettings檔中，只變更HoseConnection.cs、HoseView.cs、LocalPlayerInput.cs、TutorialGreybox.unity；398檔SHA256一致。新增17個Assets檔含meta，無刪檔、無Missing Meta，既有GUID／Prefab／模型／Packages／Settings未改。

Scene diff只有171新增行：窗口marker、兩個Prefab instance、玩家窗口引用；沒有重存或改動其他場景物件。備份在 `Archive/WindowHoseBaseline-2026-10-22`；舊水／乾粉報告與截圖已先備份，收集本輪回歸報告後恢復原證據檔。

## 尚未涵蓋

- 單人測試，來源無限且非獨占；兩個API夾具不代表四人連線完成。
- 手動單一固定點，不是整條供應管的碰撞、繞牆或軟體物理。其他牆角仍可能穿管。
- 窗框位於剖面牆上，沒有切改使用者原始.blend；不能爬窗。
- 未增加破門、教學UI／流程、正式美術、容量、氣體／通風或傷害系統。
