using UnityEngine;

namespace ScrapDash
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FeedbackHub : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const string VolumeKey = "scrap_dash_master_volume";

        public static FeedbackHub Instance { get; private set; }

        private AudioSource _source;
        private AudioClip _jump;
        private AudioClip _dash;
        private AudioClip _collect;
        private AudioClip _hit;
        private AudioClip _checkpoint;
        private AudioClip _stomp;
        private AudioClip _spring;
        private AudioClip _win;

        public float MasterVolume => AudioListener.volume;

        private void Awake()
        {
            Instance = this;
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            _jump = CreateTone("Jump", 320f, 620f, 0.11f, 0.18f);
            _dash = CreateTone("Dash", 760f, 150f, 0.15f, 0.22f, true);
            _collect = CreateTone("Collect", 680f, 1320f, 0.13f, 0.2f);
            _hit = CreateTone("Hit", 170f, 70f, 0.18f, 0.25f, true);
            _checkpoint = CreateTone("Checkpoint", 420f, 920f, 0.28f, 0.2f);
            _stomp = CreateTone("Stomp", 260f, 110f, 0.12f, 0.23f, true);
            _spring = CreateTone("Spring", 230f, 980f, 0.18f, 0.21f);
            _win = CreateTone("Win", 440f, 1040f, 0.55f, 0.22f);

            SetMasterVolume(PlayerPrefs.GetFloat(VolumeKey, 0.8f), false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMasterVolume(float volume, bool save = true)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            if (!save) return;
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            PlayerPrefs.Save();
        }

        public void PlayJump() => Play(_jump);

        public void PlayDash(Vector3 position)
        {
            Play(_dash);
            Burst(position, ProceduralVisuals.Hex("#62E5FF"), 7, 4.2f);
        }

        public void PlayCollect(Vector3 position)
        {
            Play(_collect);
            Burst(position, ProceduralVisuals.Hex("#FFE066"), 9, 3.8f);
        }

        public void PlayHit(Vector3 position)
        {
            Play(_hit);
            Burst(position, ProceduralVisuals.Hex("#FF426D"), 8, 4.8f);
        }

        public void PlayCheckpoint(Vector3 position)
        {
            Play(_checkpoint);
            Burst(position, ProceduralVisuals.Hex("#62E5FF"), 12, 4f);
        }

        public void PlayStomp(Vector3 position)
        {
            Play(_stomp);
            Burst(position, ProceduralVisuals.Hex("#8B5CF6"), 12, 5.2f);
        }

        public void PlaySpring(Vector3 position)
        {
            Play(_spring);
            Burst(position, ProceduralVisuals.Hex("#FFE066"), 10, 4.7f);
        }

        public void PlayWin(Vector3 position)
        {
            Play(_win);
            Burst(position, ProceduralVisuals.Hex("#FFE066"), 18, 6f);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && _source != null) _source.PlayOneShot(clip);
        }

        private static AudioClip CreateTone(
            string name,
            float startFrequency,
            float endFrequency,
            float duration,
            float amplitude,
            bool squareBlend = false)
        {
            var sampleCount = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[sampleCount];
            var phase = 0f;

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)Mathf.Max(1, sampleCount - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var sine = Mathf.Sin(phase);
                var wave = squareBlend ? Mathf.Lerp(sine, Mathf.Sign(sine), 0.28f) : sine;
                var attack = Mathf.Clamp01(t / 0.08f);
                var release = Mathf.Clamp01((1f - t) / 0.25f);
                samples[i] = wave * amplitude * attack * release;
            }

            var clip = AudioClip.Create($"SCRAP_DASH_{name}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void Burst(Vector3 position, Color color, int count, float speed)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = (Mathf.PI * 2f * i / count) + (i % 2) * 0.17f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var piece = ProceduralVisuals.Rect(
                    "FeedbackSpark",
                    position,
                    Vector2.one * (0.09f + (i % 3) * 0.025f),
                    color,
                    Instance != null ? Instance.transform : null,
                    40
                );
                piece.AddComponent<FeedbackSpark>().Configure(direction * speed, 0.34f + (i % 4) * 0.045f);
            }
        }
    }

    public sealed class FeedbackSpark : MonoBehaviour
    {
        private Vector2 _velocity;
        private float _lifetime;
        private float _remaining;
        private SpriteRenderer _renderer;

        public void Configure(Vector2 velocity, float lifetime)
        {
            _velocity = velocity;
            _lifetime = lifetime;
            _remaining = lifetime;
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;
            _remaining -= delta;
            if (_remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _velocity += Physics2D.gravity * (0.18f * delta);
            transform.position += (Vector3)(_velocity * delta);
            transform.Rotate(0f, 0f, 360f * delta);

            if (_renderer != null)
            {
                var color = _renderer.color;
                color.a = Mathf.Clamp01(_remaining / _lifetime);
                _renderer.color = color;
            }
        }
    }

    public sealed class CorePulse : MonoBehaviour
    {
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.12f;
            transform.localScale = _baseScale * pulse;
        }
    }
}
