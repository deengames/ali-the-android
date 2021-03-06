using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Audio;

namespace  DeenGames.AliTheAndroid.IO
{
    public class AudioManager
    {
        private const float PITCH_VARIATION = 0.10f;

        public static AudioManager Instance = new AudioManager();
        private const string AudioFilesDirectory = "Content";
        private bool isInitialized = false;
        private Random random = new Random();

        private IDictionary<string, SoundEffect> soundLibrary = new Dictionary<string, SoundEffect>();
        
        // Each sound and the one and only instance it can play
        private IDictionary<string, SoundEffectInstance> instances = new  ConcurrentDictionary<string, SoundEffectInstance>();

        public void Play(string sound, bool varyPitch = false)
        {
            if (!isInitialized)
            {
                return; // Running tests, didn't load sounds
            }

            if (!this.soundLibrary.ContainsKey(sound))
            {
                throw new ArgumentException($"Can't play {sound} because it wasn't preloaded. Make sure the .wav file is located in the 'Content' directory.");
            }

            // Trim dead instances
            var toRemove = instances.Where((kvp) => kvp.Value.State != SoundState.Playing).Select(kvp => kvp.Key);
            foreach (var soundName in toRemove)
            {
                instances.Remove(soundName);
            }

            // Limit to one instance at any time. This prevents audio volume from overflowing, eg. if you set the
            // max SFX volume to 10%, and 5 monsters die at once, you shouldn't get 5x SFX (playing at 50% total volume)
            if (instances.ContainsKey(sound))
            {
                instances[sound].Stop();
            }

            var instance = this.soundLibrary[sound].CreateInstance();
            instance.Volume = (Options.SoundEffectsVolume / 100f) * Options.GlobalSfxVolumeNerf;

            if (varyPitch)
            {
                var probability = random.Next(100);
                if (probability < 33)
                {
                    instance.Pitch = -PITCH_VARIATION;
                }
                else if (probability < 66)
                {
                    instance.Pitch = PITCH_VARIATION;
                }
            }

            instance.Play();
            this.instances[sound] = instance;
        }

        private AudioManager()
        {
            AudioManager.Instance = this;

            this.PreloadSounds();
        }

        private void PreloadSounds()
        {
            // Doesn't exist in test enviornment
            if (Directory.Exists(AudioFilesDirectory))
            {
                var files = Directory.GetFiles(AudioFilesDirectory, "*.wav");
                foreach (var fileName in files)
                {
                    using (var stream = System.IO.File.OpenRead(fileName))
                    {
                        var sound = SoundEffect.FromStream(stream);
                        var startIndex = fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                        var stopIndex = fileName.LastIndexOf('.');
                        var key = fileName.Substring(startIndex, stopIndex - startIndex);
                        soundLibrary[key] = sound;
                    }
                }
                isInitialized = true;
            }
        }
    }
}