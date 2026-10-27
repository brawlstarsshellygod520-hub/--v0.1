# 炎上消防隊 v0.2.1：開發上下文

更新日期：2026-10-28。專案：`D:\Documents\畢專`。

## v0.2.1 最新：文字重開、透明外牆、E統一互動與較長水流

本節取代下方v0.2的按鈕／F拔管／實心庭院牆／水初速7設定。使用者自行關閉玩家版；本輪仍不做螢幕控制，背景Unity與獨立Windows驗證後發新Release。

- ReviewMissionHud失敗只顯示小文字「失敗」「按 R 即可重來」，無GUI.Button。R經LocalPlayerInput.TryRestartRound共用重載命令；限時、死亡與完成規則不改。
- LocalPlayerInput只讀E，不再讀F：持物／安全候選物品優先拿放與交付，其餘已接則拔管、未接則找同一個最近可達來源接管並裝備噴槍。拿起物品自動收工具並斷管，避免雙手持物卻被線綁住；同一次E不能又拿又放或同時噴水。右鍵原分階段接管／噴射保留。
- 三面庭院牆只關閉Renderer，Collider與SprayWallOcclusion依然有效，不改UserProvided戶型與NPC導航條件。
- ReviewWater最高初速10、MinimumSpeedFraction=.14、MainStreamSpeedFraction=.7，保留近中程1.4/2.8/4.2/5.6m/s，只有最遠組7→10m/s；60包/s，可視倍率1.65，流量／滅火倍率不變。新MainStreamSpeedFraction缺省0回退1，舊場景保持原分布；粉末不改。管長仍10m，不是把管子加長。
- 安全失敗證據保留於Docs/Validation/2026-10-28-v021/attempt-*.txt。驗證入口ReviewFireLoopV2Validation.BatchRun；舊噴射回歸用DirectionalSprayValidation.BatchRunPatch021，兩者寫本輪資料夾，不覆蓋v0.2歷史。
- GitHub同一儲存庫ID1306325166已依使用者要求改名Enjo-Fire-Brigade（https://github.com/xin-hw/Enjo-Fire-Brigade），origin同步，不建立另一庫、不搬本機專案資料夾。歷史tag／下載保留，新增v0.2.1-prototype。發布完成以PUBLIC_RELEASE.md及真Release為準。

## v0.2 最新：五秒延燒／失敗重開／提示框／火苗減量

本節取代下方歷史的2秒首波及不發布要求。使用者已要求修復後發布GitHub v0.2，並新增操作說明、完整更新摘要、模型署名及第一關玩法。仍不做螢幕控制。

- ReleasePolishSetup.BatchApply只對目前場景設定fastSeconds=5、spaciousFire=true，核對無舊AI美術依賴，保留授權圍牆及歷史來源。角色／戶型／四道具／斧／噴槍沿用團隊提供模型。
- ReviewMissionHud失敗時顯示可點的重新開始，與R共用FirstReviewMission.RestartRound。RoundedContextFrame統一9-slice，修正IMGUI由上而下與UV由下而上的方向；框線前後對照已生成。
- FirstRescueEffects僅呈現層減少火苗到交錯三分之一並縮小，孤立燃燒格仍畫火苗，每個活火格都有暖色地面標記。沒有改FireField、碰火死亡或滅火規則。地面標記需在地板頂面上，不能藏在地板內。
- 本關仍10m、60秒交正常布丁、水清路／鍋熱復燃／粉末滅鍋／三刀門／住戶實際正門逃生。整關驗證與Windows證據改寫到Docs/Validation/2026-10-28-restart-release，不覆蓋歷史72項證據。
- 公開操作與更新總覽在README；發布完成狀態以Docs/PUBLIC_RELEASE.md及GitHub實際Release為準。Hidden玩家黑截圖不當作UI像素驗收，人工手感仍待使用者試玩。

## 最新：兩秒封路／鍋熱復燃／10m／情境提示（2026-10-28）

**本節取代下方v2的「局部慢燒／15m／無情境提示」。** FirstReview1027仍為唯一玩家入口；最新版 `Builds/Latest/FireRescue.exe` 已重建。維持不做螢幕控制、不發布GitHub。

