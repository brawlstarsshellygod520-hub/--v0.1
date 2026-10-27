# 使用者角色白模 — 2026-10-26

來源：`D:/Downloads/角色2.fbx`。實際只有一個長髮居民人形，六個 Mesh，原始材質／骨架／動畫皆無。不能把同一個人形誤當作兩份角色；目前只替換 TutorialGreybox 的 NPC，消防員外觀與操作保持不變，另一位待提供檔案。

## 外觀與資產

- 原檔逐位元備份：`ArtSource/UserCharacters2/Source.fbx`；SHA256 `F58CE494C875D2738B251121945D69955D1584DC24F570A80C595BEF347C7C62`。
- 可編輯衍生檔：`ArtSource/UserCharacters2/Resident2.blend`；重建腳本 `export_user_character.py`。原始六份網格 1127 頂點與拓樸保留，只做等比縮放、座標正規化、分色和蒙皮。
- 配色：栗棕髮、膚色臉／手、青綠上衣、暖米白胸飾、墨藍褲、深灰鞋、深色眼睛。沒有添加模型細節。
- Unity 資產：`Assets/FireRescue/Art/UserCharacters2/Resident2.fbx`、`Resident2.prefab`、`Materials/U2_*.mat`；可以直接修改各 `.mat` 顏色。
- 高 1.5m、腳底 y=0、正面 +Z；18 骨 Generic Rig，Idle 2.5 秒／Run 1.25 秒循環。這是低幅度原型動畫，不是完成的手工動作、IK、表情或髮絲模擬。

## 替換邊界

`TutorialGreybox / Step4 User Layout` 中現有 Resident 的 `Visual/User Resident 2` 是新外觀，舊 `Visual/Model` Capsule 停用保留。新外觀含 Animator 與既有 CharacterLocomotionAnimation；只讀取 NPC root 實際位移。Root Motion 關閉。

NPC root 的 CharacterController、RescueNpc、路線、出口與救援統計皆不改。消防員、斧、噴槍、Hose、3m 接點、來源與噴射 Profile 不改。九個可重用 Greybox Prefab 不改；僅目前教學住宅 Scene 使用外觀 override。

Editor 選單：`FireRescue > Art > 接入使用者居民白模（僅首次）`。已有新外觀時不覆寫；不應為了改色反覆執行舊住宅生成器。

## 驗證與人工驗收

自動證據：`Docs/Validation/2026-10-26-user-characters`。背景 Unity 測試與離線 RenderTexture 圖不等於真人鍵鼠手感已驗收，且沒有接管 Unity／Blender 視窗。

玩家版與本輪請測項目：`Docs/PLAYER_ACCEPTANCE_2026-10-26.md`。
