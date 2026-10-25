# 住宅水機制驗證 — 2026-10-21

狀態：**Ready with limitations**。僅水機制與可玩測試配置；不代表完整教學、乾粉、傳管或破門已完成。

## 環境與範圍

- Unity 6000.5.0b9，Windows Editor batchmode，實際進入 Play Mode，沒有使用 Unity MCP 或新增套件。
- 入口場景：`Assets/FireRescue/Scenes/TutorialGreybox.unity`，啟用使用者 `Step4 User Layout`。
- 本輪依最新手繪圖先做 Gameplay，沒有教學狀態機、提示、UI 或順序鎖定。
- 工作樹原本就有未提交修改／歷史刪除。本輪沒有 reset、clean、commit 或 push。

## 最終結果

| 驗收 | 證據 | 結果 |
| --- | --- | --- |
| Scene／Prefab保存後讀回；重跑不覆寫 | `install.txt`，水推力48、3個可燃初始格、家具位置、場景參數 | PASS，exit0 |
| 水反應／無來源／牆阻／外觀分離 | `water-reactions.txt`，25項 | PASS，exit0 |
| 真正從屋外進屋、接水、拉管、噴水滅火、推家具、停噴／斷接 | `playmode.txt`，27項 | PASS，exit0 |
| 舊住宅共享核心回歸 | `legacy-residential.txt`，40項 | PASS，exit0 |
| 既有資產保護 | 對照前一回合建立的 `../2026-10-20-greybox-step5/baseline.json` | 364檔中360檔hash相同；只有下列4檔變更 |
| 新資產meta、Console、實際畫面 | Missing Meta=0；最終無C# error／runtime exception；主要Game Camera已檢視 | PASS |

合計 **92項 Play Mode 檢查**。不是只有靜態檢查或編譯。
新增測試的 `Rigidbody.velocity` 警告已改成 `linearVelocity` 後重編與重跑水反應；其他既有測試的CS0618警告不在本輪改動範圍。

### 具體量測

- 水反應獨立夾具：貼地3kg Rigidbody以60Hz持續噴射，x從103.0000移到103.4441，保留原質量／摩擦／碰撞。
- 實際住宅茶几：(3.500,0,-2.500) → (4.493,0,-2.473)，約.99m。測試先確認水撞到桌體，再觀察固定物理步；沒有直接傳送家具或呼叫AddForce代替操作。
- 實景FireField保持Simulate=true；用enabled LocalPlayerInput的TickCommand驅動，未直接ApplyWater。來源來自真正馬桶水箱，原始3火種及延燒全數熄滅。
- 牆阻測試則關閉自然火模擬，以精確比較牆後火強度／濕度不變，排除自然減弱造成假陽性。

## 修改與diff

既有基線僅4個資產變更：

- `Runtime/Reaction/ElementReaction.cs`：可達性與可設定推力48；未知元素不再默認當水。
- `Runtime/Fire/FireField.cs`：保留三參數ApplyWater；新增可達性篩選版本，不引用Physics或呈現。
- `Scenes/TutorialGreybox.unity`：3火種、3個慢速參數、茶几位置、4個純Transform配置標記。未重建戶型、相機或角色。
- `Greybox/Prefabs/FireSystem.prefab`：diff僅新增 `waterPushImpulsePerUnit: 48` 一行。

另外新增WaterGameplayBuilder／WaterReactionValidation／WaterGameplayValidation及其meta，更新工作紀錄與最新方向文件，保存使用者手繪參考圖。所有其他Prefab、原住宅場景、正式模型、Packages、Settings、既有GUID不變。沒有碰撞或玩法搬進Visual。

## 失敗證據與修正

- `baseline-wall-leak.txt`：原邏輯通過15項後，薄牆後火被水花影響，FAIL；本輪修正後同一斷言及近牆地面水花均PASS。
- `baseline-ground-push.txt`／`ground-push-failure.png`：原住宅連續噴水可滅火，但推不動貼地家具。未縮小位移門檻，改為可配置推力。
- `push-calibration.txt`：同條件只改推力係數24／36／48／72，比較位移，選48。暫時量測程式已移除，正式斷言保留。
- `prefab-default-cache-failure.txt`：修改欄位初始化值後舊Prefab仍讀到18；改為正式保存Prefab參數，再驗證重開，未靠修改測試runtime值通過。
- `fixture-name-failure.txt`：測試找Prefab實例漏了(Clone)，已修正夾具。自製IEnumerator driver以Time.fixedTime等待真物理步，不把WaitForFixedUpdate誤當一般下一幀。

## 命令與畫面

可重跑命令見 `Docs/WATER_GAMEPLAY.md`。全部使用 `D:/unity/6000.5.0b9/Editor/Unity.exe`，`-batchmode -projectPath "D:/Documents/畢專"`，不加 `-quit`：

| executeMethod | 最終log |
| --- | --- |
| FireRescue.Editor.WaterGameplayBuilder.BatchInstallAndValidate | Logs/water-gameplay-install-final.log |
| FireRescue.Editor.WaterReactionValidation.BatchRun | Logs/water-gameplay-reactions-final.log |
| FireRescue.Editor.WaterGameplayValidation.BatchRun | Logs/water-gameplay-scene-final.log |
| FireRescue.Editor.ResidentialPrototypeValidation.Run | Logs/water-gameplay-legacy.log |

5張正常實測畫面：`01-before-water.png`、`01b-fire-before-spray.png`、`02-during-water.png`、`03-after-extinguish.png`、`04-water-pushed-table.png`。另留早期失敗畫面；不將其當最終結果。

## 未驗證與下一步

- 未做硬體鍵鼠測試、Windows Player build、多人或效能預算量測。
- 此處是普通地面火；沒有油鍋火種類或乾粉，不可宣稱目前水油規則符合最新油鍋設計。
- 來源無限，管線仍13m直線長度、可穿牆；噴流／水花遮擋已測，窗口傳管／轉角路由未做。
- 斧頭只有揮砍與命中，沒有破門；沒有氣體／通風、火傷／復活或教學UI。
- 手動下一步：開TutorialGreybox按Play，F接馬桶水箱，RMB朝火的地面噴射，再試推茶几。Build Settings仍指向舊ResidentialFire。
- 後續實作依 `Docs/RESIDENTIAL_GAMEPLAY_ROADMAP.md`，先決定油鍋／乾粉的來源物件與反應規則，不新增舊版油教學。