- 廚房首波依相連格距離由鍋邊擴張，**2秒內179格走道全火**，後續慢延燒。水可滅任何普通地面格；鍋仍燒時，1.55m內冷卻格約.28秒再被熱源點燃，不是水免疫。粉末能滅普通火／油鍋／鍋旁地面火，滅鍋後停止熱源復燃；粉倍率.65、普通地面抑制.325/unit，水.45/unit，粉殘留20秒。
- 原粉末命中鍋直接return造成鄰火漏算，新ElementReaction opt-in粉雲表面覆蓋有水平／垂直LOS，不穿牆。原NPC正門2.7mm門檻被導航capsule誤擋，GetCapsule只修底部25mm查詢容差；47格正門路線取代173格後院路，實走約10.39m。
- 玩家管長**10m**（使用者要求縮短，已驗可完成），只當前Scene。使用者同意加庭院西／北／東三面Primitive牆封捷徑：group `Prototype Courtyard Boundary`，AIGenerated/ReviewBoundaries材質；不改UserProvided戶型。全火時「有牆不可到倉庫、停用牆可繞到」對照PASS。
- 新SpraySettings.MinimumSpeedFraction：舊0回退.65以保留舊契約，ReviewWater .2／ImpactRadius .9，Soda .3／1.35；近端水滴／粉末可清腳前通路，仍是方向＋固定物理飛行，非鼠標距離選落點。
- PickupContextView讀Carry.NearbyItem顯示布丁金色外輪廓與圓角E框；拿起／離開關閉。管線極限顯示「管線不夠長了！按 F 解除綁定！」，退後／F立刻關閉。此為使用者最新要求，取代舊完全無提示，但不恢復整排操作／價格／警告。
- Pointer模式F固定斷線，即使靠近另一來源也不改接；右鍵才接來源。原低階TickCommand／窗口Interact契約不變。最新72項已含真正F命令與另一來源的負向測試，非只直接呼叫Disconnect。
- 最新72項整關＋40項舊噴射回歸、Windows build 0錯誤0警告、打包D3D11 25項、啟動6秒落地PASS。正常操作流程：接水清路→倉庫粉末→清前路／滅鍋→先拿布丁→帶門前放下→三刀破門→再拿起交付→NPC正門逃出，交付約剩18.617秒。正常路線未注入清火／傳送／保命；毒性與單獨水打鍋分支有隔離前提，詳RESULT。

最新設定入口 `KitchenIterationSetup.BatchApply`，驗證沿用 `ReviewFireLoopV2Validation.BatchRun` 但報告現在寫 `Docs/Validation/2026-10-28-kitchen-hints/`。`KitchenIterationValidation.BatchBefore`為隔離反應／正門對照探針，不保存場景。勿以舊59／85項或歷史15m數字當本輪證據。已看離線模型圖；OnGUI E／F框尚需使用者看字級／位置，不能以Hidden黑圖稱UI驗收。

## 最新玩法：火場循環v2（2026-10-28）

最新入口仍FirstReview1027／Builds/Latest/FireRescue.exe。**以下最新規則取代舊「所有地面展示火共用油鍋」**：地面FireField為獨立普通火，可用馬桶水清路；油鍋ObjectFire只能用粉末滅，遇水透過CookingOilIncident爆燃一次並燻黑近處布丁。焦黑CarryItem交NPC會顯示「好難吃...」、1.4秒後中毒死亡並失敗；主角踩地面火即死、掉物鎖輸入、R重開。未做通用生命值系統。

CookingOilArea在本Scene停用，OilPanFireView無area patches；FirstRescueEffects顯示真grid火，容量512。鍋旁0.2秒起第一格，前2秒spread .4、後.035；廚房走道可燃、牆／家具／戶外不燃。正常實測火格數4→13→22（約.35/1.65/6秒），水清路18→1再清完；不是開場整廚房。水無法滅油鍋是規則，鄰近普通地面火可滅。Water profile初速7/重力16，粉末6/8，固定仰角18°；方向仍只由滑鼠決定。

Player出生客廳(2.6,.1,-3.7)，戶外出口不動；只有倒數、NPC台詞、終局簡短結果，操作／互動／警告提示全移除。60秒仍限交付；新Scene requireAllFiresOut=false，清出安全路線＋滅鍋＋送正常布丁＋NPC實際逃出即可完成，不強迫清廚房每格殘火。舊關預設true不變。

