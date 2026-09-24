# Nhật ký sửa lỗi / cải tiến (sau bản Antigravity)

Các mục đều đã verify bằng test + chạy Play thực tế. Test sau cùng: 786/786 pass.

1. **Floor cuộn vô tận (`EndlessFloor.cs` trên `EndlessMapManager`)** — Tilemap sàn
   cũ tĩnh 24x10 nên camera bỏ lại phía sau. Giờ clone thành 8 chunk pool phủ
   [camY−20, camY+40], tái chế bằng dời vị trí (zero-GC). Chunk 0 giữ art vẽ tay,
   chunk 1–7 cỏ trơn 1 pattern nên tuyệt đối liền mạch. Chunk phải nằm dưới Grid
   mới render (bài học xương máu).
2. **HUD endless (M4)** — `UIManager` thêm `DIST: 0000m`, banner nhấp nháy
   `BOSS APPROACHING!` (từ 450 điểm), banner `BOSS ARENA` (khi lock camera),
   scene có sẵn 3 object tương ứng.
3. **Boss tới chậm (500 mà 800 mới gặp)** — arena spawn cách camera tới 35u.
   `QueueBossArena()` giờ kéo về trước camera 12u (dọn segment vô hình phía
   trước để khỏi chồng), boss tới sau ~5s.
4. **Đứng hình sau khi giết Boss** — modal Victory đóng băng chờ click.
   Giờ tự tiếp tục sau 3s realtime, bấm CONTINUE thì skip đợi.
5. **CONTINUE reset điểm / MENU Trento** — 3 nút rebuild bị clone nhầm persistent
   `OnRestartButtonClicked`. Đã gỡ (chỉ giữ wire runtime). `StartGame()` luôn
   reset điểm/threshold cho ván mới.
6. **Boot vào Main Menu** — scene lưu `MainMenu` thay vì `Playing`; spawner/
   shooting/movement đóng băng khi không `Playing` (test EditMode bypass);
   `StartGame()` dọn sạch quái tồn.
7. **Màn đỏ quạch khi chết** — `GameOverPanel`/`VictoryPanel` nền đỏ/xanh 0.92
   che kín màn hình → đổi đen mờ 0.6.
8. **Tường vô hình + sai tỉ lệ** — tường segment có hàng cây trang trí nhìn thấy
   được (16/segment, 18 arena, visual-only); camera khóa 16:9 letterbox/pillarbox.
9. **Mất `GameOverPanel`/`VictoryPanel` khỏi scene** — `UIManager` tự dựng lại
   khi thiếu (`EnsureBattlePanels`, gọi ở Start + trước khi hiện).
10. **Nhiễm scene từ test** — test EditMode bắn static event vào singleton của
    scene (từng bake score 4820 + Victory vào file) và rớt 234 clone rác + làm
    kẹt `timeScale=0` chéo suite. Fix: `E2ETestContext` detach scene khỏi event,
    snapshot/restore field + clock, quét rác, cấm chạy lúc Play. Scene tẩy sạch
    về 14 root, `MapBounds` dựng lại đúng số gốc (top 8.2, bottom −8.0,
    trái −16.7, phải 22.0).
11. **Kỷ luật làm việc với scene/Play** — test chỉ chạy ở EditMode; sau Play thì
    reload scene từ đĩa trước khi save; Play bấm qua tool hiện treo từ frame 2
    (bấm Play tay trong Editor vẫn chạy bình thường).
