# 畢業專題工作紀錄

> 後續每次修改請在此檔案新增一筆版本紀錄，不覆蓋舊紀錄。

## 炎上消防隊 v0.1 — 2026-10-25（精簡公開說明）

- 依使用者要求，README與Release正文只保留「下載與執行」及「操作」，保留遊戲名稱。移除首頁的介紹、截圖、流程、規則、架構與驗證段落；技術／驗證原始紀錄仍保留在Docs，不變更遊戲檔案或下載連結。

## 炎上消防隊 v0.1 — 2026-10-25（公開名稱與介紹調整）

- 依使用者要求，公開遊戲名稱改為「炎上消防隊」，同步README、發布頁與專案介紹；既有儲存庫網址、Release標籤及Windows下載檔名保留。
- 對外介紹移除指定的NPC逃生／安撫說明。不變更NPC規則、核心程式、場景或既有Windows二進位檔；本輪為命名與文字更新。

## 消防遊戲 v0.1 — 2026-10-25（公開 GitHub 交付準備）

- 使用者明確要求將目前進度上傳 GitHub 並給公開連結。沿用既有公開儲存庫，不建立新倉庫、不改名、不強制推送。整理 README 的開啟方式、Windows下載、遊戲內容、控制與限制；保留歷史工作紀錄。
- 補齊住宅整合 RESULT。最終測試數為64項（原60項再加4項F窗口邊界），Windows build為0錯誤／0警告；Windows實體鍵鼠操作被使用者Escape中斷，未標示為通過。
- 發布分開為 Unity 原始碼與 Windows x64 ZIP；不提交 Library／Temp／Logs／UserSettings／Builds／Archive，另排除Python快取、Blender自動備份、原始process log與環境密鑰檔。未加入或替使用者決定開源授權。
- 公開提交使用該 GitHub 帳號的 noreply 作者信箱，避免新增提交公開個人郵箱；不改全域 Git 身分。既有已公開歷史不重寫。
- 已完成公開推送，程式提交 `b0a45fb`，發布標籤 `v0.1.0-prototype`；Windows ZIP共64,470,412 bytes。匿名下載與README均HTTP200，遠端大小／SHA-256一致。連結與發布證據見 `Docs/PUBLIC_RELEASE.md`。

## 消防遊戲 v0.1 — 2026-10-25（破門、3m整關、逃生與統計）

- 依使用者五項清單完成核心整合，保留最新「不需安撫」規則。使用 Unity 實作／驗證流程約束修改邊界；核心、碰撞、Visual 獨立。
- 新增三刀門、每次揮砍一次傷害、單次破壞事件；門模型可獨立替換，破後玩家及NPC能實際穿過。
- 原戶型及水箱位置不動，北移測試窗並配置浴室西側NPC及火路障。路由實測2.87m，不放寬3m；F重疊時優先較近窗口，不搶走其他來源換接。
- NPC沿用既有BFS／CharacterController，加入ObjectFire危險區與途中重新判斷。GameManager同時計地板與物件火、去重來源引用，救援完成恰一次，另提供LevelComplete觀察。
- 實際Unity編譯、場景保存重開、重複安裝不覆寫通過；Play Mode整關39項＋重開4項＋針對性17項共60項PASS。先後遇到馬桶本體遮水與測試把F換油誤當拔管，修正操作路線後通過，未削弱碰撞或改成直接滅火。
- 當輪基線沿用前回合432檔 `2026-10-24-residential-integration/baseline.json`；保留原dirty工作樹、模型／GUID，不commit。最新可玩路線與Windows／鍵鼠結果在 `RESIDENTIAL_PLAYABLE_LOOP.md`、`Validation/2026-10-25-residential-integration/RESULT.md`。

## 消防遊戲 v0.1 — 2026-10-24（移除安撫，NPC自動逃生）

- 使用者要求「路通就自己走」。移除NPC的WaitingForHelp、IsCalmed、TryCalm及安撫距離／視線檢查；開場直接等待安全路線，路通即自行離開，遇到阻路等待恢復。
- 移除LocalPlayerInput的E鍵讀取與NPC互動引用，舊calm命令參數及survivors初始化參數僅為既有呼叫相容保留並忽略。WASD、F、左右鍵噴槍／斧頭規則不變。
- 沿用現有FireField安全門檻、四方向尋路、CharacterController碰撞及救援事件，不要求全場火全滅，也不因玩家距離決定啟動。開場已經路通就直接逃生，不人工加延遲或新障礙。
- 僅局部修改Runtime並同步7份舊驗收的安撫相關案例，新增自動逃生針對性測試。既有Scene／Prefab／美術／GUID／Packages／Settings不修改。依Unity實作與驗證流程，邏輯與呈現仍分離。
- 基線與本輪結果：`Docs/Validation/2026-10-24-npc-autoescape`；修改前備份：`Archive/NpcAutoEscapeBaseline-2026-10-24`。驗證完成狀態以該目錄`RESULT.md`為準，歷史通過報告不覆寫。
- 實際Unity編譯及Play Mode完成：自動逃生18項、Handling回歸25項，共43項PASS，兩程序exit0。真Tutorial住宅NPC保持原Autonomous Update、火場模擬且無E命令，自行走到原出口；fixture確認火／牆受阻及清除後真正抵達、途中起火停止再恢復、玩家擋路不穿越、Visual停用與完成事件恰一次。
- 430檔基線中421不變、9個腳本修改、新增2個驗收資產檔，無刪除／Missing Meta；Scene、Prefab、模型、既有GUID及Settings不變。其他歷史驗收只遷移API及編譯，不宣稱全部已重跑；未做Player build或實體鍵鼠驗證。

## 消防遊戲 v0.1 — 2026-10-24（噴槍、白色3m管線與分段點擊；10-23開始）

### 完成內容

- 使用者`消防管.blend`實際為噴槍：評估原7份mesh後匯出FBX，.55m／4247三角形，保留黑色本體、握把、金屬圈；舊Primitive噴槍停用備份。柔性白管仍由程式呈現，不用噴槍FBX假裝管線。
- 供應管長改3m、噴流仍6.5m；新增後端Hose Inlet，前端Nozzle獨立，Gameplay接點不置於模型內。來源2m內以同一最近可達候選畫金圈，已接藍圈，離開隱藏。
- 右鍵先拿出、下一下接來源、之後點／按住噴；左鍵先拿出、下一下揮一刀、按住連砍。裝備／接管的第一次長按不得直接啟動動作；切回已接噴槍先拿出再噴。雙鍵右優先，失焦／暫停解除，F與E保留。
- 依Unity實作／驗證技能維持輸入、狀態、模型分離；新增Pressed／Held共用命令入口，歷史低階API保留，不重寫反應或NPC系統。

### 驗證與發現

- 真Unity6000.5.0b9匯入／編譯／保存重開／冪等安裝PASS；新點擊操作與住宅實景43項、水反應回歸25項，共68項Play Mode通過。主代理檢視實際Game Camera金圈、藍圈、白管／噴流畫面與關鍵diff。
- 首輪39項後因測試要求走到3m外目標而誤判；實際角色已正確停住。只修driver為持續推界線及最後45幀穩定檢查，未弱化3m限制或改Runtime來通過。
- 基線419檔中只改5個Runtime與Tutorial場景，其餘413hash一致；新增11個Assets檔含meta，無刪除／Missing Meta。舊Prefab、戶型、GUID、Packages、Settings不變，未commit。
- Downloads原檔在匯出快照後有另存、檔案hash不同；只讀比對確認評估後的7份幾何、材質、變換與尺度一致，沒有覆寫原檔或使用者Blender視窗。細節`source-comparison.txt`。
- 操作`Docs/NOZZLE_CONTROLS.md`；完整結果`Docs/Validation/2026-10-23-nozzle-controls/RESULT.md`；備份`Archive/NozzleControlsBaseline-2026-10-23`。

### 進度與剩餘

- 已完成接水／普通火、油鍋／乾粉、單窗口路由系統與本次新工具操作；消防斧破門尚未完成。教學流程暫不做。
- 3m短於原水箱到西窗約4.8m，現場舊路線不可玩；沒有擅自移來源或放寬距離。下一輪需調整來源／窗口配置並重走整關／NPC逃生，不用歷史13m成功冒充3m整關完成。
- 無一般管線碰撞／自動繞牆、正式握持動畫、多人；未測實體鍵鼠手感或Player build。本次實景反應目標只存在於Play Mode，不新增永久關卡內容。

## 消防遊戲 v0.1 — 2026-10-23（窗口導管；10-22開始）

### 完成內容

- 依四機制順序接著做窗口傳管，不加破門或教學UI。浴室西側外牆新增Primitive青色窗框、窗外普通火測試木塊，原戶型、牆碰撞與.blend不改。
- `HoseWindow`保存Route／Interaction；`HoseConnection`每人持有單一窗口與來源。F固定／解除，來源互動優先；換接／斷開清路由，停用／刪除窗口回退。固定後依XZ來源→窗口→玩家折線計算13m與最後支點移動限制，舊直線API／規則保留。
- `LocalPlayerInput`只新增可用窗口清單並轉交F；`HoseView`兩段36點、窗口頂點精確固定，其餘平滑。短段下垂與側擺依段長限幅，玩法／視覺不互相依賴。窗口Anchors與Visual獨立，換模型不改玩法。

### 發現與修正