PhysicalHoseTube固定128節點、內部不釘contact，僅兩端升起，連續物理節點不因牆角而新增／重排。HoseConnection仍唯一15m與拓樸權威，接觸折線是隱藏約束，不代表可見硬折點。最大節點位移.1845m/幀、段長.0996m，此為特定60Hz自動路線，不宣稱所有極端幀率／角落已驗。

最新59項Play Mode、Windows建置0錯誤0警告／打包載入22項、正常ReadDevices6秒落地PASS。已看離線渲染、不做螢幕控制；Hidden黑圖未當UI視覺證據。手感仍需使用者驗收。入口窄遷移ReviewFireLoopV2Setup.BatchApply、驗證ReviewFireLoopV2Validation.BatchRun，勿跑舊85項作為新版驗收（那是舊規則歷史）。完整證據 `Docs/Validation/2026-10-28-fire-loop-v2/RESULT.md`，操作說明 `Docs/FIRST_REVIEW_1027.md`。不推送／發布。

## 最新模型：四件正式道具（2026-10-28）

新同名1027 Blend已新增馬桶／粉袋／油鍋／盤裝布丁，原戶型逐頂點及三角面完全相同。新快照 `ArtSource/UserProvided/Level1027/PropsUpdate20261028/Source.blend` SHA256 `7f7c3bb7170a0787c79d2bc4e9a1c29425a1523a45db8c187e1524c295229504`，不覆寫舊Source。当前 FirstReview1027 四件 Visual 全為使用者幾何，基本色及4份純展示Prefab在 `Art/UserProvided/Level1027/Props20261028`；舊AIGenerated資產保留但不使用。立方體.002是已擺在倉庫的粉袋，兩個離屋巨型立方體排除。水／油面原朝下，僅派生Mesh修向，保留原Blend。

更新擺位：馬桶root(-.65907,.12636,3.88725)、粉袋root(-5.60250,.12647,5.35217)、油面火點(-5.79357,1.31121,-3.46286)、盤裝布丁中心(-5.83156,1.02176,-4.08973)。接管口／互動點／碰撞同步，核心Runtime未改；歷史Setup靜態常數是初版佔位配方，不再當前場景的定位依據。只用窄遷移 `ReviewUserPropsImport.BatchApply`，勿重跑Install覆寫整關。

85項整關Play Mode、新Windows建置0錯誤0警告、打包exe D3D11 19項PASS；已看離線渲染，沒有螢幕控制／真人手感驗收。證據 `Docs/Validation/2026-10-28-user-props/RESULT.md`。目前應請使用者驗收，不擴張新系統。下方「臨時四模型」為歷史紀錄。

## 最新小修：布丁心聲標籤／啟動落地（2026-10-28）

NPC預設改「好想吃布丁...」，How to Fish參考的白色細框／淡色透明底／布丁黃字，1.65m頭上投影、不再有「居民」標題。使用者曾回報啟動穿地，原Editor與實際Windows正常ReadDevices路徑6秒均未重現、地板Collider及layers正常；已補長幀與門口來回回歸。**使用者後來說「已經沒這問題」**，因此未改地板、重力或加Rigidbody，不能宣稱確認並修復掉落根因。新的啟動診斷只在--verify-startup啟用，不影響正常遊戲。細節證據 `Docs/Validation/2026-10-28-grounding-tag/RESULT.md`。維持不做螢幕控制。

## 目前入口：一審布丁救援 FirstReview1027（2026-10-28）

