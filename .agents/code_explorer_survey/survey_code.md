# BÁO CÁO KHẢO SÁT KIẾN TRÚC CODEBASE VÀ HỆ THỐNG DỰ ÁN
**Dự án**: Unity 2D Top-Down Endless Shooter  
**Agent thực hiện**: Codebase Architect Explorer  
**Thời gian khảo sát**: 2026-09-21 (UTC 16:59:30)  
**Mục tiêu**: Đánh giá toàn diện hiện trạng mã nguồn, tài nguyên, thiết lập Unity Editor, phân tích khoảng trống tính năng (Gap Analysis) so với yêu cầu phát triển (ORIGINAL_REQUEST.md), và đề xuất kiến trúc phần mềm chuẩn hóa, module hóa cho nhóm phát triển.

---

## 1. TỔNG QUAN HIỆN TRẠNG DỰ ÁN (PROJECT & RUNTIME STATE)

### 1.1. Cấu hình Unity & Trạng thái Trình biên dịch (Compiler Status)
- **Trình biên dịch**: 0 lỗi biên dịch (`0 compiler errors`), kiểm tra trực tiếp qua Unity Editor MCP (`read_console`). Trình soạn thảo bên ngoài ghi nhận thông báo đường dẫn VS, không ảnh hưởng runtime.
- **Assembly Definition**: Không có file `.asmdef` tùy biến trong `Assets/`. Toàn bộ mã nguồn C# được biên dịch mặc định vào `Assembly-CSharp.dll`.
- **Hệ thống Input**:
  - Không cài đặt gói mới `com.unity.inputsystem`.
  - Dự án sử dụng hoàn toàn **Unity Legacy Input Manager** (`ProjectSettings/InputManager.asset`), đã có sẵn các trục và nút:
    - Trục di chuyển: `"Horizontal"` (A/D, Left/Right), `"Vertical"` (W/S, Down/Up).
    - Nút bắn: `"Fire1"` (Chuột trái - `mouse 0`, Left Ctrl).
    - Nút phụ: `"Fire2"` (Chuột phải - `mouse 1`, Left Alt).
    - Phím phụ: Dễ dàng đọc qua `Input.GetKeyDown(KeyCode.E)`, `Input.GetKeyDown(KeyCode.Escape)`, `Input.GetKeyDown(KeyCode.P)`.
- **Hệ thống Physics 2D**:
  - `Physics2DSettings.asset`: Trọng lực `m_Gravity: (0, 0)` (chuẩn cho game 2D top-down nhìn từ trên xuống).
  - `m_QueriesHitTriggers: 1` (Raycast/Overlap bắt được Trigger).
  - Layer Collision Matrix đang bật va chạm cho tất cả các layer (`0xFFFFFFFF`).
- **Tags & Layers**:
  - `TagManager.asset`: Chỉ có tag tùy biến `Colliders`, các tag mặc định `Untagged`, `Player`, `MainCamera`.
  - Chưa định nghĩa layer riêng cho Player, Enemy, Bullet, Item.
  - *Khuyến nghị kiến trúc*: Nên tận dụng component-based checking (`GetComponent<IDamageable>()`, `GetComponent<PlayerHealth>()`) thay vì phụ thuộc hoàn toàn vào chuỗi tag cứng để tránh lỗi ngoại lệ runtime (`Tag undefined`).
- **Build Settings & Scenes**:
  - `EditorBuildSettings.asset`: Chỉ có duy nhất scene `Assets/Scenes/shooting.unity` (Build Index 0).
  - Scene thứ hai là `Assets/Tiny RPG Forest/Scenes/Demo.unity` đóng vai trò showcase môi trường của asset pack.

---

## 2. KIỂM KÊ TÀI NGUYÊN VÀ MÃ NGUỒN HIỆN CÓ

### 2.1. Mã nguồn hiện có (`Assets/scripts/`)
Hiện tại dự án chỉ có đúng **3 script C#** cơ bản:

#### A. `PlayerMovement.cs` (45 dòng)
- **Chức năng**: Điều khiển nhân vật di chuyển và xoay theo con trỏ chuột.
- **Cơ chế**:
  - `Update()`: Đọc `Input.GetAxisRaw("Horizontal")`, `Input.GetAxisRaw("Vertical")` và tính `mousePos = cam.ScreenToWorldPoint(Input.mousePosition)`.
  - `FixedUpdate()`: Gọi `rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime)` và xoay `rb.rotation = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f`.
