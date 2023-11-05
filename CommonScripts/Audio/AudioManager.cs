using UnityEngine;

namespace LobbyLogic.Audio
{
    public class AudioManager : MonoSingleton<AudioManager>
    {
        AudioSource bgmAudio;
        AudioSource soundAudio;
        AudioSource loopAudio;

        string soundFile { get { return "audio"; } }

        public bool isMusicOn { get { return musicVolume > 0; } }
        public bool isSoundOn { get { return soundVolume > 0; } }

        float musicVolume = 1;
        float soundVolume = 1;

        string tweenBgmKey = string.Empty;
        string tweenLoopKey = string.Empty;
        string tweenCustomBGM = string.Empty;
        string tweenOnceKey = string.Empty;

        public void Awake()
        {
            initAudios();
        }

        void initAudios()
        {
            bgmAudio = gameObject.AddComponent<AudioSource>();
            bgmAudio.playOnAwake = true;
            bgmAudio.loop = true;
            bgmAudio.priority = 100;
            bgmAudio.pitch = 1;

            soundAudio = gameObject.AddComponent<AudioSource>();
            soundAudio.playOnAwake = false;
            soundAudio.loop = false;

            loopAudio = gameObject.AddComponent<AudioSource>();
            loopAudio.playOnAwake = false;
            loopAudio.loop = true;
            loopAudio.priority = 110;
            loopAudio.pitch = 1;

            setSettingVolume();
        }
        public void setSettingVolume()
        {
            musicVolume = PlayerPrefs.GetFloat(ApplicationConfig.MusicVolumeSaveKey, 1);
            soundVolume = PlayerPrefs.GetFloat(ApplicationConfig.SoundVolumeSaveKey, 1);
            resetAllAudioVolume();
        }

        void resetAllAudioVolume()
        {
            setBgmVolume(musicVolume);
            setSoundVolume(soundVolume);
            setLoopVolume(soundVolume);
        }
        void setBgmVolume(float volume)
        {
            bgmAudio.volume = volume;
        }

        void setSoundVolume(float volume)
        {
            soundAudio.volume = volume;
        }

        void setLoopVolume(float volume)
        {
            loopAudio.volume = volume;
        }

        public void setBgmPitch(float pitch)
        {
            bgmAudio.pitch = pitch;
        }

        public void setLoopPitch(float pitch)
        {
            loopAudio.pitch = pitch;
        }

        public void fadeBgmAudio(float fadeTime, bool isFadeOut = true)
        {
            if (!isMusicOn)
            {
                return;
            }

            float startVal = musicVolume;
            float endVal = 0;

            if (!isFadeOut)
            {
                startVal = 0;
                endVal = musicVolume;
            }

            if (!string.IsNullOrEmpty(tweenBgmKey))
            {
                breakFadeBgmAudio();
                startVal = bgmAudio.volume;
            }

            tweenBgmKey = TweenManager.tweenToFloat(startVal, endVal, fadeTime, onUpdate: setBgmVolume, onComplete: clearTweenBgm);
        }

        void clearTweenBgm()
        {
            TweenManager.tweenKill(tweenBgmKey);
            tweenBgmKey = "";
        }

        public void fadeLoopAudio(float fadeTime, bool isFadeOut = true)
        {
            if (!isSoundOn)
            {
                return;
            }

            float startVal = soundVolume;
            float endVal = 0;

            if (!isFadeOut)
            {
                startVal = 0;
                endVal = soundVolume;
            }

            if (!string.IsNullOrEmpty(tweenLoopKey))
            {
                breakFadeLoopAudio();
                startVal = loopAudio.volume;
            }

            tweenLoopKey = TweenManager.tweenToFloat(startVal, endVal, fadeTime, onUpdate: setLoopVolume);
        }

        public void fadeOnceAudio(float fadeTime, bool isFadeOut = true)
        {
            if (!isSoundOn)
            {
                return;
            }

            float startVal = soundVolume;
            float endVal = 0;

            if (!isFadeOut)
            {
                startVal = 0;
                endVal = soundVolume;
            }

            if (!string.IsNullOrEmpty(tweenOnceKey))
            {
                breakFadeOnceAudio();
                startVal = soundAudio.volume;
            }

            tweenOnceKey = TweenManager.tweenToFloat(startVal, endVal, fadeTime, onUpdate: setSoundVolume);
        }

        public void breakFadeBgmAudio(bool returnVolume = false)
        {
            clearTweenBgm();
            if (returnVolume)
            {
                setBgmVolume(musicVolume);
            }
        }

        public void breakFadeLoopAudio(bool returnVolume = false)
        {
            if (string.IsNullOrEmpty(tweenLoopKey))
            {
                return;
            }
            TweenManager.tweenKill(tweenLoopKey);
            if (returnVolume)
            {
                setLoopVolume(soundVolume);
            }
        }

        public void breakFadeOnceAudio(bool returnVolume = false)
        {
            if (string.IsNullOrEmpty(tweenOnceKey))
            {
                return;
            }
            TweenManager.tweenKill(tweenOnceKey);
            if (returnVolume)
            {
                setSoundVolume(soundVolume);
            }
        }
        public void stopBGM()
        {
            if (null != bgmAudio)
            {
                bgmAudio.Stop();
            }
        }
        public void stopLoop()
        {
            if (null != loopAudio)
            {
                loopAudio.Stop();
            }
        }