- 實測前門門板擋住出門左轉，向南繞又超出13m，改走既有後門進庭院，不移門或放寬長度。舊地圖測試落點被新增乾粉瓶佔用，另繞來源東側。兩份失敗證據保留，實景驗證沒有用傳送角色略過通行。
- 47項首輪通過後仍檢視真Game Camera，發現短管段下垂穿窗台。只對固定路由按段長限幅，補窗口附近線段／窗台淨空檢查，重跑最終50項通過；沒有把「折線頂點存在」當成完整視覺驗收。
- 審查中先修C#區塊同名區域變數、已刪除Unity Object回退、過長路由測試其實只測到距離、近共線位置不應斷言大於直線、及刪除窗口前需真的重新選取等問題。

### 驗證與保護

- Unity6000.5.0b9實際編譯、Scene／Prefab保存重開、重跑不覆寫PASS，exit0。窗口50項、水27項、乾粉36項Play Mode通過；結果與執行順序詳見 `Docs/Validation/2026-10-22-window-hose/RESULT.md`。
- 從出生點實際進屋F接水，繞後門／庭院到窗外固定，沿牆走動，Game Camera螢幕瞄準後RMB滅火；13m約束在z10.63停止而非撞邊界。另測獨立玩家API狀態、Visual停用不影響機制、實體牆不能穿、斷接無殘留路由。
- 402檔基線只改3個既有Runtime和Tutorial場景，其餘398檔hash一致；新增17個Assets檔含meta，Missing Meta=0，無刪檔／既有GUID變更，原Prefab／模型／Packages／Settings保留。Scene只有171行新增，未commit／push／清理工作樹。
- 備份 `Archive/WindowHoseBaseline-2026-10-22`；操作與模型合約 `Docs/WINDOW_HOSE_GAMEPLAY.md`。舊水／乾粉回歸證據已備份並恢復，不拿本輪截圖冒充上輪畫面。
- 未測硬體鍵鼠、Player build或多人；保留既有CS0618警告。仍是無限來源、手動單窗口，沒有一般管線碰撞／自動繞牆，其他牆角仍可能穿管。下一步才是消防斧破門。

## 消防遊戲 v0.1 — 2026-10-21（使用者消防斧＋油鍋／乾粉）

### 完成內容

- 先接入使用者 `D:/Downloads/消防斧.blend`，再依「下一步繼續做」加入油鍋／乾粉。乾粉先採家用乾粉滅火器，沿用F接來源、右鍵噴射；先做水無效、乾粉有效，不加入爆炸、容量或教學UI。
- 油鍋位於既有流理台右端，乾粉來源在其右側主空間；沒有重建住宅。來源與鍋子皆是Greybox Prefab，Gameplay／Collider在root，接點在Anchors，模型在Visual。原水箱、沙拉油瓶、NPC、地板火與計分不重寫。
- 增加ElementReceiver反應入口、ObjectFire物件火種類／強度與ObjectFireView；乾粉每單位減.5強度，水只減普通物件火，鍋火不受水抑制。普通FireField另支援乾粉減火而不加濕；乾粉不推物，沿用牆阻，不把檯面鍋火投影到地板。
- 為檯面接收器補滑鼠表面瞄準，其他位置保留地面瞄準。沒有目標鎖定、世界文字或新UI。Kind舊Water=0、Oil=1不動，DryPowder追加2。
- 消防斧六個Mesh是同一把斧頭的拆件，不是三把備份。保留Skin／Subdivision評估與原材質，單斧16,844三角、約.72m，柄尾在握點下.12m；FBX只放Visual，沒有物理或玩法Component。原Handle／Head停用留作回退，原Axe Pivot與輸入／命中程式不動。

### 檢查中發現與修正

- 首輪匯出在切到空場景後才取depsgraph，漏掉柄部修改器且預覽空白；改在原場景先烘焙。最終檢視真正Blender網格與Unity Game Camera畫面，不用概念圖代替。
- 避免先轉Y-up再由FBX二次轉軸；另核對刀刃原始頂點在source+X，將寬刀刃轉向工具+Z。QA需在完整Prefab掛點座標檢查，不能取FBX內部root座標抵消匯入轉換；補上刀刃材質頂點方向驗收。
- Unity6000.5新API把GetInstanceID及EntityId隱式轉int列為編譯錯誤，安裝快照改用GetEntityId().ToString()。兩次錯誤log保存，並非忽略編譯失敗。
- 審查測試時修正單位乾粉誤判全滅、隔離測試碰撞體留在走路路線、未等物理步就判斷無推力、及靠近舊油瓶時按F其實換來源而非斷接。修好夾具／站位後再驗收，沒有放寬功能斷言。

### 驗證、保護與限制

- Unity6000.5.0b9實際匯入／編譯／Scene與Prefab保存重開／重跑不覆寫；斧頭16項、乾粉36項、水實景27項、水反應25項Play Mode，共104項。最新詳細結果見 `Docs/Validation/2026-10-21-axe-powder/RESULT.md`。
- 乾粉實景不是直接呼叫滅火API：共用輸入指令實際由屋外走進浴室F接水、拉管到廚房，依GameCamera螢幕位置瞄準鍋子，再換來源／持續乾粉／鬆鍵／斷接。補真噴流牆阻與完整Visual停用仍可滅；水推桌及薄牆水花回歸也測過。
- 基線370檔中只有5個既有Runtime與Tutorial場景改動，其他364檔SHA256相同；新增32個Assets檔含meta，Missing Meta=0，沒有刪檔或變更既有GUID。舊Prefab／其他場景／原模型／Packages／Settings保留，未commit／push／清理工作樹。
- 原消防斧與副本hash均 `2D5B151C556CEDAED19F1BC50B1C4D8EBFE4F41D25C0938590C3F5635D58F4EF`。備份 `Archive/AxePowderBaseline-2026-10-21`；水上回合驗收保存在 `Archive/WaterValidationBeforePowder-2026-10-21`。
- 仍是單人、無限來源、13m直線供應管且可穿牆。獨立鍋火尚無地板蔓延／復燃／NPC額外條件；無窗口路由、破門、正式握持動畫或教學流程。未測實體鍵鼠或Player build；已有舊CS0618警告。下一步窗口傳管，再做破門。

## 消防遊戲 v0.1 — 2026-10-21（住宅水機制；暫不做教學）

### 完成內容與方向

- 依最新手繪圖改為先完成四個遊戲機制，不做教學流程／UI／步驟鎖定。保存參考圖與新路線 `Docs/RESIDENTIAL_GAMEPLAY_ROADMAP.md`；油鍋／乾粉、窗口傳管、斧頭破門待後續，不沿用舊油教學方向。第五點氣體／通風及火傷／復活備註未實作。
- 保留第一關.blend戶型；在主空間配置3個相鄰普通火格，把可推茶几移到容易練習的位置。場景燃料消耗、自然減弱與蔓延速率各.01，留出找水時間。WaterGameplayBuilder只首次配置，重跑不覆寫手動調整。
- ElementReaction加入水花可達性：撞靜態Layer8固體停止地面水花，直接地面命中逐格檢查牆阻。FireField保留舊3參數ApplyWater，增加Predicate版本；不依賴模型或物理查詢，保持玩法／呈現分離。
- 水推家具可設定waterPushImpulsePerUnit=48，明確保存到FireSystem Prefab。沒有改家具質量、摩擦、Collider或其他8個Prefab；沒有新增Runtime管理器或套件。

### 發現與修正

- 真Play Mode重現「水柱撞牆停住，但.95m水花熄滅牆後火」；原失敗證據保留。修正同時涵蓋地面近牆水花，沒有只處理正面撞牆。
- 實景發現原每幀小推力推不動貼地茶几；舊測試一次累積大impulse或抬高物件不能證明連續噴射。新測試在地面上以60Hz持續噴射，固定物理步量測。係數24／36／48／72的位移分別約.003／.049／.467／1.252m，選48。
- Unity首次匯入新增序列化欄位的預設18保留在Prefab快取，光改C#初始化值不會更新既有實例。把48正式保存為Prefab參數，重開／重跑驗證；僅初始化尚無保存值的資產，不覆蓋使用者已保存設定。
- 新測試最初未匹配Prefab實例的(Clone)名稱；修正測試夾具並保留失敗檔。檢查也將高於.8m牆頂的瞄準降低，確保測的是撞牆，不是越牆。未刪除或放寬負向斷言。

### 驗證與保護

- Unity6000.5.0b9真編譯／匯入／Scene與Prefab保存重開／冪等安裝PASS。最終水反應25項、住宅實景27項、原住宅40項，共92項Play Mode PASS，4個入口皆exit0，最終log無C#錯誤或runtime例外。
- 實景用與輸入共用的TickCommand實際從屋外進屋、F接馬桶水箱、拉管回主空間、RMB持續熄滅火、推桌約.99m、鬆鍵停止／F斷管後無噴流。火模擬保持啟用；不是直接呼叫ApplyWater代替操作。
- 反應測試驗證無來源不滅火不推物、薄牆後火不受水花影響、牆後物體不被推、同側地面仍濕、停用火外觀仍可滅火。水反應貼地3kg物體位移.444m；Game Camera截圖已檢視。
- 既有364個Assets／Packages／Settings檔案中4個改動（2個Runtime、Tutorial場景、FireSystem Prefab），360檔hash相同；Prefab差異只有1個新增參數。原模型／GUID／其他場景／Packages／Settings未改，Missing Meta=0，未commit／push／清理工作樹。
- 證據 `Docs/Validation/2026-10-21-water-gameplay`；沿用前一回合 `Archive/GreyboxStep5Baseline-2026-10-20` 備份，另補FireSystem.prefab。操作說明 `Docs/WATER_GAMEPLAY.md`。
- 未做硬體鍵鼠、Windows Player build或多人；舊CS0618警告仍在。管線仍可直線穿牆，窗口傳管尚未做。這是水機制可玩驗證，不是完整教學關卡完成。

## 消防遊戲 v0.1 — 2026-10-20（使用者第一關戶型＋教學第四步）

