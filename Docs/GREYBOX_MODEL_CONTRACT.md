# Greybox：Gameplay 與模型替換合約

2026-10-20第四步更新：原九個Prefab不變；當前場景以override新增LocalPlayerInput、FirefighterTools（root）、FirefighterToolsView（Visual）。架構改用使用者戶型，MeshCollider在root，Visual／Collision為獨立Mesh。替換整個Player Visual時重新連ToolsView與HoseView，玩法不改；沒有View仍可命中／噴射。當前說明與驗收見 `Docs/USER_LEVEL_ONE_STEP4.md`。

2026-10-12，教學關卡第二步。Prefab 位於 `Assets/FireRescue/Greybox/Prefabs`。

2026-10-19 更新：第三步住宅已加入同一 Tutorial 場景，第二步組裝保留但停用。九個 Prefab 未修改；新牆／家具也沿用本文件的 Visual 邊界。當前地圖與相機驗證入口見 `Docs/GREYBOX_LAYOUT.md`。

## 分工

```text
Player / Source / Furniture / Resident / FireSystem / Exit
├─ Gameplay components     行為、資料、事件，掛在根節點
├─ Collider / Rigidbody    固定的玩法尺寸，掛在根節點
├─ Anchors                 噴嘴、互動點、來源出口、NPC 到達點
└─ Visual                  可關閉／替換
   ├─ Primitive 或正式模型  只負責外觀
   └─ Presentation         HoseView／FireFieldView／日後 Animator
```

Gameplay 可以參考同一玩法層的 Component／Anchor；Presentation 可以讀 Gameplay。Gameplay 不搜尋模型名稱、不使用 Renderer bounds 決定碰撞、距離、命中或尋路。Prefab 建立工具的物件名稱只供作者定位。

## 目前可重用的九個 Prefab

| Prefab | 保留在玩法層的內容 | 可替換的外觀 |
| --- | --- | --- |
| Player | CharacterController、Motor、HoseConnection、SprayEmitter、Nozzle／Interaction anchors | Capsule、頭盔、噴嘴模型、HoseView |
| Resident | CharacterController、RescueNpc，自動安全尋路／救援事件；無安撫前置條件 | Capsule；此版關閉 NPC 舊狀態染色 |
| WaterSource | Water 類型與流量、BoxCollider、Outlet／Interaction | Primitive 馬桶與水箱 |
| OilSource | Oil 類型與流量、BoxCollider、Outlet／Interaction | Primitive 沙拉油瓶 |
| Pushable | BoxCollider、Rigidbody，質量與平面移動限制 | Cube 小家具 |
| FixedObstacle | BoxCollider、FixedObstacle Layer | Cube 障礙物 |
| Ground | BoxCollider，地面頂端 y=0 | 地板 Cube |
| FireSystem | FireField、ElementReaction、RescueGameManager | 子物件 FireFieldView，序列化連結 root FireField |
| Exit | Anchors/Arrival，NPC 真正到達目標 | 地面色塊 |

來源連結仍以 Source root 做 2m 距離與 13m 管長計算；低位互動線使用獨立 Interaction anchors。噴射從 Nozzle anchor 出發。來源 Outlet 供管線呈現使用。全部接點都不掛在模型或骨架上。

既有住宅沒有新增 anchor 欄位的配置時，會使用原有位置公式，因此不用重存舊場景。舊家具工具依模型 bounds 產生碰撞的做法保留在歷史工具；Greybox 的碰撞尺寸是明確資料，不呼叫該工具。

## 如何替換模型

1. 在 Prefab Mode 開啟目標 Prefab，替換 `Visual` 裡的模型子物件。只調整模型自己的縮放／旋轉／偏移，讓它落在既有碰撞範圍內，Actor 腳底對齊 root 的 y=0。
2. 模型子樹不得含 Collider、Rigidbody、移動／元素／救援元件。保留 root、Anchors 及其序列化引用。這樣改模型大小、名稱或 pivot 都不會改物理判定。
3. 模型有 Animator 時關閉 Root Motion。日後可將既有 `CharacterLocomotionAnimation` 放在呈現層並連結 Actor root；移動由 Motor／NPC 掌管。揮砍判定日後也由玩法控制，不綁美術 Animation Event。
4. 換消防員模型時可保留 `Visual/Hose Presentation`。若整個 Visual 都換掉，要將 HoseView 的 emitter 指回 Player root；若換火焰呈現，FireFieldView 的 field 指回 FireSystem root。少了 View 只會少畫面，不會停掉規則。
5. 模型需要不同碰撞尺寸時，這是另一次玩法配置調整；不讓模型匯入器自動變更 Collider 或 Anchor。

火場網格的 Origin／Width／Height 在進 Play 前由關卡作者設定；這一步沒有製作 runtime 動態擴容或模型自動對齊系統。

## 教學場景的第二步測試組裝

`TutorialGreybox.unity` 新增 `Step2 Test Assembly`：12m 平台、一名玩家、一名 NPC、水箱、油瓶、可推家具、固定障礙、一個火場與出口。
它用真 Prefab 接好場景引用，以便之後建立教學關卡。建立時先停用組裝根節點，完成 FireField／NPC／Manager 等接線後才啟用，避免 OnEnable 早於配置。

此組裝尚未接上 LocalPlayerInput；可在 Scene View 檢查模型與引用。第三步已另建住宅配置並停用此測試組裝；WASD／左鍵斧／右鍵管仍屬第四步。

選單 `FireRescue > Tutorial > 建立第二步 Greybox 元件與測試組裝（僅首次）` 只建立缺少的 Prefab 與組裝；已存在時不覆寫手動調整。不要把 `CreateTestAssembly` 的測試擺設當成正式關卡格式，它是 Editor 組裝範例，場景執行不依賴 Editor 程式。

## 驗證方式

Unity `6000.5.0b9`，先關閉同專案的 Editor，再分別批次執行；不加 `-quit`，入口自行退出：

```text
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.GreyboxPrefabBuilder.BatchInstallAndValidate -logFile "D:\Documents\畢專\Logs\greybox-step2-setup.log"
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.GreyboxSeparationValidation.BatchRun -logFile "D:\Documents\畢專\Logs\greybox-step2-separation.log"
```

第一條驗證九個 Prefab 沒有把物理或核心引用放進 Visual；第二條在臨時場景，用原外觀、關閉外觀、替換外觀三種組裝，驗證實際移動、接來源、噴流、元素反應與自主救援。
第一條是第二步場景專用的歷史入口；它以啟用的 Step2 Test Assembly 驗收，不適用第三步已停用該組裝的場景。第三步使用 `TutorialGreyboxLayoutBuilder.BatchInstallAndValidate` 與 `TutorialGreyboxLayoutValidation.BatchRun`；第二條獨立臨時場景的分離驗證仍可使用。
報告放在 `Docs/Validation/2026-10-12-greybox-step2`。舊的 `TutorialGreyboxSetup.BatchCreateAndValidate` 是第一步「空場景」驗收，第二步之後使用上述入口。
