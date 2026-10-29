# 畢業專題工作紀錄

> 後續每次修改請在此檔案新增一筆版本紀錄，不覆蓋舊紀錄。

## 炎上消防隊 v0.2.1 — 2026-10-28（R文字、透明外牆、E整合、水流與儲存庫改名）

- 失敗重開按鈕／大面板移除，只留小文字「按 R 即可重來」；R共用LocalPlayerInput.TryRestartRound。外圍三面防繞路牆只停Renderer，不刪Collider或改戶型。
- E優先拿放／交付，其餘狀況接管／拔管；拿起物品自動收工具並拔管，避免雙手持物被管線綁住。同次E禁止順帶觸發鼠標噴射。右鍵原接管／噴射保留，F不再讀取，極限提示改E。
- Water最高初速7→10m/s、60水包/s、可視大小1.65。前三次線性／指數速度調參造成近中程清路缺口，保留失敗紀錄。最終MainStreamSpeedFraction=.7、minimum=.14，保留1.4/2.8/4.2/5.6近中程，只將最遠組7→10。缺省值0回退1保留舊Profile契約；不增加流量、傷害，不改粉末／10m管長／NPC／5秒首波／60秒要求。
- 已完成93項整關（含E實際拿放交付、右鍵接管、R重置、透明牆與射程對照）；最遠空地落點3.179562→5.301241m，最近0.464092不變，可視水包18→26，相同總水量3.333333→3.333332。一次測試fixture漏接ElementReaction導致零水滴，修正測試接線，沒有放寬發射器安全門檻。相關attempt-*.txt均保留。
- 使用者要求停止追加驗證、直接給下載頁；停止進Play／玩家測試，只執行必要建置打包與上傳。v0.2.1沒有新Windows實際執行／解壓試玩證據，不沿用v0.2的28項當新版測試。
- 使用者追加要求儲存庫不應叫v0.1，原ID1306325166透過GitHub API改名Enjo-Fire-Brigade，origin更新為https://github.com/xin-hw/Enjo-Fire-Brigade.git。保留原歷史、tag、舊Release及本機專案路徑；無畫面控制。新版下載說明與署名在Docs/RELEASE_NOTES_v0.2.1.md，發布完成見PUBLIC_RELEASE.md。
- 發行完成：v0.2.1-prototype已公開，Release426080404／Asset683144195，ZIP65,761,438 bytes，來源6a54b86。93項整關、40項回歸與必要建置已完成；沒有追加Windows試玩。發行後追加紀錄日期2026-10-29，接續白地板／米牆與物理工作另行處理，不回寫已發行包。

## 炎上消防隊 v0.2 — 2026-10-28（重新開始、5秒首波、圓角修復與較疏火苗）

- 使用者要求失敗按鈕、首波2秒改5秒、修正提示框上下反向，完成後上傳同一GitHub專案。另要求v0.2操作／全部更新摘要／其澔與飛魚、風信子等模型署名／第一關玩法。確認只移除舊AI美術，保留管線、水火、臨時圍牆；後續補充火苗過密，按截圖方向調整。
- 小範圍修改：場景fastSeconds=5；ReviewMissionHud失敗按鈕與R共用場景重載；PickupContextView改用RoundedContextFrame。舊9-slice將GUI頂部採到Texture底部，實際重建出與使用者相同錯框，修正UV後圓角連續。
- 首版減半火苗仍擁擠；自行檢視Unity前後渲染，改交錯三分之一、降低高度／寬度。每個活火格保留暖色地面標記，修正標記位於地板下的高度；孤立格仍有火苗，避免不可見危險。僅改View，不改傷害／傳播／接水與粉末規則。
- Scene遞迴資產核對只引用授權庭院材質，沒有舊AI美術模型。不刪歷史Blend、FBX與GUID；不把團隊模型的程式綁骨／配色誤認成AI原創。
- 驗證與交付證據集中Docs/Validation/2026-10-28-restart-release；完整結果見RESULT.md。Windows人工按鈕點擊、初見難度與操作手感仍須使用者驗收；背景命令通關不能宣稱人工試玩。
- 最終79項整關PASS、Windows建置0錯誤0警告、Latest及ZIP重新解壓包各28項D3D11 PASS。提交f9307a6已推送原master；公開發布v0.2.0-prototype，Windows ZIP 65,760,394 bytes。未登入實際下載HTTP200、SHA256與本機／GitHub digest一致；v0.1保留。README已含完整操作、更新、其澔／飛魚／風信子等模型署名與第一關說明。發行收據見PUBLIC_RELEASE.md與release-receipt.json；本輪未操作使用者畫面。

## 炎上消防隊 v0.1 — 2026-10-28（兩秒封廚房、鍋熱復燃、10m與E／F情境提示）