- **Hạn chế & Rủi ro phát hiện**:
  - Biến `cam` nếu không gán trong Inspector sẽ gây `NullReferenceException`. Cần fallback tự động `if (cam == null) cam = Camera.main;`.
  - Chưa có cơ chế giới hạn biên độ di chuyển (Map Boundary Clamping). Người chơi có thể đi ra ngoài vùng màn hình.
  - Chưa có hệ thống máu, chưa có xử lý nhận sát thương hay thời gian bất tử (i-frames).

#### B. `Shooting.cs` (27 dòng)
- **Chức năng**: Bắn đạn cơ bản từ `firePoint`.
- **Cơ chế**:
  - Bắt sự kiện `Input.GetButtonDown("Fire1")`.
  - Khởi tạo `Instantiate(bulletPrefab, firePoint.position, firePoint.rotation)`.
  - Đẩy đạn bằng xung lực: `rb.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse)`.
- **Hạn chế & Rủi ro phát hiện**:
  - Không có tốc độ bắn (Fire Rate / Cooldown), người chơi có thể spam click chuột không giới hạn.
  - Chưa tích hợp cơ chế ném lựu đạn (phím E hoặc Chuột phải).
  - Chưa có âm thanh hay hiệu ứng lóe sáng đầu nòng (muzzle flash).

#### C. `Bullet.cs` (15 dòng)
- **Chức năng**: Xử lý va chạm vật lý của đạn.
- **Cơ chế**:
  - Sử dụng sự kiện `OnCollisionEnter2D(Collision2D collision)`.
  - Sinh `hitEffect` (`Fire Effect.prefab`) tại vị trí va chạm, hủy effect sau 0.5s và hủy viên đạn `Destroy(this.gameObject)`.
- **Hạn chế & Rủi ro phát hiện**:
  - **Không có logic gây sát thương**: Đạn chạm bất kỳ collider nào cũng tự hủy mà không trừ máu đối tượng bị bắn trúng!
  - **Memory Leak**: Nếu đạn bay ra ngoài map mà không trúng vật cản, viên đạn tồn tại vĩnh viễn trong Scene. Cần có lifetime tự hủy sau 3 - 5 giây.
  - Sử dụng `OnCollisionEnter2D` khiến đạn va chạm vật lý tạo phản lực đẩy lùi quái vật/vật thể, thay vì xuyên chạm dạng `Trigger` mượt mà (chuẩn 2D shooter).

---

### 2.2. Kiểm kê Prefabs & Scenes

#### A. Scene chính `Assets/Scenes/shooting.unity`
- **Main Camera**: Vị trí `(1.96, 0.04, -10.0)`, Orthographic Size = 5.0. Chưa có script Camera Follow (camera đứng yên).
- **Player**:
  - Vị trí ban đầu: `(2.23, 0.11, 0)`.
  - Sprite: `soldier-no-bg.png`.
  - Rigidbody2D: Dynamic, mass = 1, `freezeRotation = true` (FreezeRotationZ).
  - BoxCollider2D: Kích thước `(1.56, 1.86)`, `isTrigger = false`.
  - Child Object `Fire Point`: Vị trí tương đối `(0.35, 1.18, 0)`.
- **Bản đồ môi trường**:
  - `floor` (Tilemap): Kích thước vùng lưới 24 x 11 ô, trải dài từ X: `[-9.3, 14.7]`, Y: `[-4.9, 6.1]`.
  - `arvores` (Trees Tilemap): Chứa các ô cây cảnh trí.
  - `Colliders` (GameObject cha): Chứa 3 vật cản có collider: `Arvore` (Cây), `Arbusto` (Bụi cây), `Cerca` (Hàng rào).
  - **Chưa có biên bản đồ kín (Boundary Colliders)**: Map chưa có 4 bức tường bao quanh để chặn người chơi không lọt ra ngoài hư không.
- **Giao diện người dùng (UI)**: Trong Scene hoàn toàn **chưa có Canvas, EventSystem hay bất kỳ UI component nào**.
- **Kẻ địch & Quản lý**: Chưa có Spawner, chưa có Enemy, chưa có GameManager.

