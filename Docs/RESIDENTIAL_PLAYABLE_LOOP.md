# 住宅 Greybox 整關測試（2026-10-25）

**目前統一入口：**專案根目錄 `PlayLatest.cmd` → `Builds/Latest/FireRescue.exe`。預設建置已改為TutorialGreybox，Player Prefab／Scene都是5m，NPC Prefab／Scene都套色。模型新分類及交付差異見 `Docs/ASSET_CLASSIFICATION.md`，舊版本路徑只作歷史保留。

**2026-10-27目前版本：**管長已改為 **5m**，茶几兩側單椅改朝內、待救者已套橘黃色參考配色。最新本機版 `Builds/ResidentColor5m/FireRescue.exe`，原操作與來源／火點位置不變。後文3m與舊玩家版路徑為歷史紀錄；最新證據 `Docs/Validation/2026-10-27-resident-color-5m/RESULT.md`。

2026-10-27最新修正見 `Docs/HOSE_FLUID_PHYSICS.md`：本機版改為 `Builds/HoseFluidPhysics/FireRescue.exe`；下方6.5m舊噴射距離已由各來源Profile取代。3m管長不變，新增沿線牆阻；窗口固定點已移到室內窗緣，原操作與整關流程保留。

目前場景：`Assets/FireRescue/Scenes/TutorialGreybox.unity`。
Windows 執行檔：`Builds/ResidentialPrototype/FireRescue.exe`；請保留旁邊的 Data、DLL 等完整資料夾。

## 目前操作

- WASD：螢幕方向移動；滑鼠瞄準。
- 左鍵第一下拿出斧頭；下一下揮一刀；按住連砍。浴室門三刀破，短按也會完整揮完一刀。
- 右鍵第一下拿出噴槍；靠近來源再按一下接上；下一下／按住噴射。
- F：來源旁接／換／拔管。已接管且窗口比「目前來源」更近時，F 固定窗口，再 F 解除固定而保留來源。靠近不同來源仍優先換接。
- 管長仍為 **3m**，噴射距離仍為 **6.5m**。兩者不是同一個限制。
- NPC 不需安撫、不需 E；有完整安全路徑就自己走。玩家身體仍能擋路，請讓開門口。

## 可玩的流程

1. 出生在正門外，進屋後走到左側浴室門。左鍵拿斧，再砍三刀；完整門片消失、碰撞開通。
2. 走進浴室，靠近馬桶水箱，用兩次分開的右鍵拿出噴槍、接水。
3. 馬桶本體是實體障礙，從南側繞到西側噴地板火。清出路後，浴室西側的測試 NPC 會經破門、正門自行離開。
4. 維持接水，靠近浴室北牆青色窗框，按 F 固定白管。站在窗口內側噴外面的木塊火；路由總長約 2.87m。再 F 解除窗口。
5. 拉回浴室門口，向主室地面火噴水。門口仍在水箱 3m 可達範圍內。
6. 油鍋不能用水熄滅。回水箱旁 F 拔管，再到主室北側紅色家用乾粉來源旁，右鍵接上，對油鍋持續噴射。
7. NPC 抵達出口後消失，救援事件只發一次。所有地板火、窗外物件火、油鍋火都滅掉後，`GameManager.LevelComplete` 為 true；本輪刻意不做過關 UI。

停止再 Play 即重置。Windows 版關閉後重開；沒有主選單／重開按鈕。

## 配置與責任邊界

- 原 `第一關.blend` 及匯入網格不改。原浴室開門葉 `Imported Architecture/Door.004` 停用保留，新增獨立耐久門；不重匯出戶型。
- 水箱位置不變；窗口移至 `(-5.2, 0, 3.6)`、窗外目標移至 `(-5.2, 0, 4.8)`，NPC 暫放浴室西側 `(-6.25, 0, 2.25)`，便於這版短管線驗收。這不是正式房間用途或教學順序。
- `AxeDamageReceiver` 管耐久、`BreakableDoor` 管明確指定的阻擋碰撞；`BreakableDoorView` 只顯示完整／破壞模型。替換門時只換 `Visual`，不要把 Collider／Gameplay root 一起刪除。
- NPC 沿用既有四方向尋路；除了地板火和 Layer 8 障礙，也檢查明確配置的 `ObjectFire[]`。物件火的危險半徑在 ObjectFire 設定，不讀火焰 Renderer。
- GameManager 明確接收兩個 ObjectFire，不全場搜尋。重複／空引用不重計；停用 Visual 不影響統計。停用整個 ObjectFire gameplay 元件／root 則代表將該火點排除當局。
- `RescueComplete` 只表示居民逃出；`AllFiresOut` 包含地板＋物件火；`LevelComplete` 為兩者同時成立。沒有強迫 NPC 等全部火熄滅。
- 安裝器只首次遷移，已有 `Residential Integration v1` 標記就不覆寫手動配置；舊場景與 Prefab 不批次改造。

## 目前驗證入口

- `FireRescue.Editor.ResidentialIntegrationBuilder.BatchInstallAndValidate`
- `FireRescue.Editor.ResidentialIntegrationValidation.BatchRun`：當前場景全程步行／左右鍵命令、實際碰撞與噴流、再退出並重新 Play。
- `FireRescue.Editor.BreakableDoorValidation.BatchRun`：工具命中、牆遮擋、門 Visual 分離、NPC 物件火中斷恢復、統計與事件去重。
- `FireRescue.Editor.ResidentialIntegrationBuilder.BatchBuildWindows`：只建目前 TutorialGreybox，不覆寫全域 Build Settings。

歷史 Water/Window/Nozzle 場景驗收含舊位置與舊 13m／西窗不可達假設，不應直接當成這版整關測試。最新證據見 `Validation/2026-10-25-residential-integration/RESULT.md`。

## 仍保留的 Prototype 限制

單人本機、來源無限、手動單窗口路由。消防管尚無整條碰撞／自動繞牆；其他牆角可能穿管。沒有教學 UI、多人、氣體／通風、火傷／復活、正式模型或音效。
這些是遊戲規則，不是現實消防操作教學。
