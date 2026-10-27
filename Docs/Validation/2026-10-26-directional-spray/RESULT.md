# 方向噴射驗證 — 2026-10-26

狀態：Ready with limitations。核心與整關自動驗收通過；硬體鍵鼠手感尚未驗收，不代表噴射表現已定稿。

## 基線與邊界

- Unity 6000.5.0b9，內建渲染管線，Windows x64／Mono／Development。
- 修改前 commit `9cdfa496ec9690ec5559262fd5c27eb4891d97f1`，工作樹乾淨。基線於10-25建立，見 `../2026-10-25-directional-spray/baseline.json`。
- 445 檔中 434 SHA-256 相同，11 檔變動：9 Runtime、ResidentialIntegrationValidation、TutorialGreybox。無既有檔案刪除，既有 meta GUID／模型／Prefab／Packages／ProjectSettings 不變。
- 新增 Profile、Particle View、牆阻、安裝／驗收腳本及來源設定資產；不改 README、GitHub、發布檔，不 commit/push。

## 最終結果

| 驗收 | 結果 | 證據 |
|---|---|---|
| 編譯、三來源設定、場景存讀、重複安裝、3m | PASS，exit 0 | `install.txt` |
| 方向、飛行、空中期限、碰撞／窗口／槍口穿牆、殘留、30/60Hz、視覺粒子數 | 40 PASS，exit 0 | `playmode.txt` |
| 屋外進屋→破門→接水→清路→窗口滅火→換粉滅鍋→NPC離開→重新Play | 39＋4 PASS，exit 0 | `residential-loop.txt` |
| 舊水反應與阻擋回歸 | 25 PASS，exit 0 | `legacy-water.txt` |
| 門／窗口／NPC／統計回歸 | 21 PASS，exit 0 | `doors-and-stats.txt` |
| Windows x64建置 | Succeeded，0 errors／0 warnings，exit 0 | `build.txt` |
| Windows Player 啟動 | PASS：Mono、PhysX、D3D11及場景載入完成，無例外 | 本機 `Logs/directional-player-startup-2026-10-26.log` |
| 實體鍵鼠／多人／效能預算 | 未執行 | 不以命令測試冒充硬體操作 |

共 **129 項最終 Play Mode 檢查通過**。完整關卡使用原 command input、CharacterController、實際噴射碰撞與自然 NPC 更新，不傳送、不直接呼叫熄滅替代流程。獨立反應測項使用不自然衰減的 fixture，避免只等火燒完誤判。

## 可重現入口

以 `D:/unity/6000.5.0b9/Editor/Unity.exe -batchmode -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <log>` 執行；這些入口自行 exit，勿另加 `-quit`。一次只啟動一個 Editor。

- `FireRescue.Editor.DirectionalSprayBuilder.BatchInstallAndValidate`
- `FireRescue.Editor.DirectionalSprayValidation.BatchRun`
- `FireRescue.Editor.ResidentialIntegrationValidation.BatchRunDirectional`
- `FireRescue.Editor.WaterReactionValidation.BatchRun`
- `FireRescue.Editor.BreakableDoorValidation.BatchRun`
- `FireRescue.Editor.DirectionalSprayBuilder.BatchBuildWindows`

最終原始日誌位於 `Logs/directional-install-aperture-2026-10-26.log`、`directional-final-tests-2026-10-26.log`、`directional-loop-aperture-2026-10-26.log`、`directional-final-water-2026-10-26.log`、`directional-final-doors-2026-10-26.log`、`directional-final-build-2026-10-26.log`。舊回歸報告在執行後複製到本資料夾，再還原原報告，避免覆寫歷史證據。

## 視覺與失敗紀錄

- 主代理檢視住宅 `spraying-Water.png`、`spraying-DryPowder.png` 與 `Evidence/directional-spray-*.png`：可見三類不同色／大小的飛行粒子與地面殘留。這是 greybox 點狀視覺，不是正式水柱／霧化特效。
- 初次剖面牆回歸失敗，戶外火強度維持1。診斷落點為窗口 local x=0.353，原開孔半深0.3漏掉實際內牆。開孔深度調1m並明確存場景 override 後通過；寬高、實心牆、3m不放寬。失敗原紀錄保留 `residential-loop-before-window-fix.txt`／`failure.png`，不是最終狀態。
- 一次回歸曾撞上測試腳本編寫中的中間態而編譯失敗；另一次嘗試在 Editor 未退出時被專案鎖拒絕。完成檔案並序列執行後，以上最終入口均exit0。沒有隱藏或停用失敗斷言。
- `git diff --check` 的空白提示僅為 Unity 自動序列化的空值行；忽略該既有格式後檢查通過。

## 限制／下一步

- 測試版：`Builds/DirectionalSpray/FireRescue.exe`。保留同目錄 Data、MonoBleedingEdge 與 DLL。
- Player以隱藏批次程序啟動，確認場景載入後僅停止本次驗證程序；沒有測實體滑鼠／鍵盤、玩家版完整操作或正常關閉流程。原始Player log含本機網路資訊，因此不複製到公開Docs。
- 仍是單人、單層住宅；新舊地面沉積 API 不在同格混用。乾粉殘留沒有持續滅火，油可能在期限前燒完。
- 參數尚待玩家手感回饋：先試水的距離與速度，再試油的黏滯感與粉末範圍；不擴充新系統。詳見 `Docs/DIRECTIONAL_SPRAY.md`。