- **最新使用者要求先不做螢幕控制。** 曾實看Windows UI及R，Escape中止後已停止所有鍵鼠。後來使用者已自行關閉玩家；僅可背景編譯／Play Mode／建置，不擅自開互動視窗或完成MCP UI設定。
- 新 `第一關1027.blend` 備份／衍生幾何在 `ArtSource/UserProvided/Level1027`／`Art/UserProvided/Level1027`，SHA256 `30a764941f7df43e12341048c539cead3ffaece8a82d1882ec6f7226342ecdff`。原始來源未改。新Scene `Assets/FireRescue/Scenes/FirstReview1027.unity` 是唯一enabled Build Scene，舊圖不刪。正常交付仍 `PlayLatest.cmd`→`Builds/Latest/FireRescue.exe`，不要改叫玩家開歷史資料夾。
- 新任務取代「馬桶水直接救人」：水使CookingOil變大、蘇打粉正確滅火、三刀破門、E拿／交布丁。**使用者已確認此關15m，非5m。** 60秒從出生開始、只限交付；成功後凍結，油火熄滅／門破／安全路線後NPC才自走出口。要求是可選NpcItemRequest，不恢復安撫、不改舊NPC預設。
- 最新追加：開局2秒CookingOilArea擴及布丁、熱區禁止拿取；同一連通油火共享ObjectFire強度，非獨立每格燃燒、無玩家傷害。霧面MattePlaster純Diffuse shader、NPC頭上OnGUI投影NameTag、移除左上要求區。臨時模型只有馬桶／蘇打粉／鍋／布丁，檯面沿用用戶住宅，不放生成家具。
- 新Scene管線用PhysicalHoseTube連續3D管身＋Verlet/PBD顯示節點，舊HoseView停用。HoseConnection仍唯一15m／碰撞繞角權威；視覺碰牆不再整條隱藏。不是完整PhysX軟體。新玩家距離式程序步態／雙手姿勢，原Animator在該玩家停用；NPC原Idle/Walk保留。新四份Skin屬UserProvided加工，不是AI原創，原拓撲不改。
- 早期流程63→70→73項後持續補驗；最新整關81、舊管線22／繞牆29／門21。本輪Windows建置／smoke的最終狀態以 `Docs/Validation/2026-10-28-first-review/RESULT.md` 為準。背景黑圖不算UI視覺PASS。還需玩家手感、NameTag位置、連續轉身與替換正式模型後再驗。
- **Latest已更新完畢**：Windows x64 Development／Mono建置0錯誤0警告；真正打包exe D3D11載入14項PASS（含產品名、Matte shader、3D管身元件、NameTag、2秒熱區）。`PlayLatest.cmd`可直接驗收。背景擷取黑圖已明示限制，未再操作視窗；結束時無Unity／玩家程序。
- 詳細操作、替換點、限制：`Docs/FIRST_REVIEW_1027.md`。下方1026／5m／F門口固定／無布丁為歷史，不是最新玩法。MCP套件曾裝但未Bridge連線，這次沒有繼續裝其他MCP或發布Github。

## 最新接續：自動繞牆尚未整合（2026-10-27）

使用者更改要求：管線要實體感自動繞牆，不再遇牆卡住。Runtime接觸折線與29項獨立Play Mode已PASS，但**目前FirstRescue1026仍未啟用、完整救援未重測、Windows Latest未重建，fixture離線圖全白待修驗證**。不能把獨立測試稱交付。Unity MCP單一CoplayDev10.3.0與uv0.13.6已安裝／匯入，但服務／Bridge尚未連線；使用者已允許螢幕控制並確認安裝。剩餘額度先收尾，下一輪從 `Docs/HOSE_WRAPPING_HANDOFF.md` 繼續，裡面有精確操作與未完成事項。下方38／40項與玩家版PASS屬前一輪，不能充作繞牆新版證據。

## 目前入口：第一人馬桶水救援 1026（2026-10-27）

