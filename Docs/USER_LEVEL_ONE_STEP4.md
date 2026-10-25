# 使用者戶型／教學第四步

> 2026-10-21：本檔保留為第四步歷史。當前新增水機制，操作見 `Docs/WATER_GAMEPLAY.md`；不做教學流程，最新方向見 `Docs/RESIDENTIAL_GAMEPLAY_ROADMAP.md`。以下「沒有初始火」與舊第四步驗收入口只適用當時基線。

更新：2026-10-20。本輪範圍是替換地圖、接入操控；不是完成步驟 5–9。

## 如何測試

開啟 `Assets/FireRescue/Scenes/TutorialGreybox.unity`，按 Play，再點 Game View 取得焦點。

| 輸入 | 行為 |
| --- | --- |
| WASD | 相機平面方向移動；斜向不加速 |
| 滑鼠 | 地面瞄準；不旋轉 Camera |
| 按住左鍵 | 拿出斧頭，連續揮砍；放開停止 |
| 按住右鍵 | 拿出管嘴，接上來源後持續噴射；放開停止 |
| F | 2m 內接上／切換來源；對當前來源再按解除，沒有可用來源時也解除 |
| E | 靠近居民安撫，沿用既有安全路線逃生規則 |

雙滑鼠鍵同按時右鍵優先。放開保留裝備；失焦、暫停、停用輸入立即停止動作。
起點在圖面前方正門外，綠色出口旁。馬桶在左側窄房，油瓶在主空間的流理台上。沒有永久文字／UI。
斧頭只有揮動與近距離命中查詢，還不會破壞門、家具或傷害 NPC。

## 戶型來源與調整

- 原檔：`D:/Downloads/第一關.blend`。SHA256：`CAB169D02A78BC7AF1C7416B88591E382C4444C438F7DE659429730AD6993351`。
- 專案來源副本：`ArtSource/LevelOne/第一關.blend`，沒有重新存檔或修改原 Blender 視窗。
- 使用已儲存的檔案；使用者開啟的 Blender 視窗曾有未儲存變更，不包含於此匯入。
- 實際 evaluated mesh 經 `ArtSource/Tools/export_level_one.py` 轉為 `level-one.geometry.json`，再以 Unity Editor 建立原生 Mesh 資產；不是照原戶型概念重新畫牆，也不依賴 Unity 自動呼叫 Blender。
- 原檔尺度偏小，遊戲副本統一放大 14 倍，主體約 17×18m。門口外框約 1.32m、內部約 1.1m；角色直徑 .56m。
- 原檔沒有地板，補可碰撞地面／室外邊界；依使用者戶型保留主空間、左房、左窄房、後房和獨立小房，不套回第三步三房格局。
- 牆與門片做 .8m 可見剖面，五扇門片固定打開 90°，不新增開門或破壞系統。固定斜視 50°／40°、正交 Size4.5 平滑跟隨。
- 加入最低限度 Primitive 家具、馬桶水箱、沙拉油瓶、居民、玩家與出口；不做精細美術。
- 來源牆保留原 top-face topology；不可對所有裁切環直接 `holes_fill`，否則會把房間內部誤封為 .8m 天花板。門框用切割移除門楣，不把門楣壓到門洞內。
- 原檔浴室 `CTRL_Hole.004` 切洞深度不足，實測中心線仍碰到薄牆；僅在匯出程序中將該切洞的法向深度加深三倍，門寬、格局與來源檔不變。其他四個門洞法向深度不調整。
- 門洞切割器底面在地面上約 3.8mm，留下橫跨入口的薄條：玩家可跨，但 NPC 的地面淨空檢查會阻擋。匯出副本將切洞延伸到地板下，不改 NPC 的碰撞或尋路規則。

## 玩法／視覺邊界

`Step4 User Layout/Imported Architecture/<part>` 保留 MeshCollider；其 `Visual` 子物件只有 MeshFilter／Renderer。Visual 與 Collision 為兩份獨立 Mesh 資產，換畫面不會暗改碰撞。

Player root 新增 `LocalPlayerInput`、`FirefighterTools`；Visual 新增 `FirefighterToolsView` 與 Primitive 斧頭。既有九個 Prefab 不修改，只加場景 override。工具玩法讀 Collider／朝向，不讀斧頭 Mesh 或動畫事件。

