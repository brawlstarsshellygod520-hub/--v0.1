# 主角白模與相機驗證 — 2026-10-27

狀態：Ready with limitations。請玩家確認外觀與距離偏好；不代表實體連續按鍵或手感已通過。

## 基線／變更

- 專案D:/Documents/畢專、Unity6000.5.0b9、TutorialGreybox、Windows x64 Mono Development；保留既有未提交噴射與NPC變更。
- `baseline.json` / `audit.json`：494舊檔中491 SHA相同，只改RescueCamera.cs、TutorialGreybox.unity、ResidentialIntegrationValidation.cs；新增11個Assets檔含meta，無刪檔／缺meta。Packages、ProjectSettings、舊Prefab／模型、其餘Runtime完全未改。
- 角色1原始FBX、Unity匯入檔、ArtSource副本SHA相同：`1171EC5F05F29731BEB0916A9591A3E4396C9AAFA476466AFA70EA78EB2A7147`。10個Mesh、無rig/動作，沒有重造型、增加骨架或上色。
- Scene實際diff：只相機位置／旋轉／Size／viewAngles，以及新主角Prefab與舊Body/Helmet停用override，沒有改Root、Collider、Nozzle、Hose或NPC。
- 相機50/0，正交Size6（原4.5）；使用者明確確認是對齊房屋的正向俯視。原相機平滑跟隨、不隨玩家旋轉；舊場景預設50/40保留。

## 執行證據

| 項目 | 結果 | 檔案 |
| --- | --- | --- |
| 匯入、白模1.6m／feet0／+Z、純Visual邊界、Gameplay快照不變 | PASS | install.txt |
| Scene保存重讀／安裝重跑冪等 | PASS | install.txt |
| 模型顯示、W/D實際CharacterController移動、角度固定、smooth follow、Visual關閉仍能動、legacy預設 | 17 PASS | player-camera-playmode.txt |
| 破門／3m／接水／窗口／地板火／乾粉鍋火／NPC逃生／救援一次／重新Play | 39+4 PASS | residential-loop.txt |
| 新遊戲Camera實際渲染 | 已檢視 | 01-start.png、02-window-water.png、spraying-DryPowder.png（另保留其他捕捉圖） |
| Windows x64 Mono Development | Succeeded，0 errors／0 warnings | build.txt |
| Windows玩家版啟動與畫面 | 已實際開啟確認白模、房屋對齊與較遠視野 | 本輪Computer Use回報；Player.log錯誤0，關閉後Shutdown |
| 長按鍵鼠、操作手感、多人／效能 | Not run | 不以自動命令冒充實體輸入 |

命令入口均為 `FireRescue.Editor.` 前綴：`PlayerWhiteboxSetup.BatchInstallAndValidate`、`PlayerWhiteboxValidation.BatchRun`、`ResidentialIntegrationValidation.BatchRunPlayerWhiteboxCamera`、`PlayerWhiteboxSetup.BatchBuildWindows`。

以 `-batchmode -nographics -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <Logs/player-whitebox-*-2026-10-27.log>` 執行；整關為檢視畫面改用 `-force-d3d11`，由入口自行退出，不加-quit。最終成功日誌為install、tests-r2、loop、build。沒有另外重跑歷史整套測試。

## 失敗處理

首版針對性測試的Visual關閉移動段，先W再D後繼續往+Z會撞住宅正門旁實體牆，故未移動；初次失敗報告保留在 `player-camera-playmode-first-failure.txt`，日誌tests也保留。依實際戶型把該段改往無阻擋的戶外-Z，仍使用相同實際輸入命令、同時長、同距離斷言與原碰撞，最終通過。**沒有改Gameplay或關牆碰撞來讓測試通過**。

主代理另修正子代理草稿的yield/try-catch語法與NPC階層假設，編譯前審查後才執行。主代理檢查了實際程式、Scene diff、測試證據、匯入模型圖與玩家版畫面，未僅採子代理摘要。

## 交付

`Builds/PlayerWhitebox/FireRescue.exe` 及 Data／MonoBleedingEdge／UnityPlayer.dll 等檔案已確認存在。沒有覆蓋先前UserCharacters建置、沒有commit／push／發布GitHub。

角色1目前是未綁骨架的靜態白模，玩法可操作不等於已有跑步／待機肢體動畫。角色2既有NPC與動畫保留。後續上色版只替換Visual／材質，主角骨架與握持精修需另行處理。規格見 `Docs/PLAYER_WHITEBOX_CAMERA.md`。
