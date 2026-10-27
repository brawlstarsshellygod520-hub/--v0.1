# v0.2 發布前驗證 — 2026-10-28

範圍：五秒首波、失敗重新開始、上下圓角修復、降低火苗視覺密度、舊AI美術排除及公開文件。不做螢幕控制。

## 通過證據

- `install.txt`：Scene首波5秒、spaciousFire啟用；遞迴依賴沒有舊AI美術，只剩使用者同意保留的ReviewBoundaries庭院材料。團隊模型、歷史來源與GUID不刪。
- `playmode.txt`：**79項PASS**。2秒仍未全廚房、5秒179格全火；View降低火苗不改燃燒數，每個活火格仍有地面標記。
- 同一Play Mode透過真控制器、碰撞與方向噴射，走完接水清路→倉庫粉末→滅鍋→布丁拿放→三刀破門→正常交付→住戶正門逃生，交付約剩18.617秒。未放寬10m管長、未傳送／關閉傷害求通關。住戶實走10.39256m，沒有繞後門。
- 玩家踩火與住戶中毒失敗均驗證重新開始按鈕所呼叫的同一handler會實際重載Scene；確認死亡、布丁品質、管線、爆燃與倒數重置。進行中按鈕不可用，R與按鈕共用RestartRound。
- 毒性分支有注入門破／冷卻前提，水打鍋分支預清地面以隔離真水包碰撞；不將這些隔離測試冒充完整人工遊玩。
- `build.txt`：Windows x64 **Succeeded，0 errors / 0 warnings**，Application.version=0.2.0，唯一入口FirstReview1027。
- `player-smoke.txt`：真Windows D3D11執行 **28項PASS**，新版本、模型／骨架、倒數、首波與稀疏火苗／危險地面均已打包。
- `zip-player-smoke.txt`：對發行ZIP解壓到全新資料夾，再執行其中玩家版，**28項PASS**；不是只測工作目錄裡的EXE。
- `package.txt`：ZIP 65,760,394 bytes、246項，必要EXE／Managed組件齊全、沒有絕對或跳脫路徑，不含ReviewSmoke／StartupProbe診斷資料夾。SHA256 `5b26fcbe3591a2325c5692c100ef0c5b16fb3f03d54c8ae8baf2074a6f92d470`。

## 視覺核對

- `frame-before.png`完整重現使用者回報的上下分離／反向圓角；`frame-after.png`使用修正後共用UV mapping，四角與邊框連續。這是同一runtime貼圖及9-slice資料的離線重建，不是人工實際點擊畫面。
- `fire-density-before.png`／`fire-density-after.png`由Unity實際RenderTexture取得。首版減半仍太密且地面標記藏在地板下，自我檢視後改交錯三分之一、降低火苗高度與寬度、地面標記提高至地板頂面上。孤立格仍有火苗，不靠看不到的火封路。已開圖核對較疏火苗、地面標記與原模型。
- 其餘圖保留清路、布丁金邊、爆燃、倒地與完成等狀態證據。

## 未宣稱完成的驗收

Windows Hidden擷取仍為黑圖，不能用它證明OnGUI字級、按鈕位置或真滑鼠點擊。按鈕可見條件／callback／實際重置已測，但實體滑鼠命中與初見難度、管線極端角落／幀率手感仍須使用者試玩。本輪不控制使用者畫面，未新增多人或真實軟管模擬。

發行包核對及GitHub公開驗證見 `Docs/PUBLIC_RELEASE.md`；保留v0.1，不覆蓋舊下載。