- 本輪新增要求：粉末應可滅全部火種與鍋旁地面火、兩秒燒滿廚房封路、NPC應走正門、布丁金色外輪廓＋圓角E提示、滿管顯示F解除提示。使用者再補充：鍋旁不是水免疫，而是滅掉會被油鍋快速點燃；縮短管線，水只清往倉庫的路，粉末滅鍋後才能安全取布丁。使用者明確同意以簡單圍牆封屋外捷徑。
- 使用Unity問題調查／功能整合／建置驗證流程；不做螢幕控制，不新增套件、不改模型／相機、不推送GitHub。前狀態保存於 `Docs/Validation/2026-10-28-kitchen-hints/before`。
- **粉末根因**：ApplySpray命中ObjectFire後直接return，因此打鍋只減鍋強度、鄰格仍燒。新版opt-in PowderSurfaceSplash：命中鍋／固定表面也有粉雲覆蓋，採「水平擴散→往地面沉降」兩段LOS，牆仍阻擋，不把水改成穿櫃濺射。隔離原分支重現：dose4後鍋0、鄰格1；新分支鍋0、鄰格0。ReviewSoda滅火倍率.65，普通地面約.325/unit、水.45/unit，粉較慢；粉末殘留20秒抑制普通鄰火回燒。
- **兩秒封路**：CookingOilIncident按廚房相連可燃格的洪水距離安排首波火線，約.12秒開始、2秒抵達所有179個可燃走道格；不是大幅抬高全局spread或第一幀整片點燃。後續保留慢速鄰格延燒，水濕區仍可壓制。
- **鍋熱復燃**：鍋未熄時，1.55m內已滅地面格約.28秒會被持續熱源再點燃。測試明確驗立即水滅成功、稍後復燃，並驗遠處水路不受鍋熱強制點火；粉末滅掉鍋後停止這個新點火源。沒有設定「只有水永遠滅不了這一格」。水打鍋爆燃／黑布丁／NPC中毒與踩火即死保留。
- **NPC繞路根因**：正門Boolean孔底比地板高約2.7mm，CC可跨，貼地放大的導航capsule卻判堵門。只把查詢底部的25mm視為CC步階容差，不改模型洞口、門寬或CC。重現舊貼地查詢路線173格，新47格；完整操作NPC實走約10.39m、最大z=3，未繞後院。證據reaction-route-probe.txt。
- **管長／外牆**：當前Scene由15縮10m，舊關預設／歷史Prefab不改。庭院西／北／東三面簡單Cube牆封住從屋外繞入倉庫；全火時有牆無路、暫停牆有路的正負尋路對照PASS，非任務旗標或隱形牆。牆group獨立 `Prototype Courtyard Boundary`，材質歸AIGenerated/ReviewBoundaries，使用者住宅網格未更動；新牆納入管線壁面。
- **近端噴射可玩性**：整關測到舊初速65%下限會飛越腳前火，留下清路斷帶；新增SpraySettings.MinimumSpeedFraction，舊資料0仍回退.65。只ReviewWater設.2、落點半徑.9；ReviewSoda設.3、半徑1.35；保留真初速／重力／碰撞、不依鼠標距離命中，不增加射程作弊。粉末也要先清腳前通路再前進，不能穿火走到鍋前。
- **情境呈現**：PickupContextView讀同一Carry.NearbyItem，只有安全可拿的最近布丁顯示同款金色Mesh外輪廓與物件旁圓角E框；拿起／離開／失敗消失。管長接近10m顯示「管線不夠長了！按 F 解除綁定！」，退回或拔管即消失，有.2m退出遲滯避免抖動。未恢復整排操作說明、物價或油水警告。
- 收尾核對F語意：滑鼠模式舊RunCommand仍沿用Interact，站在另一來源旁可能改接而非拔管。改為pointer F一律Disconnect，右鍵才接來源；歷史TickCommand／窗口低階API保留。72項最終重測含「滿管且旁有另一來源，真正F命令仍斷線並立即收提示」，不是只在測試直接呼叫Disconnect。最終記錄kitchen-final-validation.log／kitchen-final-build.log。
- 保留測試失敗：attempt-1/2近端水落點斷帶；attempt-3/4粉末進場近端未清；attempt-5先破門再回來踩到鄰火。修近端分布與粉末殘留，正常驗收採滅鍋後先取布丁→門前放下→三刀破門→再拿起交付，未關閉死亡或直接清掉正常流程的火求通過。交付約剩18.617秒，仍需初見玩家難度驗收。
- 最終整關 **72項PASS**、噴射相容回歸 **40項PASS**。Windows Latest新建置0錯誤0警告，實際D3D11包 **25項PASS**，正常ReadDevices啟動6秒14191 grounded frames（Hidden時focused=false）。離線圖已看圍牆、179格全火、清路、布丁實體外輪廓；OnGUI的E與F提示只驗字串／條件／元件，Hidden黑圖不算像素驗收。
- 最新證據 `Docs/Validation/2026-10-28-kitchen-hints/RESULT.md`。完成時無Unity／玩家程序；舊生成資產、原Blend／FBX／GUID不刪。下一步請使用者驗收兩個情境提示的位置／字級與無其他提示下的60秒難度。

## 炎上消防隊 v0.1 — 2026-10-28（地面火／油鍋分離、黑布丁與死亡、管線連續性）

- 使用者Windows截圖與回饋：管線遇牆浮出V形節點，火開局鋪滿廚房且不能逐區滅、入口不清楚；新增水碰油鍋爆燃燒黑布丁、黑布丁毒死NPC、踩火死與不顯示提示文字。全程不做螢幕控制；使用者自行關閉舊玩家版後才更新Latest。
- 根因核對：舊CookingOilArea只是半徑熱區，72個視覺火共用一個ObjectFire；地板FireField實際全不可燃，水不可能逐格清理那些展示火。新Scene停用舊Area／patch呈現，地面改既有FireField獨立強度、濕度、相鄰蔓延；油鍋保留ObjectFire，只接受粉末滅火。ElementReaction沿用原命中邊界，不把鍋與地面混為同一個火。
- CookingOilIncident於0.2秒從鍋旁第一個地面位置引燃；前2秒spread=.4，之後=.035。原型可燃範圍只廚房走道，牆／櫃體／戶外不可燃。實測約.35秒4格、1.65秒13格、6秒22格；不是開場整片火。地面火可被水／粉末逐區熄滅，水濕痕5秒阻止立刻復燃。火VFX容量由Scene設512，避免活火超過舊48個可視上限造成不可見危險。
- 油鍋累積水劑量.08觸發一次爆燃：短時火焰擴張、局部引燃、1.5m內布丁Scorch。連續水粒子不每幀重複爆；水永不滅油鍋。焦黑是CarryItem狀態，BurntFoodView以renderer property block把布丁／焦糖變黑、盤子仍白，不寫回使用者材質。換View不改毒性，洗水／拿放不還原品質。
- NpcItemRequest收黑布丁後先顯示「好難吃...」，1.4秒後IsDead與ResidentPoisoned失敗，不可逃生或計救援。正常布丁仍需油鍋熄滅、門破、安全路線與實際走出出口。PlayerFireContact沿實際移動路徑檢查地面火，踩入立即死亡、收工具、掉持物、鎖玩法輸入；R仍重開。ActorDefeatView只使Visual倒地，CC不加Rigidbody／不移動玩法root。
- 完成條件本關改為NPC實際逃出＋油鍋熄滅，不要求遠離逃生路線的每格火全滅；以Scene選項保留舊關完整清火契約。60秒仍是布丁交付期限，失敗不能補交；完成只觸發一次。
- 管線原本每次contact新增硬釘住的bend ring、下游粒子重排。PhysicalHoseTube改固定128材質節點，只有來源／入管口固定，內部碰撞／距離上限／彎曲平滑控制形狀，只有兩端65cm升起、不再沿每個內部路由點升起。隱藏的HoseConnection接觸折線仍負責15m／繞牆拓樸，不改成穿牆捷徑。實測接觸期間不變更節點數、不閃斷、最大單幀內點位移.1845m、最長段.0996m；仍是簡化PBD，不是完整軟管PhysX。
- 出生改客廳(2.6,.1,-3.7)，出口保持戶外。HUD去除底部操作、E／接來源等互動提示、油水警告、重來提示；只留倒數、NPC心聲／反應、簡短成功／失敗。沒有更動模型、戶型、相機、門血量、管長或新增教學。
- 為使固定方向噴射能落到地面，本Scene的Water改初速7／重力16，粉末6／重力8，仰角仍18°、空中壽命1.5秒；只改兩份Review profile，不改滑鼠成點選命中。實測實際飛行水可清地面且可命中鍋觸發爆燃，粉末實際命中鍋熄火。
- 首輪新測試抓到擴張過快與靠牆隔火誤殺：保留attempt-1-path-died.txt，降低快／慢速率、接觸加固定障礙LOS後重測。最新**59項PASS**，正常通關全程pointer commands／真CC／真噴射，交付約剩30.18秒。毒性分支注入門／安全前提以隔離NPC狀態；另有真水粒子打鍋分支，只預清地面避免人物先死。這些不是人工鍵鼠通關。
- Windows Latest新建置0錯誤0警告，實際D3D11玩家**22項PASS**；真ReadDevices啟動6秒室內落地穩定。已看實際Unity離線圖：局部延燒、清路、白管連續、黑布丁、NPC倒地；Hidden玩家擷取黑圖仍不算UI驗收。證據 `Docs/Validation/2026-10-28-fire-loop-v2/RESULT.md`。未commit／push／發布；請使用者驗收管線轉角、噴射距離與無提示下的一分鐘難度。

