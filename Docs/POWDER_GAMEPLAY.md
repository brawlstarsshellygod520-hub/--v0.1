# 住宅油鍋／乾粉玩法

> 2026-10-24：目前Tutorial的裝備／接管／噴射改為分開點擊，管長3m，詳見`NOZZLE_CONTROLS.md`。本頁原反應規則保留；早期操作測試不是新點擊語意的驗證證據。

更新：2026-10-21。先做機制，沒有教學 UI、順序鎖定或過關步驟。

## 如何操作

1. 開 `Assets/FireRescue/Scenes/TutorialGreybox.unity`，按 Play。
2. WASD 依畫面方向移動。到馬桶水箱附近按 F 接水，右鍵噴水可以滅主空間的普通地面火。
3. 主空間後方流理台右端有一口著火的鍋子。滑鼠指著鍋子、按右鍵：水會撞到鍋子，但不減少油鍋火強度。
4. 往流理台右側找紅色白帶的家用乾粉滅火器，靠近按 F，管線改接乾粉來源。
5. 回到鍋子前面，滑鼠指鍋子並持續按右鍵。淺白色乾粉噴流會逐漸縮小火焰，直至熄滅。中途放開立即停止。
6. 左鍵仍可拿出使用者提供的消防斧並揮砍；右鍵切回管子，雙鍵右鍵優先。遠離來源後按 F 可解接；沒接來源不會噴出元素。

乾粉每單位降低 0.5 火強度；來源流量 2，持續直接命中強度 1 的鍋火約需 1 秒。這是可調的原型遊戲規則，不是現實消防操作建議。

## 本版規則

- 水能滅普通地面火／普通物件火，但不能滅油鍋火。
- 乾粉能降低油鍋火與普通火；不生成濕地或油地、不施加水的推力。
- 乾粉與水一樣有最先碰撞阻擋；地面範圍反應也不能穿過牆壁。
- 鍋火完全熄滅後不自動復燃；沒有加入噴水爆炸、油鍋擴散到地板、來源容量或補充。
- 地板 `FireField` 原有燃料／油／水分與蔓延仍運作；獨立鍋火沒有加入 NPC 尋路格或額外任務完成條件，鍋本身在不可行走的流理台上。

## 替換模型不改玩法

- `ElementKind` 明確保留 Water=0、Oil=1，追加 DryPowder=2。
- `FluidSource` 仍提供種類、流量與接點；每名玩家各自持有 `HoseConnection`，沒有新單例或全域玩家狀態。
- `ElementReaction` 由實際噴流的最近碰撞找 `ElementReceiver`。物件反應不投影到下方地板；沒有物件接收器時才處理 `FireField`。
- `ObjectFire` 保存火種類／強度，不依賴 Renderer、模型名、動畫事件或物理；`ObjectFireView` 只根據強度顯示火焰。
- `OilPan.prefab` 的碰撞／反應在 root；鍋身與火焰在 `Visual`。`DryPowderSource.prefab` 的來源與碰撞在 root、接點在 `Anchors`、模型在 `Visual`。兩者都可直接換 `Visual` 子物件。
- `LocalPlayerInput.ResolveAimPoint` 讓滑鼠可直接指向檯面上的接收器碰撞區域；其他位置仍維持地面瞄準。未加入鎖定目標或 UI。

資產在 `Assets/FireRescue/Greybox/Powder`；場景配置在 `Step4 User Layout/Powder Gameplay Test`。`PowderGameplayBuilder` 只首次新增，重跑不覆寫。水箱、油瓶、舊場景與其 Prefab 保留。

## 驗證與下一步

真 Unity 結果、失敗／修正紀錄與截圖集中於 `Docs/Validation/2026-10-21-axe-powder`，以 `RESULT.md` 為準。
接續應做窗口傳管，再做斧頭破門；目前供應管線仍是 13m 直線限制，可能穿牆。這次沒有把管線穿牆誤稱為窗口路由完成。
