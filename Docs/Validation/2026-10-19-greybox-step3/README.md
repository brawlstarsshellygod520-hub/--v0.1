# 第三步驗證結果

日期：2026-10-19；Unity 6000.5.0b9；Windows Editor；僅第三步住宅 Greybox／相機。

狀態：本階段通過，完整可玩教學尚未完成。

- `TutorialGreyboxLayoutBuilder.BatchInstallAndValidate`：編譯、安裝、保存讀回、再執行不覆寫，exit 0。最終再開啟批次亦 exit 0，未改場景。
- `TutorialGreyboxLayoutValidation.BatchRun`：68項 Play Mode 檢查 PASS，exit 0；實際載入保存的 TutorialGreybox，沒有由測試器重建替代地圖。
- 房門實測1.80m、走廊2.08m；玩家從室外經各房間到水箱與油瓶；遮擋／碰撞／相機固定角度與平滑收斂；原 NPC 在無火條件下自主出門。
- 1,039個移動幀採樣角色1.2m高上半身至正交相機的平行視線，全部未被固定障礙擋住；這不是逐像素全身可見率測試。
- 六張Unity實際渲染截圖全部檢視：`00`為開發總覽相機，`01`～`05`為未調整構圖的正常跟隨Camera，不應把總覽當成實際遊戲視野。
- 全部基線267檔中，只有Tutorial場景修改；其餘266檔SHA256相同。沒有修改核心Runtime、九個Prefab、正式美術、原場景、Packages或ProjectSettings。
- 既有CS0618過時API警告未處理；三份日誌沒有C#編譯錯誤或Exception。新資產都有Unity產生的meta；diff空白檢查通過（僅既有CRLF轉換提醒）。

未執行：硬體鍵鼠、Player build、多人、第四至九步教學。第二步161項／住宅40項為歷史證據，本輪未重跑。

日誌：`Logs/greybox-step3-install.log`、`Logs/greybox-step3-playmode.log`、`Logs/greybox-step3-reopen.log`（本機忽略檔）。
持久證據：本目錄 `layout-install.txt`、`layout-playmode.txt`、`baseline.json`、`preservation.json`。