## 炎上消防隊 v0.1 — 2026-10-28（使用者正式四件道具替換）

- 最新 Downloads/第一關1027.blend SHA256 `7f7c3bb7170a0787c79d2bc4e9a1c29425a1523a45db8c187e1524c295229504`，唯讀快照保存於 `ArtSource/UserProvided/Level1027/PropsUpdate20261028/Source.blend`；舊 Source.blend 不覆蓋。逐頂點及三角面核對：原戶型／家具沒有變，只新增道具；不重建地圖。
- 四組使用者幾何：馬桶本體／馬桶座／水箱／水；立方體.002（倉庫粉末袋）；油鍋／握把／油；盤子／布丁／黑糖。依原擺位、原比例替換目前四個 Root 的 Visual，簡單象牙白／水藍／粉袋藍／深灰鍋／琥珀油／黃色布丁與焦糖配色。衍生 Mesh、材質、純外觀 Prefab 歸 `Art/UserProvided/Level1027/Props20261028`，不是 AIGenerated。遠處兩個大型測試立方體不放進場景，原檔仍保留。
- 保留 FluidSource/Profile、CarryItem 身分、門／NPC／60秒規則。水箱東側接管口、粉袋前側接管口、油面火點、布丁盤中心和獨立碰撞按新擺位對齊。沒有放寬15m或改射程。原生成外觀已從活躍道具撤換，歷史生成資產不刪。
- 視覺檢查抓到水／油平面朝下；只修 Unity 衍生網格的面向與法線，原 Blend 不回寫。保留 Blender corner normals，避免將平面稜角全平滑。匯入當次的 GPU cache 預覽未更新，重開背景 Unity 後重新渲染確認鍋內油面與新材質。馬桶低角度水面仍會被碗身遮擋，不擅改使用者幾何。
- 新場景 Play Mode **85項 PASS**：真 CharacterController 行走、新馬桶連接、15m繞牆、水增火、粉末滅火、三刀破門、近鍋布丁禁拿／滅火後拿取、交手／尺寸、NPC逃出、60秒及重開。腳本路線改為新位置，不搬回舊擺位求通過。交付約剩26.5秒，但這不是人工操作難度驗收。
- Windows Latest建置 **0錯誤／0警告**，真正D3D11玩家程式 **19項 PASS**，包括四組正式模型在包內。首個 `-batchmode` Windows測試無法擷取畫面而失敗，保留證據；改正常圖形模式 Hidden 執行同一包後通過。Hidden截圖仍黑，明確不當成UI驗收；已人工查看實際Unity RenderTexture的道具與整關離線圖。
- 工作證據 `Docs/Validation/2026-10-28-user-props/RESULT.md`。全程未螢幕控制、未關閉使用者正在編輯的Blender、未commit／push／發布。下一步請從檔案總管啟動同一 Latest，驗收接管提示、噴射命中距離與布丁交付的實際手感。

## 炎上消防隊 v0.1 — 2026-10-28（NPC布丁心聲標籤／啟動落地調查）

- 使用者提供How to Fish截圖，NPC預設心聲改為精確文字「好想吃布丁...」；移除「居民」前綴，改細白框、淡色透明底、白字、布丁淡黃重點字與輕微文字陰影。標籤錨點由1.9m降至1.65m，仍隨NPC／攝影機投影，不做螢幕控制。
- 使用者回報啟動就穿地、持續下墜。先查：48×.3×48 Ground的BoxCollider啟用、非Trigger，玩家CC與floor layer碰撞未被忽略，出生點在範圍內；沒有加Rigidbody或亂改質量、重力、Physics設定。
- 舊smoke關了ReadDevices，不涵蓋正常啟動更新，因此新增ReviewStartupValidation／明確旗標--verify-startup的ReviewStartupProbe，保留真LocalPlayerInput.Update，不在測試內SyncTransforms或傳送求通過。原Editor6秒與真正Windows玩家6秒均穩定：玩家y=.1→.125、持續grounded；另補30/60/144Hz及.1/.333/1秒長幀、進出正門檢查通過。
- **使用者後續明確回覆「已經沒這問題」**。掉落未在本機重現、根因未確認，不能回報「修掉地板bug」；本輪不變動地板或玩法，只更新標籤並保留啟動回歸。證據 `Docs/Validation/2026-10-28-grounding-tag/`。Windows更新／最終smoke狀態見該目錄RESULT.md。
- Latest已重建0錯誤／0警告，Windows實際D3D11 smoke15項PASS（含新字串）。只做背景程序、不送鍵鼠；完成時無Unity／玩家程序。未commit／push／發布。