- 最新使用者要求改為新 `第一關1026 (1).blend`，只聚焦馬桶水救第一人；其他生成家具排除於新場景，舊資產／場景不刪。使用者人物、戶型、消防斧／噴槍保留；不做螢幕控制。
- 唯一啟用 Build Scene 現為 `Assets/FireRescue/Scenes/FirstRescue1026.unity`，`PlayLatest.cmd` 仍固定開 `Builds/Latest/FireRescue.exe`。下方 TutorialGreybox 為歷史狀態，不是目前開機場景。
- 新圖 40 mesh，10倍比例，方位已對照圖修正。來源 Blend 不變；Boolean 貼齊牆背造成浴室門阻擋，只修匯出記憶體 cutter 深度，五門射線可穿／側牆仍擋，UserProvided/Level1026 保存原檔與衍生幾何。
- 新水 Profile opt-in PhysicalBallistics，初速／重力／阻力與掃掠碰撞，停噴保留飛行；唯讀 VFX 比較2D粒子／Mesh／Hybrid，預設Hybrid，火是共用低模火苗。舊曲線液體契約保留。只有一個馬桶來源，5m限制不放寬，浴室門口F明確固定導管，不自動繞牆。
- NPC淨空根因已修：controller大小的capsule中心／edge／實際移動一致，受阻重判；新網格對齊門洞。新片段門預設開、不做安撫；臥室門口火清出路後NPC自走經正門逃出。
- 完整命令Play Mode與重開38項、共享噴射回歸40項PASS；本機Windows build 0 errors/0 warnings，實際D3D11玩家版載入smoke PASS。真人手感未驗、沒有螢幕控制、沒有發布GitHub。不要將玩家版載入smoke稱為玩家版手動通關。
- 詳細路徑／限制／操作 `Docs/FIRST_RESCUE_1026.md`；證據 `Docs/Validation/2026-10-27-first-rescue-1026/RESULT.md`。下一步請使用者驗收噴射／導管手感，再接其餘橘色火點與後續階段。
- 下方分類統計是當次快照；後來NPC材料調亮及本輪新增場景不包含在624不變／15修改的歷史數字內。

## 最新：Windows交付入口修復與模型來源分類（2026-10-27）

- 使用者回報上一輪三項在Windows玩家版尚未完成。重核不是改回舊Scene：當前TutorialGreybox原本已單椅朝內／Scene配色／5m，但BuildSettings仍唯一啟用舊ResidentialFire、Player基底Prefab仍13m、Role1基底Prefab仍白模。使用者未提供當時exe所在資料夾，不能確定是哪個歷史包；這些入口未同步已實際修復。
- 現在預設BuildSettings唯一啟用TutorialGreybox（舊Scene保留但disabled）；Player Prefab5m、Role1 Prefab保存6組palette，Scene已有正確值保留。根目錄`PlayLatest.cmd`固定啟動`Builds/Latest/FireRescue.exe`，之後勿再交付多個不同Latest路徑。
- Assets/FireRescue/Art分UserProvided與AIGenerated：前者Characters/{Character1,Character2,Rigged}、Tools/{FireAxe,Nozzle}、LevelOne；後者Models/Prefabs/Materials/Animations/Household。Shader與玩法Prefab不混入模型分類。ArtSource同樣分UserProvided、AIGenerated，Tools共用；加工骨架／上色不等於AI原創。
- 163舊GUID全保存、21 FBX/Blend雜湊完全一致；GUID-aware基線639檔624不變／15有意修改／0遺失。已修live生成路徑與computed Rigged父目錄；角色來源改專案內Source.fbx不靠Downloads。歷史metadata不改舊路徑以保留來源證據，現行表`Docs/ASSET_CLASSIFICATION.md`。
- 背景17針對性＋43整關重開＝60項PASS；引用掃描Missing Script/Mesh/Bone=0，Windows x64 Development建置0錯誤／0警告。**本次直接執行打包後Windows exe，背景D3D11確認新Scene、Hose5m、6配色、骨架與單椅朝內PASS**，不是只有Editor。Null graphics device曾造成顏色讀取假陰性，已留失敗與明確guard，不用-nographics驗色。無螢幕／鍵鼠控制、未做真人手感驗收，未發布GitHub。
- 證據`Docs/Validation/2026-10-27-asset-classification/RESULT.md`。下一步仍是NPC導航淨空與實體碰撞一致；這輪未改此已知問題。

## 最新：單椅對調／居民參考配色／5m管長（2026-10-27）

