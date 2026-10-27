# 雙角色交換、骨架與基礎動畫

2026-10-27，僅當前 `TutorialGreybox` 住宅，不重建地圖或修改滅火／NPC 規則。

## 玩家看到的變更

- 玩家消防員由角色1改成 **角色2長髮模型**（沿用基礎配色，1.6m）。
- 待救 NPC 由角色2改成 **角色1貓耳／尾巴白模**（1.5m），等待後續上色版。
- 兩者是真實 SkinnedMesh＋Generic 骨架：玩家18骨，NPC20骨含尾巴。原始六／十件網格與拓樸保留，不重新生成角色。
- 兩者 Idle 2.5秒、Walk 1.25秒循環；移動以實際位移自動切換，停止回 Idle，Root Motion 關閉。
- 平常 WASD 朝移動方向轉身，停下不追滑鼠。只拿著工具、裝備那一下、接來源那一下及持續按住這兩次操作，都不視為攻擊。
- 斧頭實際揮砍整段（含鬆鍵後完成的一刀）、噴槍實際噴射時朝滑鼠；第一刀／第一顆液體發射前先對齊。停止動作後移動接回朝向。

## 架構與替換

- 新派生來源：`ArtSource/RoleSwapRig/`，包含兩份可編輯 blend、生成／重匯驗證腳本、來源 hash、原始頂點數與預覽。
- Unity：`Assets/FireRescue/Art/RoleSwapRig/`，兩份 FBX／外觀 Prefab／Idle-Walk Controller。玩家沿用 U2 外部材質，NPC 沿用 Whitebox 材質；不修改舊來源、舊材質或模型 GUID。
- 場景新增 `Player/Visual/Swap Player Role2`、`Resident/Visual/Swap Resident Role1`；舊 User Player Model／User Resident 2 僅停用保留。
- 角色外觀之下的 `CharacterLocomotionAnimation` 讀 actorRoot 的實際位移，使用既有 `Speed`、`CycleSpeed`。模型不擁有碰撞、尋路或滅火判定。
- LocalPlayerInput 的 `faceMovementWhenIdle` 僅當前場景開啟；只有 Pointer 命令套新規則。歷史 `TickCommand` 仍維持舊 point-aim 合約。
- FirefighterTools 提供唯讀 `WillPointerAimAtCursor`，與自己掌握的裝備／動作狀態一致，先定朝向再輸出攻擊；不增加第二份裝備狀態。
- 原始碰撞、速度、相機 Pitch50／Yaw0／Size6、Source、3m 管長、窗口與槍口／入管接點不變。接點在玩法 Anchors，沒有改掛骨架。

生成腳本修正了角色1側髮下緣誤綁手臂：整片302頂點連同眼／眉固定隨 Head。每個 Action 建立前清零 Pose，避免上一段動作殘值；原始 FBX 不覆寫。

## 驗證與操作

真 Unity 6000.5.0b9：場景保存重讀、重複安裝、朝向／骨架／動畫44項、整關39項＋重新Play4項全部PASS，合計87項。Idle/Walk確實改變網格頂點而非整個模型漂移，Walk首尾差小於2.5cm，停用／啟用不誤播走路；視覺不含玩法碰撞。另以Unity Direct3D11離線渲染檢視兩角色、遊戲鏡頭、噴射與完成場景。證據見 `Docs/Validation/2026-10-27-character-swap-animation/`。

本機玩家版：`Builds/CharacterSwap/FireRescue.exe`；沿用WASD、左右鍵分段裝備／操作、F接管與窗口操作。開Scene按Play也可測。未上傳GitHub／未commit／未改公開README。

請真人驗收：

1. 滑鼠放另一邊，WASD四方向應朝行走方向；停住仍保留最後朝向。
2. 左鍵拿出斧頭後繼續走，仍朝走路；再按／按住揮砍才朝滑鼠。右鍵裝備、接來源各一下也不要直接噴。
3. 接好後再按／按住右鍵，噴射才朝滑鼠；鬆手移動會接回步行朝向。NPC等待時Idle，逃生時Walk。
4. 觀察側髮／尾巴與手腳是否有可接受的基本動作；後續正式上色另換外觀。

限制：這是基礎動畫，不是正式手部IK／全身揮斧／雙手持槍動畫；工具沿用原Pivot呈現。沒有增加真人跑步、四人同步或正式美術。自動命令測試不是實體鍵鼠手感驗收。完整素材交付清單見 `Docs/LEVEL_ASSET_REQUIREMENTS.md`。
