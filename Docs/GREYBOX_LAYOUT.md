# 教學 Greybox 第三步：住宅配置

2026-10-20：本文件保留第三步歷史配置。當前Tutorial已改用使用者第一關.blend，原Step3根節點停用；請改看 `Docs/USER_LEVEL_ONE_STEP4.md`，不要用舊第三步驗收檢查新戶型。

2026-10-19。只處理地圖、物理通行及相機；不提前加入第四步裝置輸入或工具切換。

## 開啟與修改

- 場景：`Assets/FireRescue/Scenes/TutorialGreybox.unity`。
- 新地圖：`TutorialGreybox/Step3 Residential Layout`。
- `Step2 Test Assembly` 保留但停用，不要與新地圖同時啟用。
- 第三步建立選單：`FireRescue > Tutorial > 建立第三步住宅 Greybox 配置（僅首次）`。已有配置時只開啟、不重建，保留手動修改。
- 原 `ResidentialFire`、`FireRoom`、九個 Greybox Prefab 與 Build Settings 不需重建。

## 空間與尺度

住宅範圍 x = -8～8、z = -5～9（16 × 14 公尺）。後排三臥室，中央走廊，前左客廳／開放廚房，前右浴室，正門通往屋外出生點與出口。

| 項目 | 尺度／位置 |
| --- | --- |
| 角色 | 約 1.6m 高；CharacterController 半徑 .28m |
| 牆 | .72m 高、.32m 厚；不透明、始終可見且保留碰撞 |
| 走廊 | 兩牆內緣淨寬 2.08m |
| 正門／臥室門／浴室門 | 1.8m 開口；此階段沒有開關門行為 |
| 客廳通走廊 | 2.2m 開口 |
| 馬桶水箱 | (6.5, 0, -3.5)，從浴室內靠近 |
| 沙拉油瓶 | 廚房流理台上；來源 root 留在地面，Visual／Outlet／實體瓶身提高到檯面 |
| 出生／出口 | (.5, 0, -7)／(.5, 0, -7.5) |
| NPC | 第二間臥室 (.5, 0, 5.5) |

房間、地面區塊、床、沙發、桌、廚房流理台、衛浴皆為簡單 Primitive。房間標記只在 Hierarchy，Game View 無常駐世界文字。

## 玩法／外觀合約

沿用第二步的 `Visual` 分離：新增牆與家具的 BoxCollider 在玩法 root，渲染模型在 `Visual`，不靠 Renderer bounds 決定物理尺寸。相機追蹤 Player 玩法 root，不追蹤可替換模型。

復用 `RescueCamera`：固定俯角 50°、水平角 40°，LateUpdate 以 .18s SmoothDamp 跟隨，正交 Size 4.5。沒有玩家旋轉相機輸入，也不做整面牆隱藏。

火場與 NPC 接線沿用原元件。本步初始沒有火，避免地圖驗收時先啟動尚未製作的教學；水油、火場、NPC 的核心程式不變。

## 驗證入口

關閉同專案 Editor 後，使用 `D:\unity\6000.5.0b9\Editor\Unity.exe`；入口自行結束，不加 `-quit`：

```text
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.TutorialGreyboxLayoutBuilder.BatchInstallAndValidate -logFile "D:\Documents\畢專\Logs\greybox-step3-install.log"
-batchmode -projectPath "D:\Documents\畢專" -executeMethod FireRescue.Editor.TutorialGreyboxLayoutValidation.BatchRun -logFile "D:\Documents\畢專\Logs\greybox-step3-playmode.log"
```

報告與實際相機截圖：`Docs/Validation/2026-10-19-greybox-step3`。
驗證器開啟已保存場景，僅在 Play Mode 以現有 Motor API 驅動角色；不把測試移動、傳送、NPC 救出或暫時關閉外觀寫回場景。

本階段沒有接上鍵鼠：不能把自動通行測試說成已完成手動教學玩法。第四步才接 WASD、左斧／右管與對應互動；尚未驗證實體輸入、Windows Player build 或多人。
