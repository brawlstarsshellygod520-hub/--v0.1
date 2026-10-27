# 第一關1026 戶型唯讀檢視

- 原檔：`D:/Downloads/第一關1026 (1).blend`
- SHA-256：`d68d113f535f89d4dc28246005413e4f14746c537dfea0921dd3321d44cf142c`
- 檢視方式：Blender 5.2.2 background、`--disable-autoexec`；沒有儲存或修改原 Blend。

## 結構摘要

- 40 個 mesh；總 bounds：`X -1.20119..-0.00473`、`Y -1.29064..0.00075`、`Z -0.00935..0.19871`。
- 單位為 Metric、scale length 1。門高約 `0.19` Blender units；若門高目標是 1.9 m，匯入玩法幾何時建議採 **10 Unity units / Blender unit**。
- `wall` 是一個合併的牆體 mesh，不可個別替換/隱藏單面牆；不要切割或刪改它。
- 有 5 扇獨立室門：`Door` 至 `Door.004`，各自也有 `DoorFrame`、把手及 `CTRL_Hole` 開口。
- 沒有名為 window/窗的獨立 mesh；窗洞/開口若存在，已合併於 `wall`。冰箱門因名稱含 door，已列在完整 inventory，但不是房門。
- 室內家具有 `kitchen counter`、`refrigerator`、`trash can` 等；本輪新關卡可停用，不應以它們作為 AI/碰撞來源。Blend 內也沒有獨立馬桶 mesh，因此保留現有遊戲生成的馬桶 Source。

## 舊戶型差異

舊 `level-one.geometry.json`：11 個幾何 mesh、5 個門、匯入 scale 14、cut height 0.8；平面範圍約 `17.03 × 18.88`。

新戶型：40 mesh（牆、門、框、把手與少量家具拆分）、原始平面約 `1.20 × 1.29`，依門高推算後約 `12.0 × 12.9 m`。這是不同幾何與比例，不能沿用舊戶型的門/房間座標。

## Unity 衍生幾何建議

為符合提供平面圖的「北上、南下、東右、主門東側、浴室在臥室左、廚房西南」閱讀方向，衍生匯入器可採：

`unity = (10 * blender.y, 10 * blender.z, -10 * blender.x) + levelOrigin`

這是**匯入/抽取幾何時的座標映射**，不要直接對來源 FBX/Prefab 套負縮放。主門、臥室、浴室、馬桶 Source、臥室火與 NPC 的最終 gameplay anchors 應在 Unity 場景依這個方向人工確認一次；Blend 沒有提供這些語意名稱，不能安全從物件名自動指派。

## 產物

- `level1026.inspection.json`：全部 mesh 的名稱、世界 bounds、材質、統計、相機/units、舊檔摘要。
- `mesh-inventory.csv`：可快速檢視全部 mesh。
- `level1026-top.png`、`level1026-angled.png`：俯視/斜俯視檢視。
- `level1026-unity-north-top.png`：依匯出 JSON 的 Unity 座標檢視；畫面上方固定為 `+Z`（北）、右側固定為 `+X`（東），橙色 `D1..D5` 為 DoorFrame 開口中心的 `(X,Z)`。

## Unity 方位核對

`level1026-unity-north-top.png` 已採用最終 X/Z 交換映射，畫面上方固定為北（`+Z`）、右側固定為東（`+X`）。這個映射同時使廚房位於西南，並把儲物間、浴室／臥室與入口的相對方位對齊使用者圖；不要再額外旋轉。

新 Blend 沒有臥室、浴室等語意名稱，且右側兩個房間的精確用途不能單靠 mesh 檔案保證；因此「浴室在臥室左」需由主場景匯入視覺後指定 gameplay anchor，不能以旋轉模型來猜測修正。
