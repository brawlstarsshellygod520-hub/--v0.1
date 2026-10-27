# 住宅家具與物體外框驗證：Ready with limitations

2026-10-27；Unity6000.5.0b9，Windows x64 Mono Development，Built-in／Direct3D11。範圍只補家具與外框，不移動既有玩法熱點或新建關卡系統。

## 基線與差異

- 工作樹本來有大量未提交修改，HEAD `9cdfa496ec9690ec5559262fd5c27eb4891d97f1`；本輪完整保留，沒有commit／push／發布。
- `baseline.json`修改前535檔SHA256；`file-audit.json`：531不變、4改動、85新增、0刪除，新增Unity檔都有meta。
- 改動舊檔：SourceHighlightView、NozzleControlsValidation的舊圓環斷言、ResidentialIntegrationValidation的新報告入口、TutorialGreybox Scene。
- Scene文字diff有大量重排，因此另按Unity fileID檢查：舊777段全部保留，新998段；僅9個舊段內容變更（3個PrefabInstance的外觀override、2個Transform子節點、3個舊模型GameObject停用、SourceHighlight Shader序列化引用），新增221段。沒有偷偷刪掉既有配置。
- Install以核心組件、來源／角色／門／火點Root及Anchors、Camera快照前後比對一致後才Save；舊模型／Prefab／GUID／材質／數值與Packages／Settings保留。備份在忽略的 `Archive/HouseholdBaseline-2026-10-27/`。

## 驗收表

| 條件 | 結果 | 證據 |
| --- | --- | --- |
| 25種Primitive家具與六區配置保存重讀、重跑不覆寫 | PASS | install.txt、layout-before/after.png |
| 家具Visual無碰撞／玩法腳本，碰撞在Root；茶几仍可推動 | PASS | household-outline-playmode.txt |
| 近距離只選同一NearbySource，金色／已接藍色 | PASS | 同上＋source-gold-game.png、source-blue-game.png |
| 舊地面圓環移除、離開／切換／停用不留殘影 | PASS | 同上 |
| Visual替換Refresh、Renderer隱藏、FBX不可讀Mesh fallback | PASS | 同上；含UploadMeshData(true) fixture |
| Shader編譯與實際GPU呈現 | PASS | ShaderUtil無error、房間／來源圖已人工檢視 |
| 真實3m步行／破門／窗口噴水／清火／粉滅油鍋／NPC救援 | PASS | residential-loop.txt，39項＋重新Play4項 |
| Windows建置 | PASS | build.txt：Succeeded，0 Errors、0 Warnings |
| 玩家版實體鍵鼠手感／長時間操作 | Not run | 由真人驗收，不以自動命令冒充 |
| 舊NozzleControls整套歷史入口 | Not run | 只遷移外框斷言並編譯；未計入通過總數 |

**最終45項家具／外框＋43項整關／重開＝88項PASS。** 不是全部專案測試套件。

## 失敗與修正證據

- 首次整關的破門、3m窗口、水／乾粉皆通過，但NPC沒到出口。`residential-loop-first-failure.txt`、`failure-first.png`保留。
- 單獨清火／開門Probe定位：NPC有safe path卻卡在(2.750,0.025,-1.7932)，下一格(2.75,-1.75)與主沙發前緣物理重疊；`npc-layout-probe-before.txt`保留實際路徑與Collider範圍。
- 主沙發Z -1.1→-.75、右單椅X5.45→5.65，保留現有NPC與碰撞半徑，不改尋路演算法、不忽略家具碰撞。
- 修後Probe走出室內，但玩家仍站出生點擋出口；`npc-layout-probe-player-block.txt`顯示附近Player Collider。只在孤立probe將測試玩家移開，`npc-layout-probe.txt`最終Rescued=True。這不取代整關實際操作：最終residential-loop使用原指令步行、真正砍門／噴射，NPC抵出口且單次事件、重新Play乾淨。
- 外框初步review另修正首次尚未Build就被HasRenderableVisual擋掉、物件縮放令框太細、Cube硬法線裂邊、不可讀FBX例外；最終真Play/GPU檢查才列PASS。

## 可重現入口

`D:/unity/6000.5.0b9/Editor/Unity.exe -batchmode -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <獨立log>`，入口自行Exit，不加-quit。渲染不加-nographics；不可同時啟動兩個Unity。

1. `FireRescue.Editor.HouseholdLayoutSetup.BatchInstallAndValidate`
2. `FireRescue.Editor.HouseholdLayoutValidation.BatchRun`
3. `FireRescue.Editor.ResidentialIntegrationValidation.BatchRunHousehold`
4. `FireRescue.Editor.HouseholdLayoutSetup.BatchBuildWindows`

診斷專用 `HouseholdLayoutValidation.BatchProbeNpc`直接在runtime清火／破門，僅用於家具路線隔離，不能當核心循環通過。原始log在忽略的Logs/household-*.log，最終入口exit0。授權原有Access token update訊息未阻止有效授權編譯／建置，不算C#錯誤。

## 交付／限制

- 本機 `Builds/HouseholdGreybox/FireRescue.exe`，保留同目錄Data／MonoBleedingEdge／DLL；未發布。
- 按房間對應補物件，不是圖上座標、火點與來源全面搬遷。活躍烹飪測試檯仍在客廳後側，NPC仍在原浴室路障起點；新增水槽／蘇打粉／汽水攤僅展示，新窗框非導管接點。
- 只現有可互動來源會亮，普通家具不新增功能。外框遮擋用正常深度；透明、非封閉或複雜硬邊正式模型仍需視覺確認，不聲稱所有模型已驗收。
- 詳細替換方法 `Docs/HOUSEHOLD_GREYBOX.md`。下一步真人確認擺設／外框，再依新圖安排玩法位置，而不是暗中放寬管長。
