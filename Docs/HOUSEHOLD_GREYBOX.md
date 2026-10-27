# 第一關住宅：簡單家具與物件輪廓

2026-10-27，依使用者室內配置圖補住宅佔位物件。不是正式美術或重新設計關卡。

## 本輪內容

| 區域 | 簡單 Primitive 物件 |
| --- | --- |
| 儲物間 | 櫃子、蘇打粉罐佔位 |
| 浴室 | 洗衣機、淋浴、洗手台；既有水源馬桶改用可替換外觀 |
| 臥室 | 床、枕頭、衣櫃、電腦桌、螢幕、椅子、窗框 |
| 廚房 | 冰箱、瓦斯爐、洗手台、L形組合流理臺／吧台、窗框 |
| 客廳／玄關 | 長沙發、兩張單椅、既有可推動茶几、電視與電視櫃、兩側矮櫃、後側櫃、回收桶、垃圾桶、鞋櫃、落地窗框 |
| 戶外 | 手工汽水攤佔位 |

25種可重用 Prefab 在 `Assets/FireRescue/Greybox/Household/`；家具生成配方在 `Editor/HouseholdPropCatalog.cs`。場景新增 `Step4 User Layout/Residential Household Greybox` 六個區域群組。舊床／沙發色塊停用保留，舊茶几仍為原可推動 Rigidbody，只有 Visual 換掉。

**這輪先依房間對應擺物件，不把示意圖當精準尺寸重建。** 來源、火點、NPC 出生點、可破壞門、3m管線與窗口路由仍維持前輪已驗證的位置；所以既有可操作油瓶／油鍋仍在客廳後側測試檯，而非圖中左側廚房爐具。新水槽、蘇打粉罐、汽水攤是外觀佔位，尚未賦予新來源／反應；新增窗框也不是新導管接點或可穿越洞口。這些玩法配置可下一輪依圖另調，不暗中改變本輪驗收流程。

## 靠近來源：物體輪廓取代地面圓圈

- 使用與接管完全相同的 `LocalPlayerInput.NearbySource`，同時只顯示最近且可接近的一件。
- 尚未連接：金色；已連接且仍在附近：藍色；離開原2m距離、牆阻、停用或切換來源後隱藏舊外框。
- 外框沿來源 `Visual` 裡各個網格的輪廓向外膨脹約3.5cm，**不是地面圓環或 Bounding Box**。原物件表面／材質保留，ZTest使不透明牆壁正常遮擋。
- `SourceHighlightView` 只管理顯示；`InteractionOutlineTarget` 快取展示網格，沒有複製 Collider／Source／接點。可讀Mesh會在副本平均同位置法線，改善Primitive硬邊；FBX不可讀Mesh安全共用GPU資料、不讀頂點、不破壞模型。未修改Packages、渲染管線或Project Settings。
- 支援多材質槽及基本SkinnedMesh／BlendShape同步；透明材質、非常凹陷／不封閉的正式模型需另做視覺驗收。不可讀且硬邊的模型仍可顯示，但若稜角不平順，可由美術輸出平滑法線或開啟Read/Write再重建展示快取。
- Shader在Scene明確序列化引用，避免只有Shader.Find導致Windows build剔除。外框不讓普通家具突然變成可互動物件。

## 之後收到正式模型怎麼替換

1. 保留物件Root的位置、碰撞、FluidSource／Rigidbody等玩法組件，以及 `Anchors` 裡的接點。
2. 只更換 `Visual` 底下網格／材質；腳底Y0、1單位1m。不要讓美術Prefab帶第二套Collider或玩法腳本。
3. 進入Play前換Visual不需改核心程式；若在執行時動態更換已選取來源的Visual，呼叫 `SourceHighlightView.Refresh(source)` 更新外框快取。
4. 若模型實際尺寸明顯改變，調Root的Collider與獨立接點，並重新驗收通道／3m可達性，不用修改噴射或元素反應程式。

`HouseholdPropCatalog.EnsureAll()`只補不存在的Prefab，`HouseholdLayoutSetup`重跑不覆寫已擺好的家具。狹窄通道需要留NPC實體半徑而不只看路徑格：本輪已將主沙發向後移0.35m、右單椅側移0.2m，解決新增家具導致的卡角，未改NPC程式。

## 驗證與交付

- Unity6000.5.0b9：保存重讀與冪等安裝PASS；家具／外框45項＋整關與重開43項＝**88項PASS**。舊NozzleControlsValidation的圓環斷言已改為對應外框斷言，但沒有重跑該歷史全套入口；不是把所有專案測試都算成通過。
- 看過Unity實際金色／藍色外框、房間與俯視配置圖。家具配置首次導致NPC卡角的失敗記錄與診斷保留；最終完整步行、三刀破門、水／乾粉、3m窗口、NPC單次逃生及重開全部通過。
- 本機Windows版：`Builds/HouseholdGreybox/FireRescue.exe`；建置證據見當輪build.txt。請保留旁邊Data、MonoBleedingEdge及DLL。不commit／不發布，不改公開README。
- 待真人確認：室內擺設／尺寸、外框粗細、接近來源時可讀性；不是正式美術或真人鍵鼠手感已驗收。
- 原始證據與狀態：`Docs/Validation/2026-10-27-household-outline/RESULT.md`。