### 完成內容

- 依最新授權以 `D:/Downloads/第一關.blend` 實際網格替換Tutorial地圖；保留原格局，不套回舊三房。原檔與開啟中的未儲存Blender工作未改。來源副本在 `ArtSource/LevelOne`；使用獨立禁用autoexec的Blender程序轉資料，再產生Unity Mesh資產。
- 遊戲副本放大14倍至約17×18m，補地板與邊界、.8m可見剖面牆、五扇固定開門、最低限度Primitive家具、馬桶水箱、沙拉油瓶、角色與出口。舊Step2／Step3保留但停用，原ResidentialFire／FireRoom與九個Prefab不動。
- 新控制：WASD螢幕方向；LMB按住連續揮斧；RMB按住拿管噴射，無來源不出流體；F接／換／解來源，E安撫。雙鍵右管優先；放開停止且保留裝備；失焦／暫停／停用停止。
- 新FirefighterTools掌管.55s揮砍週期、45%最近碰撞體命中、跳過自身與斧／管互斥。FirefighterToolsView只呈現裝備和向前揮動，拿斧時連接管線仍可見。不新增傷害、破壞、正式動畫、UI或教學5–9。
- Gameplay／Visual分離：MeshCollider與Visual使用不同Mesh資產；工具玩法與命中不依賴Renderer、模型名稱或動畫事件。來源／噴嘴接點沿用；LocalPlayerInput保留舊住宅六參數與左鍵噴射，新增七參數工具命令。

### 發現與改進

- 第一輪把牆切割後所有邊界直接封口，誤封房間成低天花板。改保留牆原本頂面拓樸，門框只切掉門楣；實際畫面與角色重測確認。
- 原浴室切洞深度不足，門口仍殘留薄牆。遊戲匯出副本只加深該切洞法向三倍，保留門寬；原檔不改。
- 原切洞底面約高於地面3.8mm，玩家能跨，但NPC地面淨空查詢拒絕路徑。匯出副本將切洞向下延伸穿過地面，NPC核心不改；過程也修正Blender變換更新順序。
- 審查工具diff時修正最小.75m瞄準校正未傳到噴射器的回歸；審視動作修正斧頭向後轉為向前揮。測試增補真W／斜向位移、地板支撐、連續兩週期與工具View切換。

### 驗證結果

- Unity6000.5.0b9真編譯／匯入／安裝／保存重開／序列化接線／重跑不覆寫PASS，exit0。
- 新工具 `FirefighterToolsValidation.BatchRun`：**37項Play Mode PASS**，exit0。含位移、斜向限速、最近命中／自身排除、來源切換／牆阻／13m、工具互斥、失焦／暫停／停用與視覺移除。
- 新地圖 `UserLevelOneValidation.BatchRun`：**40項Play Mode PASS**，exit0。真正通過五扇門及後方大開口、接水噴射、拉管切油、揮斧、NPC自主穿門出屋；1005移動幀檢查固定鏡頭與胸部可見；隱藏所有架構Visual後碰撞仍正常。
- 舊住宅 `HandlingValidation.Run`：**27項Play Mode PASS**，exit0，含WASD、近距離瞄準、停止、油地與牆、互動遮擋、玩家堵路與恢復。沒有重存舊住宅。
- 擷取7張真Unity截圖，檢視總覽、噴水與揮斧；不是用概念圖當完成證據。證據 `Docs/Validation/2026-10-19-greybox-step4`（沿用工作回合起始日期），失敗報告保留。當前指南 `Docs/USER_LEVEL_ONE_STEP4.md`。
- 修改前289個Assets／Packages／ProjectSettings檔案中，只有LocalPlayerInput與Tutorial場景變更；其他287檔SHA256相同。原.blend與來源副本SHA256均 `CAB169D02A78BC7AF1C7416B88591E382C4444C438F7DE659429730AD6993351`。另加本輪腳本／材質／網格；沒有修改既有GUID、套件或Build Settings，未commit／push／清理dirty工作樹。
- 仍有既有CS0618警告；本輪最終無C#編譯錯誤／Play Mode例外。未驗證硬體鍵鼠、Windows Player build或多人。

### 下一步界線

完成到第四步，還不是完整教學關卡。第五步才加入水教學／初始火／推物條件；後續仍需油、NPC教學順序與完整流程。管線仍可能直線穿牆、來源無限，斧頭尚無破壞或傷害。

## 消防遊戲 v0.1 — 2026-10-19（教學 Greybox 第三步：住宅與跟隨相機）

### 完成內容

- 使用者授權第三步。以 Primitive 建立16×14m三房一廳一衛、開放廚房、走廊、前門、外部出生點／出口與邊界；床、沙發、可推茶几、流理台、衛浴及兩個窗戶標記保持 Blockout。
- 地圖放在 `TutorialGreybox/Step3 Residential Layout`；第二步組裝保留但停用，原 ResidentialFire／FireRoom 不重建。房門1.8m、走廊淨寬2.08m、牆高.72m厚.32m，低牆不整片消失。
- 馬桶水箱與沙拉油瓶沿用來源Prefab；油瓶視覺、Outlet與瓶身Collider升至.9m流理台，root及低位Interaction不跟模型移動。所有新增實體Collider留在root，Visual可替換。
- 相機復用既有 RescueCamera，固定50°俯角／40°水平角、.18s SmoothDamp，正交Size4.5，追蹤玩家玩法root。只看近旁房間，沒有自由旋轉、世界文字或正式UI。
- `TutorialGreyboxLayoutBuilder` 只首次安裝；已有配置時不保存或重建，Prefab override明確保留。停用階層建立牆時用作者尺寸生成Bounds，避免inactive Collider.bounds為空；牆與完整火格相交後標記防燃。本步無初始火、沒有新增NPC或元素規則。

### 驗證與實際結果

- Unity6000.5.0b9實際編譯／場景安裝exit0；同輪讀回保存場景、重跑安裝不覆寫通過。
- `TutorialGreyboxLayoutValidation.BatchRun`：**68項真Play Mode檢查通過、exit0**。實際Motor走過正門、三個臥室門、浴室門、客廳／廚房；碰撞量測門口1.80m、走廊2.08m；水箱與油瓶符合既有可達規則。
- 路線上1,039幀檢查1.2m高上半身到相機方向的視線；移動時角色偏離畫面中心最大.058 viewport；固定角度、平滑追蹤、停止後收斂通過。關閉全部Visual後牆仍擋住玩家，恢復後牆保持可見。
- 沿用NPC規則，在無火地圖安撫後自主穿過臥室／走廊／客廳／正門抵達外部出口。這是地圖幾何驗收，不代表第七步火場逃生教學已完成。
- 擷取六張實際畫面（總覽、屋外、客廳、臥室、浴室、廚房），已人工檢視總覽及主要Game Camera。總覽用獨立臨時相機，不是正常遊戲視野。
- 修改前保存場景／文件至 `Archive/GreyboxStep3Baseline-2026-10-19`；Assets／Packages／ProjectSettings共267個基線檔，只有Tutorial場景改動，其餘266檔SHA256相同。包含所有Runtime、九個Prefab、正式美術、原場景、GUID與Build Settings。
- 報告／截圖：`Docs/Validation/2026-10-19-greybox-step3`；指令／配置：`Docs/GREYBOX_LAYOUT.md`。新增兩個Editor腳本及九個平色材質，無新套件。既有CS0618舊API警告仍在，無新增C#錯誤或runtime例外。

### 範圍界線

- 本輪只做第三步。WASD／左鍵斧／右鍵管的裝置操作仍待第四步；目前Play沒有鍵鼠操控，測試以共用Motor API移動。
- 第四至九步、完整火場教學、Player build、實體輸入與多人未驗證。第二步161項分離／40項住宅回歸屬歷史，因核心及舊場景未改，本輪沒有重跑。
- 未commit／push／發布；既有髒工作樹及舊刪除狀態完整保留。

## 消防遊戲 v0.1 — 2026-10-12（教學 Greybox 第二步：Gameplay／Visual 分離）

### 完成內容

- 使用者授權第二步。沿用已有水油／移動／NPC 規則，新增 `Greybox/Prefabs` 九個 Primitive 元件：Player、Resident、WaterSource、OilSource、Pushable、FixedObstacle、Ground、FireSystem、Exit。
- 每個元件的 Collider／Rigidbody／玩法資料固定在 root，外觀在 Visual。Nozzle／Interaction／Outlet／Arrival 接點在獨立 Anchors；Gameplay 不藉模型名稱、Renderer bounds 或骨架尋找物理／射線位置。呈現可以讀玩法，玩法不依賴呈現。
- `SprayEmitter` 增加可配置 Nozzle anchor；`HoseConnection`／`FluidSource` 增加低位互動接點，仍維持 root 的接管距離／管長。舊場景欄位空時使用原公式。`FireFieldView.field` 加序列化，使 View 能在子節點讀 root 模擬；`HoseView.OnDisable` 關閉管線並重置插值。其餘元素規則、移動參數、NPC 逃生與輸入不重寫。
- `GreyboxPrefabBuilder` 用明確碰撞尺寸建立資產，不從正式模型 bounds 推算。根節點先停用、接好場景引用後才啟用；加入 `TutorialGreybox/Step2 Test Assembly`，提供12m平面測試組裝。建立工具只補缺少資產／組裝，不覆寫現有手動修改。
- 這輪沒有三房一廳一衛地圖、教學 UI、斧頭或 LocalPlayerInput。新 WASD／左斧／右管仍是第四步規格，現在組裝由測試命令驅動；第三步才製作完整 Greybox 住宅。原住宅及正式模型不重建。
- 模型替換與引用設定說明：`Docs/GREYBOX_MODEL_CONTRACT.md`。更新計畫與開發上下文；原版本內容保留。