#### B. Prefabs có sẵn
1. `Assets/Bullet.prefab`: Sprite đạn vàng, BoxCollider2D non-trigger, Rigidbody2D gravity 0, script Bullet.
2. `Assets/Fire Effect.prefab`: Hiệu ứng nổ lửa 16x16, Animator phát clip `FireAnimation.anim`.
3. `Assets/tilemap/floor.prefab` & `environment.prefab`: Prefab chứa dữ liệu gạch và địa hình.

#### C. Kho tài nguyên đồ họa (Sprite Assets) phong phú có thể tận dụng
Tại thư mục `Assets/Tiny RPG Forest/Artwork/sprites/`:
- **Quái vật & Kẻ địch**:
  - `mole/`: 15 sprite chuột chũi (Idle, Walk các hướng). Rất phù hợp cho **Chaser (Cận chiến)** hoặc **Rusher (Tốc độ)**.
  - `treant/`: 15 sprite người cây (Idle, Walk các hướng). Rất phù hợp cho **Shooter (Bắn xa)** hoặc biến thể to lớn làm **Boss**.
  - `hero/`: 45 sprite nhân vật với hoạt cảnh vung kiếm, đi bộ. Có thể dùng làm thêm loại quái hoặc biến thể quái dạng người.
- **Hiệu ứng & HUD**:
  - `misc/hearts/`: Gồm `hearts-1.png` (Trái tim đỏ đầy) và `hearts-2.png` (Trái tim rỗng/mất máu) -> Hoàn hảo cho **HUD 5 HP**.
  - `misc/enemy-death/`: 6 frame hoạt họa nổ/tiêu diệt (`enemy-death-1.png` đến `6.png`) -> Làm hiệu ứng quái chết hoặc vụ nổ lựu đạn.
  - `misc/arrow.png`: Mũi tên định hướng hoặc đạn quái.
  - `misc/gem/`, `misc/coin/`: Vật phẩm rơi.
  - `bullets/Fire Effect and Bullet 16x16.png`: Sprite sheet cắt sẵn hàng chục loại đạn màu sắc khác nhau (đạn tròn, đạn dài, đạn chùm) và hiệu ứng nổ.
