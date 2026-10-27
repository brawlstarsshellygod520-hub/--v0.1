# v0.2.1 — 2026-10-28

## 已取得的證據

- playmode.txt：93項PASS，包含原核心循環、E與右鍵互動、E拿放交付優先與自動收管、R重載、透明外圍牆保留碰撞。
- spray-regression.txt：停止要求到達前已完成40項舊方向噴射回歸PASS。build.txt：Windows建置Succeeded，0 errors / 0 warnings。這些不代表已啟動新版Windows玩家。
- 真噴射空地對照：最遠3.179562→5.301241m；最近0.464092m不變。可視水包18→26，總劑量3.333333→3.333332，不靠增加流量提高滅火力。water-before/after.png為Unity真渲染，已看過。
- attempt-1/2/3：增加線性／指數速度時近中程清路缺口導致死亡，未放寬傷害或通關規則。最終保留原近中程速度，僅延伸最遠水滴組。
- attempt-4：核心流程已過，隔離射程fixture漏接ElementReaction產生零水滴，修正測試接線後再取得上列結果；沒有移除runtime安全門檻。
- repository-rename.json：同一儲存庫ID改名Enjo-Fire-Brigade，origin同步，歷史與舊Release保留。

## 使用者停止驗證要求

使用者在本輪測試後要求「先不做驗證，直接給下載頁」。其後只執行必要Windows建置、打包、上傳；沒有再進Play或啟動玩家。此版Windows實際執行、解壓後試玩及UI字級位置未驗，不將v0.2舊玩家測試算作v0.2.1證據。

建置結果見build.txt；包資訊及發行狀態見package.txt、release-receipt.json與Docs/PUBLIC_RELEASE.md。