### 實際驗證

- Unity **6000.5.0b9** 真編譯／資產匯入成功，九個 Prefab 的序列化邊界檢查 PASS：Visual 無 Collider／Rigidbody／玩法腳本，root 邏輯引用不指向 Visual，沒有 FBX 依賴。報告 `Docs/Validation/2026-10-12-greybox-step2/prefab-contract.txt`。
- 新 `GreyboxSeparationValidation` 在臨時空場景建立真 Prefab 組裝。原 Primitive、關閉全部 Visual、刪除後用改名／不同位置及縮放的 Renderer 替換，三種情況共 **161 項 Play Mode 檢查通過**，exit **0**。包含實際 Motor、13m 約束、接水／切油、射線落地滅火至安全值、油地滑動、NPC 真正自主尋路至出口與 Manager 計數；也檢查 Collider／全部接點保留、火場 View 重建、View 停用後模擬仍可 Step。
- 關閉外觀負例：固定障礙仍擋移動，牆後火不受噴流命中，水仍推動真 Rigidbody。初次推物檢查在僅一個 .02s 物理 tick 後要求位移>.1m而失敗；保留初次報告，增加命中 Collider 檢查及多個真物理 tick 取樣，未降低位移門檻或修改玩法。最終 `.14s` 量得 `.2254m` 位移。主代理檢查並修正測試取樣，原失敗與最終報告皆保留。
- 原住宅 `CharacterBatchLaunch.Residential` 本輪 **40 項 Play Mode 回歸通過**，exit **0**：接水箱／拉管／滅火／居民穿住宅自主逃生、油地／助燃、牆阻、水推玩家與家具仍正常。持久副本 `residential-regression.txt`；不是引用10-07歷史結果。
- 主代理檢查實際 Runtime diff、Prefab／Scene 內容與驗證結果。既有驗證器 obsolete API、背景授權 token 更新訊息仍保留；最終通過批次無 C# 編譯錯誤或新的 gameplay 例外。未做 Windows Player build、實體鍵鼠、多人與完整教學驗收。
- 最後重新載入已保存 Tutorial 場景，Motor／噴射器／來源／兩種 View／NPC出口／Manager 的跨 Prefab 引用均解析成功，`GREYBOX_PREFAB_CONTRACT_PASS`、exit0；重跑保留已有組裝。基線224檔中只有核准的五個 Runtime 檔及 Tutorial 場景變更，其餘 **218／218** hash相同，包含原住宅／正式模型／Packages／ProjectSettings；所有新增資產都有 Unity 產生的 `.meta`。詳見 `preservation.json`。
- 修改前範圍備份 `Archive/GreyboxStep2Baseline-2026-10-12`，完整保護清單 `Docs/Validation/2026-10-12-greybox-step2/baseline.json`。未 commit／push／發布；保留原有髒工作樹。

### 下一步

第三步：以這些元件製作三房一廳一衛 Greybox、門口／走廊通行尺度與斜俯視相機。完整模型可等之後替換，不阻擋關卡工作。

## 消防遊戲 v0.1 — 2026-10-12（教學 Greybox 第一步：基線與獨立場景）

### 授權與完成內容

- 使用者要求先用 Primitive／ProBuilder 做完整可玩教學，讓正式模型日後能直接替換；先列出九步與驗收條件，本輪只核准第一步。新操作要求 WASD 移動、左鍵拿出消防斧並揮砍、右鍵拿出消防管並噴射，已記錄於 `Docs/TUTORIAL_GREYBOX_PLAN.md`，本輪不提前改 Runtime 或製作斧頭。
- 保存目前 HEAD `98ae6ba9c5d3efeb6cb23fc2a7a0ae9e1472ea33`、髒工作樹、版本／套件／Build 場景與220個既有檔案 SHA256：`Docs/Validation/2026-10-12-greybox-step1/baseline.json`。可復原備份 `Archive/GreyboxBaseline-2026-10-12-0042` 共224檔，含既有 Assets／Packages／ProjectSettings 與工作文件，不含快取。
- 新建 `Assets/FireRescue/Scenes/TutorialGreybox.unity`，只有 Environment／Actors／Sources／Objectives／Presentation 用途區段，以及48°／40°正交相機與燈光。尚無地板／房間／人物／來源／火／教學 UI，不宣稱為已可玩教學。
- 新增 Editor-only `TutorialGreyboxSetup`；選單先提示保存，目前場景保存或放棄後才切換；教學場景存在時只開啟，絕不重新生成。Unity 產生場景與腳本 `.meta`，不手編場景 YAML。不新增套件，不改預設 Build 場景，不呼叫全住宅重建工具。

### 驗證與修正

- Unity **6000.5.0b9** 基線匯入／編譯檢查 exit **0**。第一輪工具編譯成功，但建立失敗：batch 預設未儲存空白場景不允許 `NewSceneMode.Additive`。改成僅在選單保存確認後或獨立 batch 工作階段使用 Single，建立方法改 private；沒有改玩法迎合驗證。失敗報告 `scene-validation-initial-failure.txt` 保留。
- 修正後真 Unity 編譯、建立、讀回新舊場景與 **16 項 Editor 檢查 PASS**、exit **0**。包含新場景 GUID／無 Missing Script／無提前 gameplay 元件／相機／燈光／不依賴 FBX／重跑場景 bytes 不變／舊住宅可載入無 Missing Script／住宅及 GUID 不變／Build順序與 enabled flags不變。
- 最終重新啟動另一個 batch，16項檢查再次 PASS、exit0、`Created new scene: False`，確認跨 Editor 工作階段也不覆寫。全部 **220／220** 既有 Assets／Packages／ProjectSettings 檔案 hash 相同；備份也逐檔核對，0不符。只新增教學場景與 Editor 工具、文件／驗證報告，原 Runtime、模型、住宅、套件與設定未改；比對結果 `preservation.json`。
- 編譯時既有驗證器產生 obsolete API 警告；新工具沒有 C# 編譯錯誤。背景 Unity licensing access-token 更新訊息仍可能出現，但 batch 編譯／驗證正常完成；不清 Console、升級套件或更動授權設定。
- 未進 Play Mode、未重跑住宅 gameplay、自動輸入或 Windows Player build，也未驗證模型替換。先前住宅40項 PASS仍標歷史，不拿來宣稱本輪新操作能玩。未 commit／push／發布或更新全域記憶。

### 下一步與手動開啟

Unity 開啟 `TutorialGreybox.unity`，或 `FireRescue > Tutorial > 建立或開啟 Greybox 教學場景（不覆寫）`。目前 Game View 是空白背景。原 `ResidentialFire` 左鍵仍噴水／油；新左斧右管在操控階段才實作。下一步是第二步 Gameplay 根節點與 Visual 分離，等待使用者授權。

## 消防遊戲 v0.1 — 2026-10-07（角色 v0.3 六輪視覺改進）

### 使用者要求與修正

- 使用者認為上一版不夠好，要求代理自行看圖、說明原因、持續改善。主代理直接看 v0.2 Unity 基線及原參考，確認比例細長、尖衣襬／腿根分離、消防帽像圓盤、反光帶埋住、髮型像疊片，跑步缺乏可讀接觸節奏；只改人物與持管呈現，住宅／家具／水油規則不重做。
- 共六輪幾何審查。第一輪否決外掛浮動反光環和過長腿；第二輪 Unity 暴露帽髮 lathe 面序反轉、側背缺面；第三輪靜態改善但動作仍僵硬；第四輪平衣襬切太高令腿根更露出；第五輪寬衣襬覆蓋改善但帽冠太尖；第六輪改13圈半扁球帽冠與薄沿後接受。失敗圖／報告保留 `Docs/Art/Models/v0.3/iterations`，完整原因記 `Docs/Art/CHARACTERS_v0.3_REVIEW.md`。
- 保留18骨 Generic rig、Idle 2秒／Run .8秒。大頭短肢、寬衣襬、簡單髮帽、反光材質直接分到Body／Leg表面、平底圓角鞋；Run用變形頂點逐幀校正Hips支撐、增加跨步及胸部前傾，Root保持原地。沒有額外狀態／動畫套件／精確IK。
- 噴嘴視覺跟隨Chest並校正雙手平均握點，Apply重跑保留原噴嘴再替換角色；噴射器判定及Actor碰撞不改。主代理實際檢查生成器、關鍵Editor差異與Unity效果；獨立唯讀審圖最後確認無P1，P2是上半身較鎖定、整體偏持管穩定小跑。

### 發現與驗證

