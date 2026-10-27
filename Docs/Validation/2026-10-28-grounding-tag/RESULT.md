# NPC心聲標籤與啟動落地（2026-10-28）

- 標籤：預設「好想吃布丁...」，無居民前綴，細白框／淡色透明底／白字＋布丁淡黃字，頭上1.65m投影、文字陰影。使用者提供的How to Fish圖片為風格參考，不取用原遊戲資產。
- 啟動回報：使用者確認是角色持續穿地；但背景Editor及Windows玩家保持原玩法程式、正常ReadDevices=true測試6秒，均未重現。`editor-original.txt`、`player-original.txt`保存基線。
- 地板BoxCollider啟用、非Trigger，範圍含出生點、Ground(0)／Player(9)未忽略碰撞。真正Windows初始Y=.1，之後Y=.125，15732個樣本皆grounded；不需要質量或Rigidbody來充當靜態地板。
- `startup.txt`補測30/60/144Hz與.1/.333/1秒時間步、跨原正門來回及新Tag精確字串，PASS。此為針對性落地／文字檢查，不是全關手感重測。
- **使用者之後回覆「已經沒這問題」**。根因未確認，沒有修改地板、角色碰撞、出生點或重力，也不謊稱修復持續下墜。
- 新增opt-in `--verify-startup`診斷與Editor驗證，沒有正常啟動的每幀檔案紀錄或新遊戲系統；正常遊戲不啟用該probe。
- Windows建置沿用 `FirstReview1027Setup.BatchBuild`，固定Builds/Latest，版本文字檢查由`ReviewPlayerSmoke --verify-review`完成，最終結果見 `build.txt`、`player-smoke.txt`。背景黑圖不算新標籤視覺驗收，未做螢幕控制。
- 最終建置Succeeded／0錯誤0警告，真正Windows D3D11 smoke為15項PASS，含新「好想吃布丁...」字串。未重跑無關全關套件或修改既有玩法。

若未來再現掉落，先確認執行檔路徑／BUILD_INFO並取得錄影、根物件位置與地板碰撞證據，再改核心程式；不要只加更大地板或每幀傳送掩蓋。
