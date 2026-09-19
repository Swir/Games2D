using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ScrapDash
{
    public sealed class ScrapDashGame : MonoBehaviour
    {
        public static ScrapDashGame Instance { get; private set; }

        private PlayerController _player;
        private Vector2 _checkpoint;
        private int _scrap;
        private int _hits;
        private bool _paused;
        private bool _won;
        private string _message = string.Empty;
        private float _messageUntil;

        public int Scrap => _scrap;
        public int Hits => _hits;
        public bool Won => _won;
        public bool Paused => _paused;
        public Vector2 Checkpoint => _checkpoint;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
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

        public void ActivateCheckpoint(Vector2 point)
        {
            _checkpoint = point;
            ShowMessage("CHECKPOINT ONLINE");
        }

        public void RespawnPlayer()
        {
            if (_player == null || _won) return;
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
            Time.timeScale = 0f;
        }

        private void ShowMessage(string text)
        {
            _message = text;
            _messageUntil = Time.unscaledTime + 1.4f;
        }

        private void Update()
        {
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
                Screen.fullScreenMode = Screen.fullScreen
                    ? FullScreenMode.Windowed
                    : FullScreenMode.FullScreenWindow;
                Screen.fullScreen = !Screen.fullScreen;
            }
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

            GUI.Box(new Rect(18, 18, 355, 100), string.Empty);
            GUI.Label(new Rect(34, 28, 330, 34), $"SCRAP  {_scrap}/{LevelDefinition.TotalScrap}", hudStyle);
            GUI.Label(
                new Rect(34, 64, 330, 28),
                $"Goal: {LevelDefinition.ScrapRequiredForFinish}  •  Hits: {_hits}",
                new GUIStyle(hudStyle) { fontSize = Mathf.Max(15, hudStyle.fontSize - 5) }
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
            GUI.Label(
                new Rect(18, height - 58, width - 36, 40),
                "Move A/D or ←/→  •  Jump Space/A  •  Dash Shift/X or pad B  •  Pause Esc/Start  •  Restart R/Back  •  F11 Fullscreen",
                helpStyle
            );

            if (_paused) DrawCenterPanel("PAUSED", "Esc / Start to resume");
            if (_won) DrawCenterPanel("LEVEL 1 COMPLETE!", $"Recovered {_scrap}/{LevelDefinition.TotalScrap} scrap • R / Back to replay");
        }

        private static void DrawCenterPanel(string title, string subtitle)
        {
            var rect = new Rect(Screen.width * 0.25f, Screen.height * 0.34f, Screen.width * 0.5f, 180);
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
            GUI.Label(new Rect(rect.x, rect.y + 100, rect.width, 42), subtitle, subStyle);
        }
    }
}