- 新Mesh量測初期用BakeMesh default false，再TransformPoint重複乘Renderer縮放，錯報身高／腳底／覆蓋；fresh FBX reimport與分件診斷定位後改true補償，沒有放寬標準。原錯誤表標為歷史失效數據，不能把它當模型實際尺寸。作者與Unity最終身高均1.600m。
- 臨時攝影棚陰影normal bias原.2m造成漂浮感，縮小後暴露帽面影圖條紋；改臨時燈光高解析度／相機裁切。初次全域QualitySettings setter雖恢復值仍觸發Unity格式升級，SHA256比對發現後去除全域依賴、限定恢復自動新增欄位；最終原hash相同。正式場景光照、相機、畫質與設定未改。
- Unity **6000.5.0b9** 實際編譯／匯入／角色更新／場景保存成功，`CHARACTER_SETUP_PASS`、exit **0**。保存前 `CHARACTER_GAMEPLAY_PRESERVED`；原FBX／Prefab四個GUID不變。
- **15 項 Mesh 檢查通過**，`CHARACTER_MESH_PASS`：身高1.600m、頭Mesh佔總高40.3%／41.1%、Idle鞋底0、Run最低2.9mm，兩者24／24姿態有支撐；帽髮面序、髖部垂直覆蓋、反光288表面三角亦通過。這是審圖輔助，不保證完整美術無瑕疵。
- **33 項角色 Play Mode 通過**，`CHARACTER_ANIMATION_PLAYMODE_PASS`、exit **0**：原31項Generic骨架／loop／位移驅動／停止／撞牆／停用／傳送／NPC自主Run至出口，新增噴嘴Chest跟隨與實際Run平均握點<7cm；不是精確雙手IK。
- **40 項住宅 Play Mode 本輪重跑通過**，`RESIDENTIAL_PLAYMODE_PASS`、exit **0**：接水箱→拉管→安撫／滅火→NPC自主逃生，油地／助燃、牆後噴射負例、水推動等原功能正常。最終日誌無新C#編譯錯誤／runtime例外；既有obsolete警告與背景授權警告沒有靠更新套件或清Console消除。
- 最終 **58／58** Runtime／其他家具FBX與Prefab／ProjectSettings SHA256與本輪開始相同，包含角色呈現Runtime未改。比對及日誌持久副本 `Docs/Validation/2026-10-07-character-v3`。
- 真Unity正／側／背／48°斜角、主遊戲水油實拍、24幀正面Run與24幀側面Run（各800ms）、30幀Idle（2000ms）在 `Docs/Art/Models/v0.3`；主代理看圖片／動作關鍵姿態及真正Game Camera，不拿生成概念圖代替模型。來源 `.blend`／FBX、重建腳本保持可編輯，作者三角8136／8664、Unity8028／8664。
- 更新README／上下文／角色審圖與工作紀錄，v0.2改標歷史。未commit／push／發布，沒有刪除使用者既有髒工作樹資料。

### 品質界線與下一步

主代理接受此版作為圓潤低模Prototype，不宣稱等同概念圖的最終美術。Run是持管穩定小跑，沒有精確雙手IK、獨立噴射／滑倒動作。未做Windows Player build、實體鍵鼠注入、多人；來源無限／管線可能穿牆等既有限制不變。開 `ResidentialFire.unity` 按Play即可，WASD／F／E／左鍵不變。

## 消防遊戲 v0.1 — 2026-10-05（圓潤角色 v0.2、骨架與動作）

### 要求與完成內容

- 使用者附上住宅參考圖，要求人物不要與概念圖差距過大：更圓潤、少細節，並綁骨架與加入跑步／待機。本輪只重做消防員與居民，沒有按細稿重做家具、地圖、正式 UI 或多人。
- 主代理檢視參考圖與舊 Unity gallery，確認原模型是方塊軀幹／腿與偏細長比例。改大頭、圓鼓身體、短肢、簡化髮型與黃帽，取消徽章；看第一輪實際渲染後再把珠狀分段腿改成連續跨膝加權腿、平帽圓柱手臂改成圓端。
- Blender 來源與 FBX 兩者都有 18 骨架、Armature modifier 與頂點權重；膝部採上下腿混合，其餘為簡化公仔區塊單骨權重。Idle 2 秒小呼吸／點頭，Run .8 秒交替抬腿／彎膝；居民擺臂，消防員維持雙手前持噴嘴。沒有引入 IK／Humanoid 重定向／指骨／臉骨／動畫套件。
- `CharacterLocomotionAnimation` 只讀解決碰撞後的水平位移，在 LateUpdate 驅動 Speed／CycleSpeed 與 .12 秒 Idle／Run 過渡。玩家／居民參考速度 4／2.15 序列化；撞牆不空跑，停用／傳送重置。Root Motion 關閉，沒有改玩法移動或讓 Animation Event 掌管水／油／救援。
- `CharacterArtSetup` 只更新兩組角色 Prefab／Controller，替換住宅人物 Visual 並加呈現元件，不呼叫全住宅建置器。保存前比對所有既有玩法 MonoBehaviour／Collider 的序列化值相同。原 FBX／Prefab GUID 保留；舊資料備份 `Logs/CharacterV2Baseline`。
- `build_characters.py` 轉呼叫新版可重建腳本，避免沿用舊指令生成回沒有骨架的角色。建置住宅工具也接好新角色呈現，日後全重建仍有動畫，但本輪未全重建。

### 實際驗證

- Blender 作者身高兩者 1.60m、18 骨、權重有效、兩個 Action 非空；逐幀 Root 穩定，首末 pose 一致。消防員 9116／居民 8448 作者三角數；Unity 匯入分別 9024／8448。報告 `Docs/Art/Models/v0.2/character-v2-report.json`。
- Unity **6000.5.0b9** 真正編譯／匯入／只換人物／保存接線成功，`CHARACTER_SETUP_PASS`、exit **0**；未改套件／ProjectSettings。57 個既有玩法腳本／其他家具 FBX／Prefab／ProjectSettings SHA256 未變，四個 FBX／Prefab GUID 相同。
- 新 `CharacterAnimationValidation` **31 項 Play Mode 通過**，`CHARACTER_ANIMATION_PLAYMODE_PASS`、exit **0**。包含 Generic Avatar／Controller、序列化 4／2.15 基準、腳 Mesh BakeMesh 真實姿態變化／循環與根位移、實際 Motor 移動／停下／物理撞牆、停用／重新啟用／傳送、住戶自主 Run、經原住宅路線到出口及救出後重新啟用待機。
- 原 `ResidentialPrototypeValidation` 本輪重跑 **40 項通過**，`RESIDENTIAL_PLAYMODE_PASS`、exit **0**。接馬桶水箱 → 拉管跨房 → 安撫／持續噴水滅火 → 居民經門／走廊／客廳到正門外完整流程仍正常；油地／助燃／噴流牆阻／水推物件均維持。
- 命令：`CharacterBatchLaunch.ApplyAndCapture`、`.Animation`、`.Residential`，僅正常 batch 等待 Editor 初始化；日誌 `Logs/character-setup.log`、`character-animation.log`、`character-residential.log`。新 runtime 例外與 C# errors 為 0。舊驗證器的 obsolete API 警告與背景授權 token 警告未阻止本輪執行，未更新套件或清除 Console。
- 真實 Unity 正面／斜角／背面、24 幀 Run／30 幀 Idle、主遊戲鏡頭水／油畫面在 `Docs/Art/Models/v0.2`。主代理檢查 Run 接觸與抬腳 pose，不以動畫檔存在就當成動作驗收。
- 預覽中出現地板自陰影條紋與同 Editor update 的 Skinned Renderer 快取凍結姿態：僅對臨時攝影棚關閉地板投影並刷新每次 render 的 skin matrix，沒有為截圖更改正式場景光照／畫質／玩法。原預覽日誌與修正後 `character-preview-skin-refresh.log` 保留。
- 主代理檢查子代理新增檔案，修正測試 coroutine 的 yield／catch 編譯風險、單 Mesh 骨數假設、停止移動後誤等待 Run、播放速度／自動轉待機對 loop 取樣的影響；沒有改動玩法去迎合測試。
- 更新 README／開發上下文／角色交接，舊建模文件保留歷史界線。持久報告 `Docs/Validation/2026-10-05-character-v2`；未 commit／push／發布。

### 邊界與手動步驟

開啟 `ResidentialFire.unity` 按 Play：WASD 移動自動 Run，停下自動 Idle，F／E／左鍵不變。未驗證獨立 Windows Player build／多人／實體鍵鼠；未加滑倒、受傷或獨立噴射動作。雙手持管是簡化動作，不是精確 IK，細小手掌／噴嘴偏差可在後續美術微調。對參考圖的輪廓／比例有實際渲染檢查，但不宣稱所有光照、材質或住宅細節完全還原。

## 消防遊戲 v0.1 — 2026-10-05（單人操控與互動調整）

### 範圍與修正

- 使用者核准模型後回覆「開始做」，沿用已定案的住宅／低模，先調整接管、瞄準、油地移動與 NPC 互動。沒有重建場景、修改模型／Prefab、加 UI／動畫／音效／多人或大型系統。
- 在未修改玩法的隔離副本先做真實 Unity 量測：2m 內隔 .72m 低牆仍可接來源；NPC 原安撫線高度 .75m 會越過低牆；油地 held .5s 速度僅 1.1 m/s；撞牆仍保留向內 4 m/s，第一次反向輸入位移為 0。
- `HoseConnection.CanConnect` 讓 TryConnect／Interact 共用距離與 Layer 8 低位 .35m 可達檢查；略過來源自身 Collider 與包含低端點的支撐家具，流理台上的油瓶仍可接。NPC 安撫同樣改 .35m 遮擋取樣；尋路與逃生邏輯不改。
- `LocalPlayerInput` 將裝置讀取轉交共享 `TickCommand`，保持螢幕方向 WASD、F、E、左鍵不變。近身瞄準 .75m 內保留朝向，噴射時立即對準游標；未噴射仍平滑轉身。輸入／噴射器停用與失焦時 `Stop`，避免殘留噴射狀態。
- Motor 油地 held 加速度改為 6、放開減速度 3；正常速度 4／地面加減速 32 不變。CharacterController 碰撞消除向牆內的速度與外力分量，保留切向滑動，不新增全套滑行或繩索物理。

### 驗證與證據