- 茶几左右單椅交換位置（保留旋轉、重命名為空間左右），現在椅面朝內；碰撞佔地不變。Household生成配方也改正朝向。待救NPC仍為角色1，10網格依部件用橘髮／淡橘衣／淡黃飾條／膚色／黑眼基本材質；只Scene材質override，未換回角色2、不新增帽子或動骨架／動畫／FBX。
- **當前TutorialGreybox管長已由3改5m**，僅場景HoseConnection.maximumLength override；Runtime預設／歷史Prefab仍3m，牆阻與窗口公式不改。新驗證入口ResidentialIntegrationValidation.BatchRunResident5m指定預期5m；舊3m入口為歷史，不可當本輪驗收。
- 保存重讀／冪等PASS，14針對檢查＋43整關重開＝57項PASS；含真Motor可越3m並停5m、窗口總預算5m、倒退解除。實際Unity離線圖已看，沒有螢幕控制。新本機Builds/ResidentColor5m/FireRescue.exe，建置結果見當輪build.txt；舊HouseholdGreybox玩家程序未擅自關閉或覆蓋，未發布。
- 使用者補充品質要求：先不做螢幕控制。下一個最優先是NPC導航淨空與實體碰撞一致（HasSafePath卻卡桌角的根因尚未修）；不能把擺位避開當演算法修好。三道品質門與建議驗收`Docs/QUALITY_GATES.md`，本輪證據`Docs/Validation/2026-10-27-resident-color-5m/RESULT.md`。先回報與確認方向，不暗中擴張新AI或新系統。

## 最新：住宅家具Greybox與來源物件外輪廓（2026-10-27）

- 依使用者室內圖補25種Primitive家具Prefab、六區場景群組；床／桌椅／衣櫃／洗衣機／淋浴／冰箱／L型流理臺／灶爐／沙發與電視櫃／垃圾桶／鞋櫃／汽水攤等。Visual與Root碰撞獨立，既有茶几保留Rigidbody，舊色塊停用保留。
- SourceHighlightView改讀同一NearbySource，金色未接／藍色已接，物件Visual輪廓world-space外擴3.5cm，無地面圈；新InteractionOutlineTarget＋Built-in shader。只展示副本，不改碰撞、來源、材質或接點；可動態Refresh模型快取，FBX Read/Write=false安全fallback；Scene明確引用shader防build stripping。
- 此輪只擺物件，尚未按示意圖搬動可操作來源／油鍋／火點／NPC或重建戶型；既有活躍烹飪測試檯仍在客廳後側。新水槽／蘇打粉罐／汽水攤是外觀，其他窗框無新導管規則，不要誤稱整張圖玩法配置已完成。
- 首次整關NPC卡在新沙發前緣：格子判可走但實體半徑重疊。僅沙發後移.35m與右單椅側移.2m修復；不改尋路。45家具外框＋43整關重開＝88項PASS，實際GPU圖已看，建置狀態見當輪build.txt。本機版`Builds/HouseholdGreybox/FireRescue.exe`，未發布／未做本輪真人驗收。
- 架構／替換方式`Docs/HOUSEHOLD_GREYBOX.md`，證據`Docs/Validation/2026-10-27-household-outline/RESULT.md`。下一步請使用者確認擺設／外框，再決定來源／火點是否移到新圖位置，不自行擴張元素種類或多人。

## 最新：交換雙角色、Generic骨架與移動朝向（2026-10-27）

- 當前TutorialGreybox玩家換角色2長髮基本色（1.6m、18骨），NPC換角色1貓耳白模（1.5m、20骨含尾巴）。新派生資產RoleSwapRig；舊Visual停用保留、原FBX及所有舊資產不覆寫。兩者Idle／Walk、無Root Motion；角色1側髮302頂點與眼眉全Head。
- 新`faceMovementWhenIdle`只當前Scene開啟、只Pointer套用。平常移動朝WASD方向／停住保留朝向，真正揮斧／噴射才朝滑鼠；裝備或接管及其長按不誤判。舊TickCommand不受影響。槍口／入管玩法Anchor不掛骨架，碰撞、3m、窗口、相機及NPC規則不變。
- 真Unity保存重讀／冪等PASS；新角色朝向44＋整關重開43＝87項PASS，實際Unity渲染已看。新本機版`Builds/CharacterSwap/FireRescue.exe`；建置結果見當輪build.txt。不宣稱實體鍵鼠手感／多人已驗收；未發布。
- 下一步請玩家驗收兩角色比例、四向移動、裝備不瞄準、動作才瞄準、Idle／Walk。全身持工具姿勢／IK尚未做。交付需求`Docs/LEVEL_ASSET_REQUIREMENTS.md`，實作`Docs/CHARACTER_SWAP_ANIMATION.md`，證據`Docs/Validation/2026-10-27-character-swap-animation/`。

