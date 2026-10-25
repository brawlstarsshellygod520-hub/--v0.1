# 消防 Prototype v0.1：開發上下文

更新日期：2026-10-25。專案：`D:\Documents\畢專`。

## 最新：破門＋3m整關整合（2026-10-25）

- 現在 TutorialGreybox 可從出生步行進屋、三刀破浴室門、接水清 NPC 路線、以 3m 管經北窗滅戶外物件火、清主室地板火、換乾粉滅油鍋。NPC 自動抵出口，沒有安撫／E。
- 新 AxeDamageReceiver／BreakableDoor／BreakableDoorView；工具只在既有單次命中點轉送傷害。門 root／Collider／Visual 分離；破後只關指定碰撞，root 保留。
- 水箱位置、戶型網格、Prefab 與 3m 上限不變。移動窗口／戶外火／NPC，新增浴室火路障；舊開門葉停用保留。F 對「目前來源與窗口」採較近者，切不同來源仍優先，解決短管下 F 永遠只拔管的重疊。
- NPC 額外讀取明確 ObjectFire 清單，保留原尋路與途中重算；GameManager 合計地板／ObjectFire，分開 RescueComplete、AllFiresOut、LevelComplete，重複引用去重。
- Unity Play Mode：整關39項＋重新Play4項＋針對性21項，共64項PASS。完整路線用原輸入命令、CharacterController、實際噴射，沒有傳送／直接刪火取代主流程。Windows x64 Mono Development建置0錯誤／0警告；實體鍵鼠驗收被Escape中斷、尚未完成。詳見當輪 RESULT。
- 操作與可玩路線：`Docs/RESIDENTIAL_PLAYABLE_LOOP.md`。證據：`Docs/Validation/2026-10-25-residential-integration/RESULT.md`。舊章節位置與未實作項屬歷史，以此段為準。

## 最新變更：NPC路通自動逃生（2026-10-24）

- 使用者明確移除安撫。`RescueNpc`刪除WaitingForHelp／IsCalmed／TryCalm，從WaitingForSafeRoute開始，自行檢查完整安全路線；不需玩家靠近或按E。受火／固定障礙阻擋時等待，清除後繼續；開場路通就立即出發。
- `LocalPlayerInput`不再讀E、不再持有NPC清單或呼叫安撫。歷史命令的calm參數與Initialize的survivors參數僅為舊呼叫相容保留並忽略，不會啟動任何NPC功能。既有WASD／F／左右鍵操作不改。
- 不改路線演算法、FireField安全門檻、出口、Scene／Prefab／美術或門規則；仍不是要求全場火全滅。油鍋ObjectFire與地板／NPC路線的額外整合仍是既有待辦。
- 下方先前紀錄提到E安撫者均為歷史行為；當前規則以本段及`Docs/NOZZLE_CONTROLS.md`為準。當輪驗證見`Docs/Validation/2026-10-24-npc-autoescape/RESULT.md`。
- Unity6000.5.0b9編譯及Play Mode：自動逃生18項＋Handling回歸25項，共43項PASS、兩入口exit0。當前住宅NPC以原Autonomous Update自行抵達出口；另測火／牆受阻恢復、玩家物理擋路、Visual停用及單次救援事件。430檔基線只改2Runtime與7Editor、421不變，新增驗收腳本及meta；場景／Prefab／模型未改。

## 目前：使用者噴槍＋白色3m管線＋分段點擊操作（10-23至10-24）

