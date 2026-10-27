# 角色交換與動畫：Ready with limitations

日期2026-10-27；Unity 6000.5.0b9；Windows x64 Mono Development；場景 `Assets/FireRescue/Scenes/TutorialGreybox.unity`。

## 基線與修改範圍

- HEAD `9cdfa496ec9690ec5559262fd5c27eb4891d97f1`，master，原本有未提交修改，完整保留，未commit/push。
- `baseline.json`：本輪修改前513個Assets/FireRescue、Packages與ProjectSettings檔的SHA256；`file-audit.json`：509不變、4修改、22新增、0刪除。所有新增Unity資產有meta。
- 只改LocalPlayerInput、FirefighterTools、ResidentialIntegrationValidation入口及當前Scene；其餘既有Prefab／模型／材質／GUID、相機、碰撞、火與反應、NPC程式及設定不動。Scene diff為雙Visual附加、舊Visual停用、朝向旗標。
- 回復參考在忽略的 `Archive/CharacterSwapBaseline-2026-10-27/`，沒有對髒工作樹reset／stash／clean。

## 驗收證據

| 條件 | 結果 | 證據 |
| --- | --- | --- |
| 玩家角色2、待救者角色1，保存後重讀，重複安裝不覆寫 | PASS | install.txt；場景差異已人工review |
| 兩方真Generic骨架／SkinnedMesh，腳底0，正向+Z，無Root Motion | PASS | characters-facing-playmode.txt |
| Idle／Walk循環、skin變形、開始／停止切換、停用再啟用 | PASS | 同上，各角色12項；Walk首尾差<2.5cm |
| 四向移動面向行走、靜止維持朝向 | PASS | 同上，真LocalInput＋Motor命令測試 |
| 裝備／連來源和長按不誤轉向，實際揮刀／噴射才朝鼠標 | PASS | 同上，含release完成一刀、首顆方向性water樣本速度 |
| legacy命令／舊旗標相容 | PASS | 同上 |
| 破門、3m拉管與窗口、水／乾粉、NPC自行離開／單次完成 | PASS | residential-loop.txt：39項＋重新Play4項 |
| Unity實際畫面／角色姿勢 | PASS | role1/2-front-idle.png、role1/2-walk.png、01-start.png、02-window-water.png、03-completed.png、spraying-Water/DryPowder.png；已人工看過 |
| Windows玩家版建置 | PASS | build.txt：Succeeded，0 Errors／0 Warnings |
| 實體鍵鼠手感／玩家版長時間操作 | Not run | 留給真人驗收，不由自動命令冒充 |

最終 **44角色／方向＋43整關重開＝87項PASS**；不是全部專案測試套件。首次44項通過後，額外權重檢查修正角色1側髮誤綁，報告保留 `characters-facing-before-hair-fix.txt`；最終再跑44＋43，不累加重複次數。

## 重現入口

使用 `D:/unity/6000.5.0b9/Editor/Unity.exe -batchmode -projectPath "D:/Documents/畢專" -executeMethod <下列入口> -logFile <個別log>`，不要同時啟動兩個Unity。這些入口自行Exit，不加-quit；渲染不加-nographics。

1. `FireRescue.Editor.CharacterSwapSetup.BatchInstallAndValidate`
2. `FireRescue.Editor.CharacterSwapValidation.BatchRun`
3. `FireRescue.Editor.CharacterSwapPreview.BatchRender`
4. `FireRescue.Editor.ResidentialIntegrationValidation.BatchRunCharacterSwap`
5. `FireRescue.Editor.CharacterSwapSetup.BatchBuildWindows`

最終五入口exit0。原始log保存在忽略的 `Logs/character-swap-final-install.log`、`character-swap-final-playmode.log`、`character-swap-preview.log`、`character-swap-loop.log`、`character-swap-build.log`。未發現C#編譯／玩法Exception；Unity批次授權記錄有既有Access token unavailable訊息，但有效授權繼續編譯與建置，並非本輪C#錯誤。

## 交付與限制

- 新本機版 `Builds/CharacterSwap/FireRescue.exe`，不要搬走旁邊Data／MonoBleedingEdge與DLL。未發布／未改README。
- Idle／Walk為基本程序產製骨骼動畫，不是正式人工作畫、手部IK或全身揮斧／持噴槍動畫；工具仍用既有Pivot。無多人或效能驗收。
- 下一步真人先測四向面向、裝備不瞄準、攻擊／噴射才瞄準、NPC行走外觀，再調動畫步幅／補雙手握姿。
- 素材清單見 `Docs/LEVEL_ASSET_REQUIREMENTS.md`；不要求正式美術到齊才可繼續Greybox玩法。
