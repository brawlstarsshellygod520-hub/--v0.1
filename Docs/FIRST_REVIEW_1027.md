# 炎上消防隊：一審住宅展示版 v0.2.1（2026-10-28）

## 開啟

在 Windows 檔案總管開啟 `D:\Documents\畢專\Builds\Latest`，雙擊 `FireRescue.exe`。聊天內的檔案連結不會直接執行程式。請保留 exe 旁的 Data、MonoBleedingEdge 與所有 DLL。Unity 入口是 `Assets/FireRescue/Scenes/FirstReview1027.unity`；舊場景保留、不刪除。v0.2公開下載與操作見根目錄README，實際發布紀錄見PUBLIC_RELEASE.md。

## 展示玩法與操作

目前為五秒延燒v0.2.1版：直接出生客廳。保留倒數、NPC台詞與結果；安全可拿的布丁旁顯示金色輪廓與圓角E框，管長極限提示按E拔管。不恢復整排操作說明、價格或油水警告。失敗只顯示「按 R 即可重來」，無按鈕。庭院防繞路牆隱藏Renderer，碰撞仍在。

- WASD：螢幕相對移動。平常面向走路方向，真正噴射／揮斧才面向滑鼠。
- 右鍵第一次拿噴槍，靠近來源再按一次連接；之後按住右鍵持續噴射。接來源與噴射必須是不同次按壓。
- E：統一互動。優先拿放／交付；沒有物品互動時接管／拔管。管長仍 **10m**，計算整條繞行路徑，自動繞牆，取消F。
- 左鍵第一次拿斧，再點揮一刀；按住可連砍。臥室門三刀破壞，每刀只結算一次。
- 拿起布丁會自動收工具、拔管；靠近NPC按E交付。持物時先E放下才能用工具。
- R：重新開始，失敗／完成後同樣可用。

一分鐘從出生開始，**期限要求是交付正常布丁**。交付後倒數凍結，仍需油鍋熄滅、房門破開、路線安全，NPC實際走出出口才完成，不強迫熄滅遠處所有地面殘火。逾時不能補交。玩家踩到地面火會死亡、掉落持物並失敗；R重來。

開場鍋火沿相連地板擴張，**約5秒燒滿179個可燃走道格**；火苗採較疏的呈現，每個危險地面格仍保留暖色標記。庭院圍牆封住屋外捷徑，必須先清廚房路才能取得倉庫粉末。水可滅普通火，但鍋未熄時，1.55m內約.28秒會再被鍋熱點燃；不是鍋旁有水免疫格。水適合清出遠離鍋熱的通路，小蘇打粉則可較慢熄滅全部火種，並滅鍋停止復燃。水濕地約5秒，粉末殘留20秒抑制普通鄰火。

水直接打鍋仍會爆燃、燻黑布丁。黑布丁可交付，但NPC說「好難吃...」約1.4秒後中毒死亡；正常要求是「好想吃布丁...」。這是遊戲化反應，不作真實消防操作指引。

正常順序：客廳出生 → 接馬桶水清出通路，避開鍋熱區 → E拔管 → 倉庫接粉末 → 先清腳前路再前進，噴熄油鍋及布丁周圍 → E拔管 → E拿正常布丁帶到臥室門前 → E放下 → 三刀破門 → E再拿起 → 交付NPC → 她直接走正門逃出。最新實測時間與結果以v021/RESULT為準，真人初見難度尚需驗收。

廚房東側仍是牆，要走南側開口。鼠標只決定方向、不決定命中距離；水最遠初速10／重力16，近中程初速仍1.4/2.8/4.2/5.6，粉末6／8、仰角18°。落點半徑仍水.9／粉1.35，保持噴射數秒清路，不是點擊即熄。水包60/s、可視倍率1.65，不增加流量。用粉打鍋也能讓粉雲沉降到鍋旁地面，仍受牆阻擋。

## 模型來源與替換

新戶型原檔備份 `ArtSource/UserProvided/Level1027/Source.blend`，SHA256 `30a764941f7df43e12341048c539cead3ffaece8a82d1882ec6f7226342ecdff`。Downloads 原檔與備份不回寫；幾何匯出、低牆展示與基本材質為衍生資產，放在 `Art/UserProvided/Level1027`。

最新正式道具來自同名新檔的唯讀快照 `ArtSource/UserProvided/Level1027/PropsUpdate20261028/Source.blend`，SHA256 `7f7c3bb7170a0787c79d2bc4e9a1c29425a1523a45db8c187e1524c295229504`。戶型沒有改變。馬桶、倉庫粉袋、油鍋、盤裝布丁均已依你的原位和比例替換，不再使用臨時四件。只加基本色，水／油單面法線僅於Unity派生網格修正；原檔未回寫。沒有加入生成家具。離屋樣品、兩個巨型測試立方體保留在來源／JSON，不放入可玩場景。

正式道具的 Mesh／材質／4份純外觀 Prefab 集中 `Art/UserProvided/Level1027/Props20261028`；歷史 AIGenerated 資產保留。這次只替換各 Root 下的 **Visual** 並對齊獨立接點／碰撞，保留：