- 接入使用者`消防管.blend`的實際噴槍，.55m、4247三角形，保留黑色本體／握把。原Primitive噴槍停用保留；FBX只呈現手持噴槍，柔性白管由HoseView產生。
- Tutorial場景管長改3m，噴流仍6.5m。前方Nozzle與後方Hose Inlet為兩個獨立Gameplay接點，不依賴Visual啟用。既有Prefab／歷史場景不批次改成3m。
- 現在右鍵第一下拿出、第二下靠近來源連接、之後按／按住噴射；左鍵第一下拿出、第二下完整揮一刀、按住連砍。第一次拿出／接管的整次長按不直接進入動作。切回已接噴槍仍先拿出、下一下才噴。F、E沿用。
- `TickPointerCommand`／`TickPointer`接收Pressed與Held，真裝置與新測試共用；舊低階TickCommand保持相容，不能用舊連按測試冒充新操作已驗證。`SourceHighlightView`依同一最近可達來源畫金圈／已接藍圈，離開2m隱藏。
- 3m新上限使原水箱到西窗約4.8m的舊路線不可達；窗口機制保留，未移動場景來源。此限制需在下一輪關卡配置中處理，不代表窗口系統被刪除。
- 操作及模型合約見`Docs/NOZZLE_CONTROLS.md`，驗證見`Docs/Validation/2026-10-23-nozzle-controls/RESULT.md`。本輪不加破門／教學／多人；下一個尚未完成的核心機制仍是消防斧破門。
- 真Unity編譯、保存重開、冪等安裝PASS；新操作43項＋水反應25項，共68項Play Mode通過並檢視相機圖。419檔基線中6舊檔變動、413不變、新增11個Assets檔，無刪除／Missing Meta。未測硬體輸入、Player build或3m完整關卡循環；獨立接管／噴射驗證不等於整關已完成。

## 前一輪：手動窗口導管（2026-10-22至10-23；13m歷史配置）

- 在現有TutorialGreybox浴室西牆加青色Primitive窗框與戶外普通火目標，不改原戶型、碰撞、來源或火焰規則。接水後經後門／庭院繞到窗外，F固定，右鍵噴水滅火已實際走通；操作與精確路線見 `Docs/WINDOW_HOSE_GAMEPLAY.md`。
- 新 `HoseWindow` 提供獨立Route／Interaction接點；每名 `HoseConnection` 最多一個窗口，固定後按XZ來源→窗口→玩家折線計算13m與移動限制。再F解除固定但保留來源，換接／斷開清除路由，窗口停用／刪除退回原直線。來源F互動仍優先；舊API保留。
- `HoseView`以兩段各18點呈現，精確固定窗口頂點，其餘平滑。固定路由的下垂／側擺隨段長限幅，修正短段穿窗台；普通直線模式不變。Visual可停用或替換，玩法接點及原牆碰撞仍保留。
- 實際Unity6000.5.0b9編譯／保存重開／冪等安裝PASS；窗口50項、水27項、乾粉36項，共113項Play Mode檢查通過，Game Camera截圖已檢視。窗口實景全程走路，含不能穿窗、13m折線限制、真正RMB滅火，並非傳送替代通行。證據 `Docs/Validation/2026-10-22-window-hose/RESULT.md`。
- 402檔基線中只改HoseConnection、HoseView、LocalPlayerInput與Tutorial場景；398檔hash相同，新增17個Assets檔含meta，無刪檔／缺meta。既有Prefab／模型／GUID／Packages／Settings保留。備份 `Archive/WindowHoseBaseline-2026-10-22`。
- 下一步消防斧破門，不先加教學UI。仍單人、無限來源、手動單點固定；**未做整條管線碰撞或自動繞牆**，其他牆角仍可能穿管。未測硬體鍵鼠、Player build或多人。

## 前一輪：水＋油鍋乾粉＋使用者消防斧（2026-10-21）