## 炎上消防隊 v0.1 — 2026-10-28（一審住宅／布丁救援與管線修整）

- 使用 `第一關1027.blend` 建新 `FirstReview1027.unity`，舊 Scene 不覆蓋。原檔及專案備份 SHA256 都是 `30a764941f7df43e12341048c539cead3ffaece8a82d1882ec6f7226342ecdff`，未回寫。住宅、角色、斧頭、噴槍皆使用者模型；只暫用馬桶、蘇打粉盒、油鍋、布丁，未放生成桌椅。布丁放現有廚房檯面。離屋的樣品門把／流理臺保留來源，不擺入場景。
- 新流程：馬桶水使油鍋強度提高（上限3）→倉庫蘇打粉滅火→三刀破臥室門→E拿／交布丁→NPC安全走出正門，完成只觸發一次。使用者確認展示關改15m，保留戶型比例，舊5m關不動。60秒是布丁交付期限，成功交付後倒數凍結，仍需消防／門／安全路線才可逃生；超時失敗，R重來。
- 可替換邊界：CarryItem所有權／碰撞、PlayerCarryInteraction距離與隔牆判定、NpcItemRequest條件、FirstReviewMission狀態／時間、Hud純呈現。布丁附到FBX的100x骨頭時保持世界尺寸；持物可F拔管，避免卡在舊連線。不是恢復安撫功能。
- 玩家3.6m/s、乾地加速40／煞車48、轉身960°/s；新距離相位步態、站立腳IK、起停／轉向重心與工具姿勢。診斷到Role2經正面校正後L/R骨頭與actor左右相反，修正交叉拉手；衣襬／飾條不再跟腿、補前臂蒙皮權重。新增四份UserProvided衍生Skin，原頂點／三角面／FBX不改。舊Animator與舊sampling只在新玩家停用，NPC沿用原Idle/Walk。斧刃外傾，避開大頭。直線步態站立腳平均平移約0.00063m/s；不宣稱轉向與所有姿勢零滑步／零穿插。
- 使用者追加：牆面專用Lambert霧面（無金屬／鏡面瓣）；開局2秒連續油火區擴大到布丁，CarryItem本身拒絕熱區拿取，清火恢復；NPC要求改頭上Name Tag，左上要求面板移除。油火區是同一油鍋火源的有限連通範圍，不是72個獨立燃燒狀態；目前無角色傷害系統。
- 管線閃斷根因：舊HoseView的任一小段擦牆會把整條LineRenderer隱藏。新Scene停用舊呈現，改PhysicalHoseTube單一連續八邊形3D網格，128基礎節點＋牆接觸點、Verlet／距離上限／彎曲平滑／地板與物件碰撞，局部修正不整條隱藏。壓縮約束曾造成鋸齒，改只限最大距離；長段貼地，來源與噴槍附近才升起。15m與自動繞牆仍由HoseConnection唯一控制，非完整軟管剛體模擬。
- 首次一審整關63項通過後另查出握持／100x交付尺寸／持物拔管等邊界，逐項補測；最新Play Mode 81項（同pointer command、真CharacterController／飛行噴射／門碰撞／NPC步行與重開），本輪舊管線22、繞牆29、門與統計21通過。實際結果、建置與玩家smoke見 `Docs/Validation/2026-10-28-first-review/RESULT.md`，不以早期數字替代最新版證據。
- 一度使用電腦控制技能觀察Windows UI，確認繁中、倒數與實按R回到01:00；使用者Escape中止，立即停用，後明確要求先不螢幕控制。**其後只背景作業，未再送鍵鼠／關視窗。** 使用者自行關閉遊戲後再更新Latest。背景ScreenCapture黑圖不算UI驗收；後續新NameTag未真人畫面驗收。
- 原平台22m，出生跟隨鏡頭拍到右側灰底，依使用者截圖修為48m地面，16:9／21:9四角覆蓋通過；戶型不改。Windows名稱同步炎上消防隊。說明 `Docs/FIRST_REVIEW_1027.md`；無commit／push／發布。MCP仍未接Bridge，這次未把它擴為展示關阻塞。
- 最終Windows Latest已重建：0錯誤／0警告；實際D3D11打包exe的14項載入／設定／2秒熱區檢查PASS。黑色Hidden擷取明確標成不驗證UI，未再做螢幕控制。最後無Unity／FireRescue程序殘留。下一步請使用者從同一PlayLatest自行驗收頭標、2秒禁拿、粉末清火與3D管線手感，再換正式四件模型。

## 炎上消防隊 v0.1 — 2026-10-27（進行中：自動繞牆／MCP，額度收尾）

- 使用者要求管子像實體繞牆，取代先前遇牆停止。已新增HoseWrapPath接觸折線，連接HoseConnection／HoseView／SprayWallOcclusion與motor轉向inlet約束，5m改計整條繞行路徑，維持opt-in避免改壞舊場景。
- Unity獨立Play Mode **29項PASS**：舊卡住重現、新繞行／解繞、碰撞、總長限制、來源生命週期與柱子迴圈。不將此稱第一關整合完成；fixture PNG全白，視覺驗收尚未通過。
- **尚未套用至FirstRescue1026、尚未更新含F導管步驟的整關驗證、尚未重建Windows。PlayLatest目前仍是前一輪玩家版。**
- 使用者加做Unity MCP並允許螢幕控制、確認安裝。畫面開啟Unity／套件管理器，但popup操作不穩與activate_window逾時，改由套件設定安裝CoplayDev固定10.3.0，已Unity匯入及編譯；uv0.13.6在Tools/UnityMCP/bin可執行。全域MCP URL原已存在未更改；本機server／Bridge／工具連線probe尚未完成，不能回報已連線。
- 使用者要求剩餘6%先做紀錄，停止擴張。完整接續狀態、精確方法、已知限制與順序見 `Docs/HOSE_WRAPPING_HANDOFF.md`。無commit／push／發布、未改原模型；現在無Unity／玩家程序。


## 炎上消防隊 v0.1 — 2026-10-27（1026戶型／馬桶水第一人救援）

