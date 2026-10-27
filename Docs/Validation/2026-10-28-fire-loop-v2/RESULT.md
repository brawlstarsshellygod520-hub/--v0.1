# 火場循環v2 — 2026-10-28

狀態：可進行玩家驗收；非「已完成人工通關／UI手感驗收」。全程無螢幕控制、未發布。

## 需求與證據

| 驗收項目 | 結果／證據 |
|---|---|
| 地面火可水滅、油鍋只能粉末滅 | PASS；真飛行水清路，真粉末滅鍋；playmode.txt |
| 鍋開始快速局部延燒，之後變慢 | PASS；0.35秒4格、1.65秒13格、約6秒22格；spread-start/fast/slow.png |
| 水打鍋爆燃、近處布丁黑化 | PASS；直接反應與真正掃掠水粒子兩種測試；oil-burst-burnt-food.png、physical-water-oil-burst.png |
| 持續噴水不每幀爆炸 | PASS；單局爆燃一次；playmode.txt |
| NPC吃黑布丁說「好難吃...」，延遲死亡且不救援成功 | PASS；實際Scene NPC／原布丁交手，台詞字串、死亡狀態、Visual倒地；npc-poison-death.png |
| 踩地面火死亡，工具／輸入鎖住；重開乾淨 | PASS；真CC走入火區，不直接呼叫死亡；player-fire-death.png |
| 正常馬桶水清路→倉庫→粉末滅鍋→三刀破門→送正常布丁→NPC走出 | PASS；全段pointer commands、CharacterController、噴射碰撞，沒有傳送／直接清火；交付剩約30.18秒 |
| 60秒逾時拒絕晚交／只完成一次 | PASS；playmode.txt |
| 15m不放寬；管線不新增硬釘住可見頂點 | PASS；固定128、最大單幀內點位移.1845m、段長.0996m、無整條隱藏；water-cleared-route.png |
| 客廳出生 | PASS；(2.6,.1,-3.7)，出口仍在戶外 |
| 無操作／互動／警告提示文字 | 已移除HUD分支並靜態核對；只保留倒數／NPC台詞／結果。未以黑截圖宣稱像素驗收 |
| Windows x64 Development／Mono建置 | 0錯誤、0警告；build.txt |
| 真D3D11玩家版 | 22項PASS；player-smoke.txt。Hidden截圖黑，明確不算UI視覺驗收 |
| ReadDevices=true正常啟動6秒地板穩定 | PLAYER_STARTUP_PASS；startup.txt |

Play Mode最終 **59項PASS**。`fire-loop-v2-regression.log`為完整Editor輸出；`fire-loop-v2-build.log`、`fire-loop-v2-player.log`、`fire-loop-v2-startup.log`在專案根目錄。

## 調查與修正

原72個地面火只讀CookingOilArea半徑，地板FireField全不可燃，並非可獨立水滅的火。本Scene已停舊Area、改真grid火。舊管顯示把每個接觸點硬釘並插入ring、改動下游索引；改固定材質粒子與端點升高，拓樸權威仍留HoseConnection。

首次新測試（attempt-1-path-died.txt）抓到快相1.5／慢相.14仍太快，且足部範圍可能讀到牆後火。調為.4／.035，加地面危險LOS後正常通關。失敗證據保留，未以停用死亡來求通過。舊85項是上一版規則，未冒用為本輪證據。

## 測試隔離與限制

- 正常成功流程未直接注入清火、破門、NPC傳送或交付。
- 毒性反應分支注入門已破／地面已清以隔離NPC接物、台詞和死亡；真正水打鍋分支只預先壓制地面火，保留真來源接管、CC行走和水粒子命中。不能把這兩個反應分支稱完整人工操作失敗通關。
- 爆燃為短時間放大火焰、局部引燃和食物黑化；不是完整油滴噴濺、熱力學或爆炸碎片物理。
- 目前蔓延限廚房可燃走道，不燒遍整棟房；水濕地約5秒抑制復燃。
- 管仍是簡化Verlet/PBD＋權威接觸折線，非全PhysX軟體；特定路線60Hz位移通過，不表示所有極端角落／低幀都驗畢。
- 原角色／戶型／四正式道具／相機未重建。Shared Material不會被Scorch寫黑。

下一步：從檔案總管啟動 D:/Documents/畢專/Builds/Latest/FireRescue.exe，人工驗收無提示時的辨識、液體站位、牆角連續拉管與一分鐘難度。R仍可重來，但畫面不再提示此按鍵。