- 使用者提供消防斧並要求繼續下一步；先採家用乾粉滅火器作可接管來源，不做教學流程／UI。現在可實際接水噴油鍋（水無效）、換乾粉再持續熄滅；普通地面火仍可用水，乾粉也能抑制普通火且不加濕、不推物。
- 新增 `ElementReceiver`／`ObjectFire`／`ObjectFireView`，離散鍋火的種類與強度獨立於模型與地板網格。水油枚舉0/1保留，乾粉追加2。ElementReaction先分流最近物件接收器，否則到地板；牆阻沿用。ObjectFire不自動擴散到地板、不要求NPC新條件、不加入水爆炸或容量。
- `LocalPlayerInput.ResolveAimPoint` 讓滑鼠可指向抬高的鍋子碰撞區域，其餘仍地面瞄準。新Prefab在 `Greybox/Powder`，來源(4.4,0,4.6)、鍋子(1.5,0,3.8)。root保存碰撞／玩法，Anchors保存接點，Visual可直接替換。`PowderGameplayBuilder`首次附加、重跑不覆寫。
- 使用者 `消防斧.blend` 保留原檔及來源副本，6拆件完整評估Skin／Subdivision後合併單斧FBX。約.72m、柄尾低於握點.12m；接到原 `Visual/Axe Pivot`，舊Handle/Head只停用。不改工具命中／揮砍／右鍵優先，無破門。原檔及副本hash相同。
- 驗證入口：UserAxeSetup.BatchInstallAndValidate、PowderGameplayBuilder.BatchInstallAndValidate、UserAxeValidation.BatchRun、PowderGameplayValidation.BatchRun；水回歸沿用WaterGameplayValidation及WaterReactionValidation。結果以 `Docs/Validation/2026-10-21-axe-powder/RESULT.md` 為準。操作：`Docs/POWDER_GAMEPLAY.md`，模型合約：`Docs/USER_FIRE_AXE.md`。
- 本輪基線370檔，修改5個既有Runtime與Tutorial場景，其餘364檔hash一致；既有Prefab／模型／GUID／Packages／Settings保留，新增32個資產檔含meta。工作樹原先變更未重設。水驗收舊版備份在 `Archive/WaterValidationBeforePowder-2026-10-21`。
- 下一步為窗口管線路由，再做斧頭破門。供應管線仍13m直線且可穿牆；未做多人、正式美術／手部握持、教學、Player build或實體鍵鼠驗收。

## 前一輪：住宅水機制，不做教學流程（2026-10-21）

- 使用者最新手繪圖指定住宅四個基本機制；先遊戲架構，暫不做教學。最新路線是接水／普通火 → 油鍋／乾粉 → 窗口傳管 → 斧頭破門；第五點氣體／通風與火傷／復活先保留待定。見 `Docs/RESIDENTIAL_GAMEPLAY_ROADMAP.md`，不直接續做舊版油教學。
- 保留第一關.blend戶型，TutorialGreybox內加入3個主空間普通火種，移動茶几到(3.5,0,-2.5)，此場景燃料消耗／自然減弱／蔓延各.01。沒有教學UI、順序鎖定或完成狀態。操作見 `Docs/WATER_GAMEPLAY.md`。
- ElementReaction修正水花穿薄牆：靜態Layer8命中不投射地面水花，直接地面命中逐格檢查低位視線；FireField新增可達性Predicate overload，原3參數API保留，模擬不依賴Physics／Renderer。油路徑未改，尚非油鍋玩法。
- 水推家具新增可設定waterPushImpulsePerUnit=48，FireSystem Prefab明確保存此欄位；原7在60Hz連續噴射時推不動貼地3kg家具。碰撞、質量、摩擦與力方向不改；水推玩家沿用原規則。其餘8個Prefab與模型邊界不改。
- 真Unity6000.5.0b9：水反應25項、住宅實景27項、舊Residential40項，共92項Play Mode PASS，4個batch入口exit0。實景由enabled LocalPlayerInput命令走進屋、F接水、拉管、RMB熄滅全部初始火、推桌約.99m、鬆鍵／斷管停止；火場Simulate=true。新Scene／Prefab保存重開與重跑不覆寫PASS，實際截圖已檢視。
- 修改前364檔基線中，只有FireField.cs、ElementReaction.cs、TutorialGreybox.unity、FireSystem.prefab改動，其餘360檔SHA256一致；新增3個Editor腳本及meta，無Missing Meta。Packages／Settings／既有GUID不改，未commit。證據 `Docs/Validation/2026-10-21-water-gameplay`，基線仍使用前一回合建立的 `2026-10-20-greybox-step5/baseline.json`。
- 水機制入口：WaterGameplayBuilder.BatchInstallAndValidate、WaterReactionValidation.BatchRun、WaterGameplayValidation.BatchRun。舊UserLevelOne驗收假設無初始火，只適用第四步歷史；不要用它驗收本輪配置。
- 仍單人、無限來源、13m直線管長且管線可穿牆；未做乾粉、油鍋火種類、窗口路由、破門、傷害或教學。未驗證硬體鍵鼠、Player build或多人。

