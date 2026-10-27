# 單椅／居民配色／5m：Ready with limitations

2026-10-27；Unity6000.5.0b9；Windows x64 Mono Development；TutorialGreybox。使用者明確先不做螢幕控制，本輪僅背景Unity、自動命令與離線GPU圖。

## 已完成與證據

| 驗收 | 結果／證據 |
| --- | --- |
| 茶几兩側單椅對調並朝內 | PASS，install.txt＋armchairs-after.png；位置交換、旋轉保留、空間名稱修正，碰撞佔地不變 |
| NPC照參考圖基本配色 | PASS，resident-front.png／resident-walk.png；橘髮、淡橘衣、黃飾、膚色與黑眼的六個外部材質。沒有新增帽子或重造角色 |
| 角色原網格、骨架、Idle／Walk保留 | PASS，tuning-playmode.txt；10 SkinnedMesh，Visual無Collider，無Root Motion |
| Scene 5m、舊Runtime預設3m保留 | PASS，同上；不修改HoseConnection或HoseView核心碼 |
| 真Motor能過3m，外拉受限於5m，可反向離開 | PASS，同上；持續外向不儲存動量，free-space clamp精確5m |
| 窗口共用單一5m預算 | PASS，同上；1.25m前段＋3.75m後段，不是各5m |
| 真實整關與重新Play | PASS，residential-loop.txt；39＋4項，砍門、接管、噴水／乾粉、窗口、NPC自動逃生且完成只觸發一次 |
| 保存重讀與冪等 | PASS，install.txt；CoreSnapshot保留除明確管長設定外的核心組件／碰撞／接點／相機 |
| Windows建置 | PASS，build.txt：Succeeded，0 Errors／0 Warnings |
| 真人鍵鼠、好玩程度或正式美術 | Not run，不以自動測試冒充 |

**14項針對性＋43項整關／重開＝57項PASS。** 數量只追蹤本輪覆蓋範圍，不表示全專案無問題或手感已驗收。

## 檔案安全與失敗紀錄

- baseline.json／file-audit.json：620舊檔617不變，3修改、19新增、0刪除；新增Unity檔有meta。Scene998舊fileID段全保留，修改5段（兩椅、NPC材質、玩家管長、marker父節點），新增marker的2段。FBX／blend／既有Prefab／Runtime／Packages／ProjectSettings未改。
- 備份在忽略的 `Archive/Resident5mBaseline-2026-10-27/`；沒有reset、stash、commit或發布。新本機版 `Builds/ResidentColor5m/FireRescue.exe`，不要覆蓋／抽走同目錄Data、MonoBleedingEdge與DLL。
- 最初背景編譯失敗：新增Editor快照腳本錯用Component.parent（CS1061），下一次發現此Unity版本禁止GetInstanceID（CS0619）；已改為Transform判斷與type＋JSON快照。Scene遷移尚未執行就停止，未把失敗隱藏成成功。原始 `Logs/resident5m-install.log`、`resident5m-install-fixed.log` 保留。
- 最終編譯通過，既有Editor腳本仍有CS0618過時API警告，屬此前檔案；Windows建置summary的0警告不代表所有Editor檔都零警告。

## 重現

使用 `D:/unity/6000.5.0b9/Editor/Unity.exe -batchmode -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <個別log>`，入口自行Exit、不加-quit；渲染不用-nographics，不可開兩個Unity同時佔專案。

1. `FireRescue.Editor.ResidentialTuningSetup.BatchInstallAndValidate`
2. `FireRescue.Editor.ResidentialTuningValidation.BatchRun`
3. `FireRescue.Editor.ResidentialIntegrationValidation.BatchRunResident5m`
4. `FireRescue.Editor.ResidentialTuningSetup.BatchBuildWindows`

最終四入口exit0。相應log為resident5m-install-final／playmode／loop／build.log。新整關入口明確預期5m，其他版本的3m入口是歷史驗證，不列本輪PASS。

## 尚存問題與下一步

本輪沒有修NPC「HasSafePath卻擦家具卡住」的根因；現在佔位配置能通關，不表示任意模型尺寸都可通。下一輪先將導航淨空／邊段掃掠與實體碰撞一致，補桌角／窄門／玩家擋路恢復等正反案例，再優先調噴射與拉管手感。品質門見 `Docs/QUALITY_GATES.md`，不是再堆美術或新系統。
