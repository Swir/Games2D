using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ScrapDash
{
    public sealed class ScrapDashGame : MonoBehaviour
    {
        private const string BestTimeKey = "scrap_dash_level_1_best_time";

        private static readonly Vector2Int[] DemoResolutions =
        {
            new(1280, 720),
            new(1600, 900),
            new(1920, 1080)
        };

        public static ScrapDashGame Instance { get; private set; }

        private PlayerController _player;
        private Vector2 _checkpoint;
        private int _scrap;
        private int _hits;
        private int _deaths;
        private int _enemiesDefeated;
        private float _elapsedSeconds;
        private float _bestSeconds;
        private bool _paused;
        private bool _won;
        private string _message = string.Empty;
        private float _messageUntil;
        private int _resolutionIndex = 2;

        public int Scrap => _scrap;
        public int Hits => _hits;
        public int Deaths => _deaths;
        public float ElapsedSeconds => _elapsedSeconds;
        public float BestSeconds => _bestSeconds;
        public bool Won => _won;
        public bool Paused => _paused;
        public bool BlocksPlayerControl => _paused || _won;
        public Vector2 Checkpoint => _checkpoint;
        public int EnemiesDefeated => _enemiesDefeated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
            _bestSeconds = PlayerPrefs.GetFloat(BestTimeKey, 0f);
#if !UNITY_EDITOR
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        public void RegisterPlayer(PlayerController player, Vector2 spawn)
        {
            _player = player;
            _checkpoint = spawn;
        }

        public void CollectScrap()
        {
            _scrap++;
            ShowMessage($"SCRAP {_scrap}/{LevelDefinition.TotalScrap}");
        }

        public void NotifyHit()
        {
            _hits++;
            ShowMessage("OUCH! Recalibrating...");
        }

        public void NotifyEnemyDefeated()
        {
            _enemiesDefeated++;
            ShowMessage("TOY BOT RECYCLED!");
        }

        public void ActivateCheckpoint(Vector2 point)
        {
            _checkpoint = point;
            ShowMessage("CHECKPOINT ONLINE");
        }

        public void RespawnPlayer()
        {
            if (_player == null || _won) return;
            _deaths++;
            _player.transform.position = _checkpoint;
            _player.ResetMotion();
            ShowMessage("REBOOTED");
        }

        public void TryFinish()
        {
            if (_won) return;
            if (_scrap < LevelDefinition.ScrapRequiredForFinish)
            {
                ShowMessage($"NEED {LevelDefinition.ScrapRequiredForFinish - _scrap} MORE SCRAP");
                return;
            }

            _won = true;
            _paused = false;
            if (_bestSeconds <= 0f || _elapsedSeconds < _bestSeconds)
            {
                _bestSeconds = _elapsedSeconds;
                PlayerPrefs.SetFloat(BestTimeKey, _bestSeconds);
                PlayerPrefs.Save();
            }
            FeedbackHub.Instance?.PlayWin(_player != null ? _player.transform.position : Vector3.zero);
            Time.timeScale = 0f;
        }

        public string GetRunGrade()
        {
            if (_scrap == LevelDefinition.TotalScrap && _deaths == 0 && _elapsedSeconds <= 60f) return "S";
            if (_scrap >= LevelDefinition.ScrapRequiredForFinish + 1 && _deaths <= 1) return "A";
            if (_deaths <= 3) return "B";
            return "C";
        }

        public static string FormatTime(float seconds)
        {
            var totalCentiseconds = Mathf.Max(0, Mathf.FloorToInt(seconds * 100f));
            var minutes = totalCentiseconds / 6000;
            var remainingSeconds = totalCentiseconds / 100 % 60;
            var centiseconds = totalCentiseconds % 100;
            return $"{minutes:00}:{remainingSeconds:00}.{centiseconds:00}";
        }

        private void ShowMessage(string text)
        {
            _message = text;
            _messageUntil = Time.unscaledTime + 1.4f;
        }

        private void Update()
        {
            if (!_paused && !_won) _elapsedSeconds += Time.unscaledDeltaTime;

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            if ((keyboard?.escapeKey.wasPressedThisFrame ?? false)
                || (gamepad?.startButton.wasPressedThisFrame ?? false))
            {
                if (!_won) TogglePause();
            }

            if ((keyboard?.rKey.wasPressedThisFrame ?? false)
                || (gamepad?.selectButton.wasPressedThisFrame ?? false))
            {
                RestartLevel();
            }

            if (keyboard?.f11Key.wasPressedThisFrame ?? false)
            {
                ToggleFullscreen();
            }
            else if (gamepad?.leftStickButton.wasPressedThisFrame ?? false)
            {
                ToggleFullscreen();
            }

            if ((keyboard?.f10Key.wasPressedThisFrame ?? false)
                || (gamepad?.rightStickButton.wasPressedThisFrame ?? false))
            {
                CycleResolution();
            }

            if ((keyboard?.leftBracketKey.wasPressedThisFrame ?? false)
                || (gamepad?.leftShoulder.wasPressedThisFrame ?? false))
            {
                ChangeVolume(-0.1f);
            }

            if ((keyboard?.rightBracketKey.wasPressedThisFrame ?? false)
                || (gamepad?.rightShoulder.wasPressedThisFrame ?? false))
            {
                ChangeVolume(0.1f);
            }
        }

        private void ToggleFullscreen()
        {
            var targetMode = Screen.fullScreenMode == FullScreenMode.Windowed
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Screen.SetResolution(Screen.width, Screen.height, targetMode);
            ShowMessage(targetMode == FullScreenMode.Windowed ? "WINDOWED MODE" : "FULLSCREEN MODE");
        }

        private void CycleResolution()
        {
            _resolutionIndex = (_resolutionIndex + 1) % DemoResolutions.Length;
            var resolution = DemoResolutions[_resolutionIndex];
            Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
            ShowMessage($"RESOLUTION {resolution.x}x{resolution.y}");
        }

        private void ChangeVolume(float delta)
        {
            var feedback = FeedbackHub.Instance;
            if (feedback == null) return;
            feedback.SetMasterVolume(feedback.MasterVolume + delta);
            ShowMessage($"VOLUME {Mathf.RoundToInt(feedback.MasterVolume * 100f)}%");
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
        }

        private void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            var width = Screen.width;
            var height = Screen.height;

            var hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(height / 40, 18, 30),
                fontStyle = FontStyle.Bold,
                normal = { textColor = ProceduralVisuals.Hex("#62E5FF") }
            };

            GUI.Box(new Rect(18, 18, 420, 118), string.Empty);
            GUI.Label(new Rect(34, 28, 390, 34), $"SCRAP  {_scrap}/{LevelDefinition.TotalScrap}     TIME  {FormatTime(_elapsedSeconds)}", hudStyle);
            GUI.Label(
                new Rect(34, 64, 390, 28),
                $"Goal: {LevelDefinition.ScrapRequiredForFinish}  •  Bots: {_enemiesDefeated}/{LevelDefinition.EnemyCount}  •  Deaths: {_deaths}",
                new GUIStyle(hudStyle) { fontSize = Mathf.Max(15, hudStyle.fontSize - 5) }
            );
            GUI.Label(
                new Rect(34, 91, 390, 28),
                _bestSeconds > 0f ? $"Best: {FormatTime(_bestSeconds)}  •  Hits: {_hits}" : $"Best: --:--.--  •  Hits: {_hits}",
                new GUIStyle(hudStyle) { fontSize = Mathf.Max(14, hudStyle.fontSize - 7) }
            );

            if (Time.unscaledTime < _messageUntil && !string.IsNullOrEmpty(_message))
            {
                var messageStyle = new GUIStyle(hudStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Clamp(height / 30, 22, 42),
                    normal = { textColor = ProceduralVisuals.Hex("#FFE066") }
                };
                GUI.Label(new Rect(width * 0.25f, height * 0.16f, width * 0.5f, 60), _message, messageStyle);
            }

            var helpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(height / 58, 13, 19),
                normal = { textColor = new Color(0.75f, 0.82f, 0.88f) }
            };
            GUI.Label(new Rect(18, height - 70, width - 36, 24),
                "Move A/D or ←/→  •  Jump Space / pad A  •  Dash Shift/X / pad B  •  Stomp enemies from above", helpStyle);
            GUI.Label(new Rect(18, height - 43, width - 36, 24),
                "Pause Esc/Start  •  Restart R/Back  •  F11/L3 Fullscreen  •  F10/R3 Resolution  •  [ ]/LB RB Volume", helpStyle);

            if (_paused) DrawCenterPanel("PAUSED", "Esc / Start to resume");
            if (_won)
            {
                DrawCenterPanel(
                    $"LEVEL 1 COMPLETE!  GRADE {GetRunGrade()}",
                    $"Time {FormatTime(_elapsedSeconds)}  •  Best {FormatTime(_bestSeconds)}  •  Deaths {_deaths}\nRecovered {_scrap}/{LevelDefinition.TotalScrap} scrap  •  R / Back to replay"
                );
            }
        }

        private static void DrawCenterPanel(string title, string subtitle)
        {
            var rect = new Rect(Screen.width * 0.22f, Screen.height * 0.32f, Screen.width * 0.56f, 220);
            GUI.Box(rect, string.Empty);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 22, 28, 54),
                fontStyle = FontStyle.Bold,
                normal = { textColor = ProceduralVisuals.Hex("#62E5FF") }
            };
            var subStyle = new GUIStyle(titleStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 45, 16, 26),
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(rect.x, rect.y + 28, rect.width, 68), title, titleStyle);
            GUI.Label(new Rect(rect.x + 12, rect.y + 100, rect.width - 24, 82), subtitle, subStyle);
        }
    }
}
