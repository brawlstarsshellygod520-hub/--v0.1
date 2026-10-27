# 廚房封路與情境提示 — 2026-10-28

狀態：可供玩家驗收；沒有螢幕控制／人工鍵鼠通關或UI像素驗收。Latest已更新，未發布GitHub。

## 驗收

| 要求 | 結果與證據 |
|---|---|
| 粉末能滅所有火種，包括鍋旁地面 | PASS；鍋命中同時有粉雲沉降。原return分支：鍋0／鄰火1，新分支0／0；reaction-route-probe.txt。真粉末在整關熄鍋及鄰火 |
| 兩秒內填滿廚房封路 | PASS；相連179格在2秒全部點燃；playmode.txt、spread-fast.png |
| 封住屋外捷徑 | PASS；同一全火圖，有三面圍牆找不到倉庫路，停用牆可繞路。不是不可見任務旗標 |
| 鍋旁水可滅、但很快復燃 | PASS；立即水滅→持續鍋熱約.28秒重點火；遠路仍可水清。滅鍋後不再強制點火 |
| 水不能安全取得布丁，粉末滅鍋後可取 | PASS；熱區拿取拒絕／踩火死亡保留，正確流程可拿 |
| 縮短管線且整關可玩 | PASS；10m未偷偷放寬，馬桶→倉庫→粉末→油鍋→交布丁真CC／噴射通過 |
| NPC走正門，不繞後院 | PASS；原貼地capsule被2.7mm門檻誤擋；原查詢173格、新47格，實走約10.39m、maxZ=3 |
| 布丁輪廓與E圓角框 | PASS（狀態／輪廓）；同一NearbyItem、拿起後立即關閉；pudding-outline.png。OnGUI框字級位置待人工確認 |
| 管長極限F提示 | PASS（條件／字串）；滿10m顯示「管線不夠長了！按 F 解除綁定！」，退後或拔管關閉；playmode.txt |
| 既有爆燃／黑布丁／NPC中毒／踩火死／R重開／一次完成 | PASS；沿用並重測 |
| Windows建置 | Succeeded，0錯誤0警告；build.txt |
| Windows D3D11玩家 | 25項PASS；player-smoke.txt |
| 啟動地板 | 6秒ReadDevices=true，14191 grounded frames，focused=false；startup.txt |

## 最終測試

- 整關與正負邊界：72項PASS，playmode.txt（最終kitchen-final-validation.log；早先完整流程為kitchen-playmode-6.log）。成功路線未用傳送、直接清火、直接破門、關閉死亡來求過關。F測試以真TickPointerCommand在旁有另一來源時解除，沒有只直接呼叫Disconnect；歷史窗口低階互動不改。
- 噴射舊契約：40項PASS，spray-regression.txt（kitchen-spray-regression.log）。新增MinimumSpeedFraction的缺省0回退舊.65，舊場景不改。
- 粉末／尋路因果對照：reaction-route-probe.txt，包含停用新surface splash回復「鄰火1」；恢復舊貼地查詢／停用新圍牆得到173格路線，正常新查詢47格。探針未保存場景。
- 正常交付約剩18.617秒；流程為清路→粉末滅鍋→先拿布丁→門前放下→三刀破門→拿起交付→NPC正門逃出。初見玩家難度未驗。
- 毒性分支有門破／安全前提注入；單獨水打鍋分支預清地面以隔離水包命中反應，沒有當成完整人工失敗通關。

## 修復與調參

粉末滅火倍率.65，普通地面.325/unit，水.45/unit。粉雲採命中點上方水平擴散及垂直沉降雙LOS，不穿牆、不投影進櫃體底部。粉末殘留20秒，水5秒；鍋熱範圍1.55m不享受殘留永久免疫，鍋滅才停止重點火。

首波按可燃連通格的距離排定.12秒至2秒到達，之後仍慢蔓延。水新增低速近端滴（速度比例.2）、粉.3；水半徑.9、粉1.35。仍用初速、重力、遮擋與飛行壽命，不以游標距離指定命中。

新三面圍牆是使用者授權的Primitive，group獨立，材料歸AIGenerated/ReviewBoundaries。原住宅、角色與四正式道具不重建；新牆加入HoseWallOcclusion。NPC只修導航查詢底部25mm，沒有改門洞／碰撞寬度或放大步階。

## 保留的失敗證據

attempt-1/2：舊水束近端落點太遠，清路有斷帶。attempt-3/4：粉末同樣飛過近格，不能安全走到鍋前。attempt-5：先破門再回廚房遇到鄰火重燒。這些失敗導致近端分布、粉末殘留與合法操作路線的改善，未刪除火或關掉死亡來通關。

## 視覺／人工限制

已檢視真正Unity RenderTexture的全火、圍牆、清路與金色布丁外輪廓。Camera.Render不包含OnGUI；Windows Hidden擷取仍黑，未用來證明E／F文字樣式。圓角框與提示條已編譯、打包並驗狀態，但仍請使用者確認字級／位置及實際手感。

下一步：檔案總管開啟 D:/Documents/畢專/Builds/Latest/FireRescue.exe。保留Data／MonoBleedingEdge與DLL。R重開；未commit／push／發布。