- 按新提供 `第一關1026 (1).blend` 建獨立 FirstRescue1026 Scene；40來源mesh衍生匯出、按參考圖校正方位。只保留生成馬桶，其餘生成家具不進新場景；使用者人物、消防斧與噴槍沿用。原場景、模型、來源分類均保留，未刪資料。
- 原始Blend與專案備份hash一致。實走抓到浴室門背面封住：Boolean cutter深度剛貼齊Solidify背面。只修記憶體匯出深度，不存原檔、不擴門寬、不停用牆碰撞；五門雙向可過／旁側牆仍擋。牆視覺.72m，碰撞完整獨立。
- 水新增opt-in初速／重力／阻力解析飛行與substep掃掠、真命中事件；沿用資金流量／來源快照／容量／停止後尾流。新馬桶water Profile獨立；舊水油粉曲線契約40項回歸通過。視覺觀察者比較2D圓粒子、方向水滴Mesh、Hybrid，選Hybrid；低模火苗／水花／濕痕不掌管碰撞或滅火。
- 5m上限不放寬，浴室門口沿用明確F導管點，不自動繞牆。從正門真行走、兩次右鍵拿槍／連接、拉管、再按住噴水，門口火清出通道後NPC自走經兩門到屋外；不安撫、不傳送、不直接清火。片段門預先開啟，其他教學與其他房間火場未接回。
- 修NPC格子小方塊與實體淨空不一致根因：controller尺寸capsule、格間掃掠、移動前檢查及受阻重判；窄於人身通道拒絕，不能通過是假路線。新圖格子中心對齊門洞。
- 完整Play Mode／重開38項PASS（含獨立NPC、彈道、VFX probes），共享噴射40項PASS，不把重跑累加成更多測試。新Windows x64 Development build 0 errors/0 warnings；實際背景D3D11 exe驗新Scene／5m／六配色／骨架／初始水火smoke PASS。完整通關來自Editor命令測試，尚未真人鍵鼠或玩家版手動通關。
- 真看過新戶型、三種水、火焰與救援後GPU圖；修過錯誤渲染enum、錯數量斷言與異原點fixture比較，不降低門檻；原水站位打門框、火數不降的失敗也留存，改用真正能從門洞噴到地面的站位。沒有用取消物理／點選清火方式求過關。
- 交付固定 `PlayLatest.cmd` → `Builds/Latest/FireRescue.exe`，BuildSettings唯一啟用FirstRescue1026。`Docs/FIRST_RESCUE_1026.md`列操作／資料夾／水設定／限制，`Docs/Validation/2026-10-27-first-rescue-1026/RESULT.md`列證據。無螢幕控制、無commit／push／GitHub發布。


## 炎上消防隊 v0.1 — 2026-10-27（Windows入口補齊／模型來源實體分類）

- 使用者再次指出單椅、待救者配色、5m尚未完成，並要求AI生成與其提供模型分開。使用者確認看的是Windows玩家版，未提供舊exe路徑；不直接歸咎使用方式，重新檢查Scene／Prefab／預設建置／實際封裝。
- audit-before確認當前Scene三項已有，但BuildSettings仍只啟用ResidentialFire，Player Prefab13m、Role1 Prefab只一種白模材質。這次補齊Prefab5m／六色、預設唯一啟用TutorialGreybox，舊Scenedisabled保留；單椅朝內狀態保存。固定`PlayLatest.cmd`與Builds/Latest作本機交付，不再要求使用者挑歷史Build資料夾。
- AssetDatabase.MoveAsset搬12組資產，保留163既有GUID；Art分UserProvided和AIGenerated（使用者加工骨架屬UserProvided，不算AI原創），ArtSource也同分。21份FBX／Blend內容SHA256完全相同，沒有重生模型、換GUID或刪原檔；生成腳本改新路徑，角色從專案內Source.fbx重建，不依賴Downloads。外部噴槍比對改顯式--current，歷史metadata保留原路徑。
- 實測首次修復入口抓到ResidentPaletteSetup以artFolder+RoleSwapRig組合的舊路徑；已改用CharacterSwapSetup.Folder，失敗log保留。原始模型GUID及新舊SourceHash檢查與唯讀獨立review通過。
- 17針對性＋43整關重開＝60項PASS；整個Scene（含inactive）Missing Script/靜態Mesh/SkinnedMesh/Bone均0。Windows x64 Mono Development建置0錯誤／0警告，真打包exe背景D3D11啟動檢查PASS（新Scene、5m、6色、骨架、單椅朝內）。首次-nographics材質讀取假陰性已隔離：同一exe不改資產用GPU即通過，Null模式不作驗色依據並加明確guard。沒有操控螢幕或裝置輸入。
- GUID-aware基線639檔624完全不變，15有意修改，0遺失。主要變動為路徑常數、Role1/Player Prefab、BuildSettings與測試入口；Scene本輪內容雜湊不變，原本正確資料沒反覆重做。新增只讀、明確旗標才執行的Development玩家版Probe，正常遊戲不啟動。
- 最新本機版Builds/Latest/FireRescue.exe；沒有commit／push／發布，GitHub下載版未變；原歷史Build與使用者資料全保留。文件`Docs/ASSET_CLASSIFICATION.md`、來源README與證據`Docs/Validation/2026-10-27-asset-classification/RESULT.md`。NPC碰撞／導航落差仍是已知待辦，不因本輪通關就宣稱已修。

## 炎上消防隊 v0.1 — 2026-10-27（單椅對調／待救者參考色／5m管線／品質門）

