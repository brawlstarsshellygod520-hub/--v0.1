# 第一人馬桶水救援驗證

2026-10-27，Unity 6000.5.0b9。未控制螢幕或實體鍵鼠；未 commit／push／發布。

## 通過

- `playmode.txt`：38 項。完整行走經過真實正門／浴室、兩次右鍵分開拿槍與連接、F 門口導管、5m 內拉管、物理水包碰撞／持續滅火、NPC 自行經兩門逃出、完成事件一次。沒有傳送、直接 ApplyWater 或清空火場冒充救援。
- `first-full-loop-pass.txt`、`first-rescue-replay.log`（根目錄）及最終 `playmode.txt` 是獨立重開的相同路徑，重跑不累計測試數。新場景初始火、連接與 NPC 狀態重新建立。
- `fixtures-pass.txt`：獨立定位的 20 項（其中 19 項也包含在完整 38 項中），涵蓋轉角、寬／窄門、途中重新起火、清火後恢復、初速重力、時間步長一致、停噴尾流、牆攔截、每包一次碰撞、流量額度、視覺停用不改狀態。
- `directional-regression.txt`：共享舊噴射契約 40 項，確認 opt-in 不改舊來源曲線，水／油／乾粉落點及殘留、遮擋、流量與視覺契約仍成立。這不是新增了油／乾粉關卡。
- `references.txt`：沒有缺腳本、mesh、skin／bone 引用；啟用的生成模型只剩馬桶，沒有常駐世界文字或舊火柱網格視覺。
- `build.txt`：Windows x64 Development 成功，BuildReport 0 errors / 0 warnings。Editor 既有過時 API 警告不宣稱全部清零。
- `windows-player.log` / `packaged-verification.txt`：實際執行新的 Windows exe，以 D3D11 驗證新場景、5m、6 種 NPC 材質、骨架、單一馬桶水源與物理 Profile／初始狀態。**這是玩家版載入／引用 smoke check，不是玩家版完整手動通關。** 完整指令通關證據來自上面的 Editor Play Mode。
- 原下載 Blend 與專案備份 SHA256 一致；舊 TutorialGreybox Scene SHA256 保持 `492fd4651c81ae039616443672ed2941563425c96d3869e391715835d36f6bc6`，沒有覆寫舊場景。

## 真正檢視過的畫面

- `layout-overview.png`：方位與唯一救援片段。
- `water-BillboardParticles.png`、`water-DropletMeshes.png`、`water-Hybrid.png`：相同噴射狀態比較，選 Hybrid。火苗的黃色核心原先被外層不透明 mesh 遮住，已移到鏡頭側，減少火團重疊；沒有改火場權威。
- `rescue-complete.png`：NPC 抵達出口消失後的狀態。

## 過程失敗與修正

1. VFX enum 最初寫成不存在的 StretchedBillboard；依編譯證據修為 Stretch。
2. 初版測試假設兩人合計至少 20 個 skin renderer；改驗證實際兩套具有效 Avatar 的模型，沒有拿不相關數量當規格。
3. `failed-door-passage.*`：浴室門口看似打開但原來源 Boolean 背面仍封住。射線重現並修衍生開孔深度；完整牆碰撞沒有被關閉。來源檢查與五門正／負 ray 記錄在 `ArtSource/UserProvided/Level1026/Inspection/` 與匯出 JSON。
4. `failed-fixture-comparison.log`：時間步長測試把兩個不同原點的絕對座標直接比較。改比較相對各 emitter 的位移，誤差門檻維持 .002，不是放寬彈道條件。
5. `failed-spray-landing.*`：早期站位與槍口側向偏移使水實際打到門框，602 包只有牆面命中、火數不降。保留失敗，重新選擇真能從門洞噴到地面的站位；沒有回到點哪裡就直接清哪裡。最終 102 包有地面及牆面真命中，門口通道火減少，NPC 才開始逃生。

## 交付與待驗收

最終唯讀交叉審查：Ready with limitations，未發現阻擋此片段的高／中風險問題；核對實際 Runtime、場景組裝、測試與建置證據，沒有另外修改或執行 Unity。

本機 `PlayLatest.cmd` → `Builds/Latest/FireRescue.exe`；原始資產按 UserProvided / AIGenerated 分類保留。
請依 `Docs/FIRST_RESCUE_1026.md` 做真人方向／噴射距離／導管手感驗收。本版水是簡化物理包，不是完整流體模擬；其餘關卡階段尚未接回，不將此片段稱為整關完成。