- 保留原 Unity Editor 開啟狀態，以 `Logs/HandlingValidationProject`（只複製 Assets／Packages／ProjectSettings）隔離驗證，不儲存 runtime 狀態到原場景；修改前腳本與場景在 `Logs/HandlingBaseline`。原場景 SHA256 與這輪基準相同。
- 同一量測探針修正後：隔牆接管／安撫皆 false、oil held .5s 速度 3.0 m/s、撞牆向內速度歸零、第一個反向輸入立即退開 .06m。前後量測為 `Logs/handling-baseline-results.txt`、`handling-after-probe-results.txt`。
- 新 `HandlingValidation` **27 項 Play Mode 檢查通過**，`HANDLING_PLAYMODE_PASS`、exit **0**：四向／斜向、共享 F/E 命令、近身／180° 瞄準、鬆開／停用／失焦事件停止、來源距離與低牆正負例、支撐家具、油地慣性／轉向／撞牆、NPC 低牆安撫、真正 Layer 9 玩家 CharacterController 堵路及離開後到出口。
- 原 `ResidentialPrototypeValidation` **40 項回歸通過**，`RESIDENTIAL_PLAYMODE_PASS`、exit **0**：實際住宅接馬桶水箱、跨房拉管、安撫／滅火、自主逃生、換油瓶／滑地／助燃、牆後噴流與水推動皆保持正常。這是本輪重跑，不引用上一輪歷史 PASS。
- 正式檢查命令：隔離副本使用 `-executeMethod FireRescue.Editor.HandlingBatchLaunch.Handling` 與 `.Residential`，分別進入上述測試；Launch 只在測試副本等待 Editor 匯入／初始化完成。日誌 `Logs/handling-smoke.log`、`handling-residential.log` 無新 C# 錯誤或 runtime 例外。
- 早期前後量測兩次皆出現 Unity Beta `UnityEditor.Search.SearchDatabase` 啟動索引 `ArgumentOutOfRangeException`，堆疊為 Editor Search 初始化，不是 FireRescue 玩法；保存於 `handling-baseline.log`、`handling-after-probe.log`，未改套件／關閉失敗檢查／清 Console。正式測試等待 Editor 初始化後未再出現。
- 主代理核對實際 diff／測試 fixture，修正了測試的重複停用未觸發事件、格角噴油半徑、混入裝置 Update、以及錯把 Layer 8 BFS 障礙當成玩家堵路的問題；沒有修改玩法來迎合錯誤測試。獨立唯讀檢查未發現 blocker。更新 README／開發上下文與建模歷史界線。
- 持久報告副本：`Docs/Validation/2026-10-05-handling`。原先未提交的檔案保留；本輪沒有 commit／push／發布、沒有新增套件或改 ProjectSettings。

### 仍需試玩

自動化使用真實 MonoBehaviour／物理與共用命令入口，但未注入實體鍵鼠；失焦只驗證事件 hook，不宣稱已測 OS 切換視窗手感。仍請停止再 Play 一次，確認 .75m 近身瞄準與 6／3 油地操控是否符合喜好；參數可在 Motor Inspector 微調。未做獨立 Player build／多人；來源無限、管線可能穿牆的限制維持。

## 消防遊戲 v0.1 — 2026-10-05（住宅低模建模與整合）

### 核准與實作

- 使用者回覆「開始建模」，依 10-04 簡化住宅圖與圓潤公仔方向開始；室內保持必要低牆／家具色塊，不加入工作台、儲物櫃、工業來源或裝飾大系統。
- 使用本機 Blender 5.1.2 製作 12 個可編輯 `.blend` 並匯出 FBX：消防員、居民、馬桶、沙拉油瓶、噴嘴、沙發、茶几、床、廚房流理台、洗手台、淋浴與圓角 Block。角色高 1.6m，消防員黃帽／橘紅制服／反光帶；角色是靜態 Mesh，未製作骨架動畫。
- `ArtSource/Tools` 保存可重建腳本，`ArtSource/Models` 保存來源；Unity FBX／Prefab／平色材質在 `Assets/FireRescue/Art`，`.blend` 放在 Assets 外，不依賴 Unity 自動啟動 Blender。
- 新增 `ResidentialFire.unity`：後排三房、走廊、客廳與開放廚房、浴室、正門外出口。保留原 `FireRoom.unity` 與 meta，住宅設為唯一 Build 場景。
- Water 在馬桶後方開放水箱，馬桶座蓋關閉；Oil 是流理台上的小瓶沙拉油。`FluidSource` 只新增可指定 outlet 與原高度 fallback；來源根節點在地面，油瓶 Visual 提高 .94m，保持既有接管與長度合約。未新增容量或搬油瓶。
- HoseView 只改深灰管線色；新住宅居民停用全身狀態 tint，舊原型預設不變。FireField、ElementReaction、FirefighterMotor、SprayEmitter、HoseConnection 皆與本輪基準 SHA256 相同。

### 整合修正與驗證

- 首次實際住宅渲染發現 FBX 的 -90° 轉軸與非均勻根縮放令地板直立；改用零旋轉外層 wrapper 縮放，保留 FBX 轉換旋轉。新增 renderer／Collider bounds .06m 對照檢查；最初錯位圖與 setup 日誌保留在 `Logs/residential-overview-first.png`、`residential-setup-first.log`。
- 首次住宅救援失敗，加入 NPC 位置／火格診斷後確認火已熄滅但床侵入起始格的淨空範圍；床移至 z=7.2。後續發現 .22m 牆剛好落在格間，NPC 判為有路但物理門牆仍阻擋；牆增厚至 .32m，新增所有相鄰可走格間不可穿固定牆的負例。未停用碰撞、改尋路或直接傳送 NPC。
- 自動化路線修正為經真實客廳開口接油，噴水後站到一旁讓出 NPC 通道，並從噴水期間開始記錄 NPC 經過門／走廊／客廳。失敗與診斷證據保留於 `Logs/residential-smoke-npc-first.log`、`residential-smoke-npc-diagnostic.log`、`residential-smoke-wall-gap.log` 及相應 results。
- Unity **6000.5.0b9** 實際匯入／編譯／建置通過：`RESIDENTIAL_SETUP_PASS`，批次 exit code **0**。匯入 12 個 FBX／12 個 Prefab，模型尺寸／面數在 `Logs/residential-art-import.txt`；有授權背景服務警告但未阻止編譯或 Play，未改授權設定。
- 住宅 **40 項 Play Mode 定向檢查通過**：`Logs/residential-playmode-results.txt` 為 `RESIDENTIAL_PLAYMODE_PASS`，最終 batch exit code **0**，無 C# 編譯錯誤或 runtime 例外。測試以 60Hz 步進共用真實玩法方法；接水 → 跨房拉管 → 安撫／滅火 → 在火勢與 NPC Update 啟用下自行到正門外 → GameManager 記錄一次救援。
- 另涵蓋來源／Mesh／outlet、碰撞與格網合約、未接不可噴、13m 限制、噴流牆後負例、水推茶几／玩家、油助燃、水滅火／濕度、換接油瓶、油落地／滑動、放開停止。
- 主代理實際檢視住宅 overview、模型 gallery、Game Camera 水／油截圖；更新 README、開發上下文、圖稿核准狀態及 `Docs/Art/MODELING_v0.1.md`。Unity 真實預覽與報告副本保存於 `Docs/Art/Models/v0.1`。
- 獨立唯讀交付檢查確認 12 組 FBX／Prefab／meta 存在、GUID 唯一、住宅與 Prefab 引用可解析、兩個 outlet 與 tint 設定已序列化；無交付阻塞。未重複執行已通過的測試。
- 已啟動互動 Unity Editor，`Logs/residential-editor.log` 確認開啟 ResidentialFire；模型清單另已送至 Codex 預覽，供使用者按 Play 手動測試。

### 邊界與手動下一步

開啟 `Assets/FireRescue/Scenes/ResidentialFire.unity` 按 Play，手動試 WASD／滑鼠手感、F 接來源及 E 安撫。沒有獨立 Windows build、四人連線或實體鍵鼠自動化；不宣稱模型已完成最終美術／動畫。管線仍是簡化線段，可能直線穿牆；來源無限，不做容量或搬瓶。未 commit／push／發布、未刪舊資料。

## 消防遊戲 v0.1 — 2026-10-04（室內圖改為簡單 Blockout）

- 依使用者「室內圖不需要這麼細緻」要求，以內建 imagegen 將住宅第二版簡化為色塊／Primitive 風格；保留三房一廳一衛、開放廚房、門口與必要家具，拿掉植物、裝飾、衣櫃、床邊桌及室外景觀細節。
- 馬桶水箱與沙拉油來源位置不變；NPC／玩家用 Capsule 示意。消防員與住宅物件圖未重新生成。
- 最終圖為 `Docs/Art/Concepts/v0.1-r2/01-residential-blockout.png`；完整提示詞保存於 `PROMPTS-BLOCKOUT.md`。審圖文件已切換至新圖，原細稿保留作歷史。
- 視覺檢查房間數、門口連通、來源水箱而非馬桶盆，以及細節簡化；本輪未建模、未更動 Unity 場景／Runtime、未執行編譯或 Play Mode。仍待使用者同意再建模，未 commit／push。

## 消防遊戲 v0.1 — 2026-10-04（住宅概念圖第二版）

- 使用者修正高概念：消防車沒水，只能拿管進屋找日常物品噴射。水源是馬桶水箱、油源是一瓶沙拉油；完整三房一廳一衛，不要工作台、儲物櫃與室內不合理的工業來源桶。
- 以內建 imagegen 修訂住宅配置與八組住宅物件；消防員圖暫沿用。最終圖保存於 `Docs/Art/Concepts/v0.1-r2/01-residential-layout.png` 與 `03-household-props.png`，完整提示詞／修正紀錄在同資料夾 `PROMPTS.md`。
- 配置採後排三間臥室、約 2m 走廊、前側客廳與浴室，客廳一角放開放廚房，油瓶在流理台上，水箱在浴室內。使用獨立唯讀配置檢查後，補明確廚房位置、門口淨空與寬通道規格，不新增遊戲系統。
- 主代理檢視圖片：首稿水源誤畫在馬桶盆，局部修正為打開的水箱與關閉座蓋，補客廳標籤、清除臥室 2 額外缺口；修正後路線標記仍穿牆，另一次局部修改移除箭頭，以審圖文件敘述逃生動線。最終檢查三房一廳一衛、住宅來源與移除工業道具。
- 第一版圖未刪除；其審圖文件標示已被取代。更新開發上下文區分「待核准的住宅提案」與「目前已實作的單房 Prototype」。
- 本輪只新增圖片／文件，未建模、未匯入 Unity Assets、未改 Runtime／場景；未重跑編譯／Play Mode／Build。仍等待使用者同意圖片再建模，未 commit／push。

