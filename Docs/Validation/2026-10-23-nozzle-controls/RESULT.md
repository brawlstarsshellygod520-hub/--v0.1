# 使用者噴槍與分段點擊操作：驗收

完成日期：2026-10-24，工作／證據起始10-23。Unity 6000.5.0b9，Windows，Built-in。

## 結果

| 層級 | 入口／證據 | 結果 |
| --- | --- | --- |
| 匯入、C#編譯、持久化 | `UserNozzleSetup.BatchInstallAndValidate`；`install.txt`；`Logs/nozzle-controls-install.log` | PASS：FBX方向／.55m尺度、前後獨立接點、場景3m、保存重開、重跑不覆寫 |
| 新操作與實景Play Mode | `NozzleControlsValidation.BatchRun`；`playmode.txt`；`Logs/nozzle-controls-playmode-rerun.log` | 43項PASS，包含真CharacterController從出生點走至水箱，不使用F替代第二下右鍵 |
| 既有水反應回歸 | `WaterReactionValidation.BatchRun`；`water-reaction-regression.txt`；`Logs/nozzle-water-reaction-regression.log` | 25項PASS，既有Prefab獨立fixture，不宣稱覆蓋新點擊順序 |
| 畫面 | `01-near-source-gold.png`、`02-equipped-connected-blue.png`、`03-spraying.png` | 已由主代理檢視Game Camera；來源圈可見、噴槍方向正確、管白／水藍 |
| 保護檔案 | `asset-audit.json`對`baseline.json` | 原419檔中413不變，6改動，11新增Assets檔含meta；無刪除、無Missing Meta |

Play Mode合計68項。此輪沒有Player build或實體鍵鼠遊玩手感驗收；不把編譯成功等同完整遊戲完成。既有CS0618警告保留，無新C#編譯錯誤。

## 覆蓋

- 第一個裝備／接管長按不得提前噴或砍；接上後下一下才噴；放開噴射即停；點斧一次完整一刀且只有一次命中，按住才重複。
- 真正同時按住左右鍵時右鍵優先，切回已接管工具先裝備、下一下才噴；失焦／暫停／停用／零dt解除動作，需要新按下才恢復。
- 距離外／牆後不能接來源也沒有提示；只提示最近可達來源，未接金色／已接藍色；來源Visual關閉不影響玩法。
- 白管兩端為來源Port／噴槍Hose Inlet，水從不同的Nozzle接點噴出；無模型Collider代替Gameplay。
- 玩家持續往3m以外移動，停在約(-6.22,.02,2.18)，最後45幀不再移動，UsedLength約3m；離開2m來源圈隱藏。
- 獨立窗口fixture在3m內可固定並限制折線；現有西窗因前段已大於3m正確拒絕，不放寬數值讓測試過關。
- 實景水反應使用Play Mode臨時普通ObjectFire目標，經Game Camera螢幕瞄準且強度實際下降；`03-spraying.png`刻意停用來源Visual以測模型分離。此目標不保存進場景，這不是3m下完整初始火場／NPC逃生／窗口路線全程驗收。

## 首輪失敗與修正

首輪39項通過後，driver呼叫「必須抵達目標」的Walk去走一個故意超過3m的位置，因角色正確受限而失敗。改用持續推向界線並觀察穩定位置，保留3m、座標、離開互動圈斷言；沒有為過測試改Runtime。原完整log與failure.png保留，摘要`first-run-driver-failure.txt`。最終43項為同一批重新實際執行，不是合併舊結果。

## 資產與資料保護

- 只改五個既有Runtime：HoseConnection、HoseView、SprayEmitter、FirefighterTools、LocalPlayerInput，以及Tutorial場景。舊Prefab、戶型、模型、GUID、Packages、Settings與先前工作樹保留。
- 新SourceHighlightView、模型／Prefab、Editor安裝與驗收入口均分離；沒有commit、push或清除舊檔。
- 匯出來源快照SHA256為`158D98C1380AA118DD0ABA11284F557CA032111BFF707B0F69DE36004ADA67B1`。後續檢查發現Downloads原檔已另存、二進位hash不同；只讀Blender檢查確認兩者7份可見mesh的評估幾何、變換、材質、單位完全相同。詳細`source-comparison.txt`，沒有覆寫原檔或使用者Blender視窗。
- 備份`Archive/NozzleControlsBaseline-2026-10-23`。舊水驗收report已恢復，當輪回歸另存此目錄，不覆蓋歷史證據。

## 剩餘與限制

本次噴槍／管線／操作需求已完成。當前3m不支援水箱至西窗約4.8m的歷史路線；需後續關卡配置調整後重新走完整循環。消防斧只有揮砍與命中，破門仍待下一步；沒有新教學UI、正式手部動畫、多人、一般管線碰撞或自動繞牆。
