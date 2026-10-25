# 角色 v0.3 最終驗證

2026-10-07，Unity 6000.5.0b9，`D:/unity/6000.5.0b9/Editor/Unity.exe`。

## 本輪確證

| 項目 | 命令 executeMethod | 結果 |
| --- | --- | --- |
| 匯入／角色場景呈現／預覽／Mesh | FireRescue.Editor.CharacterBatchLaunch.ApplyAndCapture | exit0，SETUP＋MESH PASS，15項 |
| 骨架及真實移動／NPC動畫 | FireRescue.Editor.CharacterBatchLaunch.Animation | exit0，33項Play Mode PASS |
| 原住宅水／油／NPC核心循環 | FireRescue.Editor.CharacterBatchLaunch.Residential | exit0，40項Play Mode PASS |
| 最後只用臨時光照／相機的預覽 | FireRescue.Editor.CharacterBatchLaunch.Preview | exit0，24正面Run＋24側面Run＋30Idle |

啟動均使用 `-batchmode -projectPath "D:/Documents/畢專" -executeMethod <上表> -logFile <對應日誌>`，不加 -quit，由啟動器／測試完成回傳退出。啟動器等Editor匯入／編譯就緒，不以背景程序啟動當完成證據。

33項角色＝原31項＋真實Nozzle掛Chest／Run握點<7cm。Mesh世界座標使用BakeMesh(mesh,true)避免Renderer縮放重複乘；早期failed量測報告無效，詳見 `Docs/Art/CHARACTERS_v0.3_REVIEW.md`。沒有放寬標準、改玩法或重新建住宅。

GIF是實際Animator影格：Run／Run-side各24幀800ms、Idle30幀2000ms。作者18骨、兩個loop、stable Root；真正Unity世界高度兩者1.600m，Run最低表面2.9mm。攝影棚材質／燈光／相機都在臨時scene，最後不讀寫全域QualitySettings；形式升級曾由hash比對發現並限定恢復。

58個原Runtime、其他家具資產、ProjectSettings逐一SHA256相同；GUID4個相同，詳見PRESERVATION.md。場景保存前所有既有MonoBehaviour／Collider序列化比對相同，排除只呈現的CharacterLocomotionAnimation。此資料比對不是實體鍵鼠／網路／Player build證明。

## 保留邊界

接受Prototype人物美術：仍為簡單平色、穩定持管小跑，沒有精確雙手IK。未驗獨立Windows Player build／硬體輸入／多人；沒有正式UI、擴張地圖或元素系統。Unity舊Editor API警告、背景授權警告維持分類，不更新套件、清Console或隱藏失敗。較早輪的問題／日誌仍在Logs和Art迭代資料夾。

未commit／push／發布。手動開ResidentialFire.unity按Play即可；角色移動／停止自動Run／Idle，F接管、左鍵噴射、E安撫不變。
