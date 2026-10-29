# 炎上消防隊：v0.2.1 公開測試版

紀錄更新：2026-10-29。

- 儲存庫已依使用者要求改名：[Enjo-Fire-Brigade](https://github.com/xin-hw/Enjo-Fire-Brigade)。同一ID1306325166，原歷史與Release保留，origin已更新，本機專案不搬移。
- 最新分享／更新說明：[v0.2.1 Prototype](https://github.com/xin-hw/Enjo-Fire-Brigade/releases/tag/v0.2.1-prototype)。
- 玩家包：[FireRescue-v0.2.1-Windows-x64.zip](https://github.com/xin-hw/Enjo-Fire-Brigade/releases/download/v0.2.1-prototype/FireRescue-v0.2.1-Windows-x64.zip)。
- 遊戲建置提交：`6a54b86a58188381e5c066b501d9451de80f1ae4`。Release ID426080404，Asset ID683144195，公開Prerelease，非Draft。
- ZIP 65,761,438 bytes；SHA256 `52f4da84532d8e98082838db2d347037a2eca6821bc8e8db0ff43ddedee2ef59`，GitHub回傳size與digest一致。
- 在停止要求前取得93項整關與40項噴射回歸。Windows建置Succeeded、0錯誤0警告；依使用者要求未再啟動新版Windows玩家或解壓試玩，不引用v0.2舊28項作新版證據。
- v0.2.1內容：R文字重開、透明外圍牆、E統一接管／拔管／物品互動、較遠且更明顯水流。正式發布收據見Validation/2026-10-28-v021/release-receipt.json。後續白地板／米牆與物理工作不包含在這份已發布ZIP。

## 歷史 v0.2 公開測試版交付（2026-10-28）

- 公開專案與完整操作／更新摘要／模型署名／第一關玩法：[專案README](https://github.com/xin-hw/--v0.1)。
- 分享頁：[炎上消防隊 v0.2 Prototype](https://github.com/xin-hw/--v0.1/releases/tag/v0.2.0-prototype)。
- 玩家包：[FireRescue-v0.2-Windows-x64.zip](https://github.com/xin-hw/--v0.1/releases/download/v0.2.0-prototype/FireRescue-v0.2-Windows-x64.zip)。
- 原始碼／建置對應提交：`f9307a6768adfdd852346d557c2493c4dedf7e31`；標籤 `v0.2.0-prototype` 指向同一提交。
- Release ID `426054189`，Asset ID `683070324`；公開、非Draft、Prerelease。保留v0.1，不覆蓋舊下載。
- 沿用GitHub Desktop原專案的master與登入憑證，由背景Git／GitHub API上傳；沒有操作Desktop畫面、force push、改名或建立另一儲存庫。舊帳號網址目前由GitHub重新導向到 `xin-hw/--v0.1`，本輪未更動origin設定。

## v0.2 驗證

- Play Mode **79項PASS**；Windows x64建置 **0錯誤／0警告**；最新本機包及ZIP重新解壓後，D3D11執行各 **28項PASS**。
- ZIP **65,760,394 bytes**、**246項**；包含EXE、Data、MonoBleedingEdge、D3D12及必要DLL、BUILD_INFO與README；排除ReviewSmoke／StartupProbe診斷資料夾。必要檔案齊全，無絕對或向上跳脫路徑。
- SHA256：`5b26fcbe3591a2325c5692c100ef0c5b16fb3f03d54c8ae8baf2074a6f92d470`。
- GitHub Asset的size、digest與本機一致；未登入取得公開Release中繼資料成功，並由公開網址實際下載 **HTTP 200**，下載檔SHA256再次相同。Release標籤提交亦已核對。
- 同輪修正失敗重開按鈕、5秒首波、上下圓角UV；火苗改較疏且較小，所有活火格仍保留暖色地面提示。第一關沒有舊AI美術依賴，僅保留授權圍牆材料與程式效果；歷史來源不刪。
- 證據：`Validation/2026-10-28-restart-release/RESULT.md`、`package.txt`、`zip-player-smoke.txt`、`release-receipt.json`。後續提交只追加發布驗證文件，不更改已發布的遊戲內容。

## 驗收界線

背景Windows擷取仍是黑圖，不宣稱已人工點擊按鈕或完成真鍵鼠試玩。已驗證按鈕顯示條件、同一callback實際重載、全部狀態重置；框線與火苗有離線像素／Unity渲染核對。使用者仍需驗收按鈕實際點擊、UI字級位置、初見60秒難度與極端轉角管線手感。

## 歷史 v0.1（2026-10-25，保持原檔）

- 公開專案與操作說明：https://github.com/brawlstarsshellygod520-hub/--v0.1
- 測試版頁面：https://github.com/brawlstarsshellygod520-hub/--v0.1/releases/tag/v0.1.0-prototype
- Windows ZIP：https://github.com/brawlstarsshellygod520-hub/--v0.1/releases/download/v0.1.0-prototype/FireRescue-v0.1-Windows-x64.zip
- 原始碼／建置對應提交：`b0a45fb12a8ee6105429d8e3eb32506c2503daeb`
- 標籤：`v0.1.0-prototype`（Prerelease，非正式發行版）。

下載 ZIP 完整解壓後執行 `FireRescue.exe`；不要刪除同層的 `FireRescue_Data`、`MonoBleedingEdge` 或 DLL。操作、Unity 開啟方式、玩法與限制在根目錄 README。

### v0.1 發布驗證

- ZIP：64,470,412 bytes（約61.5 MiB），241個壓縮檔項目；包含執行檔、UnityPlayer.dll、遊戲 Assembly-CSharp.dll，無絕對／向上跳脫路徑。
- SHA-256：`43606cdf828e11131a81b1c6f61607ef49538d261894ae9e93be3f9b75716429`。
- GitHub 回傳的 asset size 與 SHA-256 均與本機一致。
- 未帶登入憑證取得公開 Release 中繼資料成功；公開下載 HTTP 200、README HTTP 200。
- 沿用既有公開 origin/master；沒有新建或改名儲存庫，也沒有 force push／重寫歷史。
- 新提交以 GitHub noreply 信箱署名。Library、Temp、Logs、UserSettings、Archive、Builds、Python快取、Blender自動備份、環境設定檔不納入 Git。Windows ZIP 另由 Releases 提供，不塞進原始碼歷史。

### v0.1 驗證界線

64項Play Mode檢查、Windows建置0錯誤／0警告已記錄。Windows實體鍵鼠完整試玩仍未完成；上傳成功不代表補完成該驗收。詳細狀態見 [整合驗證](Validation/2026-10-25-residential-integration/RESULT.md)。

本輪只整理文件、排除規則並發布既有進度，沒有重新設計玩法或改動場景。整體 staged diff 的歷史 Unity YAML／原始驗收日誌存在尾端空白，保留原始證據；本輪核心腳本與公開 README／RESULT 的 targeted diff check 通過。