- 使用者要求交換茶几左右兩張單椅、待救者參考圖上色、管長改5m；追加先不做螢幕控制，並要求說明優先修復與自我品質監督。未操作GUI，只背景Unity／離線GPU渲染；未擅自關閉原本執行中的舊玩家版。
- 單椅實際交換位置、保留各自旋轉並更名對應空間左右，現在皆朝茶几，佔地範圍不變；Household生成配方的預設朝向同步修正。NPC角色1只加Scene共享材質override，橘髮／淡橘衣／黃飾／膚色／黑眼，原10網格、骨架、Idle／Walk與FBX保留，沒有把帽子或其他參考造型重建。新材質Ensure只補不存在者，不覆蓋之後人工調色。
- 只當前Scene HoseConnection.maximumLength 3→5；Runtime預設／歷史Prefab仍3，牆阻、XZ計長、窗口公式不改。整關驗證新增顯式expectedHoseLength，5m入口檢查5，不把原3m歷史結果冒充新版本。
- 背景真Unity保存重讀／冪等PASS；14針對性檢查（含真Motor過3m並停5m、持續外拉／反向、窗口合計5m）＋43整關與重開＝57項PASS。看過單椅朝向、NPC正面／Walk實際GPU圖；核心救援流程通過，未做真人鍵鼠／手感驗收。
- 最初新增Editor驗證腳本編譯出現Component.parent錯用與Unity6000.5禁止GetInstanceID舊API，均在任何Scene遷移前修正；原始失敗log保留。最終Windows x64 Mono Development建置成功0錯誤／0警告：`Builds/ResidentColor5m/FireRescue.exe`。既有Editor CS0618過時API警告屬舊檔，不宣稱專案全部零警告。
- 620檔基線617不變，只改HouseholdLayoutSetup、ResidentialIntegrationValidation與Scene；新增19個Assets檔含meta，無刪檔。Scene998舊段無刪除、5段修改＋2段marker新增，舊模型／Prefab／Runtime／Settings／Packages／相機均保留。未commit／push／發布。
- 下一個優先問題建議：NPC的導航淨空與CharacterController碰撞一致。目前沙發卡角只靠擺位避開，根因尚未修，不宣稱已穩定支援任意新家具；其後優先噴射／拉管手感，不先增系統。`Docs/QUALITY_GATES.md`定需求／行為／畫面三道門，證據分級、失敗留存、數量不等於品質。這輪沒暗中實作新NPC系統。
- 證據：`Docs/Validation/2026-10-27-resident-color-5m/RESULT.md`；最新狀態已補入PROTOTYPE_CONTEXT與整關說明。

## 炎上消防隊 v0.1 — 2026-10-27（室內家具簡模／來源物體外框）

- 依使用者室內圖製作25種最簡Primitive Prefab，配置儲物間、浴室、臥室、廚房、客廳玄關與戶外汽水攤；床／桌椅／衣櫃／冰箱／洗衣機／淋浴／水槽／灶爐／L型流理臺／沙發／電視櫃／垃圾與回收桶／鞋櫃等可辨識輪廓。保留Root碰撞、Visual獨立；舊床沙發色塊停用備份，茶几Rigidbody及來源Root不換。
- SourceHighlightView不再建地面LineRenderer圓圈，改在同一NearbySource的Visual外膨脹約3.5cm輪廓，未接金色／已接藍色，離開／牆阻／停用隱藏，原物件材質不改。InteractionOutlineTarget快取外觀副本，硬邊Mesh平均法線、多submesh、skin與不可讀FBX安全fallback，SourceHighlightView.Refresh供動態換Visual，Shader明確序列化防build剔除。
- 只補擺設，不搬動已驗證來源／火點／NPC／門／窗口；因此活躍油瓶油鍋仍在原客廳後側測試檯，新廚房水槽／蘇打粉／汽水攤是外觀佔位，新窗框不是導管接點。沒有暗加新元素、教學或UI。
- 初次整關NPC卡新沙發前緣，診斷為格子判可走而實體半徑重疊，NPC停在(2.750,0.025,-1.7932)。只主沙發向後移.35m、右單椅側移.2m修通；另一次孤立probe停在出口是玩家站出生點擋路，移開測試玩家後NPC自動抵出口，未改尋路或3m。
- 真Unity保存重讀／冪等PASS；家具與外框45＋整關重開43＝88項PASS。首次失敗與NPC位置／附近碰撞診斷保留，最終原生步行破門／水與乾粉／窗口／NPC單次救援／重開完整通過。已看金藍外框與各房間、俯視配置的實際Unity GPU圖。
- Windows x64 Mono Development成功，0錯誤／0警告，本機`Builds/HouseholdGreybox/FireRescue.exe`；未做真人鍵鼠手感或啟動玩家視窗，未commit／push／發布。舊NozzleControls圓環斷言已遷移，該歷史入口未重跑，不能併入通過項數。
- 基線535檔531不變，只改SourceHighlightView、兩個Editor驗證入口與Scene；新增85個Assets檔含meta、無刪除／缺meta。Scene文字大量移動實為Unity檔段重排；按fileID核對777舊段無刪除，僅9段有變（舊Visual停用／附加、Shader引用、群組子節點），新增221段。相機／角色／核心Fire與Reaction／NPC程式／Profiles／Packages／Settings未變。
- 文件：`Docs/HOUSEHOLD_GREYBOX.md`及素材需求更新；證據`Docs/Validation/2026-10-27-household-outline/RESULT.md`。下一步請使用者驗收擺設與輪廓，再安排正式模型或依新圖搬動玩法熱點。

## 炎上消防隊 v0.1 — 2026-10-27（雙角色交換／骨架／Idle與Walk／移動朝向）

- 依使用者要求交換：玩家使用角色2長髮基本色，NPC使用角色1貓耳白模。原始FBX不動，另產生RoleSwapRig：玩家1.6m／18骨、NPC1.5m／20骨含尾巴，原六／十個網格拓樸保留。雙方Generic骨架、Idle 2.5s／Walk 1.25s，Root Motion關閉；舊場景Visual僅停用保留。
- 初次離線權重稽核發現角色1側髮少量誤綁手臂，修為302頂點全Head、眼眉亦全Head。每段Action建立前清零Pose並重產／重匯驗證。首次通過的44項報告另存before-hair-fix，最終資產再重跑，未隱藏證據。
- LocalPlayerInput新增當前場景專用opt-in，平常移動朝走路、停住維持朝向；斧頭實際一刀含鬆鍵收尾／實際噴射才朝鼠標。第一次拿工具、接來源及其長按不誤判。FirefighterTools提供唯讀動作預判，發射前先轉身；legacy TickCommand保留舊瞄準，WASD相機相對合約不變。
- 真Unity保存重讀／冪等PASS；雙角色朝向／skin／動畫44＋整關重開43＝87項PASS。整關實際三刀破門、3m窗口導管、普通火用水／油鍋用乾粉、NPC自行逃生和單次完成均通過。看過兩角色Idle／Walk與遊戲鏡頭離線Unity渲染。
- Windows x64 Mono Development建置成功，0錯誤／0警告：`Builds/CharacterSwap/FireRescue.exe`。未聲稱本輪實體鍵鼠手感／多人或正式全身工具動畫已完成；本機版未發布。
- 513檔基線509完全不變，只改2Runtime＋整關驗證入口＋Scene，新增22個Assets檔含meta，無刪檔。碰撞、槍口／管入口、3m、窗口、相機、火焰／反應／NPC程式、Profiles、Packages與Settings保留。
- 交付：`Docs/LEVEL_ASSET_REQUIREMENTS.md`分類已有、原型替代及正式需求，涵蓋角色／Idle／Walk、火焰、水／油／乾粉、工具、來源及門窗家具；`Docs/CHARACTER_SWAP_ANIMATION.md`記架構與手動驗收。證據 `Docs/Validation/2026-10-27-character-swap-animation/RESULT.md`。