## 第四步歷史：使用者戶型＋工具操作（2026-10-20）

第一至第四步完成；第五至第九步未實作。當前操作、匯入流程與限制見 `Docs/USER_LEVEL_ONE_STEP4.md`。

- TutorialGreybox 啟用 `Step4 User Layout`，使用 `D:/Downloads/第一關.blend` 的實際網格，放大14倍、約17×18m、.8m剖面牆、門固定打開、補地板。依使用者戶型，不套回舊三房配置。原Step2／Step3保留但停用；原始.blend與未儲存的Blender視窗未改。
- 匯出副本補通浴室門洞深度及所有門洞底部約3.8mm薄條；保留原NPC規則。每份架構網格的Visual與Collision用獨立資產，模型替換不改碰撞。
- 操控：WASD螢幕方向；按住LMB連續揮斧；按住RMB接來源後噴射；F接／換／解來源；E安撫。雙鍵右管優先，鬆開停止但保留裝備，失焦／暫停／停用停止。原ResidentialFire仍左鍵噴射。
- 新增FirefighterTools（玩法）、FirefighterToolsView（呈現）；.55s週期、45%時最近碰撞體命中，每週期一次、略過自身。沒有破壞／傷害。LocalPlayerInput保留原六參數命令；七參數入口支援新工具，ReadDevices可停裝置讀取以測共用命令路徑。
- 九個Prefab未改，以場景override接線。相機仍50°／40°、Size4.5、.18s跟隨；FireField用.5m格適應窄門，沒有初始火或教學UI。水箱、油瓶、13m直線管長與既有水油規則沿用。
- 真Unity6000.5.0b9：編譯、保存重開、重跑不覆寫PASS；新工具37項、地圖40項、舊住宅Handling27項Play Mode全通過。地圖測了五門通行、水油來源、拉管、工具、1005移動幀胸部視線、居民自主出門。實際圖像已檢視。未測硬體鍵鼠、Windows build或多人。
- 證據 `Docs/Validation/2026-10-19-greybox-step4`；基線備份 `Archive/GreyboxStep4Baseline-2026-10-19`。既有289檔中只修改LocalPlayerInput.cs與Tutorial場景，另287檔hash一致；另加本輪腳本／生成資產。原住宅、正式美術、Prefab、Packages、Settings不變，未commit。
- 當前入口：UserLevelOneBuilder.BatchInstallAndValidate、UserLevelOneValidation.BatchRun、FirefighterToolsValidation.BatchRun。舊Step3驗收只適用歷史配置；BatchRefreshExport僅在明確重匯出戶型時用，會更新生成網格與場景防燃格。

## 第三步歷史基線（2026-10-19，已由上方第四步取代）

使用者要求先以 Primitive 完成可玩教學關卡、分離 Gameplay／模型；本輪明確授權第三步。第一、二、三步已完成，計畫在 `Docs/TUTORIAL_GREYBOX_PLAN.md`，步驟 4–9 待後續指示。