## 消防遊戲 v0.1 — 2026-10-04（建模前概念圖審核）

- 依使用者「先生成圖，同意後再建模」要求，產出三張圓潤低模公仔風格概念圖：室內配置、消防員三視圖／持管姿勢、七組核心物件。
- 使用內建 imagegen，圖稿保存至 `Docs/Art/Concepts/v0.1/01-floorplan.png`、`02-firefighter.png`、`03-props.png`；完整提示詞保留於同資料夾 `PROMPTS.md`。
- `CONCEPT_REVIEW.md` 記錄空間配置、資產外形優先依據及審核範圍。室內沿用約 12×10 m 小房間、水／油來源、可推箱、初始火帶、後方 NPC 與門外出口；圖中藍色缺口是滅火後示意，不是新增開局安全路線。
- 視覺檢查三張構圖、角色方向、物件辨識；主平面圖仍為立體俯視概念，尺寸與通道規格以文件為準。未將圖稿宣稱為實際模型或拓樸驗證。
- 本輪僅新增 Docs 圖片／文件與本紀錄，未建模、未匯入 Assets、未修改 Runtime 或場景，未新增多人及大型系統。沒有重新執行 Unity 編譯／Play Mode／Build。
- 狀態：等待使用者同意三張方向，再進行建模與小步整合；未 commit／push。

## 消防遊戲 v0.1 — 2026-10-03（水＋油小房間原型）

### 改題與範圍

以最新消防題材取代《偷偷搬家》，停止尚未實作的半透明牆修改。舊 `Assets/SneakyMove` 與 meta 移至 `Archive/SneakyMove-2026-10-03`，並保留當時 README／上下文；舊題材不再參與 Unity 匯入、編譯或 Build。既有 Git 歷史及以下工作紀錄不刪除，沒有 commit／push。

新題材第一版只做一名本機玩家、一種 NPC、一個測試房間；只包含水、油與火。不實作多人連線、正式 UI、美術、動畫、音效、存檔或完整關卡。

### 實作與架構

1. 建立 `Assets/FireRescue/Runtime`，按 Core／Player／Hose／Source／Fire／Reaction／NPC／Game 分資料夾。唯一讀鍵鼠的元件是 LocalPlayerInput；玩法由明確的方法呼叫驅動，沒有全域單例玩家。
2. 建立屋頂開放的小房間、低牆、障礙、可推動箱子、兩個來源、前庭與出口。主場景為 `Assets/FireRescue/Scenes/FireRoom.unity`。
3. WASD 依畫面方向移動，滑鼠瞄準，F 接管／換來源／拔管，左鍵持續噴射，E 安撫 NPC。鏡頭固定斜角跟隨；沒有遊戲 UI。
4. 每個玩家有獨立 HoseConnection，13m 直線長度限制；HoseView 以 18 段線呈現接頭、鬆弛與跟隨，不做繩索纏繞或複雜軟體物理。來源無限、非獨占，方便以後擴充玩家。
5. SprayEmitter 執行 6.5m 物理命中查詢，ElementReaction 施加元素。水降低火勢、短暫增加濕度並可推動 Rigidbody／玩家；油產生滑動慣性，也是獨立燃料，遇火助燃及加快擴散。
6. FireField 使用 1m 格網維護燃料、油、濕度、強度；火勢先燃燒衰減，再以同一份快照擴散。FireFieldView 只顯示簡單火柱、油漬與濕地，不參與玩法判定。
7. RescueNpc 需玩家靠近安撫，且存在完整無火路線才出發。四鄰 BFS 同時檢查火格與固定障礙；路線重燃即停下重算，實際抵達路點才推進，救出事件只發出一次。NPC 不由玩家搬運。
8. RescueGameManager 記錄救出數與完成事件；完成後可繼續測試火場。未來多人需另做輸入權限、主機判定及狀態同步；本版不宣稱已支援四人連線。

### 整合修正過程

- 主代理檢查子模組實際程式後，修正 NPC 場景參照未序列化、定期重算路徑反覆退回格中心、阻擋時跳路點，以及火格更新覆蓋鄰居傳入強度的問題。
- 首次核心測試停在入口附近。診斷顯示附近只有地面和玩家，沒有門牆阻擋；Batch Mode 在 12 秒內跑了超過 10 萬小步，單幀位移低於 CharacterController 預設 `minMoveDistance=0.001` 而被吞掉。玩家／NPC 改為 0，測試採固定 60Hz 步進後可正常進房。
- 油噴射測試最初把目標選在牆後，因真實碰撞而無法噴到；將該情境保留為牆壁阻擋的負例，再增加門內可命中的正例。未為了測試穿牆或停用碰撞。
- 失敗證據保留在 `Logs/fire-smoke-first-attempt.log`、`fire-smoke-diagnostic.log`、`fire-smoke-oil-target.log` 與相應 results 檔案。

### 實際驗證

- Unity `6000.5.0b9` 編譯與場景生成通過：`FIRE_SETUP_PASS`，批次 exit code 0。
- **29 項 Play Mode 定向檢查全部通過**：`Logs/fire-playmode-results.txt` 為 `FIRE_PLAYMODE_PASS`；最終測試批次 exit code 0，沒有 C# 編譯或執行例外。
- 核心驗收實際走過：接水源 → 用 CharacterController 拉管進房 → 安撫但火帶擋路時等待 → 以實際噴射命中地面、降火開路 → 在火勢與 NPC Update 同時運作下自行走到出口 → GameManager 完成救援。
- 另驗證未接管不可噴、長度限制、遠端不可接來源、火勢對稱擴散、油助燃、不可燃鋪面與油燃料、水滅火／濕度、油地慣性、重燃停步、重複救援防護、鬆開停止噴射、水推箱／玩家、換接油源、牆後負例及實際噴油落地。
- 實際檢視 `Logs/fire-rescue-loop.png`、`Logs/fire-oil-hose.png`，確認房間、管線、水／油地塊、角色與火格可見，沒有常駐文字 UI。
- 同步更新 README 操作說明及 `Docs/PROTOTYPE_CONTEXT.md` 架構合約。設定變動包含產品名稱、唯一 Build 場景及 Layer 8–11 名稱；沒有新增套件。
- 交付時已用 Unity Editor 開啟 FireRoom，編輯器日誌確認場景載入且未進入 Play。GUI 啟動另出現一次 `Attempted to call .Dispose on an already disposed CancellationTokenSource` 訊息，未附可定位堆疊；生成及最終 Play Mode 測試日誌沒有此訊息，暫列未定位的 Editor 啟動訊息，不改動套件。

### 驗證界線與下一步

這輪自動化共用真實玩法入口，但不模擬實體鍵鼠；LocalPlayerInput 已做靜態對照，操作手感仍需使用者按 Play 測試。沒有獨立 Windows build 或多人測試。NPC／玩家暫無傷害；管線無碰撞纏繞；推箱不改變燃料；水不洗油。先依接管、噴射、滑動及清路救援是否有趣的回饋調整，不擴張完整系統。

## 偷偷搬家 P1 — 2026-09-30（WASD 畫面方向修正）

- 問題：Camera 轉成斜角後，鍵盤仍沿世界 X/Z 軸移動，因此 W 在畫面上變成右上。
- 修改：`ThiefController.Update` 改呼叫 `MoveFromView`，以鏡頭投影至地面的前／右方向換算 WASD；W 上、S 下、A 左、D 右，不受角色朝向影響。底層 `Move` 世界座標合約不變，既有測試路線不必重寫。
- 範圍：沒有改動 Camera、場景、速度、碰撞、拿放、AI 或計分。斜向輸入仍限制向量長度，不會加速；同步更新 README 及開發上下文。
- 驗證：Unity `6000.5.0b9` 實際編譯通過；`InputValidation` 定向 Play Mode **9 項通過**，包含四向畫面投影、斜向速度、角色轉向獨立、放開停止、世界座標相容及結束回合停止。結果為 `Logs/input-results.txt` 的 `INPUT_PLAYMODE_PASS`；沒有新編譯／執行例外。驗證後回到 Edit Mode，未儲存任何 runtime 場景變更。
- 重現：選單「偷偷搬家 / 驗證畫面方向控制」。測試共用實際鍵盤的方向轉換入口，未模擬實體鍵盤硬體，也未重跑完整遊戲流程或獨立 build。

## 偷偷搬家 P1 — 2026-09-30（Camera 與資訊呈現）

### 範圍與實作過程

只改善畫面資訊與 Camera，保留已有偷竊、屋主 AI、計分及三分鐘流程，不新增大型系統或套件。

