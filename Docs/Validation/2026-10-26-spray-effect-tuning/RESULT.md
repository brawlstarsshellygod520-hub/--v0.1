# 噴射效果調校驗證 — 2026-10-26

狀態：Ready with limitations。限定本機自動驗證；未做畫面控制、硬體輸入或手感判定。

- 環境：Unity 6000.5.0b9，TutorialGreybox，Windows x64 Mono Development。
- 基線：`baseline.json`；先前方向噴射仍未提交，保留工作樹。這輪只擴充五個 Runtime、三個 Profile 欄位及 Editor 驗證入口；場景、Prefab、模型與專案設定不改。
- `FireRescue.Editor.SprayEffectTuningBuilder.BatchPrepareAndValidate`：三 Profile 重新保存／匯入，Settings、Effects、GUID 與二次保存冪等 PASS，見 `prepare.txt`。
- `FireRescue.Editor.SprayEffectTuningValidation.BatchRun`：24 項 PASS，見 `playmode.txt`。
- `FireRescue.Editor.ResidentialIntegrationValidation.BatchRunEffectTuning`：39 項整關＋4 項重開 PASS，見 `residential-loop.txt`；原命令移動、破門、接水、窗口、地板火、油鍋乾粉、NPC 自主逃生及完成一次。
- `FireRescue.Editor.SprayEffectTuningBuilder.BatchBuildWindows`：Succeeded，0 errors／0 warnings，見 `build.txt`；產物 `Builds/SprayEffectTuning/FireRescue.exe`。
- 所有入口使用 `-batchmode -nographics -projectPath "D:/Documents/畢專" -executeMethod <上述入口> -logFile <Logs/effect-*-2026-10-26.log>`，由入口自行退出；不加 `-quit`。
- 測試沒有跳過失敗案例；24＋43＝67 項是本輪結果，上一輪 129 項不是這輪重跑。
- 未啟動玩家 EXE、未操控 Unity／Blender 視窗、未驗證顯示效果、真人鍵鼠、多人或效能。倍率維持 1，待玩家回饋再調參。
- 未 commit／push／更新公開 README 或 Release；舊 DirectionalSpray 產物保留。
