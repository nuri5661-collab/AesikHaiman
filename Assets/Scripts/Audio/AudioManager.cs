using UnityEngine;

namespace AesikHaiman.Audio
{
    /// <summary>
    /// BGM 무한 반복 재생 + SFX AudioSource 풀링/재생을 담당하는 싱글톤.
    /// 아직 실제 오디오 파일(mp3/wav)이 없어서 클립을 비워둬도 에러 없이 동작합니다 -
    /// Assets/Audio에 파일을 넣고 인스펙터에서 각 클립 슬롯에 연결하면 바로 재생됩니다.
    /// ===== 9주차 스프린트: 사운드 매니저 및 음향 연동 =====
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioSource bgmSource;
        [Tooltip("Assets/Audio/bgm_main.mp3 같은 평상시 배경음악")]
        [SerializeField] private AudioClip defaultBgm;
        [Tooltip("Assets/Audio/bgm_boss.mp3 같은 보스전 전용 긴장감 있는 배경음악")]
        [SerializeField] private AudioClip bossBgm;

        [Header("볼륨 밸런스 (11주차 스프린트: 사운드 볼륨 밸런스 조정)")]
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;

        [Header("SFX 풀")]
        [SerializeField] private int sfxPoolSize = 8;

        [Header("SFX 클립 (Assets/Audio에 파일 추가 후 연결)")]
        [SerializeField] private AudioClip fireSfx;        // 발사 소리
        [SerializeField] private AudioClip hitSfx;         // 타격 적중음
        [SerializeField] private AudioClip playerHitSfx;   // 플레이어 피격음
        [SerializeField] private AudioClip gameOverSfx;    // 게임 오버 효과음
        [SerializeField] private AudioClip bossWarningSfx; // 보스 등장 경고음
        [SerializeField] private AudioClip victorySfx;     // 보스 처치 승리 팡파르

        private AudioSource[] _sfxPool;
        private int _nextSfxIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxPool = new AudioSource[Mathf.Max(1, sfxPoolSize)];
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.volume = sfxVolume;
                _sfxPool[i] = src;
            }

            if (bgmSource != null)
            {
                bgmSource.loop = true;
                bgmSource.volume = bgmVolume;
                if (defaultBgm != null)
                {
                    bgmSource.clip = defaultBgm;
                    bgmSource.Play();
                }
            }
        }

        /// <summary>풀에 있는 AudioSource를 돌려가며 재생 (여러 효과음이 겹쳐도 서로 끊기지 않음).</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || _sfxPool == null) return;

            AudioSource src = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Length;
            src.clip = clip;
            src.volume = sfxVolume;
            src.Play();
        }

        public void PlayFireSfx() => PlaySfx(fireSfx);
        public void PlayHitSfx() => PlaySfx(hitSfx);
        public void PlayPlayerHitSfx() => PlaySfx(playerHitSfx);
        public void PlayGameOverSfx() => PlaySfx(gameOverSfx);
        public void PlayBossWarningSfx() => PlaySfx(bossWarningSfx);
        public void PlayVictorySfx() => PlaySfx(victorySfx);

        /// <summary>BGM을 즉시 다른 곡으로 교체(반복 재생 유지).</summary>
        public void SwitchBgm(AudioClip clip)
        {
            if (bgmSource == null || clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        public void PlayBossBgm() => SwitchBgm(bossBgm);
        public void PlayDefaultBgm() => SwitchBgm(defaultBgm);
    }
}