1. 在既有主場景上更新呈現，不執行全場景重建；修改前原始碼及場景基準保留於 `Logs/PresentationBaseline`，供本輪比對。
2. `PrototypeCamera` 改為固定 Pitch 48°／Yaw 40°、正交尺寸 5.5 的局部視野。LateUpdate 使用 SmoothDamp 0.16 秒跟隨，玩家大致置中；不隨角色旋轉，重生大位移時直接對齊。
3. 暫時隱藏 Camera 與角色之間的 Layer 8 遮擋 Renderer，離開後恢復；保留碰撞體及 AI 視線阻擋。使用頭／腳兩個取樣點與 0.18 秒恢復延遲，避免牆邊頻繁切換。
4. 關閉所有世界 TextMesh Renderer，但保留標籤資料；新增 Scene View 專用 Gizmos 顯示房間、物品價值、出生點、巡邏點與視角輔助。屋主視野扇形及額外 HUD 各有 Inspector 開關，正常 Game View 預設關閉。
5. `HeistRound` 呈現只保留總價值、倒數及單一情境提示。共用原本最近可拿物件判斷，顯示 `[F] 偷走／電視／$1,500`，離開立即清除。攜物靠近交貨區才顯示「放置／走入交貨區」，自動交貨規則不變。
6. 交貨區改為低彩度綠色地塊與淡色框線，移除巨大常駐文字。新增 `PresentationSetup` 可重複套用設定，並讓日後 `PrototypeBuilder` 重建也採用新版呈現。
7. 檢查實際差異：`ThiefController` 與本輪修改前 SHA256 完全相同；場景變更限於鏡頭、標籤 Renderer、Debug 預設值與無碰撞的交貨框線。既有地圖、物品配置、路線與遊戲流程未重做。

### 驗證與證據

- 實際 Unity `6000.5.0b9` 匯入／C# 編譯完成，這輪沒有編譯錯誤；場景套用回報 `PRESENTATION_SETUP_PASS`。
- 新增定向 Play Mode 驗證，**22 項全部通過**：`PRESENTATION_PLAYMODE_PASS`，結果在 `Logs/presentation-results.txt`，執行日誌在 `Logs/Editor.log`。
- 包含固定斜角、跟隨與置中、遠端臥室不在畫面、無常駐標籤、電視單一提示／離開清除、原本拿取及交貨 +1500、情境交貨提示、遮擋牆隱藏／恢復、隱藏牆仍阻擋屋主視線、重生對齊、Camera 停用清理及結算價值保留。
- 實際檢視四張包含 UI 的 Game View 截圖：`Logs/Presentation-20260930-041950/01-outdoors.png`、`02-near-tv.png`、`03-delivery-hint.png`、`04-wall-cutaway.png`。確認斜視高度與房間遮擋、提示中文顯示、交貨框線，以及沒有世界 Debug 文字。
- 驗證界線：本輪使用自動移動／互動入口，暫停屋主自主巡邏以隔離畫面測試；沒有重跑 9/29 的完整 26 項流程驗收，也沒有產生獨立 Player build。實際 WASD 鏡頭跟隨手感仍待使用者試玩。

### 使用方式與已知限制

主場景按 Play 即採用新呈現，不需再套用選單。WASD／F 與交貨規則不變。Debug 選單「偷偷搬家 / Debug / Scene View 輔助標示」僅影響 Scene View；Inspector 的 `Show Vision Debug`、`Show Debug Details` 可選擇開啟 Game View 診斷。遮擋處理採直接隱藏／恢復，未做材質淡出或正式美術；大型手持物件仍沿用原本搬運姿勢。

## 偷偷搬家 P1 — 2026-09-29（改題後第一階段）

### 目標與範圍

題材改為《偷偷搬家》。本階段只有一名可控制小偷及一名 AI 屋主，驗收重點是「從屋外進客廳拿電視 → 避開屋主 → 搬到屋外得分」。不沿用舊版跑蹲、手把、三格物品欄；不加入多人、正式美術、動畫、音效、存檔等完整系統。

### 實作過程與結果

1. 確認修改前 Git 工作區乾淨，基準為 `98ae6ba`。直接刪除被執行環境保護規則拒絕後，將舊 `Assets/_Project` 及 `Assets/xhs.unity` 移到 `Archive/OldPrototype-2026-09-29`，讓 Unity 不再載入；保留版本歷史及舊工作紀錄。
2. 建立六房住宅、中央走廊、前後門、兩側窗片與庭院，以 Primitive 製作無屋頂的 Blockout。房間為客廳、廚房、兩臥室、浴室、書房，放置牆及家具供躲避。
3. 新增 WASD 小偷、F 拿放、一次搬一件。空手／小件速度 4.5，大型物品 2.6；加入靠近距離與牆壁遮擋檢查。
4. 新增 10 件贓物，總價值 4650；屋外綠區自動交貨、物品消失並計分，禁止重複計價。
5. 建立屋主 22 路點循環日常行為，開場在廚房料理 8 秒。修正巡邏參照的序列化，以及廚房家具與出生點太近的碰撞風險。
6. 屋主使用 80 度、6.5 公尺、眼高 1 公尺的錐形視野，遮擋使用 Layer 8；黃色地面提示依同一眼高射線截斷。被看到即掉落手持物並回戶外，保留已交貨分數，重生保護 2 秒。
7. 三分鐘倒數與簡易中文 IMGUI；時間到顯示當局總價值，停止移動、撿放、抓捕和計分。
8. 補上可重建場景工具、Play Mode 自動驗收、操作 README 及 `Docs/PROTOTYPE_CONTEXT.md`。相機渲染檢查後縮小過大的地圖標籤。

### 環境與設定

- 依專案版本使用 `D:\unity\6000.5.0b9\Editor\Unity.exe`；未降版、未新增套件。
- C 槽的 Unity 6.4 缺少 PackageManager，首次啟動失敗；D 槽編輯器最初授權失效，使用者重新登入 Unity Hub 後恢復。
- 使用 Built-in Render Pipeline、Legacy Input。
- `EditorBuildSettings.asset` 改為唯一場景 `Assets/SneakyMove/Scenes/SneakyMove.unity`。
- `TagManager.asset` 定義 SightObstacle／Thief／Loot／Homeowner。
- `ProjectSettings.asset` 的產品名稱改為《偷偷搬家》；Unity 同時自動序列化預設 iPhone icon 欄位，沒有設定行動平台內容。

### 已執行驗證

- 實際 Unity C# 編譯與場景生成：`SNEAKY_SETUP_PASS`，10 件物品、22 路點。
- 場景參照：沒有 missing scripts，Player／Owner／Loot／Route 連結完整，恰一個 AudioListener。
- 自動 Play Mode：**26 項通過**，`SNEAKY_PLAYMODE_PASS`。
- 核心驗收：經 CharacterController 真實碰撞，由屋外走進客廳拿電視；屋主保持運作，未被抓，成功交貨 **+1500**。
- 驗證大型減速、不可重複計分、屋主走完一整圈、視角／距離／牆壁遮擋、門口視線、無法穿牆及隔牆撿物、後門通行、小件速度、放下再撿、AI 實際抓捕、掉落與戶外重生、結算及時間到凍結。
- 可重現證據：`Logs/sneaky-setup.log`、`Logs/sneaky-smoke.log`、`Logs/sneaky-playmode-results.txt`；自動相機圖在 `Logs/sneaky-world.png`。
- 標籤縮小後重新產生場景並通過編譯；在實際 Unity Game 視窗進 Play，確認中文操作提示、倒數、分數、屋主視野正常顯示，結束後停回 Edit Mode，主場景保持開啟。自動相機圖是標籤縮小前的測試證據。
- 驗證界線：自動流程使用與 WASD/F 相同的 runtime 移動／互動方法；未產生獨立 Windows／手機 build，未測手把或多人（不在本階段範圍）。

### 遊玩與後續

主場景按 Play，WASD 移動、F 拿放；先從正門進左下客廳拿電視，再搬回左下綠區。窗口目前是不可翻越的 Blockout 窗片。後續依實際遊玩回饋調整房間可讀性、巡邏空檔及難度；每次有意義的修改在此新增紀錄。

## v0.1 — 2026-07-20

### 本次目標

- 建立 Unity 3D 專案起點。
- 匯入使用者提供的 `human-body-base-mesh.zip` 角色模型。
- 建立一個可站立的平台與第三人稱測試場景。
- 完成跑步、蹲下、撿放、物品檢視、三格物品欄與手把操控。

### 完成內容

- Unity 版本：`6000.0.32f1`。
- 角色素材：`Assets/_Project/Models/HumanBody/source/body.fbx`。
- 主場景：`Assets/_Project/Scenes/Main.unity`。
- 角色移動：WASD／左搖桿，Shift／手把左搖桿按下跑步。
- 蹲下：Ctrl／手把右搖桿按下。
- 撿放：靠近物品後按 F／手把 A（Cross）。
- 檢視：滑鼠左鍵點擊場景物件，顯示名稱與說明。
- 物品欄：三格，數字鍵 1／2／3 選擇；手把 LB／RB 切換。
- 視角：按住滑鼠右鍵拖曳；手把右搖桿。
- 場景內放置三個彩色測試物品。

### 驗證紀錄

- Unity 批次匯入與 C# 編譯成功，程序回傳碼為 0。
- 自動建置工具輸出 `V0.1_SETUP_PASS`。
- `Main.unity` 已成功產生並加入 Build Settings。
- `body.fbx` 已由 Unity 成功匯入並實例化到 Player 下。
- 已確認 Legacy Input 啟用，且右搖桿的 `Gamepad Look X/Y` 軸已寫入 Input Manager。
- 驗證界線：本次完成靜態匯入、編譯與場景生成；尚未代替使用者進行實體手把與操作手感測試。

### 已知限制／下一步候選

- 原始 FBX 為基礎人體網格，目前未提供動畫，因此 v0.1 先完成控制與遊戲流程，角色不播放走跑蹲動畫。
- 不同品牌手把的軸編號可能不同；v0.1 以 Windows 常見 Xbox 配置為基準。
- 後續可加入正式 UI、美術材質、動畫控制器、互動提示與存檔。
