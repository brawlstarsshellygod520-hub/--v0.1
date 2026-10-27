# 主角白模與正向較遠視角 — 2026-10-27

目前玩家版：`Builds/PlayerWhitebox/FireRescue.exe`。保留旁邊所有 Data、MonoBleedingEdge 及 DLL。未更新 GitHub 公開下載，請用這份本機新版驗收。

## 本輪內容

- 主角先選 `D:/Downloads/角色1.fbx`（貓耳／尾巴造型）。角色2仍沿用既有長髮居民 NPC，不替換或移除它的色彩與動畫。
- 角色1原始10件Mesh、1425頂點，沒有材質、骨架、權重或動畫。原始FBX完整保留，匯入資產及ArtSource副本SHA相同；僅在Unity外層設定等比縮放、腳底對齊y=0與正面+Z，高1.6m。沒有上色、重造型或新增動作。
- 沒材質的分件共用中性白色 `Whitebox.mat`。等上色版再依新材質／貼圖更新Visual，不使用先前NPC七色配色覆蓋主角。若有外部貼圖，請和上色版FBX一併提供。
- 目前場景Player的 `Visual/User Player Model` 是新外觀；原 `Body` 與 `Helmet` 停用保留。Collider、CharacterController、移動、斧頭、噴槍、Hose及Nozzle/Interaction接點全保留原處，不依模型尺寸重建物理。

## 相機

使用者已確認「對齊房屋的正向俯視」，不是90°正上方。

- Pitch仍50°；Yaw從40°改為0°，房屋不再斜45度呈現。
- 正交Size從4.5增加至6，同長寬比可見寬高約增加33%。
- 沿用RescueCamera的.18秒SmoothDamp、固定角度、平滑跟隨玩家，不隨角色轉身、不開放自由旋轉。
- WASD仍依畫面方向；目前W沿世界+Z、D沿+X。未旋轉地圖、玩家出生點或玩法座標。
- RescueCamera新增可序列化viewAngles，缺少該欄位的舊場景保留50/40預設；只目前TutorialGreybox設定50/0。

## 資產與入口

- 原始來源副本：`ArtSource/PlayerWhitebox/Source.fbx`
- Unity原始FBX：`Assets/FireRescue/Art/PlayerWhitebox/Character1.fbx`
- 展示Prefab：同資料夾 `PlayerCharacter1.prefab`
- Scene：`Assets/FireRescue/Scenes/TutorialGreybox.unity`
- Editor：`PlayerWhiteboxSetup.BatchInstallAndValidate`，重跑不覆寫已存在的模型Prefab；不需重建住宅或Greybox基底Prefab。
- 測試／建置：`PlayerWhiteboxValidation.BatchRun`、`ResidentialIntegrationValidation.BatchRunPlayerWhiteboxCamera`、`PlayerWhiteboxSetup.BatchBuildWindows`（均加 `FireRescue.Editor.` 前綴）。

## 驗收

17項角色／相機＋43項整關及重開，共60項自動檢查PASS；Windows建置0錯誤／0警告。主代理已看實際遊戲Camera離線圖，並另外啟動Windows玩家版確認新模型與視角。實際長按鍵鼠手感仍待玩家試玩，未把自動命令驗證當真人輸入。詳見 `Docs/Validation/2026-10-27-player-whitebox-camera/RESULT.md`。

先請確認：主角白模大小與方向、拉遠幅度是否合適，以及進屋後正向視角是否容易閱讀。正式上色、主角骨架／動畫與工具握持精修不在本輪新增範圍。
