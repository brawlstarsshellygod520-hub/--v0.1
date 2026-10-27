# Windows交付與模型分類：Ready with limitations

2026-10-27，Unity6000.5.0b9／Windows x64 Mono Development／Direct3D11。未控制螢幕、滑鼠或鍵盤，沒有發布／commit／push。

## 報告與原因

使用者確認在Windows玩家版看到三項尚未完成。當輪`audit-before.txt`實際資料：

- TutorialGreybox已保存單椅朝內、Scene六色與5m。
- 預設BuildSettings仍是舊ResidentialFire；Player Prefab13m、Role1 Prefab仍白模。
- 因未取得使用者實際執行的舊exe位置，不能確定其版本；但交付入口與可重用資產未同步是確認的缺口，不能只以Editor Scene正確宣稱交付閉合。

修正後`audit-after.txt`／`audit-final.txt`確認預設唯一enabled為TutorialGreybox，Player Prefab與Scene5m，居民Prefab與Scene六組材質。兩張單椅椅面朝茶几。舊關卡disabled保留，固定入口`PlayLatest.cmd`→`Builds/Latest/FireRescue.exe`。

## 模型分類與安全

- Art/UserProvided：Characters/{Character1,Character2,Rigged}、Tools/{FireAxe,Nozzle}、LevelOne。使用者原模型加工出的骨架、動畫、上色仍屬UserProvided。
- Art/AIGenerated：Models、Prefabs、Materials、Animations、Household；共享Shader／玩法組裝Prefab保留原位置。
- ArtSource同樣實體分UserProvided與AIGenerated，Tools共用；不是只貼標籤或新增空資料夾。
- `guid-migration.json`：163個既有GUID前後可解析且一致；全Assets查無重複GUID。
- `source-model-hashes-before.json`：21份原始／加工FBX、Blend搬後內容雜湊相同。原提供檔與模型幾何未修改。
- `guid-aware-file-audit.json`：639基線檔搬移映射後624相同、15有意修改、0遺失；修改包含路徑常數、兩Prefab、BuildSettings、驗證入口。Scene雜湊不變。搬檔不按文字路徑誤算成刪模型。
- live Editor與Python輸出路徑已更新；角色重建來源改專案內副本，噴槍比較工具明確要求--current。歷史metadata原路徑不改寫成新歷史。Python語法檢查通過；本輪沒有執行模型重生／重匯出。

## 驗證

| 驗收 | 結果／證據 |
| --- | --- |
| 三項設定、Prefab持久化、預設入口、5m移動／窗口預算 | PASS，tuning-playmode.txt：17項 |
| 實際破門、接管、噴水／乾粉、窗口、NPC救援、重新Play | PASS，residential-loop.txt：39＋4項 |
| 搬移後全Scene含inactive引用 | PASS，references.txt：Missing Script／static mesh／skin mesh／bone均0 |
| 參考配色與單椅畫面 | PASS，resident-front.png、resident-walk.png、armchairs-after.png已人工檢視；只基本配色，不重建帽子造型 |
| Windows建置 | PASS，build.txt：Succeeded，0 Errors／0 Warnings |
| 真Windows exe啟動與打包內容 | PASS，player-windows.txt：TutorialGreybox、Hose5m、ResidentColors6、Generic rig、單椅朝內、Graphics=Direct3D11、exit0 |
| 真人手感、長時間遊玩、NPC導航根因修復 | Not run／非本輪，不以測試數代替 |

合計60項針對性與整關檢查；另有GUID／雜湊／引用及實際玩家版啟動證據，不混算為大量測試項。

### 失敗保留與隔離

1. `Logs/prototype-delivery-repair.log`首次失敗在computed舊RoleSwapRig父路徑；改用新Folder常數後通過，沒有刪測試／略過來源資料夾驗證。
2. `player-null-device-failure.txt`為首次`-nographics`誤判六色。靜態資產與Prefab六組正確；**同一Windows exe、同一Data，不改任何資產**改用背景GPU後即ResidentColors6／exit0，判別為Null裝置的顏色取樣限制。沒有降成只測名字求綠燈；最終Probe明確拒絕Null裝置驗色。
3. 最終以`-batchmode -force-d3d11 --verify-prototype`啟動Windows exe，沒有UI控制／輸入命令。此證據不等於真人操作手感。

## 重現入口

Unity命令：`D:/unity/6000.5.0b9/Editor/Unity.exe -batchmode -projectPath "D:/Documents/畢專" -executeMethod <入口> -logFile <獨立log>`；入口自行Exit，不加-quit。分類搬移已完成，勿用檔案系統另搬Assets或重建.meta。

- AssetProvenanceOrganizer.BatchOrganize：顯式MoveAsset、GUID前後對照，拒絕覆寫已存在目的地。
- PrototypeDeliverySetup.BatchRepairAndValidate：Prefab／Scene／BuildSettings修復。
- ResidentialTuningValidation.BatchRunDelivery：17項。
- ResidentialIntegrationValidation.BatchRunCurrentDelivery：43項。
- PrototypeDeliverySetup.BatchBuildLatest：最新Windows包。
- PrototypeDeliverySetup.BatchAuditFinal：引用與最終資料重讀。

玩家包驗證：`Builds/Latest/FireRescue.exe -batchmode -force-d3d11 --verify-prototype -logFile <log>`，完成自行退出。正常玩不要帶此旗標。原始log位於忽略的Logs/，最終入口與玩家版均exit0；既有Editor CS0618警告未在本輪清除，不宣稱整個專案全零警告。

## 剩餘風險／手動步驟

請從根目錄PlayLatest.cmd或Builds/Latest/FireRescue.exe啟動，保留Data／MonoBleedingEdge／DLL，勿再開歷史Build資料夾或舊GitHub下載包期待新內容。GitHub本輪未發布更新。正式模型替換／尺寸變動仍要驗收通道；NPC導航淨空與實體碰撞落差仍未修。
