# 消防管／液體物理驗證 — 2026-10-27

狀態：Ready with limitations。本輪完成有界的管線牆阻、張力動量清理及噴射流量修正，不宣稱真實軟管／流體或真人手感驗收。

## 基線與已確認原因

- Unity6000.5.0b9，Windows x64 Mono Development，TutorialGreybox。工作樹既有噴射／角色／相機變更全部保留；本輪未commit/push。結束時HEAD仍 `9cdfa496ec9690ec5559262fd5c27eb4891d97f1`，master。
- `baseline.json`包含505個Assets／Packages／ProjectSettings檔案，修改前相關原件另備份於Archive/HoseFluidPhysicsBaseline-2026-10-27。
- `hose-baseline.txt`：候選位置(1.6,0,2)的管線穿牆仍被允許；根節點XZ半徑3、高度2時UsedLength誤算3.605551；停用來源仍IsConnected=true。修後 `hose-probe-after.txt` 分別為限制到約x0.60、UsedLength3、IsConnected=false。
- `fluid-baseline.txt`是真實ElementReceiver接收量，不是重寫公式：0.01秒點按0.075而應0.03、五次點按0.375而應0.15、0.5秒單Tick約0.825而應1.5。確認免費首包及0.25秒截量兩個原因。

## 結果

| 範圍／入口（皆FireRescue.Editor前綴） | 結果 | 證據 |
| --- | --- | --- |
| HoseFluidPhysicsBuilder.BatchInstallAndValidate | PASS，保存重開／重跑冪等，3m與白模／相機保留 | install.txt |
| HosePhysicsValidation.BatchRun | 22 PASS，真實Controller移動與LineRenderer | hose-playmode.txt |
| FluidPhysicsValidation.BatchRun | 11 PASS，實際Receiver量、30/60/120Hz、短按／卡頓、來源銷毀換接、尾量、256容量與最近命中 | fluid-playmode.txt |
| SprayEffectTuningValidation.BatchRunPhysics | 24 PASS | effects-regression.txt |
| DirectionalSprayValidation.BatchRunPhysics | 40 PASS | directional-regression.txt |
| ResidentialIntegrationValidation.BatchRunHoseFluidPhysics | 39+4 PASS，原流程＋重新Play | residential-loop.txt |
| HoseFluidPhysicsBuilder.BatchBuildWindows | Succeeded，0 errors／0 warnings | build.txt |

共140項通過。沒有重算舊版本測試結果；這些入口本輪都實際執行。舊方向與效果報告另有新輸出入口，沒有覆蓋歷史證據。

批次命令：`-batchmode -nographics -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <Logs/*-2026-10-27.log>`；方向回歸及整關為產生圖改用`-force-d3d11`。入口自行退出，不加-quit。最終成功日誌：hose-fluid-install-final、hose-physics-tests、fluid-physics-tests、hose-fluid-effects、hose-fluid-directional、hose-fluid-loop-r2、hose-fluid-build。上述成功入口未見error CS、Exception或Shader error。既有舊Editor obsolete警告未隱藏；Player Build Report為0警告。

## 保留的整關失敗與修正

第一次整關到窗口F固定時失敗，報告與圖保留為 `residential-loop-first-failure.txt`、`window-route-first-failure.png`，日誌hose-fluid-loop仍保留。

用 `HoseFluidPhysicsBuilder.BatchProbeWindow` 量測：Source.Port=(-3.3,.95,3.1)，舊Route=(-5.2,1.15,3.6)，前段在內牆z=3.2465碰到x=-3.8567，離窗口橫向約1.3433m；不是3m不足，也不是要允許水／管穿牆。

`window-pipe-probe-before.txt`比較室內偏移0/.2/.35/.4/.5m，確認.35以上前後段皆清楚。僅將現有固定點Local X改.4，世界(-5.2,1.15,3.2)落在室內窗緣；不改孔洞大小、窗框、水源、火點、牆碰撞或3m。修後probe與整關F、射水、解除、NPC逃生／油鍋乾粉都通過，折線總長2.37m。

這是修正舊管線斜穿牆的場景接點，不是取消斷言或放寬限制。

## 審查與資料保護

- 主代理實讀並修正子代理測試草稿的fixture重疊／物理同步、缺地板／無逐幀步進、起始管線判定，以及液體舊來源假null和巨大dt整數轉換。所有結果由主代理統一執行和讀取。
- 實際diff已檢查：Scene只增加12行，兩個Hose引用與窗口Route的0.4 override；主角、Camera、NPC、模型／碰撞資產、來源Profile不變。
- `audit.json`：505舊檔497 SHA相同，變動4個Runtime、3個Editor驗證入口及Scene；新增4個Editor腳本與各meta，共8個Assets檔；無刪除／缺meta。Packages、ProjectSettings及舊Prefab／模型保留。
- 主代理看過02-window-water、spraying-Water及油視覺捕捉圖。圖來自真正遊戲Camera／飛行樣本，不是ImageGen或手繪替代。
- `Builds/HoseFluidPhysics/FireRescue.exe`、Data、MonoBleedingEdge、UnityPlayer.dll等產物均確認存在；舊PlayerWhitebox版保留，沒有發布GitHub。

## 限制

目前為遇牆卡管，沒有自動繞角／完整繩節物理，只檢查已配置住宅牆門與窗口，未對所有家具做管線碰撞。3m仍為根節點XZ／窗口折線，非整條可見3D弧長；鬆弛曲線無安全空間時改拉直，非法端點不畫穿牆線。極端256飛行份數飽和時DroppedAmount記錄未發量，不作延後回放。

地板Amount仍沿既有逐格作用與上限，不宣稱公升數／全場真實質量守恆。水／油／乾粉的既有資格、射程、飛行與殘留時間不變。本輪未操作真人長按鍵鼠、未測多人或效能；請用 `Docs/HOSE_FLUID_PHYSICS.md` 的本機版試玩再回饋。
