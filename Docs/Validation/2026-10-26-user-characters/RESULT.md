# 使用者居民接入：結果

實作／批次執行：2026-10-26；最後看圖與紀錄核對：2026-10-27。

狀態：**Ready with limitations**，可交付本機人工驗收，不代表真人操作或手感已通過。

## 範圍

一份 FBX 只有一位長髮角色，六個原始 Mesh 共1127頂點。來源未改，副本SHA一致（見 `audit.json`）。衍生資產僅等比正規化、基本色與18骨蒙皮／Idle、Run；未重新造型。這輪替換目前住宅 NPC 的 Visual；消防員暫不換。舊 Capsule 保留停用。

## 證據

| 驗收項目 | 結果 | 證據 |
| --- | --- | --- |
| Generic rig、七色材料、1.5m腳底與+Z朝向、無模型碰撞 | PASS | install.txt |
| 場景保存重開、重跑不覆寫、Gameplay快照不變 | PASS | install.txt |
| 實際蒙皮頂點變化、Loop／Root不移動、Idle↔Run | PASS | character-playmode.txt，15項總計 |
| 火阻等待、路通自走、Visual關閉仍能救援、事件一次 | PASS | character-playmode.txt |
| 原操作走路／破門／3m／接水／窗口／地板火／乾粉鍋火／NPC逃生 | PASS | residential-loop.txt，39項＋4項重開 |
| 實際Unity材質與Generic動畫離線渲染 | PASS | preview.txt、unity-front-idle.png、unity-game-angle-run.png，主代理已看圖 |
| Windows x64 Mono Development建置 | PASS | build.txt，0 errors／0 warnings |
| 實體鍵鼠、遊戲內動態觀感、噴射手感 | Not run | 使用者要求不操作畫面，待人工驗收 |

各入口 `FireRescue.Editor.` 前綴：

- `UserCharacterSetup.BatchInstallAndValidate`
- `UserCharacterValidation.BatchRun`
- `ResidentialIntegrationValidation.BatchRunUserCharacters`
- `UserCharacterPreview.BatchRender`
- `UserCharacterSetup.BatchBuildWindows`

命令：Unity `6000.5.0b9`，`-batchmode -nographics -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <Logs/user-character-*-2026-10-26.log>`，入口自行退出。唯離線 RenderTexture 預覽改用 `-force-d3d11`，沒有 UI 控制、不保存預覽場景。正面圖從角色+Z側拍攝；50°/40°遊戲角度圖將展示模型轉向相機，僅供資產看圖，不是實際玩家操作截圖。

玩家產物：`Builds/UserCharacters/FireRescue.exe`、Data、MonoBleedingEdge及DLL已確認存在。沒有啟動EXE、沒有覆蓋舊建置，也沒有發布GitHub。

## 修正過程與差異

- 初版臉／手錯色與臉部權重受手臂影響，在衍生檔修正並重新看圖；保留原幾何拓樸。
- 第一輪Editor編譯發現GetInstanceID在此Unity版本已移除，及Rescued委派需NPC參數；修正後成功編譯。舊Editor驗證器仍有既有obsolete警告，本輪新增腳本沒有編譯警告，Player Build Report為0警告。
- 初版尺寸驗證把BakeMesh含縮放結果再次TransformPoint，誤報0.9m。以Blender回匯世界尺寸、本機Unity CoreModule API說明及Renderer bounds確認是檢查程式問題，使用BakeMesh(mesh,true)補償後，原1.5m斷言通過；沒有放寬標準。
- `audit.json`：466基線檔，464 SHA不變、只改TutorialGreybox與ResidentialIntegrationValidation，新增28個Assets檔含meta、無刪檔／缺meta；Runtime、舊Prefab／模型、Packages與ProjectSettings完全保留。
- 主代理檢查實際匯出腳本、幾何資料、Unity場景差異、材質／控制器、全部58項結果及最終預覽，未僅採子代理摘要。

## 限制與交接

簡易原型蒙皮與動畫，不是正式動畫、IK或髮絲物理；低模原始面數與輪廓不變。玩家實際動態觀感、鍵鼠、手感、多人和效能尚未驗收。下一步依 `Docs/PLAYER_ACCEPTANCE_2026-10-26.md` 試玩並回饋，不再先新增大型系統。公開README／Release維持舊版，未commit或push。
