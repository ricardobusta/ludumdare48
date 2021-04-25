using UnityEngine;

namespace Busta.Diggy
{
    public class AudioSystem : MonoBehaviour
    {
        [Header("Music")] public AudioClip idleMusic;
        public AudioClip actionMusic;

        [Header("Sfx")] public AudioClip hurtSfx;
        public AudioClip digSfx;
        public AudioClip breakSfx;
        public AudioClip powerUpSfx;

        [Header("References")] public AudioSource musicSource;

        public AudioSource sfxSource;

        public void PlayActionMusic()
        {
            PlayMusic(actionMusic);
        }

        public void PlayIdleMusic()
        {
            PlayMusic(idleMusic);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (musicSource.clip == clip) return;
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlayHurtSfx()
        {
            PlaySfx(hurtSfx);
        }

        public void PlayDigSfx()
        {
            PlaySfx(digSfx);
        }

        public void PlayBreakSfx()
        {
            PlaySfx(breakSfx);
        }

        public void PlayPowerUpSfx()
        {
            PlaySfx(powerUpSfx);
        }

        private void PlaySfx(AudioClip clip)
        {
            sfxSource.Stop();
            sfxSource.clip = clip;
            sfxSource.Play();
        }
    }
}