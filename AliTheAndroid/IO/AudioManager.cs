using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace  DeenGames.AliTheAndroid.IO
{
    public class AudioManager
    {
        public static AudioManager Instance = new AudioManager();

        private IDictionary<string, SoundEffect> soundLibrary = new Dictionary<string, SoundEffect>();

        public void Play(string sound)
        {
            if (!this.soundLibrary.ContainsKey(sound))
            {
                throw new ArgumentException($"Can't play {sound} because it wasn't preloaded. Make sure the .wav file is located in the 'Content' directory.");
            }

            this.soundLibrary[sound].CreateInstance().Play();
        }

        private AudioManager()
        {
            AudioManager.Instance = this;

            this.PreloadSounds();
        }

        private void PreloadSounds()
        {
            var files = Directory.GetFiles("Content", "*.wav");
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
        }
    }
}