## 炎上消防隊 v0.1 — 2026-10-27（消防管牆阻／3m張力與液體流量）

- 使用者要求繼續修消防管／液體物理。先採遇牆卡管的簡化方案，保留手動窗口，不擴充自動繞牆、流體引擎、新元素或多人；保留白模與正向較遠Camera。
- 修前真Unity量測：管線穿牆候選位置照樣通過、XZ半徑3但高度2時UsedLength3.605551、停用來源仍有連線；液體0.01秒點按實收0.075而應0.03、五次點按0.375而應0.15、0.5秒單幀約0.825而應1.5。證據保留，不只憑症狀猜修。
- HoseConnection新增明確牆阻／入管點，3m先限長再檢查前後段，阻擋時回到可行位置；統一XZ度量、無效來源清理。Motor清除被約束的外向動量；HoseView曲線碰牆改拉直，換來源不從舊位置插值。展示停用不會取消限制。
- SprayEmitter維持短按立即發射，但每批Amount改由FlowRate*dt分配；移除0.25截量；Stop／換來源補一次舊快照尾量，來源銷毀也不把舊水變新油。發射迴圈有界256，極端飽和用DroppedAmount記未發量；不改Reaction、FireField、Profiles或碰撞演算法。
- 首次整關窗口固定失敗，量出舊外框Route讓來源到窗點的斜線碰在孔洞旁1.34m；只把Route內移0.4m至室內窗緣，未放寬3m／孔洞或搬水源。保留失敗報告／圖，最終整關與重開通過，折線約2.37m。
- 22管線＋11流量＋24效果＋40方向＋43整關重開＝140項PASS；保存重開／冪等PASS；Windows x64 Mono Development建置0錯誤／0警告，已檢視實際Camera圖。真人長按／手感、多人與效能未驗收。
- 505檔基線497不變，只改4Runtime＋3Editor驗證入口＋Scene；新增8個Assets檔含meta、無刪檔／缺meta。舊Prefab／模型、Camera、角色、Profiles、Packages／Settings保留。新本機版 `Builds/HoseFluidPhysics/FireRescue.exe`；未commit／push／發布。
- 說明：`Docs/HOSE_FLUID_PHYSICS.md`；證據：`Docs/Validation/2026-10-27-hose-fluid-physics/RESULT.md`。仍不是精確3D弧長軟管、自動繞角、家具全面管線碰撞或真實流體。

## 炎上消防隊 v0.1 — 2026-10-27（主角換角色1白模；正向較遠相機）

- 使用者提供角色1／角色2，要求先換其中一個主角、上色版之後提供。檢查後選角色1貓耳／尾巴白模；角色2與先前來源SHA相同，既有長髮居民NPC及動畫保留。
- 角色1原檔10 Mesh／1425頂點，無材質／rig／動畫；原檔、ArtSource副本及Unity匯入檔SHA一致。只用外層縮放／偏移／旋轉對齊1.6m、feet0、+Z，加中性白材質，不重做造型、上色或新增動作。主角舊Capsule／Helmet停用保留；Root碰撞、工具和管線接點全保留。
- 使用者確認視角為「對齊房屋的正向俯視」。相機Pitch50不變，Yaw40→0，正交Size4.5→6，可見寬高約增加33%；沿用.18s平滑跟隨、不跟角色轉。RescueCamera角度改序列化設定，舊場景預設50/40不變。
- 保存重讀／冪等與Gameplay快照PASS；新17項角色／相機＋43項整關／重開＝60項PASS。首版測試走到門旁牆後誤測Visual關閉不能移動，依戶型改朝無阻擋戶外重測，未改Gameplay／放寬斷言，保留失敗證據。
- 主代理檢視實際渲染與Scene diff；Windows x64 Mono Development建置0錯誤／0警告，再用Computer Use實際啟動確認模型／視角，Player.log錯誤0並正常關閉。長按鍵鼠／手感仍待玩家驗收。
- 494檔基線中491不變，只改RescueCamera、當前Scene與整關報告入口；新增11 Assets檔含meta，無刪檔／缺meta。Packages／Settings／其餘Runtime／舊Prefab／模型未變，未commit／push／發布。
- 新本機版 `Builds/PlayerWhitebox/FireRescue.exe`；說明 `Docs/PLAYER_WHITEBOX_CAMERA.md`；證據 `Docs/Validation/2026-10-27-player-whitebox-camera/RESULT.md`。

## 炎上消防隊 v0.1 — 2026-10-27（允許畫面控制，玩家版點擊檢查）

- 使用者解除先前不控制畫面的限制，使用 Computer Use 開啟現有 UserCharacters 玩家版，實際檢視住宅外出生／斜角視野與使用者斧、噴槍模型，測首次左鍵拿斧、第二次揮斧、右鍵切噴槍及未接來源不噴。
- 本輪畫面工具無持續按住 WASD／右鍵介面，短按無法穩定維持 held 狀態，因此未虛報整關／NPC／噴射手感驗收；不為了操作工具改變遊戲輸入規則。保留上一輪58項自動測試與本輪UI檢查的證據區別。
- Player.log未見Exception／NullReference／Shader error／Error（0），正常關閉進入Shutdown。未更動程式、Scene、模型或數值，不重複建置；未commit／push／發布。證據：`Docs/Validation/2026-10-27-player-ui-smoke/RESULT.md`。

## 炎上消防隊 v0.1 — 2026-10-27（使用者角色白模接入；10-26實作驗證）