- 新場景 `Assets/FireRescue/Scenes/TutorialGreybox.unity`：使用 `Step3 Residential Layout`，16×14m 三房一廳一衛、開放廚房、走廊、室外，低牆 .72m、門口1.8m、走廊淨寬2.08m。原 `Step2 Test Assembly` 保留但停用。沒有裝置輸入或初始火／教學流程。不依賴正式 FBX；舊住宅與 Build Settings 保留。
- 第三步使用 `TutorialGreyboxLayoutBuilder` 一次性生成（已有配置不覆寫）與 `TutorialGreyboxLayoutValidation` 驗收。相機復用未修改的 `RescueCamera`：50°／40°，正交 Size4.5，.18s SmoothDamp 跟隨 Player root。牆保持可見、不做遮擋隱藏。配置與入口 `Docs/GREYBOX_LAYOUT.md`。
- 第三步真 Unity 編譯／存檔重開／重跑不覆寫通過，Play Mode **68項通過**；六張實際截圖已檢視。涵蓋各房門通行、水油來源可達、牆碰撞、Visual 關閉仍有物理、相機跟隨及既有 NPC 自主出門。證據 `Docs/Validation/2026-10-19-greybox-step3`。核心 Runtime、九個 Prefab、原場景／美術／Packages／Settings 均未改；保護基線267檔中只改Tutorial場景，其他266檔hash相同。
- 新控制已核准、尚未實作：WASD 螢幕方向移動、左鍵拿出消防斧並揮砍、右鍵拿出消防管並噴射。F 接來源與 E 安撫沿用。原住宅仍左鍵噴射。
- `Editor/GreyboxPrefabBuilder.cs` 建立九個 `Greybox/Prefabs` 及元件測試組裝，存在就保留，不覆寫手動修改。`BatchInstallAndValidate` 檢查模型邊界及存檔讀回引用；`GreyboxSeparationValidation.BatchRun` 在臨時場景驗證三種 Visual 狀態。舊 `TutorialGreyboxSetup.BatchCreateAndValidate` 的空場景檢查只適用第一步。
- Collider／Rigidbody／玩法 Component 在 root；Nozzle／Interaction／Outlet／Arrival 在 root 的 Anchors；模型、HoseView、FireFieldView 在 Visual。`SprayEmitter`、`HoseConnection`、`FluidSource` 增加可序列化接點並保留舊值 fallback；`FireFieldView.field` 可序列化指回 root；`HoseView` 停用時清除管線顯示。替換操作見 `Docs/GREYBOX_MODEL_CONTRACT.md`。
- 第二步歷史 Unity 6000.5.0b9 真 Play Mode：模型分離161項、住宅40項回歸 PASS，本輪未重跑。涵蓋關閉／替換模型後移動、13m 管長、滅火、油地、NPC逃生、看不見的牆仍阻擋噴流／移動、水推物；未做硬體輸入、Player build、多人。持久報告 `Docs/Validation/2026-10-12-greybox-step2`。舊第二步 `BatchInstallAndValidate` 假設測試組裝啟用，第三步之後勿用它當場景驗收；分離測試的獨立臨時場景仍有效。
- 第一步完整基線備份 `Archive/GreyboxBaseline-2026-10-12-0042`；第二步修改前腳本／場景／文件在 `Archive/GreyboxStep2Baseline-2026-10-12`。第二步明確改五個 Runtime 檔及 Tutorial 場景，不再用第一步「全部檔案不變」描述現在。既有刪除／未追蹤狀態屬先前工作，不 reset／clean／restore。
- 第三步修改前場景／文件備份 `Archive/GreyboxStep3Baseline-2026-10-19`，完整保護檔hash在第三步報告的baseline.json。沒有新核心玩法；直線管線、無限來源與尚無斧頭玩法的限制仍保留。

## 目前住宅與模型（2026-10-05）

使用者確定高概念為「消防車沒水，消防隊拿管進住宅找日常來源」。水源改為馬桶水箱，油源是一瓶沙拉油，住宅要完整三房一廳一衛；工作台、儲物櫃與工業來源桶不列入下一輪建模。客廳一角配置開放廚房，消防員暫沿用圓潤公仔圖。

住宅初次建模依據：`Docs/Art/Concepts/v0.1-r2/CONCEPT_REVIEW.md`；原 12 項模型與重建方式：`Docs/Art/MODELING_v0.1.md`。主場景為住宅，保留 FireRoom 作歷史基準。使用者要求人物更接近原附圖的圓潤公仔，並綁骨架、加入跑步與待機；10-07 否決 v0.2 後的最新實作與審圖見 `Docs/Art/CHARACTERS_v0.3_REVIEW.md`，不是重新生成概念圖。原型仍是單人、本機、簡化管線，未新增容量、搬瓶或連線。

## 範圍與環境

新題材取代《偷偷搬家》，只驗證 **接水／油 → 拉管 → 噴射 → 反應 → 清出 NPC 逃生路線**。單人先行，未做四人連線、UI 或完整關卡。舊 Assets 移到 `Archive/SneakyMove-2026-10-03`。

