# 使用者道具來源快照 — 2026-10-28

Source.blend 為 D:/Downloads/第一關1027.blend 的完整副本，SHA256 7f7c3bb7170a0787c79d2bc4e9a1c29425a1523a45db8c187e1524c295229504。來源與副本未被匯出程式回寫；舊版在上一層 Source.blend，未覆蓋。

export_props.py 以 Blender --background --disable-autoexec 執行，採既有戶型相同10倍比例與壁面原點；props.geometry.json包含完整來源Mesh以供比對，不代表所有測試物件都會進遊戲。

| 道具 | 原始 Mesh 名稱 | Unity純外觀Prefab |
|---|---|---|
| 馬桶 | 馬桶本體、馬桶座、水箱、水 | ToiletVisual |
| 蘇打粉袋 | 立方體.002 | SodaVisual |
| 油鍋 | 油鍋、握把、油 | OilPanVisual |
| 盤裝布丁 | 盤子、布丁、黑糖 | PuddingVisual |

Unity派生資產在 Assets/FireRescue/Art/UserProvided/Level1027/Props20261028，基本色／導出法線屬使用者幾何加工。水與油原始面朝下，只修派生Mesh的winding及法線；不增減幾何、不改擺位。立方體、立方體.001是遠離戶型的大型測試Mesh，未匯入遊戲。原始住宅完全相同，因此未重建場景。

窄範圍整合入口：FireRescue.Editor.ReviewUserPropsImport.BatchApply；不要重跑初始整關生成器。腳本保留既有Source與CarryItem等玩法元件，只換Visual並更新獨立錨點／碰撞。驗證證據：Docs/Validation/2026-10-28-user-props/RESULT.md。
