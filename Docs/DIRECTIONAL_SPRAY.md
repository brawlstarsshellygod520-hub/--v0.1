# 方向噴射：第一版調整入口

目前使用 `Assets/FireRescue/Scenes/TutorialGreybox.unity`。本次只改噴射與殘留，不改關卡、消防斧、NPC、接管流程；供應管仍限制 3m。GitHub 上的既有下載版未更新。

## 現在怎麼噴

- 滑鼠決定水平朝向，不選擇命中座標；指向同方向的近處／遠處不會改變射程。
- 原操作保留：右鍵先拿出、下一次接來源、之後點擊／按住噴射；鬆開停止發射。沒有新增長按時間上限。
- 每小份噴射物從噴嘴出發，沿自己的弧線飛行；只在實際撞到物體後反應。牆面會阻擋，不會直接在游標下滅火。
- 鬆手、換工具、斷管後，已噴出的內容仍會飛完。轉向、換來源只改新噴出的內容，不改空中既有內容。
- 目前是可調參數的遊戲化軌跡，不是真實流體／軟體物理。近、中、遠落點自動交錯，方便覆蓋沿途地面；不是每滴都落在最遠點。

## Inspector 可直接調整

選取 `Assets/FireRescue/Settings/Sprays/` 裡的 Water、Oil、DryPowder 資產。這些數值是起始手感，不是定案平衡。

| 設定 | 水 | 油 | 乾粉 |
|---|---:|---:|---:|
| 最近／最遠落點（從噴嘴計，m） | 1.5／5.5 | 0.8／3.2 | 1／3.5 |
| 最長空中時間（秒） | 0.65 | 1.1 | 0.85 |
| 地面保留時間（秒） | 6 | 18 | 4 |
| 水平擴散完整角度 | 4° | 12° | 26° |
| 落地作用半徑（m） | 0.85 | 0.65 | 1.1 |
| 額外弧線抬升（m） | 0 | 0.12 | 0.3 |

- `AirLifetime`：最遠噴射物的最長存活時間，近距離飛得較短；撞牆／地面／家具會提早結束。沒有碰撞面則到期消失，不憑空產生地面效果。
- `GroundLifetime`：每格的殘留時間。再次命中取「目前剩餘／新設定」較長者，並非逐次疊加秒數。改為 0 時仍能即時滅火，但不新增殘留；油為 0 則不留油。
- 水殘留影響既有濕度規則，油殘留沿用滑動／燃燒，油也可能先燒完。乾粉殘留目前僅供視覺，滅火在命中時發生，不額外持續傷害。
- `CollisionRadius` 是空中碰撞粗細，`ImpactRadius` 是落地影響範圍，兩者獨立。
- `PacketsPerSecond` 是每秒離散樣本數，不是流量。2026-10-27修正後，總發射遊戲量由來源`FlowRate × held時間`資助，分配到實際樣本；短按仍立即一小份，不再免費多一整份。Stop／換來源補一次不足份尾量；卡頓時間不再截成0.25秒。極端256存活份數已滿時以DroppedAmount記錄未發量，不回放到新來源。量測與限制見 `Docs/HOSE_FLUID_PHYSICS.md`。
- 來源的 `Spray Profile` 可換成另一份資產，方便日後同種水有不同壓力／範圍。不要在 Play Mode 中把暫時場景改動存回關卡。

### 效果強度（2026-10-26 第二輪）

每份 Profile 新增 `Effects`。三個倍率各自可調 0～4，現有三份資產都維持 1，因此預設玩法不變。這次只建立調校能力，沒有在未試玩的情況下改掉平衡。

| 欄位 | 作用 | 不會連動改變 |
|---|---|---|
| Extinguish Multiplier | 水／粉末每次命中的滅火量；0不直接降火，2為兩倍 | 推力、殘留量、射程、流量 |
| Push Multiplier | 水推動玩家／動態物件的力度 | 滅火量、殘留量；油／粉仍不推物 |
| Residue Multiplier | 落地增加的濕度／油量／粉末量 | 直接滅火量、推力、地面保留秒數 |

