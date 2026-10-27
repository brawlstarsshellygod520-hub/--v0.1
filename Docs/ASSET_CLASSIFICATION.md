# 模型分類與目前啟動方式

2026-10-28封路追加：使用者同意用簡單圍牆封住屋外通往倉庫的捷徑。三面Unity Cube牆放Scene獨立group `Prototype Courtyard Boundary`，灰綠霧面材質放 `AIGenerated/ReviewBoundaries`；不塞進使用者戶型資料夾，不修改住宅Mesh。其餘目前正式道具仍為UserProvided。

2026-10-28四件道具更新：正式馬桶、蘇打粉袋、油鍋、盤裝布丁已從最新用戶Blend替換進FirstReview1027，原位／原比例。資產 `UserProvided/Level1027/Props20261028`（Mesh、基本色、Visual Prefab）；唯讀來源 `ArtSource/UserProvided/Level1027/PropsUpdate20261028`。基本色與面向修正屬使用者幾何加工，不歸AI原創。下方臨時四件說明是舊版，原生成檔案保留但活躍場景已不使用。

2026-10-28最新入口是FirstReview1027，仍透過PlayLatest固定啟動。新戶型是UserProvided/Level1027；Role2四份新蒙皮權重在UserProvided/Characters/Rigged/Review1027（原用戶幾何加工，不是AI原創）；蘇打粉／油鍋／布丁臨時材質放AIGenerated/ReviewPlaceholders，馬桶沿用原生成資產。其餘生成家具不在這個展示Scene。連續消防管是PhysicalHoseTube於執行時產生的3D網格，沒有冒充使用者模型。操作與限制見 `Docs/FIRST_REVIEW_1027.md`。

2026-10-27。這次修復的是交付入口與可重用Prefab，不只是再回報一次Scene通過。

後續更新：目前 `PlayLatest` 已換成第一人馬桶水救援 `FirstRescue1026`，新戶型位於 `Art/UserProvided/Level1026`／`ArtSource/UserProvided/Level1026`；此片段只啟用生成馬桶，其餘生成家具留在原資料夾但不在新場景。操作與最新驗證見 `Docs/FIRST_RESCUE_1026.md`。下方TutorialGreybox入口／已知問題描述為本文件建立時的歷史狀態；NPC淨空已在新片段修復並驗證。

## 目前請從這裡開啟

在專案根目錄執行 `PlayLatest.cmd`，它固定開 `Builds/Latest/FireRescue.exe`。不需從多個歷史Build資料夾選版本；舊版保留、不刪除。GitHub已發布下載版本輪未上傳更新。

重新核對發現：新TutorialGreybox的單椅、配色、5m原本已保存，但Unity預設建置還指向舊ResidentialFire，Player基底Prefab仍13m，角色1外觀Prefab仍白模，只有Scene上有配色override。這些未同步的入口可能讓玩家版或重用模型看到舊內容；沒有證據判定使用者當時執行了哪個舊exe，不能直接歸咎使用方式。

現在預設建置啟用TutorialGreybox，舊ResidentialFire保留但停用；Player Prefab與當前Scene皆5m，角色1Prefab與Scene都有六組參考配色。兩張單椅朝茶几，骨架／Idle／Walk、碰撞／接點與核心玩法保留。診斷見當輪audit-before/after.txt。

## Unity內模型

| 位置（Assets/FireRescue/Art/） | 內容／來源 |
| --- | --- |
| UserProvided/Characters/Character1 | 角色1原始FBX副本、舊靜態展示與白模材質 |
| UserProvided/Characters/Character2 | 角色2加工模型、材質、舊NPC展示與動畫控制器 |
| UserProvided/Characters/Rigged | 使用者兩角色加工的骨架、Idle／Walk、Prefab與NPC配色；不是AI原創 |
| UserProvided/Tools/FireAxe | 你的消防斧Blend加工匯出的FBX與展示Prefab |
| UserProvided/Tools/Nozzle | 你的「消防管.blend」噴槍加工匯出的FBX與展示Prefab |
| UserProvided/LevelOne | 你的第一關Blend匯入的視覺／碰撞網格與隨附材質 |
| UserProvided/Level1026 | 新1026戶型的衍生完整碰撞／cutaway視覺網格與隨附材質；原Blend在ArtSource備份 |
| UserProvided/Level1027 | 最新1027戶型衍生幾何與基本色，原Blend唯讀備份 |
| UserProvided/Level1027/Props20261028 | 團隊正式馬桶、蘇打粉、油鍋、盤裝布丁；由新1027 Blend匯出並保留相對位置 |
| UserProvided/Characters/Rigged/Review1027 | 使用者Role2同拓撲的新衣服／前臂／腿部蒙皮權重 |
| AIGenerated/ReviewPlaceholders | 歷史蘇打粉盒、鍋、布丁臨時呈現，已退出目前FirstReview1027場景依賴 |
| AIGenerated/ReviewBoundaries | 使用者同意保留的臨時庭院圍牆材質，不屬舊AI美術模型 |
| AIGenerated/Models、Prefabs、Materials、Animations | 舊版由程式生成的模型與附屬展示資產 |
| AIGenerated/Household | Unity Primitive生成的家具／家電佔位Prefab與材質 |
| Shaders | 共用Shader，不歸模型來源 |

`Greybox/Prefabs`、`Powder`、`Window`等玩法組裝Prefab仍保留原處，它們包含Source／碰撞／腳本與示意Visual，不應與原始模型混為一談。這次不搬Runtime或Scenes、不更換meta GUID。

## 可編輯來源

`ArtSource/UserProvided/`對應Character1、Character2、RiggedCharacters、FireAxe、Nozzle、LevelOne；`ArtSource/AIGenerated/Models`為生成模型。原始副本與加工檔隨同一來源bundle存放，名稱及metadata明確區分。Tools為共享程式，保留原處。

21份FBX／Blend搬前後SHA256完全相同；Unity的163個既有相關資產／資料夾GUID全保留。搬移對照`guid-migration.json`記before/after；歷史metadata舊路徑不改寫成新歷史。匯出工具改用新位置，角色重建從專案內Source.fbx讀取，不依賴Downloads；噴槍外部比對工具需明確`--current`指定新版來源。

## v0.2 使用範圍

使用者確認移除舊AI美術，保留程式管線、水火及臨時圍牆。FirstReview1027遞迴資產依賴核對：AIGenerated只剩ReviewBoundaries/Courtyard plaster.mat；沒有舊AI角色、家具、道具模型。歷史來源檔不刪除，避免破壞追溯與舊場景。詳細記錄見Validation/2026-10-28-restart-release/install.txt。

## 替換與驗證

- 正式模型只替換Root下Visual，保留Collider、FluidSource／Rigidbody、Anchors與骨架的玩法邊界。
- 材質、骨架、Prefab屬使用者模型的加工成果，不能因為由AI協助綁骨就移到AIGenerated。
- 本輪以背景Unity與離線圖檢視，並實際執行最新Windows包的背景Direct3D11啟動檢查，沒有控制滑鼠／鍵盤或操作視窗。
- 不使用`-nographics`來判斷材質顏色：Null裝置曾造成Material.color檢查假陰性；同一包使用GPU後六色驗證通過，原失敗記錄保留。
- 分類當時的NPC導航淨空問題，後續已在2026-10-28-kitchen-hints中修正門檻查詢；v0.2再驗實際正門逃生，不以素材分類本身代表玩法完成。