        public AudioClip getAudioClip(string path)
        {
            return getAudioClip(path, string.Empty);
        }

        public AudioClip getAudioClip(string path, string bundleType)
        {
            //Debug.Log($"getAudioClip {path}");
            AudioClip audioClip = null;
            if (string.IsNullOrEmpty(bundleType))
            {
                audioClip = ResourceManager.instance.load<AudioClip>($"{soundFile}/{path}");
            }
            else
            {
                audioClip = ResourceManager.instance.loadWithResOrder<AudioClip>($"{soundFile}/{path}", new string[] { bundleType });
            }
            if (null == audioClip)
            {
                Debug.LogError($"Can't load AudioClip in {path}");
            }
            return audioClip;
        }

        #region Once

        public AudioManager playAudioOnce(string path)
        {
            return playAudioOnce(path, string.Empty);
        }

        public AudioManager playAudioOnce(string path, string bundleType)
        {
            var audioClip = getAudioClip(path, bundleType);

            if (null != audioClip)
            {
                soundAudio.PlayOneShot(audioClip);
            }

            return this;
        }

        public AudioManager stopOnceAudio()
        {
            if (null != soundAudio)
            {
                soundAudio.Stop();
            }

            return this;
        }

        public AudioSource PlaySoundOnObj(GameObject o, string path)
        {
            return PlaySoundOnObj(o, path, string.Empty);
        }

        public AudioSource PlaySoundOnObj(GameObject o, string path, string bundleType)
        {
            var audioClip = getAudioClip(path, bundleType);
            AudioSource s = null;
            if (null != audioClip)
            {
                s = o.getOrAddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.volume = soundVolume;
                s.PlayOneShot(audioClip);
            }
            return s;
        }

        public AudioSource playBGMOnObj(GameObject obj, string path)
        {
            return playBGMOnObj(obj, path, string.Empty); ;
        }

        public AudioSource playBGMOnObj(GameObject obj, string path, string bundleType)
        {
            var audioClip = getAudioClip(path, bundleType);
            if (null == audioClip)
            {
                return null;
            }
            stopBGM();
            AudioSource audioSource = obj.getOrAddComponent<AudioSource>();
            audioSource.playOnAwake = bgmAudio.playOnAwake;
            audioSource.loop = true;
            audioSource.priority = bgmAudio.priority;
            audioSource.volume = musicVolume;
            audioSource.clip = audioClip;
            audioSource.Play();
            return audioSource;

        }

        public void fadeCustomBGM(AudioSource source, float fadeTime, bool isFadeOut)
        {
            if (!isMusicOn)
            {
                return;
            }

            float startVal = musicVolume;
            float endVal = 0;

            if (!isFadeOut)
            {
                startVal = 0;
                endVal = musicVolume;
            }

            if (!string.IsNullOrEmpty(tweenCustomBGM))
            {
                clearCustomBGM();
                startVal = musicVolume;
            }

            tweenCustomBGM = TweenManager.tweenToFloat(startVal, endVal, fadeTime, onUpdate: volume =>
            {
                source.volume = volume;
            }, onComplete: clearCustomBGM);
        }

        void clearCustomBGM()
        {
            TweenManager.tweenKill(tweenCustomBGM);
            tweenCustomBGM = string.Empty;
        }

        public void stopBGMOnObjAndResetMainBGM(AudioSource source)
        {
            source.Stop();
            bgmAudio.Play();
        }

        #endregion Once

        #region Loop

        public AudioManager playAudioLoop(string path, bool isLoop)
        {
            return playAudioLoop(path, isLoop, string.Empty);
        }

        public AudioManager playAudioLoop(string path, bool isLoop = true, string bundleType = "")
        {
            var audioClip = getAudioClip(path, bundleType);

            if (null != audioClip)
            {
                stopLoop();
                loopAudio.loop = isLoop;
                loopAudio.clip = audioClip;
                loopAudio.Play();
            }

            return this;
        }

        #endregion Loop

        #region BGM

        public AudioManager playBGM(string path)
        {
            return playBGM(path, string.Empty);
        }

        public AudioManager playBGM(string path, string bundleType)
        {
            var audioClip = getAudioClip(path, bundleType);

            if (null != audioClip)
            {
                stopBGM();
                bgmAudio.volume = musicVolume;
                bgmAudio.clip = audioClip;
                bgmAudio.Play();
            }

            return this;
        }

        #endregion BGM

        public void stopAllAudio()
        {
            stopBGM();
            stopLoop();
            stopOnceAudio();
        }

        public void pauseAll()
        {
            if (null != bgmAudio)
            {
                //bgmAudio.ignoreListenerPause = true;
                bgmAudio.Pause();
            }

            if (null != loopAudio)
            {
                //loopAudio.ignoreListenerPause = true;
                loopAudio.Pause();
            }

            if (null != soundAudio)
            {
                //soundAudio.ignoreListenerPause = true;
                soundAudio.Pause();
            }
        }

        public void resumeAll()
        {
            if (null != bgmAudio)
            {
                //bgmAudio.ignoreListenerPause = true;
                bgmAudio.UnPause();
            }

            if (null != loopAudio)
            {
                //loopAudio.ignoreListenerPause = true;
                loopAudio.UnPause();
            }

            if (null != soundAudio)
            {
                //soundAudio.ignoreListenerPause = true;
                soundAudio.UnPause();
            }
        }
    }
}

