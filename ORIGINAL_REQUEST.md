# Original User Request

## Initial Request — 2026-09-21T16:54:55Z

Phát triển một game 2D top-down endless shooter hoàn chỉnh trên Unity dựa trên source code có sẵn tại repository `top-down-shooting-unity`. Người chơi điều khiển nhân vật trong bản đồ kín bằng WASD, ngắm chuột và bắn chuột trái; tiêu diệt quái vật spawn vô tận từ rìa bản đồ, nhặt và ném lựu đạn gây sát thương diện rộng, và chiến đấu chống Boss có kỹ năng bắn đạn tỏa tròn khi đạt mốc 500 điểm.

Working directory: c:/Users/vclmi/Documents/Class/LTGCB/top-down-shooting-unity-main/top-down-shooting-unity
Integrity mode: development

## Requirements

### R1. Player Combat & Health System
- Nhân vật di chuyển 8 hướng bằng phím WASD trong phạm vi bản đồ kín (có biên giới ngăn rơi khỏi map), xoay mặt theo con trỏ chuột, và bắn đạn cơ bản bằng chuột trái.
- Người chơi có tối đa 5 HP. Mỗi lần va chạm quái vật, đạn của quái hoặc đòn tấn công của boss sẽ bị mất đúng 1 HP, kèm hiệu ứng bất tử chớp nháy tạm thời (i-frames) để tránh trừ liên tục trong 1 frame.
- Khi HP giảm về 0, kích hoạt trạng thái Game Over.

### R2. Endless Enemy Spawning & Varied Enemy Types
- Hệ thống Spawner sinh quái tự động từ các vị trí ngoài biên màn hình/rìa map theo thời gian thực.
- Nhịp độ xuất hiện (tần suất spawn và số lượng quái đồng thời) tăng dần theo thời gian sinh tồn và tổng điểm số của người chơi.
- Có ít nhất 3 loại quái vật với hành vi phân biệt rõ rệt:
  1. *Chaser (Quái cận chiến)*: Di chuyển thẳng về hướng người chơi với tốc độ trung bình, gây sát thương khi chạm vào.
  2. *Shooter (Quái bắn xa)*: Giữ cự ly nhất định với người chơi và định kỳ bắn đạn nhắm vào người chơi.
  3. *Rusher (Quái tốc độ)*: Máu thấp hơn nhưng tốc độ di chuyển áp sát rất cao.
- Tiêu diệt mỗi loại quái sẽ cộng điểm số tương ứng vào tổng điểm của người chơi.

### R3. Grenade Mechanic (AoE Pickup & Throw)
- Khi quái vật bị tiêu diệt, có xác suất rơi ra vật phẩm Lựu đạn (Grenade item) tại vị trí quái chết.
- Người chơi di chuyển chạm vào để nhặt lựu đạn (cộng vào số lượng dự trữ hiển thị trên HUD).
- Người chơi kích hoạt ném lựu đạn bằng phím E hoặc Chuột phải về hướng con trỏ chuột.
- Lựu đạn phát nổ (sau khoảng trễ thời gian hoặc khi va chạm), tạo vụ nổ diện rộng (AoE) gây sát thương và tiêu diệt tất cả quái vật trong bán kính vụ nổ.

### R4. Boss Encounter
- Khi người chơi đạt mốc 500 điểm, một Boss duy nhất xuất hiện tại vị trí chiến trường.
- Boss có lượng máu lớn hơn đáng kể so với quái thường và hiển thị thanh máu riêng (Boss HP bar) trên màn hình.
- Boss sở hữu một kỹ năng tấn công đặc trưng: bắn vòng đạn tỏa tròn 360 độ (radial burst) theo chu kỳ.
- Khi người chơi tiêu diệt Boss, người chơi nhận điểm thưởng lớn và trò chơi chuyển tiếp cho phép tiếp tục chế độ Endless (không sinh thêm Boss thứ hai).

### R5. UI, HUD & Game Flow Management
- **In-Game HUD**: Hiển thị trực quan 5 đơn vị HP, Điểm số hiện tại (Score), Điểm cao nhất (High Score - lưu qua PlayerPrefs), số lượng lựu đạn đang có, và thanh máu Boss khi Boss xuất hiện.
- **Game State Screens**:
  - *Main Menu*: Nút Play, hướng dẫn điều khiển phím, và Quit.
  - *Pause Menu*: Kích hoạt khi ấn phím ESC/P (dừng `Time.timeScale`), gồm nút Resume và Quay về Menu.
  - *Game Over Screen*: Xuất hiện khi HP về 0, hiển thị điểm số vừa đạt, điểm kỷ lục, nút Chơi lại (Restart) và Menu.
  - *Victory / Continue Screen hoặc Thông báo*: Xuất hiện khi hạ Boss, cho phép người chơi bấm Tiếp tục (Continue) để thử thách kỷ lục điểm endless.
- Kiến trúc code module hóa, phân chia các thành phần độc lập (Player, Enemy, Spawner, Weapon, UI/GameManager) chuẩn phong cách Unity C#, giữ scope tinh gọn cho nhóm 5 người dễ bảo trì và phân công.

### R6. Visuals & Audio Feedback
- Tận dụng hệ thống spritesheet/tileset có sẵn trong `Assets/Tiny RPG Forest` và bổ sung các asset 2D pixel art/sprites phù hợp phong cách visual cho đạn, hiệu ứng vụ nổ lựu đạn, quái và boss.
- Cung cấp phản hồi thị giác rõ ràng (visual feedback): flash trắng/đỏ khi nhận sát thương, particle/effect khi đạn trúng và khi lựu đạn phát nổ.