- Unity `6000.5.0b9`：`D:\unity\6000.5.0b9\Editor\Unity.exe`。
- Built-in／Legacy Input，未新增套件。
- 唯一 Build 場景：`Assets/FireRescue/Scenes/ResidentialFire.unity`；`FireRoom.unity` 未覆寫。
- Layer 8 FixedObstacle、9 Firefighter、10 Pushable、11 Survivor。
- 地面 y=0，Actor 根節點在腳底。低牆高 .72m、厚 .32m，一直可見，不做整牆消失；厚度需與 NPC 的格中心 .38m 淨空檢查相容，避免牆落在兩格之間卻漏判。
- FireField 原點 (-8,0,-8)，16×18，格寬 1m；住宅 x[-8,8]、z[-5,9]。後排三臥室 z[3,9]，走廊 z[1,3]；前左客廳與開放廚房，前右浴室。正門中心 (.5,0,-5)，出口 (.5,0,-6.5)。
- 水源根節點 (6.6,0,-3.7)，油源根節點 (-5.02,0,.15)；油瓶 Visual 提高 .94m 到流理台。根節點在地面維持既有距離合約，`FluidSource.outlet` 分別指向水箱上方與瓶口，舊來源未設定 outlet 時仍使用原 .6m fallback。
- NPC 起點 (.5,0,5.5)；兩個初始火格中心 (-.5,0,3.5)、(-.5,0,4.5)。床擺在 z=7.2，避免侵入 NPC 起始格的固定障礙檢查。消防員／居民約 1.6m，角色 Collider 與 Visual 分開。
- `ArtSource` 保存 Blender 來源與腳本；Unity 只匯入 FBX，不依賴 Editor 自動呼叫 Blender。Prefab／平色 Standard 材質在 `Assets/FireRescue/Art`。Block 在 FBX 轉軸外用零旋轉父節點縮放，視覺 bounds 必須與碰撞 bounds 一致。

## 模組責任

| 模組 | 責任 |
| --- | --- |
| LocalPlayerInput | 唯一裝置輸入入口；Update 與測試共用 TickCommand，鏡頭相對 WASD／F／E／滑鼠 |
| FirefighterMotor | CharacterController 移動、油地慣性、外力；minMoveDistance=0；碰撞消除向牆內速度但保留切向 |
| HoseConnection | 每名玩家獨立來源／可選窗口連結；2m接來源＋低位可達檢查；13m直線或固定窗口折線限制 |
| HoseWindow | 窗口固定點、1.8m互動範圍；Gameplay接點不依賴Visual |
| FluidSource | 水／油／乾粉種類與流量、視覺 outlet；目前無限且非獨占 |
| SprayEmitter | 6.5m 射線碰撞、略過持有者、計算命中和流量 |
| ElementReaction | 元素施加入口；先處理物件接收器，再處理地面水／油／乾粉；水花有牆阻 |
| FireField | 燃料、油、濕度、強度；兩階段更新避免方向性擴散偏差 |
| FireFieldView / HoseView | 只讀狀態呈現 Primitive／LineRenderer |
| RescueNpc | 安撫、完整安全路徑、四鄰 BFS、等待／逃生／救出狀態 |
| RescueGameManager | 救出數、RescueCompleted 事件；不代管其他模組 |
| CharacterLocomotionAnimation | LateUpdate 觀察實際水平位移，驅動 Idle／Run；無權移動角色或改玩法 |

油也是燃料，所以非可燃鋪面沾油後仍能燒。水降低強度並短暫抑制復燃。強度 >0.05 的格子不可供 NPC 行走；尋路還檢查 Layer 8 固定障礙。NPC 不穿牆、不走對角，受阻時不能跳過路點，火重新阻斷路線就停下重算。

NPC 條件：距離 1.8m 內按 E 安撫，且完整路線無火。救援完成後沙盒繼續運作，方便測試剩餘火勢。

