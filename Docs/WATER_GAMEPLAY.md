# 住宅 Greybox：水機制

> 2026-10-24：現在Tutorial採白色3m管線與「右鍵拿出→接來源→再噴」；詳見`NOZZLE_CONTROLS.md`。下方早期F操作及13m路線屬歷史配置，不表示已用新距離走通全程。

日期：2026-10-21。這是遊戲機制測試，不是教學流程。
最新四個機制方向見 `RESIDENTIAL_GAMEPLAY_ROADMAP.md`；原始手繪圖保存在 `Design/2026-10-21-residential-layout.png`。

## 怎麼玩

開啟 `Assets/FireRescue/Scenes/TutorialGreybox.unity`，按 Play：

1. WASD 以畫面方向移動，從屋外穿過正門進主空間。
2. 去左側狹長浴室，靠近馬桶水箱按 **F** 接水。
3. 回主空間，把滑鼠指向火焰所在的地面，按住 **右鍵** 持續噴水；火會逐漸變弱直到熄滅。
4. 用水柱噴主空間前半部的方塊茶几，可把它推開。瞄準家具稍後方地面，讓水柱打到家具而非落在腳前。
5. 遠離來源按 F 可斷開；沒有來源時右鍵只拿出管子，不會出水。停止 Play 再 Play 會回到初始火與家具位置。

左鍵連續揮消防斧、E 安撫 NPC 保留；本輪尚無破門、教學進度或完成畫面。
`ResidentialFire` 是舊場景且仍使用左鍵噴射；Build Settings 沒有切換到 Greybox。

## 本輪配置

- 保留使用者 `.blend` 戶型、.8m 剖面牆、相機與九個可替換 Primitive Prefab 的結構；FireSystem Prefab 只新增保存水推力參數，其餘八個未改。
- 主空間增加三個相鄰普通火起始格：`(2.25,0,.75)`、`(2.75,0,.75)`、`(2.25,0,1.25)`。
- 茶几改放 `(3.5,0,-2.5)`，仍使用既有 Collider／Rigidbody；沒有在模型上增加玩法。
- 僅此場景把燃料消耗、自然減弱、蔓延速率各設定為 `.01`，提供進屋找水時間。火仍會蔓延，也會因燃料耗盡自然熄滅；沒有永久燃燒或教學條件控制。
- `Water Gameplay Test` 只含 Editor 用火點 Transform，不含教學 Component，也不顯示世界文字。

## 程式責任

- `SprayEmitter` 沿用最近碰撞、已連來源才噴射與流量計算。
- `ElementReaction` 負責牆壁可達性：打中 Layer 8 靜態固體時不把水花投影到其底下；直接打地面時，水花逐格檢查低位視線。Layer 8 的碰撞不能放進可替換 Visual。
- `FireField.ApplyWater` 保留三參數 API；新增四參數可達性篩選，不引用 Physics／Renderer／模型名稱。只處理範圍內的格子。
- 水推玩家規則保留。Rigidbody 推力改為可設定 `waterPushImpulsePerUnit=48`，維持質量、摩擦、碰撞及原力方向，避免原本每幀小推力推不動貼地家具。原油膜路徑未改；不代表油鍋／乾粉已實作。
- `WaterGameplayBuilder` 是一次性場景配置工具：已有 marker 就不覆寫，保留手動調整；沒有新增通用 Manager、套件或 Runtime 教學框架。

## 驗證入口

同專案 Unity 必須先關閉。使用 Unity `6000.5.0b9`：

```text
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.WaterGameplayBuilder.BatchInstallAndValidate -logFile "D:\Documents\畢專\Logs\water-gameplay-install.log"
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.WaterReactionValidation.BatchRun -logFile "D:\Documents\畢專\Logs\water-gameplay-reactions.log"
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.WaterGameplayValidation.BatchRun -logFile "D:\Documents\畢專\Logs\water-gameplay-scene.log"
```

不要加 `-quit`。每個入口等待匯入完成後自行退出；依序執行，不要同時開兩個 Unity 操作同一專案。
反應測試使用臨時場景，實景測試只在 Play Mode 操作，不保存 runtime 狀態。只有明確的 Builder 入口會配置並保存 Tutorial 場景。
結果與失敗證據在 `Validation/2026-10-21-water-gameplay`。

2026-10-21 最終驗證：安裝／重開／重跑不覆寫PASS；水反應25項、實景命令路徑27項、舊住宅40項，共92項Play Mode PASS。實景茶几被連續水柱推動約.99m，沒有改動其碰撞／質量／摩擦；5張實際Game Camera截圖已擷取，主要滅火／推物畫面已檢視。

## 已知限制

- 單人本機。測試命令與實際輸入共用邏輯，但不等於硬體鍵鼠實測或多人驗證。
- 管線仍是 13m 直線長度限制，視覺可能穿牆；這與本輪已處理的「噴射／水花撞牆」是不同問題。窗口傳管與轉角路由待後續。
- 來源無限，沒有容量或消防車供水。水花是平面格子反應，不是流體模擬。
- 低牆是實際碰撞高度；水柱若確實越過牆頂，屬於越過實體而非穿牆。
- 尚無乾粉、油鍋火種類、斧頭破門、氣體／通風、火傷／復活或教學 UI。
- 尚未製作 Windows Player build。本輪不重建地圖或正式美術。