倍率不能改元素資格：水仍無法熄滅油鍋火、油不會變成滅火劑，粉末不增加濕度。油量變多仍可能影響原本的滑動／燃燒，濕度仍影響原本的復燃規則；「獨立」指直接命中量分開，而非移除既有元素反應。

`Residue Multiplier = 0` 不新增殘留，也不刷新現有殘留的剩餘時間，但仍可滅火／推物。數量仍有每格上限1，已飽和時倍率不會讓格子無限累加。滅火0與殘留0可獨立設定。

飛行時保存發射當下的 Effects 副本：修改 Profile 或換來源不會把已在空中的水突然改成另一種強度；新噴出的部分才套新值。缺少新欄位的舊 Profile、未指定 Profile 的來源，以及舊 API，都保留1倍。程式自行建立 `SprayEffectSettings` 時，用 `Neutral` 表示1倍；`default` 是全0，表示停用三種直接效果。

本輪驗證以 `SprayEffectTuningBuilder.BatchPrepareAndValidate` 與 `SprayEffectTuningValidation.BatchRun` 執行；證據在 `Validation/2026-10-26-spray-effect-tuning`。不操作 Unity 視窗，也不進行畫面或實體鍵鼠驗收。

## 程式邊界

`LocalPlayerInput` 產生方向參考點 → `SprayEmitter` 保存每份的元素／方向／設定並推進碰撞 → `ElementReaction` 套用物件或地板反應 → `FireField` 管理殘留計時。

`SprayParticleView`／`FireFieldView` 只呈現狀態；關閉或替換 Visual 不應停止核心模擬。每位玩家擁有獨立 emitter，最多 256 份存活樣本；可關閉 `AutomaticSimulation` 由外部固定時鐘驅動，尚未實作多人同步。

本次只將 TutorialGreybox 啟用方向模式；歷史場景／Prefab 保留舊點瞄準及舊反應 API。勿在同一場景混用新舊地面沉積 API：它們共用每格數量，新舊殘留並非獨立兩層。

`SprayWallOcclusion` 只引用住宅的 Gameplay 牆／門 Collider，在低位投影檢查完整牆面，避免弧線飛越剖面矮牆；噴嘴伸到牆另一側也會攔下。家具仍採實際 3D 碰撞，破門後停用的 Collider 不再擋水。窗口由 `HoseWindow` 的 `Spray Opening Center/Size` 定義可通過範圍，不依賴外框 Visual。這是單層住宅的簡化方案，不處理多樓層；新增牆或窗口時須在 Occlusion 的 Inspector 清單加入參照，安裝器不覆寫既有自訂設定。

地面採既有格子制：作用半徑內的格中心及直接命中格會受到影響，仍逐格檢查牆阻，不是精細連續液體表面。零殘留時間的水／粉仍會在命中時抑制火焰。

## 驗收入口

- `FireRescue.Editor.DirectionalSprayBuilder.BatchInstallAndValidate`：參照、存讀、冪等安裝、3m。
- `FireRescue.Editor.DirectionalSprayValidation.BatchRun`：方向、飛行、碰撞、殘留與生命週期。
- `FireRescue.Editor.ResidentialIntegrationValidation.BatchRunDirectional`：真實碰撞下整關與重新 Play。
- `FireRescue.Editor.DirectionalSprayBuilder.BatchBuildWindows`：另建 `Builds/DirectionalSpray/FireRescue.exe`，不覆蓋已發布版。

實際結果見 `Docs/Validation/2026-10-26-directional-spray/RESULT.md`。批次命令測試不是硬體鍵鼠手感驗收；下一輪應集中試調水的距離、速度與散佈，不擴充其他遊戲系統。
