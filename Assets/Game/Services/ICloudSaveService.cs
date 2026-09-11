using System;

namespace Game.Services
{
    public interface ICloudSaveService
    {
        bool IsAvailable { get; }
        void SaveData(string key, string data, Action<bool, string> callback);
        void LoadData(string key, Action<bool, string, string> callback);
    }
}