公開交付已完成：沿用 `brawlstarsshellygod520-hub/--v0.1`，程式提交 `b0a45fb`、Release `v0.1.0-prototype`，提供Windows ZIP與公開操作README。發布資訊見 `Docs/PUBLIC_RELEASE.md`。這不改變實體鍵鼠驗收尚未完成的狀態。

## 最新：消防管牆阻與液體流量修正（2026-10-27）

- 本輪暫採遇牆卡管、不自動繞角，3m不放寬。HoseConnection以既有SprayWallOcclusion檢查Source.Port→可選窗口→Inlet，Move被約束時Motor移除外向動量；UsedLength統一XZ。Source停用／銷毀不殘留幽靈連線；View曲線若碰牆改拉直、切來源重建，View關閉不改玩法。
- 原窗口外框接點造成管線從水箱斜穿孔洞旁1.34m牆面。Route僅向室內移0.4m，世界(-5.2,1.15,3.2)，框與孔洞不變；實際3m整關含固定／解除再通過，測試折線2.37m。Scene12新增行只兩引用及窗點override。
- SprayEmitter按FlowRate*dt分配Amount，短按仍立即、不免費多一整份；取消.25截量、Stop/換來源補一次舊快照尾量。發射有界256，極端滿額DroppedAmount記未發量不重播；Reaction/FireField/Profile數值與元素資格未改。不是全場真實公升質量守恆。
- 真Unity：22+11+24+40+43＝140項PASS；Window首次失敗／前後probe均保留；Windows建置0錯誤／0警告，實際遊戲Camera圖已看，未做本輪真人長按／效能／多人。新本機版 `Builds/HoseFluidPhysics/FireRescue.exe`，未發布。白模／正向較遠相機保留。
- 文件與限制 `Docs/HOSE_FLUID_PHYSICS.md`；證據 `Docs/Validation/2026-10-27-hose-fluid-physics/RESULT.md`。下一步請玩家測卡管與噴射手感；自動繞牆、軟管節點及家具完整碰撞尚未做，避免直接宣稱完整真實物理。

## 前一輪：主角角色1白模＋正向較遠相機（2026-10-27）

- 使用者要求先換其中一個主角，之後給上色檔。已選角色1（貓耳／尾巴），原始FBX10Mesh／1425v完整保留，新增靜態展示Prefab放目前Player的 `Visual/User Player Model`；Body／Helmet停用保留。沒有新上色、rig或主角肢體動畫；角色2既有NPC／配色／動作保留。
- 使用者已明確確認「對齊房屋的正向俯視」，不是90°正上方。現在RescueCamera Pitch50/Yaw0，正交Size6（原4.5），仍.18s跟隨、不隨角色旋轉。viewAngles欄位預設50/40供舊場景相容，只當前Scene改50/0；W沿+Z、D沿+X仍螢幕相對。
- 17項新角色／相機＋43項整關／重開＝60項PASS；Windows x64建置0錯誤／0警告；已看離線遊戲圖並實際啟動玩家版確認畫面，未宣稱長按鍵鼠／手感已驗收。
- 最新本機版 `Builds/PlayerWhitebox/FireRescue.exe`。規格／後續上色替換：`Docs/PLAYER_WHITEBOX_CAMERA.md`；證據 `Docs/Validation/2026-10-27-player-whitebox-camera/RESULT.md`。未發布，舊UserCharacters版保留。下一步請玩家確認這個距離與白模比例，未授權擴張多人／教學或重造型。

## 前一輪驗證：玩家版畫面控制（2026-10-27）

- 使用者已允許畫面控制；不再沿用「禁止操作畫面」限制。但不擴張為發布／刪資料授權。
- 現有 `Builds/UserCharacters/FireRescue.exe` 實際啟動、左鍵拿斧／揮斧、右鍵切噴槍、未接來源不噴等可見檢查通過，Player.log錯誤0，正常關閉。程式／資產／數值沒改，沒有重建。
- 本輪畫面API只能短按／點擊、沒有持續held介面，不能靠它完成可靠WASD與長按噴射；沒有為了測試改遊戲規則。整關真人輸入／NPC動態／手感驗收仍未完成；不可把先前58項自動測試當成已完成。證據 `Docs/Validation/2026-10-27-player-ui-smoke/RESULT.md`。

