# Kiến trúc game

Ngôn ngữ: Unity C#. Scene: `Assets/Scenes/shooting.unity` (14 root chuẩn:
`Main Camera`, `Player`, `floor`, `arvores`, `Colliders`, `Fire Effect`,
`MapBounds`, `EnemySpawner`, `GameManager`, `SoundManager`, `EventSystem`,
`Canvas`, `EndlessMapManager`, `[MapManager]`).

## 1. Module code (`Assets/scripts/`)

| Nhóm | File | Vai trò |
|---|---|---|
| Player | `PlayerMovement.cs` (5 u/s, kẹp viewport), `PlayerHealth.cs` (5 HP, i-frames 1s), `Shooting.cs` (20 force, 0.2s cooldown), `Bullet.cs` (1 dmg), `DamageFlash.cs` | Điều khiển, máu, bắn |
| Enemy | `EnemyBase.cs` (`IDamageable`, flash 0.1s, rớt đồ), `ChaserEnemy.cs`, `ShooterEnemy.cs` (kiting 3.8–5.5u), `RusherEnemy.cs`, `EnemyBullet.cs` (5 u/s, 1 dmg), `EnemySpawner.cs` (interval 3.0→0.6s, concurrency 5→25) | 3 archetype + spawner |
| Grenade | `GrenadePickup.cs`, `GrenadeThrower.cs` (tối đa 5, ném xa 7u, cooldown 0.3s), `GrenadeProjectile.cs` (ngòi 1.2s), `ExplosionAoE.cs` (R 3.5u, 50 dmg, miễn nhiễm player) | AoE nhặt/ném/nổ |
| Boss | `BossController.cs` (60 HP, 1.8 speed, 500 điểm, barrage 16 viên/22.5°/5 u/s, telegraph 0.5s, rớt 2 lựu đạn) | Encounter 500 điểm |
| Endless | `ScrollingCameraController.cs` (2.0→3.5 u/s, lock/unlock arena, khóa 16:9), `MapSegment.cs` (20x15u, hành lang ≥4u), `MapSegmentPool.cs` (zero-GC), `MapManager.cs` (spawn/tái chế, trigger arena, lead 12u), `EndlessFloor.cs` (lát sàn theo camera) | Cuộn +Y vô tận |
| System/UI | `GameManager.cs` (state machine, điểm, PlayerPrefs HighScore), `UIManager.cs` (HUD, panels, tự dựng lại panel khi thiếu), `SoundManager.cs` (8-bit procedural, không cần file nhạc) | Vòng lặp game + UI |

Prefabs: `Assets/Prefabs/` (`PlayerBullet`, 3 quái, `EnemyBullet`, `BossEnemy`,
`GrenadePickup`, `GrenadeProjectile`, `ExplosionAoE`, `Fire Effect`) và
`Assets/Prefabs/MapSegments/` (3 segment + `MapSegment_BossArena`, dựng bằng
menu `Tools/Build Map Segment Prefabs` từ `MapSegmentPrefabBuilder.cs`).

## 2. Contract chính giữa các module

- `ScrollingCameraController`: `CurrentSpeed` (= 0 khi `isScrollLocked`),
  `DistanceTravelled`, `LockAt(worldY)` / `UnlockAndResume()`, `EnforceAspect()`.
- `MapManager` đọc `camera.y` để spawn (`nextSpawnY`, cleanup `camY - 25u`) và báo
  `bossArenaCenterY`; khi điểm ≥ 500 (`GameManager.CheckBossScoreTrigger`) thì
  `QueueBossArena()` — arena đặt trước camera 12u, tới nơi thì lock + spawn Boss.
- `BossController.OnBossDefeatedEvent` → `GameManager.TriggerVictory()` (đứng
  hình + panel) → auto `ResumeEndlessAfterBoss()` sau 3s realtime (mở camera,
  mở tường arena, spawn tiếp, nâng mốc Boss +500).
- `UIManager`: `UpdateScore/HighScore/Grenades/Hearts/BossHealth`, `UpdateDistance`
  (`DIST: 0000m`), `ShowBossWarning` (điểm ≥ mốc−50), `ShowBossArenaStatus`
  (khi lock camera), `EnsureBattlePanels()` (tự dựng lại GameOver/Victory nếu mất).

## 3. Quy ước kỹ thuật

- Mọi logic runtime dừng khi `CurrentState != Playing` (camera, map, floor,
  spawner, shooting, movement) — riêng test EditMode bypass để hermetic.
- Không `Instantiate/Destroy` trong frame nóng: segment + floor dùng pool/chunk
  tái chế (coverage kiểm chứng 2000u).
- UI: sự kiện nút wire runtime ở `UIManager.Start` (không persistent trong scene);
  panel end-screen nền đen mờ 0.6 để còn thấy thế giới phía sau.
- Test (`Assets/scripts/Tests/`): 786 case — SCM 120 (map cuộn), E2E 505,
  Tier5 36, Milestone 12+16+20+20+19, Challenger 14+17+26. `E2ETestContext`
  cách ly mỗi test (snapshot object, chuẩn hóa `timeScale=1`, detach singleton
  scene khỏi static event, quét rác Instantiate, cấm chạy lúc Play).
- Chạy test qua Unity MCP (`execute_code` gọi `RunAllFormatted()`, hoặc
  `run_tests` EditMode). **Không chạy test khi đang Play, không save scene sau
  khi Play mà chưa reload/decontaminate.**