斧頭週期 .55s、45% 時做 .16m 半徑／1.3m 範圍 SphereCast，略過持有者，最近障礙先命中，每週期最多記錄一次。`HitCount`／`LastHit` 是後續破壞功能可接入的結果，目前無傷害行為。

`LocalPlayerInput` 是唯一讀裝置處；七參數 `TickCommand` 可由測試或未來權威端餵命令。`ReadDevices=false` 只關裝置讀取，不停掉玩法。舊住宅未配置 Tools，保留左鍵噴射與舊六參數命令合約。

既有 `HoseView` 保持啟用：拿斧時仍看得到接著的管線。替換整個 Visual 時需重新接 View 引用，但 Motor／Source／SprayEmitter／NPC 不需修改。

FireField 改為此地圖配置的 .5m 網格，支援較窄門口。這是場景參數，不改尋路或火焰核心。第四步沒有初始火／水油教學流程。

## 安裝、驗證與安全

- `UserLevelOneBuilder.BatchInstallAndValidate`：首次安裝、保存後重開、序列化接線、模型／碰撞分離、重跑不覆寫。
- `UserLevelOneBuilder.BatchRefreshExport`：明確刷新本輪生成的 22 份 Mesh，重算此場景防燃格。會改生成資產與場景，只有決定重新匯出戶型時才使用；不應當一般驗收入口。
- `UserLevelOneValidation.BatchRun`：讀取已保存場景；真正走過所有門、接水、噴射、切油、拉管、揮斧、居民出門、Visual 關閉後仍有碰撞；不保存測試狀態。
- `FirefighterToolsValidation.BatchRun`：臨時場景驗證 WASD、斧頭命中／互斥／停用、來源距離／牆阻／13m 管長、View 移除仍有玩法。
- `HandlingValidation.Run`：舊住宅輸入與近身瞄準回歸；不保存住宅場景。

使用 Unity `D:/unity/6000.5.0b9/Editor/Unity.exe`，關閉同專案其他 Editor 後執行：

```text
-batchmode -projectPath "D:/Documents/畢專" -executeMethod FireRescue.Editor.UserLevelOneValidation.BatchRun -logFile "D:/Documents/畢專/Logs/step4-scene-final.log"
```

不加 `-quit`，驗證入口自行等待匯入與結束。批次測試檢查共用命令路徑，不代表實體鍵鼠已驗收。

證據目錄沿用本工作回合起始日期：`Docs/Validation/2026-10-19-greybox-step4`。本輪基線備份 `Archive/GreyboxStep4Baseline-2026-10-19`。
原第三步與第二步地圖保留但停用；ResidentialFire、FireRoom、原美術、套件與 Build Settings 不重建。舊第三步驗收只適用第三步配置，不可拿它檢查新戶型。

## 仍保留的 Prototype 限制

管線是簡化跟隨線、最大 13m 直線約束，可能穿牆，不是繩索繞障；來源無限。此輪沒有新破壞、正式動畫、UI、教學流程、多人或 Windows Player build。

## 本輪驗證結果

Unity6000.5.0b9編譯、安裝、保存重開、序列化讀回、重跑不覆寫均exit0。`scene-playmode.txt`40項、`tools-playmode.txt`37項、`legacy-handling.txt`27項Play Mode全部通過。共用命令確實驅動CharacterController，不只檢查旗標；新地圖五門通行、接水／切油／噴射／拉管、斧頭互斥、NPC穿門出屋、Visual關閉仍保留物理通過。

擷取7張實際Unity畫面；已檢視地圖總覽、噴水與斧頭Game Camera。低牆始終存在，1005個移動幀胸部到Camera視線未被遮擋。總覽使用臨時相機，不是遊戲視野。沒有宣稱硬體鍵鼠、完整教學、Windows build或多人驗證。

保留失敗證據：第一輪匯出cap錯封房間、原浴室切洞不足、門底薄條阻斷NPC。已分別修正後重跑，不刪除失敗報告。原289個Assets／Packages／Settings基線中，只有LocalPlayerInput與Tutorial場景變更，其餘287檔hash一致；原.blend及來源副本hash一致。既有CS0618警告仍在，本輪最終無C#編譯錯誤或Play Mode例外。
