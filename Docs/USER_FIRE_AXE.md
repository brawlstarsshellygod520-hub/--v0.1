# 使用者消防斧：模型接入

日期：2026-10-21。這輪只替換工具外觀，不改揮砍、噴射或命中規則；不表示斧頭破門已完成。

## 來源與邊界

- 原檔：`D:/Downloads/消防斧.blend`，保持不變。
- 來源副本：`ArtSource/FireAxe/消防斧.blend`；Unity 只讀匯出的 FBX，不自動執行 Blender 匯入。
- 原模型是同一把斧頭的六個拆件；保留柄部 Skin／Subdivision 的評估結果與材質，不用 Primitive 重做外形。
- 遊戲尺寸目標全長約 0.72m，握點為局部原點，柄尾低於握點 0.12m；長軸 +Y、斧刃朝角色前方 +Z。
- 模型與碰撞／輸入分離。模型不得帶 Collider、Rigidbody 或玩法 MonoBehaviour；命中仍由 Player root 上的 `FirefighterTools` 決定。

## 場景接線

場景：`Assets/FireRescue/Scenes/TutorialGreybox.unity`。

`TutorialGreybox/Step4 User Layout/Gameplay/Player/Visual/Axe Pivot/Fire Axe Model`

- 模型 Prefab：`Assets/FireRescue/Art/UserFireAxe/FireAxe.prefab`。
- 保留原 `Axe Pivot` 的位置、引用及揮動方式。
- 舊 `Handle`／`Head` 只停用，不刪除。要退回 Greybox，停用 `Fire Axe Model`，重新啟用這兩個子物件即可；不要移除 pivot 或工具邏輯。
- 左鍵拿出斧頭並持續揮砍；鬆開停止但保持裝備。右鍵切消防管，雙鍵右鍵優先。操作規則不變。

## 重建與驗證入口

- 匯出腳本：`ArtSource/Tools/export_fire_axe.py`，以 Blender background／`--disable-autoexec` 執行；輸出到 `ArtSource/FireAxe`，不保存來源 blend。
- `UserAxeSetup.BatchInstallAndValidate`：匯入、保存、重開，檢查既有玩法序列化不變及重跑不覆寫。
- `UserAxeValidation.BatchRun`：實際 Play Mode 左鍵命中、連續揮砍、右鍵切管、關閉模型後仍可命中；包含原遊戲鏡頭和只供 QA 的暫時放大截圖。
- 安裝工具遇到已存在 `Fire Axe Model` 就保留它，不覆寫使用者自訂。若要更新模型，先更新 FBX 再檢查尺寸、材質與握點；不要刪除 `.meta`。
- 本輪實際結果以 `Docs/Validation/2026-10-21-axe-powder/RESULT.md` 為準；僅有腳本不代表驗收通過。

## 尚未處理

- 沒有門耐久／破壞、傷害或新角色握持動畫。
- 使用者接續要求繼續下一步後，同輪加入油鍋／乾粉機制，來源先用家用乾粉滅火器；見 `Docs/POWDER_GAMEPLAY.md`。不因匯入斧頭而跳過窗口管線直接新增破門。
- 未改 Build Settings；測試時直接開上方 TutorialGreybox 場景，不是舊 ResidentialFire。