| Root | 必須保留的玩法／接點 |
|---|---|
| Toilet Water Source | FluidSource、Outlet、InteractionPoint、水 Profile |
| Soda Powder Source | FluidSource(DryPowder)、Outlet、蘇打粉 Profile |
| Oil Pan Fire | ObjectFire、碰撞、OilPanFireView（可另換 VFX，不重複套反應） |
| Pudding Item | CarryItem(itemId=pudding)、物品碰撞；原點在盤裝布丁的整體中央 |
| User House 1027/door.002 | BreakableDoor、BoxCollider、BreakableDoorView；Visual 與門把一起替換 |

請提供模型的尺度與正面方向；來源另標接管口、噴槍另標手握點／出口／入管口。這些是模型對齊工作，不需要改反應、倒數或 NPC 核心程式。

角色保留使用者 Role2 的原始幾何、材質與骨架，新的四份皮膚權重位於 `Art/UserProvided/Characters/Rigged/Review1027`，不歸 AI 原創。改進包括：依實際位移計算短步相位、站立腳 IK、起停混合、轉向重心、穩定持槍／持斧、斧刃外傾避開大頭。玩家以新程序姿勢驅動，舊 Animator 與舊 locomotion sampling 在新 Scene 停用，避免兩套同時搶骨架；NPC 沿用既有 Idle／Walk。沒有 Root Motion 移動角色。

## 程式責任

- ObjectFire：新關 opt-in 遇水增火（0.65/unit，上限3）；乾粉降低強度。其他場景預設不增火。
- CarryItem／PlayerCarryInteraction：單件物品所有權、距離及隔牆檢查、拿放交付。附到 100x FBX 骨頭時保留世界尺寸。
- NpcItemRequest：布丁、門、油鍋條件；不是安撫系統。舊 NPC 未設定此元件仍只看安全路線。
- FirstReviewMission：60秒要求期限、失敗／撤離／完成狀態；ReviewMissionHud 只讀狀態。
- FirefighterCartoonMotion：唯讀移動、程序骨架姿勢與視覺工具姿勢，不控制碰撞、命中時機或水的反應。
- ReviewSkinRefinement：編輯器衍生蒙皮權重，修衣襬／飾條被綁到腿與前臂未分配權重，頂點與三角面不重建。
- FireField：每格普通地面火與濕地權威，獨立於油鍋強度；CookingOilArea在本Scene停用。
- CookingOilIncident：依相連格距離在5秒內覆蓋廚房、鍋熱範圍快速復燃、遇水爆燃一次與食物Scorch，不以VFX判定命中。
- PlayerFireContact：帶障礙LOS的足部掃掠、踩火死亡；ActorDefeatView／BurntFoodView只呈現倒地與焦黑。
- PhysicalHoseTube：單一連續八邊形3D網格、固定128粒子；重力／距離上限／彎曲平滑及地板／固定物碰撞。只有兩端升起；HoseConnection是10m／繞牆權威，不是完整軟管PhysX。
- NpcRequestNameTag：NPC頭頂投影標籤，NPC逃出或不在畫面時不常駐HUD。

ReviewMissionHud顯示時間、結果與失敗重開按鈕；PickupContextView只呈現可拿布丁的E框與拉滿管線的F提示，不讀鍵盤或控制玩法。提示與實際E共用候選物件，不會顯示可拿卻拿不到的另一套距離規則。

## 驗證與限制

最新證據 `Docs/Validation/2026-10-28-restart-release/RESULT.md`：79項整關、Windows28項載入、建置0錯誤0警告；上一輪40項噴射回歸保留於kitchen-hints，本輪未改噴射核心。正常成功路線使用PointerCommand、CharacterController、飛行噴射、真門碰撞與NPC路徑，不傳送／直接刪火。毒性與單獨水打鍋分支有隔離前提；真人鍵鼠手感、60秒初見難度、E／F框字級位置與按鈕實體點擊仍需驗收。

火與水仍是有限樣本遊戲模擬，不是CFD／完整軟管剛體。普通地面火有獨立格子狀態，滅鍋不會直接刪掉全部地面火；目前只在廚房指定走道蔓延，沒有油滴濺射全屋。玩家有即死接觸規則，不是完整生命值／受傷系統。其他火點與完整家具未加入。

移動使用程序Idle／短步跑走／轉向，沒有新增Shift衝刺鍵。直線站立腳量測不能代替所有急轉與持械穿插的真人動畫審查。維持不做螢幕控制；背景黑圖不算UI驗收，仍需人工確認台詞位置、操控手感及噴射站位。

**蘇打粉接消防管是虛構遊戲設定，不是現實操作教學。現實油鍋火不可潑水。** 安全參考：[臺北市政府消防局烹飪火災說明](https://fsm.119.gov.taipei/w/fsm119en/Fire_21103115123848522)。動作美術方向參考 [Overcooked! 2 官方展示](https://www.team17.com/games/overcooked-2)，未取用其模型或動畫。
