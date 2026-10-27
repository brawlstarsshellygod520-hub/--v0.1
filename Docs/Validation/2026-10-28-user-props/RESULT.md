# 正式道具替換驗證 — 2026-10-28

狀態：通過，仍需真人手感驗收。無螢幕控制、無發布。

## 變更與來源

- 來源：D:/Downloads/第一關1027.blend；唯讀快照 ArtSource/UserProvided/Level1027/PropsUpdate20261028/Source.blend。
- SHA256：7f7c3bb7170a0787c79d2bc4e9a1c29425a1523a45db8c187e1524c295229504。原歷史Source未覆寫。
- 原戶型頂點及三角面逐項相等；只替換四組Visual，保持來源的世界位置與比例。面向下的水／油派生面修正，保留Blender corner normals。相應Collider、接管口、火點、布丁拿取中心對齊。
- 資產：Assets/FireRescue/Art/UserProvided/Level1027/Props20261028。原生成模型不再顯示，但資產不刪除。
- 活躍Scene：Assets/FireRescue/Scenes/FirstReview1027.unity。其餘玩法、角色、地板、相機及15m上限不變。

## 證據

| 驗證 | 結果 | 檔案 |
|---|---|---|
| 原戶型相等、四組引用、保存重開 | PASS | import.txt |
| 真CharacterController／pointer commands整關 | 85 PASS | playmode.txt、trace.txt |
| 新水箱連接→繞牆→水增火→換粉滅火 | PASS | playmode.txt |
| 2秒熱區禁拿→三刀破門→新布丁拿取／交手／NPC自行走出 | PASS | playmode.txt |
| 60秒邊界／超時拒絕／完成一次／重新Play乾淨 | PASS | playmode.txt |
| Windows x64 Development／Mono | 0錯誤、0警告 | build.txt |
| 真Windows D3D11載入及四新模型／2秒熱區 | 19 PASS | player-smoke.txt |
| 首次Windows -batchmode擷取 | FAIL（無法擷取畫面） | player-smoke-batchmode.txt、根目錄user-props-player.log |
| 同一包正常圖形模式Hidden重測 | PASS；新檔截圖仍黑，不算UI驗收 | player-smoke.txt、根目錄user-props-player-final.log |
| 離線實際Unity RenderTexture人工查看 | 四件道具與關卡擺位已查看 | 四張道具PNG、01-layout.png、02-water-mistake.png、pudding-delivered.png |

新背景整關交付時約剩26.5秒，不代表真人第一次玩也能達成。離線Camera.Render不含OnGUI，不宣稱本輪完成標籤／HUD實際畫面驗收。馬桶低角度水面會受碗身遮擋，未為露出水面更動用戶模型。

## 執行與下一步

檔案總管前往 D:/Documents/畢專/Builds/Latest，執行 FireRescue.exe。請驗收兩個來源提示與接管位置、鍋邊命中距離、布丁E拿取及交付手感。現有一分鐘與15m不放寬；沒有commit／push／GitHub更新。
