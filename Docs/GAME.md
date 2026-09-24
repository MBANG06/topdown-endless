# Top-Down Endless Shooter — Tài liệu game

Game bắn súng góc nhìn từ trên xuống (top-down) 2D trên Unity **2022.3.62f2**.
Người chơi sống sót trong map cuộn vô tận hướng lên (+Y), farm điểm, nhặt/ném lựu đạn
và đánh Boss mỗi 500 điểm. Scene chính: `Assets/Scenes/shooting.unity`.

## 1. Điều khiển

| Phím / Chuột | Hành động |
|---|---|
| `W A S D` | Di chuyển 8 hướng (tốc độ 5 u/s), kẹt trong khung camera |
| Chuột (di) | Nhân vật xoay mặt theo con trỏ |
| Chuột trái | Bắn đạn (sát thương 1, tốc độ đạn 20, cooldown 0.2s) |
| `E` / Chuột phải | Ném lựu đạn về hướng chuột (tối đa 7u) |
| `ESC` / `P` | Pause / Resume |
| Nút UI | PLAY GAME, CONTROLS, QUIT, RESUME, RESTART, MAIN MENU, PLAY AGAIN, CONTINUE |

Art phong cách pixel (Tiny RPG Forest). Text HUD: `SCORE: 00000`, `HIGH: 00000`,
`x N` (lựu đạn), `DIST: 0000m`, `BOSS APPROACHING!`, `BOSS ARENA`.

## 2. Luật chơi core

- **HP:** người chơi có **5 HP**. Mỗi đòn trúng (chạm quái, đạn quái, đạn Boss,
  kẹt đáy màn hình) mất **đúng 1 HP**, kèm **bất tử chớp nháy 1.0s (i-frames)**
  chống trừ dồn. HP về 0 → Game Over.
- **Điểm:** giết quái cộng điểm theo loại (xem §3). Điểm cao nhất lưu `PlayerPrefs`
  key `"HighScore"`, giữ qua các phiên chơi.
- **Camera cuộn +Y liên tục:** tốc độ cơ bản **2.0 u/s**, tăng dần theo quãng đường
  tới trần **3.5 u/s**. Người chơi bị kẹp trong viewport (X 0.05–0.95, Y 0.08–0.92);
  tụt lại sau mép dưới camera sẽ bị đẩy lên / mất 1 HP / Game Over.
- **Main Menu → Playing → (Paused) → Game Over / Victory → Continue (endless).**
  Boot game vào Main Menu; PLAY bắt đầu ván mới (điểm về 0).

## 3. Quái (spawn vô tận, độ khó tăng dần)

Spawner đẻ quái ngoài rìa màn hình / điểm spawn của map. Nhịp spawn (3.0s → tối thiểu
0.6s) và số quái đồng thời (5 → tối đa 25) tăng theo thời gian sống + tổng điểm.

| Loại | HP | Tốc độ | Điểm | Rớt lựu đạn | Hành vi |
|---|---|---|---|---|---|
| Chaser (cận chiến) | 3 | 2.8 | 10 | 20% | Lao thẳng vào người chơi, gây sát thương khi chạm |
| Shooter (bắn xa) | 2 | 2.0 | 20 | 25% | Giữ cự ly 3.8–5.5u, bắn đạn nhắm vào người chơi (đạn bay 5 u/s, 1 damage) |
| Rusher (tốc độ) | 1 | 6.2 | 15 | 15% | Máu giấy, áp sát rất nhanh |

## 4. Lựu đạn (AoE)

- Quái chết có tỉ lệ rớt pickup lựu đạn tại chỗ (xem bảng trên).
- Đi qua để nhặt, tối đa **5 quả** (bắt đầu ván có 2 quả), HUD hiện `x N`.
- Ném bằng `E` / chuột phải, đạn bay vòng cung, nổ sau ngòi **1.2s**.
- Vụ nổ bán kính **3.5u**, **50 damage** lên mọi quái trong vùng (kể cả Boss),
  **miễn nhiễm với người chơi**.

## 5. Boss (mốc 500 điểm)

- Đạt 500 điểm: ngừng sinh segment thường, spawn **Boss Arena** (24u x 18u) phía
  trước camera (~12u); camera tới nơi thì khóa lại, Boss xuất hiện kèm thanh máu
  riêng + banner `BOSS ARENA` (có cảnh báo sớm `BOSS APPROACHING!` từ 450 điểm).
- Boss: **60 HP**, tốc độ 1.8, đòn đặc trưng **tỏa đạn tròn 360° — 16 viên, mỗi
  viên cách nhau 22.5°, bay 5 u/s**, chu kỳ 3.5s kèm chớp vàng báo trước 0.5s.
- Hạ Boss: **+500 điểm**, rớt **2 lựu đạn chắc chắn**, hiện bảng Victory
  (`BOSS SLAIN! +500 PTS`) rồi **tự tiếp tục endless sau 3s** (bấm CONTINUE để
  bỏ qua chờ). Mốc Boss tiếp theo +500 điểm nữa.

## 6. Map vô tận

- **Segment mô-đun** dài 20u rộng 15u, 3 mẫu luân phiên (Corridor / ChokePoint /
  Slalom), mỗi segment đảm bảo hành lang trống ≥ 4u. Segment tụt sau camera 25u
  được tái chế vào pool (zero-GC).
- **Sàn (floor)** lát gạch vô tận theo camera: chunk đầu giữ nguyên art vẽ tay,
  các chunk sau là cỏ trơn tuyệt đối liền mạch (phủ từ sau camera 20u tới trước
  40u). Tường biên segment có hàng cây trang trí để nhìn thấy được ranh giới.
- Camera khóa tỉ lệ **16:9** (letterbox/pillarbox) trên mọi màn hình.