- **Âm thanh (Audio)**: Hiện tại dự án chưa có bất kỳ file âm thanh nào (`.wav`, `.mp3`, `.ogg`). Cần xây dựng hệ thống phát âm thanh hỗ trợ hoặc synthesizer âm thanh procedural (tạo sound wave trực tiếp bằng C#) để game có đầy đủ tiếng bắn, nổ, nhận đòn mà không bắt buộc phụ thuộc file ngoài.

---

## 3. PHÂN TÍCH KHOẢNG TRỐNG TÍNH NĂNG (GAP ANALYSIS)

Dưới đây là bảng so chiếu chi tiết giữa yêu cầu (ORIGINAL_REQUEST.md) và hiện trạng codebase:

| Yêu cầu kỹ thuật | Thành phần cần có | Hiện trạng codebase | Đánh giá & Hướng giải quyết |
| :--- | :--- | :--- | :--- |
| **R1. Player Combat & Health** | - Di chuyển WASD 8 hướng<br>- Xoay theo chuột<br>- Bắn đạn chuột trái<br>- 5 HP, mất 1 HP/hit, i-frames chớp nháy<br>- Biên giới bản đồ kín | - Di chuyển WASD cơ bản đã có.<br>- Xoay theo chuột đã có.<br>- Bắn đạn đã có nhưng chưa có cooldown.<br>- **Chưa có**: Máu (HP), i-frames, Game Over khi chết, tường chặn map. | Cần tạo `PlayerHealth.cs` quản lý 5 HP, Coroutine chớp nháy SpriteRenderer khi bất tử, phát sự kiện Game Over. Cập nhật `Shooting.cs` thêm cooldown. Tạo 4 BoxCollider2D làm biên giới kín quanh sân đấu. |
| **R2. Endless Enemy Spawning** | - Spawner tự sinh quái từ rìa màn hình theo thời gian thực<br>- Độ khó tăng dần theo thời gian/điểm số<br>- Tối thiểu 3 loại quái: Chaser, Shooter, Rusher<br>- Cộng điểm khi diệt quái | - **Chưa có gì** (0 enemy script, 0 spawner). | Xây dựng interface `IDamageable`, lớp cha `EnemyBase.cs`. Kế thừa ra `ChaserEnemy.cs`, `ShooterEnemy.cs` (bắn `EnemyBullet`), `RusherEnemy.cs`. Xây dựng `EnemySpawner.cs` tính toán tọa độ spawn ngoài rìa Camera/Map và quản lý nhịp spawn tăng tiến. |
| **R3. Grenade AoE Mechanic** | - Quái chết rơi item lựu đạn (tỉ lệ %)<br>- Người chơi nhặt được, hiển thị số lượng trên HUD<br>- Phím E / Chuột phải để ném<br>- Nổ AoE gây sát thương diện rộng | - **Chưa có gì**. | Tạo `GrenadePickup.cs` (rơi khi quái chết). Thêm `GrenadeThrower.cs` trên Player lắng nghe phím E / Fire2. Tạo `GrenadeProjectile.cs` bay đến đích và `ExplosionEffect.cs` dùng `Physics2D.OverlapCircleAll` quét tiêu diệt quái vật. |
| **R4. Boss Encounter** | - Kích hoạt khi đạt 500 điểm<br>- Máu lớn, thanh máu riêng (Boss HP bar)<br>- Kỹ năng bắn đạn tỏa tròn 360 độ theo chu kỳ<br>- Hạ Boss nhận điểm thưởng lớn, tiếp tục chế độ Endless | - **Chưa có gì**. | Tạo `BossController.cs` kế thừa hoặc mở rộng từ Enemy, kích hoạt khi `GameManager.Instance.Score >= 500`. Viết thuật toán sinh đạn hình nan quạt/vòng tròn 360 độ (12-16 viên đạn). Tích hợp Boss HP UI. Khi chết, chuyển cờ `bossDefeated = true` và cho phép wave endless tiếp diễn. |
| **R5. UI, HUD & Game Flow** | - In-Game HUD: 5 HP (trái tim), Score, High Score (PlayerPrefs), Grenade count, Boss HP bar<br>- Main Menu: Play, Guide, Quit<br>- Pause Menu: ESC/P dừng game, Resume, Menu<br>- Game Over Screen: Restart, Menu, High Score<br>- Victory / Continue Screen | - **Chưa có gì** (Scene không có Canvas UI nào). | Xây dựng hệ thống UI hoàn chỉnh bằng `UnityEngine.UI` tiêu chuẩn (tương thích 100%, không lo lỗi font TMP). Tạo `GameManager.cs` (Finite State Machine: Menu, Playing, Paused, GameOver, Victory) và `UIManager.cs` quản lý bật tắt các Panel. |
| **R6. Visuals & Audio Feedback** | - Flash trắng/đỏ khi trúng đòn<br>- Hiệu ứng hạt nổ, đạn va chạm<br>- Âm thanh bắn, nổ, nhặt đồ, game over | - Có sẵn prefab `Fire Effect.prefab`.<br>- **Chưa có** âm thanh, chưa có flash sát thương. | Viết hàm `DamageFlash` đổi màu Sprite sang đỏ/trắng trong 0.1s. Tận dụng animation `enemy-death` làm prefab nổ quái chết. Tạo `AudioManager.cs` với procedural sound synthesis (tạo âm thanh retro 8-bit bằng code) để chạy ổn định không cần asset ngoài. |

---

## 4. BẢN THIẾT KẾ KIẾN TRÚC MÃ NGUỒN ĐỀ XUẤT (SYSTEM ARCHITECTURE)

Để phục vụ tốt cho việc phân chia công việc trong nhóm 5 người dễ bảo trì, tránh xung đột mã nguồn (merge conflict) và chuẩn hóa theo quy chuẩn Unity C#, kiến trúc game được chia thành 5 module độc lập:

```
                          ┌────────────────────────┐
                          │      GameManager       │ (Score, HighScore, State, Flow)
                          └───────────┬────────────┘
                                      │
          ┌───────────────────────────┼───────────────────────────┐
          │                           │                           │
┌─────────▼────────┐        ┌─────────▼────────┐        ┌─────────▼────────┐
│    UIManager     │        │   AudioManager   │        │   EnemySpawner   │
│ (HUD, Menus,     │        │ (SFX, Procedural │        │ (Wave, Endless,  │
│  State Panels)   │        │  Sound Generator)│        │  Difficulty)     │
└──────────────────┘        └──────────────────┘        └─────────┬────────┘
                                                                  │ spawns
          ┌───────────────────────────────────────────────────────┼─────────────────────────┐
          │                                                       │                         │
┌─────────▼────────┐                                    ┌─────────▼────────┐      ┌─────────▼────────┐
│      Player      │                                    │     Enemies      │      │   BossController │
│ ├── PlayerMovement                                    │ ├── ChaserEnemy  │      │ ├── 360 Radial   │
│ ├── PlayerHealth (5 HP, i-frames)                     │ ├── ShooterEnemy │      │ ├── Big HP       │
│ ├── Shooting (Pistol)                                 │ ├── RusherEnemy  │      │ └── Boss HP Bar  │
│ └── GrenadeThrower                                    │ └── EnemyBullet  │      └──────────────────┘
└─────────┬────────┘                                    └─────────┬────────┘
          │ fires / throws                                        │ drops on death
┌─────────▼────────┐                                    ┌─────────▼────────┐
│ Bullets & Grenade│ ───deals damage via IDamageable──> │  GrenadePickup   │
│ ├── Bullet       │                                    └──────────────────┘
│ └── GrenadeProj  │
└──────────────────┘
```

### 4.1. Định nghĩa Interface cốt lõi (`IDamageable.cs`)
Nhằm tách biệt hoàn toàn giữa hệ thống vũ khí (Đạn, Lựu đạn) và hệ thống kẻ địch, tránh việc phải ép kiểu (`GetComponent<Chaser>()`, `GetComponent<Boss>()`):

```csharp
public interface IDamageable
{
    void TakeDamage(int damage);
    bool IsAlive { get; }
}
```

### 4.2. Phân chia Module & Danh mục File cần xây dựng

#### Module 1: Core & Game Flow
- `GameManager.cs`: Singleton, quản lý trạng thái trò chơi (`Playing`, `Paused`, `GameOver`, `BossFight`), điểm số, kỷ lục (lưu `PlayerPrefs.GetInt("HighScore", 0)`), cờ diệt Boss.
- `UIManager.cs`: Quản lý hiển thị:
  - HUD: 5 hình ảnh trái tim (`Image[] hearts`), văn bản Điểm số (`scoreText`), Điểm cao (`highScoreText`), số lựu đạn (`grenadeText`), cụm thanh máu Boss (`bossHealthSlider`, `bossPanel`).
  - Panels: `MainMenuPanel`, `PauseMenuPanel`, `GameOverPanel`, `VictoryPanel`.
- `AudioManager.cs`: Singleton phát âm thanh (Shoot, Hit, Explosion, Pickup, Hurt, GameOver, Victory). Tích hợp bộ phát âm thanh Procedural Sound Synthesis nếu không có file âm thanh tĩnh.
- `CameraFollow.cs`: Gắn vào `Main Camera`, bám theo vị trí của Player với độ trễ mượt (`Vector3.Lerp`) và bị giới hạn (clamp) trong phạm vi biên bản đồ.

#### Module 2: Player & Weapon Systems
- `PlayerMovement.cs` *(Kế thừa & Cải tiến)*:
  - Bổ sung fallback camera: `if (cam == null) cam = Camera.main;`.
  - Bổ sung giới hạn biên tọa độ (`Mathf.Clamp` trong phạm vi Min/Max của sàn đấu).
- `PlayerHealth.cs` *(Mới)*:
  - Máu tối đa: 5 HP.
  - Quản lý thời gian bất tử (i-frame: 1.0 giây sau khi bị đánh trúng).
  - Coroutine chớp nháy SpriteRenderer (đổi alpha hoặc màu đỏ/trắng).
  - Khi HP = 0: Gọi `GameManager.Instance.TriggerGameOver()`.
- `Shooting.cs` *(Cải tiến)*:
  - Thêm `fireRate` (thời gian trễ giữa các lần bắn, ví dụ 0.2s).
  - Kích hoạt âm thanh bắn qua `AudioManager`.
- `GrenadeThrower.cs` *(Mới)*:
  - Quản lý số lượng lựu đạn (`grenadeCount`).
  - Lắng nghe phím `KeyCode.E` hoặc Chuột phải (`Input.GetButtonDown("Fire2")`).
  - Ném `GrenadeProjectile` về phía con trỏ chuột.
- `Bullet.cs` *(Cải tiến)*:
  - Chuyển sang va chạm trigger (`OnTriggerEnter2D`) hoặc hỗ trợ cả collision.
  - Thêm thời gian tự hủy (`Destroy(gameObject, 3f)`) tránh rò rỉ bộ nhớ.
  - Kiểm tra đối tượng trúng có `IDamageable` hay không để trừ máu.

#### Module 3: Enemy Systems
- `EnemyBase.cs` *(Mới)*: Lớp cơ sở trừu tượng kế thừa `MonoBehaviour, IDamageable`.
  - Quản lý HP quái, tốc độ di chuyển, điểm thưởng khi chết (`scoreValue`), tỉ lệ rơi lựu đạn (`grenadeDropChance`).
  - Xử lý hiệu ứng flash đỏ khi nhận sát thương.
  - Xử lý khi chết: Rơi lựu đạn nếu may mắn, cộng điểm cho `GameManager`, sinh hiệu ứng nổ `enemy-death`, tự hủy.
- `ChaserEnemy.cs` *(Mới)*:
  - Di chuyển trực tiếp hướng về phía Player (`Vector2.MoveTowards`).
  - Tốc độ trung bình (ví dụ 3.0f), máu 2 - 3 HP.
  - Gây sát thương khi chạm vào Player (`PlayerHealth.TakeDamage(1)`).
- `ShooterEnemy.cs` *(Mới)*:
  - Giữ khoảng cách cố định với người chơi (nếu gần hơn cự ly an toàn thì lùi lại hoặc dừng, nếu xa thì tiến lại).
  - Định kỳ bắn `EnemyBullet` hướng về phía Player.
- `RusherEnemy.cs` *(Mới)*:
  - Máu thấp (1 HP), nhưng tốc độ di chuyển rất nhanh (ví dụ 5.5f).
  - Áp sát nhanh tạo áp lực cho người chơi.
- `EnemyBullet.cs` *(Mới)*:
  - Đạn của quái bắn ra, bay theo hướng xác định.
  - Chạm vào Player sẽ gây 1 sát thương và tự hủy. Chạm vào tường/vật cản tự hủy.

#### Module 4: Boss Encounter
- `BossController.cs` *(Mới)*:
  - Kế thừa `EnemyBase`, lượng máu lớn (ví dụ 40 - 60 HP).
  - Cập nhật giá trị lên Boss HP Slider trên HUD.
  - Cơ chế tấn công:
    1. Di chuyển áp sát chậm rãi.
    2. Kỹ năng đặc trưng: Bắn đạn tỏa tròn 360 độ (Radial Burst) theo chu kỳ (ví dụ mỗi 3 - 4 giây bắn một đợt 12 - 16 viên đạn dàn đều 360 độ).
  - Khi Boss chết: Cộng 200 điểm thưởng, ẩn thanh máu Boss, gọi `GameManager.Instance.OnBossDefeated()` (hiển thị thông báo chiến thắng / mở nút Tiếp tục Endless).

#### Module 5: Spawner, Items & VFX
- `EnemySpawner.cs` *(Mới)*:
  - Lấy các điểm spawn hoặc tính toán vị trí ngẫu nhiên dọc theo 4 cạnh ngoài rìa bản đồ (hoặc ngoài tầm nhìn camera).
  - Tần suất spawn (`spawnInterval`) bắt đầu từ 2.5s, giảm dần theo thời gian/điểm số xuống tối thiểu 0.8s.
  - Tỉ lệ sinh 3 loại quái dựa trên trọng số thay đổi theo thời gian sống sót.
  - Khi điểm >= 500 và Boss chưa từng xuất hiện: Tạm dừng spawn quái con, sinh Boss; sau khi Boss chết, kích hoạt lại spawner với nhịp độ dồn dập hơn.
- `GrenadePickup.cs` *(Mới)*:
  - Vật phẩm có collider trigger. Khi Player chạm vào, tăng 1 lựu đạn, phát âm thanh nhặt đồ, tự hủy.
- `GrenadeProjectile.cs` *(Mới)*:
  - Đạn lựu đạn được ném ra, giảm tốc dần (simulate quăng đồ) hoặc bay theo đường đạn đến vị trí đích, sau đó phát nổ.
- `ExplosionDamage.cs` *(Mới)*:
  - Thực hiện `Physics2D.OverlapCircleAll(position, explosionRadius)`.
  - Quét tất cả `IDamageable` và gây sát thương lớn (đủ hạ gục quái thường trong bán kính).
  - Tạo hiệu ứng hình ảnh vụ nổ và âm thanh nổ vang.

---

## 5. THIẾT KẾ BẢN ĐỒ VÀ RÀO CHẮN VẬT LÝ (ARENA BOUNDARIES)

Hiện tại bản đồ sân khấu (`shooting.unity`) chưa có tường rào bao quanh, người chơi có thể chạy vô tận ra khỏi màn hình. Cần bổ sung GameObject cha `MapBounds` chứa 4 cạnh BoxCollider2D:
- **Tường Trên (Top Wall)**: Y = +6.5, kích thước (30, 1).
- **Tường Dưới (Bottom Wall)**: Y = -5.5, kích thước (30, 1).
- **Tường Trái (Left Wall)**: X = -10.5, kích thước (1, 14).
- **Tường Phải (Right Wall)**: X = +15.5, kích thước (1, 14).
Vật lý: Đặt layer hoặc tag phù hợp (`Colliders`), đảm bảo Player và Đạn chạm tường bị chặn lại / phát nổ.

---

## 6. PHÂN CÔNG ĐỀ XUẤT CHO NHÓM 5 NGƯỜI (5-MEMBER ROLES)

Kiến trúc trên được module hóa trực giao, rất thuận tiện phân chia cho nhóm 5 người:
1. **Dev 1 (Player & Combat Lead)**: Hoàn thiện `PlayerMovement` (clamping), `PlayerHealth` (5 HP, i-frames, blink), `Shooting` (cooldown), `GrenadeThrower`.
2. **Dev 2 (Enemy Systems Lead)**: Xây dựng `IDamageable`, `EnemyBase`, 3 loại quái (`ChaserEnemy`, `ShooterEnemy`, `RusherEnemy`), `EnemyBullet`.
3. **Dev 3 (Boss & Spawner Specialist)**: Xây dựng `BossController` (Radial 360 burst), `EnemySpawner` (Endless scaling, tọa độ spawn rìa map), logic kích hoạt Boss mốc 500 điểm.
4. **Dev 4 (Grenade & Environment / VFX)**: Xây dựng `GrenadePickup`, `GrenadeProjectile`, `ExplosionDamage`, dựng rào chắn bản đồ `MapBounds`, Camera Follow, các prefab hiệu ứng nổ.
5. **Dev 5 (UI & Game Loop & Audio)**: Xây dựng `GameManager`, `UIManager` (HUD 5 trái tim, điểm số, kỷ lục PlayerPrefs, Boss HP bar, Main Menu, Pause, Game Over, Victory/Continue), `AudioManager`.

---

## 7. KẾT LUẬN & KHUYẾN NGHỊ KỸ THUẬT

1. **Tính tương thích tuyệt đối**:
   - Sử dụng Unity Legacy Input Manager giúp code chạy mượt mà, không gặp bất kỳ xung đột nào từ package mới.
   - Sử dụng standard `UnityEngine.UI` thay cho TextMeshPro tránh triệt để lỗi thiếu TMP Font Asset.
2. **Độ ổn định vật lý**:
   - Chuyển đổi đạn và vật phẩm nhặt sang cơ chế `isTrigger: true` kết hợp `OnTriggerEnter2D` để chuyển động bắn không làm xô đẩy vị trí người chơi và quái vật.
3. **Bảo tồn tài nguyên có sẵn**:
   - Tận dụng tối đa sprite sheet `mole`, `treant`, `enemy-death`, `hearts`, `bullets` có sẵn trong thư mục dự án mà không cần import thêm thư viện đồ họa nặng nề.
