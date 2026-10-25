# 公開測試版交付（2026-10-25）

- 公開專案與操作說明：https://github.com/brawlstarsshellygod520-hub/--v0.1
- 測試版頁面：https://github.com/brawlstarsshellygod520-hub/--v0.1/releases/tag/v0.1.0-prototype
- Windows ZIP：https://github.com/brawlstarsshellygod520-hub/--v0.1/releases/download/v0.1.0-prototype/FireRescue-v0.1-Windows-x64.zip
- 原始碼／建置對應提交：`b0a45fb12a8ee6105429d8e3eb32506c2503daeb`
- 標籤：`v0.1.0-prototype`（Prerelease，非正式發行版）。

下載 ZIP 完整解壓後執行 `FireRescue.exe`；不要刪除同層的 `FireRescue_Data`、`MonoBleedingEdge` 或 DLL。操作、Unity 開啟方式、玩法與限制在根目錄 README。

## 發布驗證

- ZIP：64,470,412 bytes（約61.5 MiB），241個壓縮檔項目；包含執行檔、UnityPlayer.dll、遊戲 Assembly-CSharp.dll，無絕對／向上跳脫路徑。
- SHA-256：`43606cdf828e11131a81b1c6f61607ef49538d261894ae9e93be3f9b75716429`。
- GitHub 回傳的 asset size 與 SHA-256 均與本機一致。
- 未帶登入憑證取得公開 Release 中繼資料成功；公開下載 HTTP 200、README HTTP 200。
- 沿用既有公開 origin/master；沒有新建或改名儲存庫，也沒有 force push／重寫歷史。
- 新提交以 GitHub noreply 信箱署名。Library、Temp、Logs、UserSettings、Archive、Builds、Python快取、Blender自動備份、環境設定檔不納入 Git。Windows ZIP 另由 Releases 提供，不塞進原始碼歷史。

## 驗證界線

64項Play Mode檢查、Windows建置0錯誤／0警告已記錄。Windows實體鍵鼠完整試玩仍未完成；上傳成功不代表補完成該驗收。詳細狀態見 [整合驗證](Validation/2026-10-25-residential-integration/RESULT.md)。

本輪只整理文件、排除規則並發布既有進度，沒有重新設計玩法或改動場景。整體 staged diff 的歷史 Unity YAML／原始驗收日誌存在尾端空白，保留原始證據；本輪核心腳本與公開 README／RESULT 的 targeted diff check 通過。
