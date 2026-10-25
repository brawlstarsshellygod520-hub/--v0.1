# 消防教學 Greybox：執行與驗收計畫

更新：2026-10-21。使用者改為「住宅四個基本機制，先做遊戲機制與架構，不做教學流程」。
最新方向以 `Docs/RESIDENTIAL_GAMEPLAY_ROADMAP.md` 為準；水機制配置見 `Docs/WATER_GAMEPLAY.md`。
下方九步工程表保留作歷史，不再表示當前教學順序或後續自動授權；尤其不要直接續做舊版油教學或教學 UI。

## 產品界線

單人先驗證：進屋找日常來源 → 接管 → 拉管 → 噴射／水油反應 → 清出路線 → NPC 自主逃生。
角色／家具用Unity Primitive，牆／門依最新授權使用第一關.blend；不等待正式美術，不新增ProBuilder或連線套件。
第三步三房格局已由使用者戶型取代；水源是馬桶水箱、油源是沙拉油瓶。
不新增工業來源、工作台、儲物櫃、正式 UI、美術或多人連線。

## 新操作合約（第四步已實作）

| 輸入 | 教學關卡目標行為 |
| --- | --- |
| WASD | 螢幕方向移動；斜向不加速 |
| 左鍵 | 拿出消防斧並揮砍 |
| 右鍵 | 拿出消防管並噴射；必須已接上來源才有流體 |
| F | 沿用接上／切換／解除來源，不改成拿斧或噴射 |
| E | 沿用靠近 NPC 安撫 |

操控階段暫定按住左鍵連續揮砍、按住右鍵持續噴射；放開停止，斧與管互斥，切換／失焦／停用時中止舊動作。
雙鍵同按右鍵水管優先。斧頭每週期命中最近一個碰撞體，略過持有者；不增加破壞或傷害。
原 `ResidentialFire` 仍是 **左鍵噴射**；只有Tutorial配置新工具。

## 分階段工作與過關條件

| 步驟 | 工作 | 驗證過關條件 |
| --- | --- | --- |
| 1 | 保存基線、建立獨立 `TutorialGreybox` 場景骨架 | 真 Unity 編譯；新舊場景可載入且無 Missing Script；重跑建立不覆寫；既有檔案與 Build Settings 不變 |
| 2 | 玩法根節點與 `Visual` 分離，Collider／互動／出口與噴嘴 Anchor 留在玩法層 | 關閉／替換 Visual 不影響碰撞、來源連結、噴流、火場和 NPC；玩法不得查模型名稱或 Renderer bounds 判定規則 |
| 3 | Primitive 三房一廳一衛、廚房、走廊、正門與室外；斜俯視相機 | 玩家尺度與门口可通行；前景牆不整片消失；能看清自己與附近房間，不一次看全圖 |
| 4 | WASD、斧／管切換、瞄準、揮砍、接來源與拉管 | 左鍵揮斧且不噴流；右鍵噴流且不揮斧；停止／切換無殘留；接管距離與牆阻正常；斧可透過相同玩法入口被測試，不綁動畫事件 |
| 5 | 水教學：馬桶水箱 → 噴水 → 熄火／推物件 | 無來源不能噴出元素；持續水降低火強度至熄滅；水能推物件，牆後不能命中 |
| 6 | 油教學：沙拉油瓶 → 油地 → 遇火助燃 → 水處理火勢 | 油地較滑，遇火助燃；水可降低火勢；火焰邏輯不依賴視覺；不額外假設水會清除油膜 |
| 7 | NPC 安撫與完整安全逃生路線 | 未安撫／路線仍有火時不逃；清出完整路線後自主走到出口；阻路時停下，恢復後續走，不能穿牆 |
| 8 | 最小教學順序、完成狀態與重新測試 | 未完成先決條件不跳步；救出 NPC 正確完成；重新 Play 狀態重置；只加必要提示，不做正式 UI |
| 9 | 模型替換驗收與整輪回歸 | 替換人物／來源／家具／火焰 Visual 後，不改核心程式即可完成接水、拉管、噴射、反應、NPC 自主逃生 |

## 第一步場景架構（歷史基線）

`Assets/FireRescue/Scenes/TutorialGreybox.unity` 是獨立場景，不覆寫 `ResidentialFire` 或 `FireRoom`。

```text
TutorialGreybox
├─ Environment    （後續 Primitive 地圖）
├─ Actors         （後續玩家與 NPC）
├─ Sources        （後續水箱／油瓶玩法）
├─ Objectives     （後續教學／出口）
└─ Presentation
   ├─ Main Camera （48°／40°正交相機骨架，尚未接跟隨）
   └─ Directional Light
```