住宅居民停用 `tintBodyByState`，避免覆蓋匯入角色的原色；原單房仍預設啟用。FireField、ElementReaction、FirefighterMotor、SprayEmitter、HoseConnection 的玩法未重寫。HoseView 僅把管線改成深灰色。低牆保持可見，噴流有牆壁阻擋；管線本身仍可能直線穿牆。

### 操控調整（2026-10-05）

- `TickCommand` 提供與裝置 Update 相同的命令路徑；測試可停用原始裝置讀取，明確呼叫命令，不代表已測過硬體鍵鼠。`ViewRelativeDirection` 保持螢幕方向與斜向速度上限。
- 近身平面瞄準半徑 .75m 內保留當前朝向；按住噴射時 `Face(..., immediate:true)` 立即對準游標，未噴射仍以原 720°/s 轉向。`SprayEmitter.Stop` 在輸入／噴射器停用或失焦時立即停止；沒有新增 UI。
- 油地預設轉向加速度 6 m/s²、放開減速度 3 m/s²；一般地面 32 不變、速度 4 不變。撞牆投影消除向內速度／外力，保留沿牆滑動；油地依然有較長慣性。
- 接管與安撫取樣高度 .35m，符合住宅 .72m 低牆；接管略過來源本體，以及包含來源低端點的支撐 Collider，避免把流理台誤當牆。新增 `CanConnect`，TryConnect 與 Interact 共用；來源容量／搬瓶仍未實作。
- NPC 尋路／逃生未改；新增真實 Layer 9 CharacterController 堵路測試，玩家離開後居民會繼續走到出口。
- 住宅場景與模型／Prefab 未重建。這一輪修改了 Motor、Input、HoseConnection、SprayEmitter 與 NPC 安撫遮擋，不再引用建模階段的「這些腳本 hash 未變」作為目前狀態。

### 角色與骨架動畫（2026-10-05 歷史基線）

- 消防員／居民各 18 骨 Generic rig，Idle 2 秒、Run .8 秒，原地循環；動畫不接管 CharacterController。膝部腿網格用上下腿權重過渡，其餘公仔區塊單骨權重，沒有手指／臉部骨架、IK 或骨架套件。
- 根 motion 關閉；每名 actor 的呈現元件只取樣真實水平位移，玩家參考速度 4、NPC 2.15，序列化到住宅。Animator float `Speed`／`CycleSpeed`；Idle→Run >.06，Run→Idle <.04，.12 秒過渡。撞牆不空跑，傳送與停用重置取樣；油地／外力仍能驅動動作。
- 新 `CharacterArtSetup.Apply` 只替換住宅兩個角色 Visual 並接動畫，保存前比對玩法 MonoBehaviour／Collider 序列化值；沒有重建房屋。FBX／Prefab GUID 保留，原玩法腳本、其他家具、ProjectSettings 共 57 檔 SHA256 未變。
- `ArtSource/Tools/build_characters.py` 現在轉呼叫 `build_rounded_characters.py`，避免依舊命令重建回無骨架版本；來源是兩個 `.blend`／FBX，動畫嵌在 FBX，Controller 在 `Art/Animations`。
- 新 31 項角色 Play Mode 與原 40 項住宅救援回歸均通過；結果 `Docs/Validation/2026-10-05-character-v2`。角色、Idle／Run GIF 與主鏡頭實拍在 `Docs/Art/Models/v0.2`。渲染預覽臨時旗標／照明不寫回遊戲設定。

### 最新角色 v0.3（2026-10-07）