- `角色2.fbx` 實際只有一位長髮居民，6 Mesh、1127 原始頂點、無材質／骨架／動畫。使用者同意先用這份；保留原檔及 SHA 相同副本，不虛構第二種角色。消防員保持既有外觀。
- 衍生模型保留原拓樸、加七種基本色、18 骨 Generic Rig 與 Idle／Run 循環，對齊 1.5m、腳底 y=0、+Z 朝向。首輪分色／權重錯把臉和手當衣服，按實際大頭短身比例修正，已檢視修正後 Blender 與 Unity 離線圖。
- 只接入 TutorialGreybox 目前住宅 NPC 的 `Visual/User Resident 2`，舊 Capsule 停用保留；重用 CharacterLocomotionAnimation，不新增或修改 Runtime。NPC Controller、尋路、救援、消防員／工具／管線／噴射皆保留。
- Unity 初次驗證修正兩個 Editor API 編譯錯誤；另查明 BakeMesh 預設縮放加 TransformPoint 重複套用 .599 倍，依本機 Unity API 文件改用補償縮放檢查，沒有改錯原模型尺寸或放寬斷言。失敗日誌保留在 Logs。
- 保存重讀／重跑冪等 PASS；角色動畫 15＋整關／重新 Play 43＝58 項 PASS。Windows x64 Mono Development 建置 0 錯誤／0 警告，`Builds/UserCharacters/FireRescue.exe`。離線 Direct3D11 渲染已人工看圖，沒有操作使用者視窗或啟動玩家版；硬體輸入／手感待使用者驗收。
- 466 檔基線中 464 不變，只修改當前 Tutorial 場景與整關驗證的報告入口；新增 28 個 Assets 檔含 meta，沒有刪除或缺 meta。原始角色 SHA 與副本一致，舊 Prefab／模型／Runtime／Packages／Settings 不變。未 commit／push／發布。
- 模型文件：`Docs/USER_CHARACTERS.md`；人工清單：`Docs/PLAYER_ACCEPTANCE_2026-10-26.md`；證據：`Docs/Validation/2026-10-26-user-characters/RESULT.md`。下一步等玩家試玩噴射與角色，再依回饋調參，暫不新增大型系統。

## 炎上消防隊 v0.1 — 2026-10-26（噴射效果獨立調校；無畫面控制）

- 每種 SprayProfile 新增獨立的滅火力、推力與落地殘留量倍率（0～4），預設全為 1，保留原關卡數值、方向射程與接管操作。只有水能推動物體；油不滅火、水不滅油鍋、乾粉不加濕。
- 飛行樣本保存發射當下的效果設定，修改 Profile 不會追溯改動已在空中的噴射。殘留量 0 不新增／刷新殘留計時，但可獨立滅火。歷史 API 保留中性倍率相容。
- Unity 6000.5.0b9 背景編譯、三資產保存重讀／冪等驗證、24 項效果測試＋43 項住宅整關與重開全部通過。Windows x64 Mono Development 建置 0 錯誤／0 警告，`Builds/SprayEffectTuning/FireRescue.exe`。
- 本輪不操作畫面、不截圖、不啟動玩家視窗，不宣稱鍵鼠／視覺／手感驗收。未改場景、碰撞、模型、Prefab、Packages 或 ProjectSettings；未發布。參數見 `Docs/DIRECTIONAL_SPRAY.md`，證據 `Docs/Validation/2026-10-26-spray-effect-tuning`。

## 炎上消防隊 v0.1 — 2026-10-26（方向噴射、飛行與殘留；10-25開始）

- 依使用者澄清，「持續多久」指空中／地面停留，不是長按上限。滑鼠只決定方向，移近移遠不指定落點；水、油、乾粉各有獨立 ScriptableObject 設定射程、擴散、弧線、空中時間及殘留時間，保留按住連噴與原分段裝備／接管控制。
- 新增有上限的飛行樣本、逐段球形碰撞與只讀 Particle View；鬆手、換工具、斷管後已噴出的內容繼續飛行。落地後由 FireField 計時，不依賴美術。水保留濕度、油可滑／燃燒且到期清除，粉末殘留僅呈現、不新增持續滅火。
- 審查修正近游標被舊 deadzone 強制改朝向、小 footprint 在格角無反應、密集碰撞查詢滿額漏近處物體。新增剖面牆專用遮擋，保留人物／家具真實碰撞及明確窗口開孔；噴嘴伸牆後也不生出穿牆水。
- 住宅實景第一次新牆阻回歸失敗：窗口框在外側，實際內牆距框 0.353m，原開孔深度不足。依量測擴充開孔深度並存為場景 override；沒有放寬窗口寬高、3m 管長或跳過滅火斷言。最終整關與重新 Play 通過。
- 依 Unity 實作／驗證流程保留歷史場景 opt-in 相容，只有 TutorialGreybox 啟用新模式。主代理檢查實際 diff、住宅噴射畫面及三類自由飛行圖；自動 Play Mode 40＋43＋25＋21＝129 項 PASS，Windows x64 Mono Development 建置 0 錯誤／0 警告。
- 445 檔修改前基線中 434 hash 不變，只變動 9 Runtime、1 Editor 驗證與 Tutorial 場景，無刪除；新增腳本／設定與 meta。既有模型、Prefab、GUID、Packages、ProjectSettings 保留。尚未做本輪實體鍵鼠手感／多人／效能量測；目前僅簡化點狀噴射視覺。
- 本機測試版：`Builds/DirectionalSpray/FireRescue.exe`。參數與限制：`Docs/DIRECTIONAL_SPRAY.md`；完整證據：`Docs/Validation/2026-10-26-directional-spray/RESULT.md`。未 commit／push／更新 README 或 GitHub Release，已發布下載版未覆蓋。

## 炎上消防隊 v0.1 — 2026-10-25（精簡F鍵說明）

- 依使用者要求，README與Release操作表刪除窗口固定／解除說明，F僅保留「在來源旁接管、改接或拔管」。本輪只改公開文字，不移除窗口機制或重建遊戲。

## 炎上消防隊 v0.1 — 2026-10-25（下載文字定稿）

- README與Release的「下載與執行」僅保留使用者提供的Windows下載／解壓／執行文字，移除其他開啟方式與補充句；操作表、遊戲與下載檔案不變。

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
