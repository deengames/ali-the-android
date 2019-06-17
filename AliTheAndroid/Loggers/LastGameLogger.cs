using System;
using System.IO;

namespace DeenGames.AliTheAndroid.Loggers
{
    class LastGameLogger
    {
        private const string LogFileName = "LastGame.txt";

        public static LastGameLogger Instance { get; private set; } = new LastGameLogger();

        private LastGameLogger()
        {
            File.Delete(LogFileName);
            Log($"Started a new game!");
        }
        
        public void Log(string message)
        {
            File.AppendAllText(LogFileName, $"{DateTime.Now} | {message}\n");            
        }
    }
}