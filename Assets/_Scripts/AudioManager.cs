// _Scripts/AudioManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static ServiceLocator;

public class AudioManager : MonoBehaviour
{
    #region Fields & Properties
    [Header("Background Music")]
    [Tooltip("Lista de músicas que podem tocar durante a partida. Uma será sorteada.")]
    public AudioClip[] backgroundMusicTracks;
    [Tooltip("Música que toca na tela de Game Over.")]
    public AudioClip victoryMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.4f;

    [Header("Weapon Sounds")]
    public AudioClip[] arSounds;
    public AudioClip[] arEquipSounds;
    public AudioClip[] mrSounds;
    public AudioClip[] mrEquipSounds;
    public AudioClip[] srSounds;
    public AudioClip[] srEquipSounds;
    public AudioClip[] sgSounds;
    public AudioClip[] sgEquipSounds;
    public AudioClip[] smgSounds;
    public AudioClip[] smgEquipSounds;
    public AudioClip[] lmgSounds;
    public AudioClip[] lmgEquipSounds;
    public AudioClip[] ptSounds;
    public AudioClip[] ptEquipSounds;
    
    [Header("Other Sounds")]
    public AudioClip[] hitSounds;
    public AudioClip[] hsSounds;
    public AudioClip[] missSounds;

    [Header("UI Sounds")]
    public AudioClip buttonClickSound;
    public AudioClip[] equipCardSounds;
    public AudioClip[] prepSounds;
    public AudioClip dieBuffSound;
    public AudioClip conditionFailSound;
    public AudioClip skillSuccessSound;
    
    [Header("Audio Settings")]
    public float weaponVolume = 0.7f;
    public float equipVolume = 0.6f;
    public float hitVolume = 0.5f;
    public float otherVolume = 0.5f;
    
    private AudioSource sfxSource;
    private AudioSource musicSource;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
    }
    #endregion

    #region Music Management
    public void PlayBackgroundMusic()
    {
        if (backgroundMusicTracks == null || backgroundMusicTracks.Length == 0)
        {
            Debug.LogWarning("AudioManager: Nenhuma música de fundo (BGM) foi definida.");
            return;
        }
        musicSource.Stop();
        AudioClip musicToPlay = backgroundMusicTracks[Random.Range(0, backgroundMusicTracks.Length)];
        musicSource.clip = musicToPlay;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayVictoryMusic()
    {
        if (victoryMusic == null)
        {
            Debug.LogWarning("AudioManager: Nenhuma música de vitória foi definida.");
            return;
        }
        musicSource.Stop();
        musicSource.clip = victoryMusic;
        musicSource.volume = musicVolume;
        musicSource.loop = false;
        musicSource.Play();
    }
    #endregion

    #region Weapon Sounds
    // --- MÉTODO CORRIGIDO ---
    // Agora toca o som instantaneamente, sem corrotina ou delay interno.
    public void PlayWeaponSound(WeaponType weaponType)
    {
        AudioClip[] soundArray = GetWeaponSounds(weaponType);
        if (soundArray != null && soundArray.Length > 0)
        {
            AudioClip randomSound = soundArray[Random.Range(0, soundArray.Length)];
            sfxSource.PlayOneShot(randomSound, weaponVolume);
        }
    }

    public void PlayEquipSound(WeaponType weaponType)
    {
        AudioClip[] soundArray = GetEquipSounds(weaponType);
        if (soundArray != null && soundArray.Length > 0)
        {
            AudioClip randomSound = soundArray[Random.Range(0, soundArray.Length)];
            sfxSource.PlayOneShot(randomSound, equipVolume);
        }
    }
    
    public void PlayHitSound()
    {
        if (hitSounds.Length > 0)
        {
            AudioClip randomHit = hitSounds[Random.Range(0, hitSounds.Length)];
            sfxSource.PlayOneShot(randomHit, hitVolume);
        }
    }
    
    public void PlayMissSound()
    {
        if (missSounds.Length > 0)
        {
            AudioClip randomMiss = missSounds[Random.Range(0, missSounds.Length)];
            sfxSource.PlayOneShot(randomMiss, hitVolume);
        }
    }

    public void PlayHSSound()
    {
        if (hsSounds.Length > 0)
        {
            AudioClip randomHS = hsSounds[Random.Range(0, hsSounds.Length)];
            sfxSource.PlayOneShot(randomHS, hitVolume * 1.2f);
        }
        else
        {
            PlayHitSound();
        }
    }
    #endregion

    #region UI & Effect Sounds
    public void PlayButtonClickSound()
    {
        if (buttonClickSound != null)
            sfxSource.PlayOneShot(buttonClickSound, otherVolume);
    }

    public void PlayEquipCardSound()
    {
        if (equipCardSounds.Length > 0)
        {
            AudioClip randomSound = equipCardSounds[Random.Range(0, equipCardSounds.Length)];
            sfxSource.PlayOneShot(randomSound, otherVolume);
        }
    }

    public void PlayPrepSound()
    {
        if (prepSounds.Length > 0)
        {
            AudioClip randomSound = prepSounds[Random.Range(0, prepSounds.Length)];
            sfxSource.PlayOneShot(randomSound, otherVolume);
        }
    }

    public void PlayDieBuffSound()
    {
        if (dieBuffSound != null)
            sfxSource.PlayOneShot(dieBuffSound, otherVolume);
    }



    public void PlaySkillSuccessSound()
    {
        if (skillSuccessSound != null)
            sfxSource.PlayOneShot(skillSuccessSound, otherVolume);
    }

    public void PlayConditionFailSound()
    {
        if (conditionFailSound != null)
            sfxSource.PlayOneShot(conditionFailSound, otherVolume);
    }
    #endregion

    #region Utility Methods
    private AudioClip[] GetWeaponSounds(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.AR => arSounds,
            WeaponType.MR => mrSounds,
            WeaponType.SR => srSounds,
            WeaponType.SG => sgSounds,
            WeaponType.SMG => smgSounds,
            WeaponType.LMG => lmgSounds,
            WeaponType.Pistol => ptSounds,
            _ => ptSounds
        };
    }

    private AudioClip[] GetEquipSounds(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.AR => arEquipSounds,
            WeaponType.MR => mrEquipSounds,
            WeaponType.SR => srEquipSounds,
            WeaponType.SG => sgEquipSounds,
            WeaponType.SMG => smgEquipSounds,
            WeaponType.LMG => lmgEquipSounds,
            WeaponType.Pistol => ptEquipSounds,
            _ => ptEquipSounds
        };
    }
    #endregion
}