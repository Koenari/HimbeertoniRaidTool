﻿using System.ComponentModel;
using System.IO;
using Dalamud.Utility;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal interface IModuleConfigurationManager
{
    bool SaveConfiguration<T>(string internalName, T configData) where T : IHrtConfigData, new();
    bool LoadConfiguration<T>(string internalName, ref T configData) where T : IHrtConfigData, new();
}

internal class ModuleConfigurationManager : IModuleConfigurationManager
{
    private readonly DirectoryInfo _moduleConfigDir;
    private readonly HrtDataManager _parent;
    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,

    };
    internal ModuleConfigurationManager(HrtDataManager parent, string configDir)
    {
        _parent = parent;
        _moduleConfigDir = new DirectoryInfo(configDir + "\\moduleConfigs\\");
        try
        {
            if (!_moduleConfigDir.Exists)
                _moduleConfigDir.Create();
        }
        catch (IOException)
        {
        }
    }

    public bool SaveConfiguration<T>(string internalName, T configData) where T : IHrtConfigData, new()
    {
        configData.BeforeSave();
        FileInfo file = new(_moduleConfigDir.FullName + internalName + ".json");
        string json = JsonConvert.SerializeObject(configData, _jsonSerializerSettings);
        bool writeSuccess;
        try
        {
            Util.WriteAllTextSafe(file.FullName, json);
            writeSuccess = true;
        }
        catch (Win32Exception)
        {
            writeSuccess = false;
        }
        return writeSuccess;
    }
    public bool LoadConfiguration<T>(string internalName, ref T configData) where T : IHrtConfigData, new()
    {
        FileInfo file = new(_moduleConfigDir.FullName + internalName + ".json");
        if (!file.Exists) return true;
        if (!_parent.TryRead(file, out string json))
            return false;
        var fromJson = JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        if (fromJson != null)
        {
            configData = fromJson;
            configData.AfterLoad(_parent);
            return true;
        }
        else
            return false;
    }
}