- 使用者否決 v0.2 後，實際審圖六輪：修大頭短肢、寬衣襬腿根連接、半球帽冠／薄帽沿、整合簡單髮帽、衣服／腿部真正表面反光材質、平底鞋與原地 Run 支撐。仍 18 骨、2 秒 Idle／0.8 秒 Run，沒有新增動畫狀態或 IK。
- `CharacterArtSetup` 把原 Nozzle 視覺掛到 Chest 並對齊雙手平均握點；重跑會先保留舊 Nozzle 再替換模型，具冪等性。`SprayEmitter` 原判定射線及玩法不改；住宅建置工具也接同一呈現方法，但本輪未全重建。
- `CharacterMeshValidation` 15 項通過；使用 `BakeMesh(mesh, true)` 補償 Renderer 縮放後轉世界座標。早期 default false 的數據重複縮放，已標記為失效量測而非模型尺寸確證。最終兩者身高1.600m、Idle 鞋底0、Run最低2.9mm／24個姿態均有支撐；這些不能代替人工審圖。
- 33 項角色 Play Mode（原31＋噴嘴Chest跟隨／Run握點），40 項住宅 Play Mode 本輪重跑皆通過，exit 0。受保護檔案增含既有 CharacterLocomotionAnimation，共58／58 hash相同；四個角色GUID未變。玩法序列化保存前比對成功。
- 最終預覽只改臨時燈光解析度／相機裁切；移除會觸發全域 QualitySettings 格式更新的設定依賴，原檔hash已恢復。真實正／側／背／遊戲斜角、兩種 Run GIF／Idle GIF及水油Game Camera在 `Docs/Art/Models/v0.3`。
- 最終報告 `Docs/Validation/2026-10-07-character-v3`；審圖及失敗原因 `Docs/Art/CHARACTERS_v0.3_REVIEW.md`。畫面接受為Prototype公仔，Run偏穩定小跑；未做獨立Player build／實體輸入／多人。水油、房屋、相機、Runtime與碰撞規則均未改。

## 日後四人／連線

場景共用一份 FireField／NPC／GameManager，每名玩家使用獨立 Motor／HoseConnection／SprayEmitter。不要把鍵盤移入玩法模組，不要新增全域單例玩家。

未來主機／伺服器負責驗證接管、噴射與碰撞，驅動 `FireField.Step`／`RescueNpc.Tick`；客戶端可關閉 `Simulate`／`Autonomous`。仍需另外同步角色位置、火格、來源連結與 NPC 狀態；不能把現有 Unity 物理宣稱為確定性網路模擬。此版未預先加入 RPC、網路框架或大型抽象層。

## 驗證

批次前關閉同專案 Editor；重建會覆寫住宅、模型 Prefab 及材質。不要在有未保存的手動修改時直接批次重建。

```powershell
$unity = 'D:\unity\6000.5.0b9\Editor\Unity.exe'
$setup = Start-Process $unity -ArgumentList '-batchmode -quit -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.ResidentialPrototypeBuilder.Build -logFile "D:\Documents\畢專\Logs\residential-setup.log"' -WindowStyle Hidden -Wait -PassThru
$setup.ExitCode
$test = Start-Process $unity -ArgumentList '-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.ResidentialPrototypeValidation.Run -logFile "D:\Documents\畢專\Logs\residential-smoke.log"' -WindowStyle Hidden -Wait -PassThru
$test.ExitCode
```

測試命令不加 `-quit`，完成後自動回傳結果；保留圖形裝置以擷取相機畫面。自動測試採 60Hz 模擬步進，共用實際移動／噴射方法，逃生段落啟用實際火勢與 NPC Update。實體鍵盤手感及多人另行驗證。

最新驗證結果見 `Docs/WORKLOG.md` 與 `Logs/residential-playmode-results.txt`，不以舊 `fire-playmode-results.txt` 的單房 PASS 代替。住宅驗證包括來源／Mesh／碰撞合約、真實 CharacterController 經門找水、13m 拉管、安撫／滅火／自主逃生、油地慣性、牆後不可噴及水推茶几／玩家。

本輪保留已開啟的 Editor，在 `Logs/HandlingValidationProject` 隔離副本執行：操控 27 項、住宅回歸 40 項皆通過（exit 0）。結果在 `Logs/handling-results.txt`、`Logs/handling-residential-results.txt`；持久副本在 `Docs/Validation/2026-10-05-handling`。正式測試日誌無新 C#／runtime 例外；早期量測的 Unity Beta SearchDatabase 啟動索引例外另保留分類，沒有修改套件或清除 Console 來隱藏。

只在有意義的修改後更新 `Docs/WORKLOG.md`，保留失敗證據，不把舊題材 PASS 當成新遊戲的結果。未經要求不得 commit／push／發布或更新全域 AI 記憶。