## 最新功能：使用者居民白模＋基本色／動畫（2026-10-26實作，10-27整理）

- 使用者 `角色2.fbx` 僅含一位長髮居民，已確認先用此份。只替換當前 TutorialGreybox NPC：`Visual/User Resident 2`；消防員未換。原六件網格與1127頂點不重建，七種基本色、18骨 Generic、Idle／Run；舊 Capsule 停用保留。規格與資產路徑見 `Docs/USER_CHARACTERS.md`。
- 新外觀沿用 CharacterLocomotionAnimation，Root Motion 關閉；碰撞、NPC 尋路、工具接點及其他 Runtime 完全不改。Scene diff 僅外觀 override；466 基線檔僅 Scene＋整關報告入口兩檔變動。
- 真 Unity 保存重讀／冪等 PASS、角色15＋整關重開43＝58項 PASS。Windows x64 Development 建置0錯誤／0警告：`Builds/UserCharacters/FireRescue.exe`；實際 Unity 離線圖已檢視。沒有 UI 操控、沒有啟動玩家視窗、未發布。
- **現在應先請使用者實際驗收**：新居民外觀／動作、裝備接管操作、方向噴射的距離／範圍／空中與落地停留，再調參；不要在無回饋下直接做教學或多人。清單 `Docs/PLAYER_ACCEPTANCE_2026-10-26.md`，完整證據 `Docs/Validation/2026-10-26-user-characters/RESULT.md`。

## 前一輪：噴射效果獨立調校（2026-10-26，本機尚未發布）

- SprayProfile.Effects 分離 ExtinguishMultiplier／PushMultiplier／ResidueMultiplier，三個資產全為 1。預設與既有玩法相同，沒有再平衡；已噴出的樣本保存發射時設定。水推力、油殘留與乾粉滅油鍋資格保留。
- 真 Unity 背景 Play Mode 24＋43＝67 項 PASS；資產保存重讀通過；Windows x64 建置 0 錯誤／0 警告：`Builds/SprayEffectTuning/FireRescue.exe`。本輪未操作畫面／啟動玩家視窗、未做手感驗收、未發布。文件與證據見 `Docs/DIRECTIONAL_SPRAY.md`、`Docs/Validation/2026-10-26-spray-effect-tuning`。

## 前一輪：方向噴射與殘留（2026-10-26，本機尚未發布）

- TutorialGreybox 的滑鼠僅控制水平朝向，不再點哪裡就直接命中哪裡。水／油／乾粉從 Nozzle 沿各自設定飛行，撞到後反應；鬆手或斷管後空中內容繼續。`Assets/FireRescue/Settings/Sprays` 內三個 Profile 為調整入口；射程、AirLifetime、GroundLifetime、SpreadAngle、ImpactRadius 獨立，不增加長按時間上限。
- 新 `SprayParticleView` 僅呈現飛行樣本；FireField 管理濕度／油／粉末的剩餘時間。API、數值表、單格刷新規則及歷史 API 不可混用限制見 `Docs/DIRECTIONAL_SPRAY.md`。不是流體模擬，地面仍為格子視覺。
- `SprayWallOcclusion` 明確綁住宅碰撞幾何，防止飛越剖面牆／槍口穿牆；`HoseWindow` 有獨立可調開孔尺寸，窗口深度 1m 覆蓋外框到內牆的偏移。換 Visual 不改判定；新增牆窗要補 Inspector 清單。原3m、接管／左右鍵、NPC／斧頭／統計不改。
- 最終自動 Play Mode 129 項 PASS（方向40、整關＋重開43、水反應25、門與統計21）；Windows x64 Development建置0錯誤／0警告。本機 `Builds/DirectionalSpray/FireRescue.exe`，GitHub／README／已發布下載版未更新。硬體鍵鼠手感、多人及效能尚未驗收。
- 新證據：`Docs/Validation/2026-10-26-directional-spray/RESULT.md`。下方的點瞄準／固定6.5m噴流描述是歷史，不適用當前 TutorialGreybox。

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