第一步完成時沒有地板、房間、玩家、斧、來源、火或 UI。第二步已另加元件測試平台，見下方目前進度。
`FireRescue > Tutorial > 建立或開啟 Greybox 教學場景（不覆寫）` 只在場景不存在時建立；已有場景只開啟，保留日後手動修改。
不更改 Build Settings 或預設啟動場景。不呼叫會整體重建住宅／正式模型的舊工具。

## 第一步驗證入口與紀錄（歷史）

以下入口驗收空場景。第二步之後請使用 `Docs/GREYBOX_MODEL_CONTRACT.md` 的 Prefab／Play Mode 驗證入口，避免拿空場景條件檢查已有玩法元件的場景。

關閉同專案 Unity Editor 後，以 `D:\unity\6000.5.0b9\Editor\Unity.exe` 執行：

```text
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.TutorialGreyboxSetup.BatchCreateAndValidate -logFile "D:\Documents\畢專\Logs\tutorial-step1-validation.log"
```

不要加 `-quit`；入口等待匯入後自行 exit。它只建立新教學場景、讀回新舊場景、驗證重跑安全，不保存舊場景。
基線與驗證結果：`Docs/Validation/2026-10-12-greybox-step1`。
後續以 `Docs/WORKLOG.md` 記錄每次實作與實際測試；不將歷史測試 PASS 當成當前結果。

## 第四步完成時的歷史進度（2026-10-20）

第一至第四步完成。九個Primitive Prefab未改，第四步用場景override接工具。使用者戶型取代原住宅Greybox；Step2／Step3保留但停用。步驟5–9未開始，沒有初始火、教學順序、破壞或傷害。當前文件：`Docs/USER_LEVEL_ONE_STEP4.md`。

第四步Unity驗證：地圖40項、工具37項、舊住宅操控27項Play Mode全PASS；實際圖像已檢視。編譯、序列化接線、保存重開與重跑不覆寫通過。證據 `Docs/Validation/2026-10-19-greybox-step4`；未測硬體鍵鼠、Player build或多人。

### 以下為第三步歷史結果

`TutorialGreybox` 現在使用 `Step3 Residential Layout`：16×14m 三房一廳一衛、開放廚房、走廊、正門、室外出生點／出口、Primitive 家具及兩個日常來源。保留 `Step2 Test Assembly` 但停用，避免雙重物理／火場。沒有接上裝置輸入；新左斧／右管與 WASD 在第四步接入。

第三步相機復用 `RescueCamera`，固定 50°／40°、正交 Size 4.5、SmoothDamp .18s 跟隨 Player root。牆高 .72m 始終可見，保留碰撞；房門1.8m，走廊內緣2.08m。詳細配置與當前驗證入口見 `Docs/GREYBOX_LAYOUT.md`。

2026-10-19 第三步 Unity Play Mode **68項通過**：重開已保存場景、角色通過各房門、兩來源可達、碰撞不依賴 Visual、固定平滑鏡頭、1,039個移動幀的上半身視線檢查、既有 NPC 自主走到屋外。實際 Game Camera 與地圖總覽已擷取檢視。此步沒有初始火源、裝置輸入或新教學流程；不能等同完整可玩教學完成。

第二步歷史紀錄：Unity Play Mode **161 項檢查通過**，涵蓋原始外觀、關閉外觀、刪除後替換外觀，以及碰撞、噴流遮擋、水推物與 NPC 自主逃生；原住宅 **40 項回歸通過**。結果見 `Docs/Validation/2026-10-12-greybox-step2`，本輪沒有重跑這兩套。尚未驗證 Windows Player build、實體鍵鼠或多人。

### 保留的第一步基線

可復原的原始碼／資產基線備份：`Archive/GreyboxBaseline-2026-10-12-0042`（224 檔，不含 Library／Logs 快取）。
原 220 個 Assets／Packages／ProjectSettings 檔案有 SHA256 清單，既有髒工作樹保留，未建立 Git commit。
第一步當時沒有進入 Play Mode；其編譯／骨架驗證仍保留為歷史證據。第二步已修改五個 Runtime 腳本的呈現連結／接點及 Tutorial 場景，以第二步報告為準。
管線仍可能直線穿牆、來源無限；第四步斧頭已有揮動與命中查詢，尚無破壞／傷害。