## Acceptance Criteria

### Combat, Spawning & Items
- [ ] Nhân vật điều khiển mượt mà bằng WASD, xoay chuẩn xác theo chuột, bắn đạn từ họng súng (FirePoint).
- [ ] Người chơi có đúng 5 HP, mất 1 HP khi trúng đòn (có i-frames chống trừ dồn máu); Game Over được kích hoạt ngay khi HP chạm 0.
- [ ] Spawner sinh quái từ ngoài rìa map; tần suất hoặc số lượng tăng lũy tiến theo thời gian / điểm số.
- [ ] Tối thiểu 3 loại quái (Chaser, Shooter, Rusher) hoạt động đúng hành vi riêng biệt (cận chiến, bắn tỉa tầm xa, áp sát nhanh).
- [ ] Quái rơi lựu đạn; nhặt được khi đi qua; ném bằng phím E / Chuột phải; nổ AoE gây sát thương chuẩn xác trong bán kính.

### Boss, Game Loop & Technical Stability
- [ ] Boss kích hoạt khi điểm người chơi đạt 500 điểm; có thanh máu Boss; thực hiện kỹ năng bắn đạn tỏa tròn 360 độ; hạ Boss cho phép tiếp tục endless.
- [ ] Hệ thống UI đầy đủ: HUD (5 HP, score, high score, grenade count, boss status), Main Menu, Pause Menu (ấn ESC), Game Over, Victory/Continue. High score được lưu trữ bền vững giữa các phiên chơi.
- [ ] Không có lỗi biên dịch (0 compiler errors) trên Unity Editor qua kiểm tra Unity MCP (`read_console`).


## 2026-09-22T14:17:51Z

Implement a continuous upward (+Y) endless scrolling map system for the existing Unity 2D top-down shooter, maintaining 8-directional WASD movement, mouse aiming, grenade drops, 5 HP survival, and a 500-point boss encounter with radial projectile barrage.

Working directory: c:\Users\vclmi\Documents\Class\LTGCB\top-down-shooting-unity-main\top-down-shooting-unity
Integrity mode: development

## Requirements

### R1. Forward Camera Scrolling & Player Viewport Clamping
The main camera must continuously scroll along the +Y axis at a baseline speed (default 2.0 u/s) that scales with progression up to a safety ceiling (3.5 u/s). The player's WASD movement must be restricted inside the visible camera viewport; if pushed behind the camera's bottom edge threshold, the player receives damage or triggers game over.

### R2. Modular Map Segment Spawning & Object Pooling
The map must be generated procedurally ahead of the camera using interchangeable MapSegment prefabs (length 20 units) chosen with controlled randomness. Segments moving behind the camera's cleanup distance must be deactivated and recycled into an object pool to guarantee zero runtime GC allocations and memory stability over long play sessions. Each segment must guarantee at least one passable corridor (minimum width 4.0 units) free of impassable obstacles.

### R3. Seamless Boss Arena Encounter & Resume Loop
Upon reaching the configurable threshold (500 points), standard segment generation temporarily halts and a dedicated Boss Arena segment spawns ahead. When the camera aligns with the boss arena, camera scrolling locks in place. The boss spawns and executes its 360-degree radial projectile barrage. When defeated, the boss drops 2 guaranteed grenades, awards 500 points, unlocks the camera, and resumes normal endless scrolling.

### R4. HUD Indicators & Telegraphed Warnings
The HUD must display current Score, High Score, 5 HP Heart icons, Grenade count, travelled Distance (in meters), an early telegraph warning ("BOSS APPROACHING!"), and a "BOSS ARENA" status banner while in the arena.

### R5. unity-MCP Automation & Automated Verification
The system must be inspectable and verifiable using unity-MCP tools and must pass automated unit/integration tests covering segment connection, pooling recycling, player viewport bounds clamping, and boss encounter state transitions.

## Acceptance Criteria

### Camera & Player Movement
- [ ] Camera auto-scrolls along +Y smoothly without jitter in FixedUpdate/LateUpdate.
- [ ] Player WASD movement remains responsive within camera boundaries (viewport X: 0.05 - 0.95, Y: 0.08 - 0.92).
- [ ] Bottom kill/push plane prevents player from getting lost behind the moving screen.

### Segment Generation & Pooling
- [ ] At least 3 distinct interchangeable MapSegment prefabs with varied obstacle layouts exist and spawn seamlessly without collision overlap.
- [ ] Passing corridors are guaranteed (width >= 4 units) so no impossible dead-ends occur.
- [ ] Off-screen segments are cleanly recycled to object pool without GameObject.Destroy or memory leaks.

### Combat & Spawning Mechanics
- [ ] Enemies spawn exclusively from valid SpawnPoints inside upcoming segments or dynamic perimeter relative to moving camera.
- [ ] Grenade pickups spawn or drop from killed enemies; grenade throw AoE functions accurately during scrolling.
- [ ] Player health behaves correctly: 5 HP maximum, losing 1 HP per hit, triggering Game Over at 0 HP.

### Boss Arena Flow
- [ ] At 500 points, boss arena segment spawns ahead and camera stops scrolling when reaching arena center.
- [ ] Boss executes telegraphed radial barrage (16 bullets, 360 degrees).
- [ ] Defeating boss awards 500 points, drops 2 guaranteed grenades, triggers victory fanfare, and resumes endless scrolling upon continuation.

### Verification & Stability
- [ ] Game runs continuously for 10+ minutes without FPS drop or unbounded memory usage.
- [ ] NUnit tests in Unity Test Runner pass via unity-MCP `run_tests`.
