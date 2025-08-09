using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.DataManagement;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class LocalIdProvider : IIdProvider
{
    private const string CONFIG_FILE_NAME = "LocalIDProvider";
    private const int SIGNATURE_SIZE = HMACSHA512.HashSizeInBytes;
    private const int KEY_SIZE = 1024;
    private const int KEY_SIZE_BYTES = KEY_SIZE / 8;
    private static readonly RandomNumberGenerator _numberGenerator = RandomNumberGenerator.Create();
    private readonly ConfigData _data = new();
    private readonly HrtDataManager _dataManager;
    private readonly HMACSHA512 _signingProvider;
    internal LocalIdProvider(HrtDataManager dataManager)
    {
        _dataManager = dataManager;
        if (!dataManager.ModuleConfigurationManager.LoadConfiguration(CONFIG_FILE_NAME, ref _data))
            throw new FailedToLoadException("Could not create ID Authority");
        _signingProvider = new HMACSHA512(_data.Key);
        if (_data.Authority != 0)
            return;
        SecureRandom(ref _data.Authority);
        _numberGenerator.GetBytes(_data.Key);
        if (!dataManager.ModuleConfigurationManager.SaveConfiguration(CONFIG_FILE_NAME, _data))
            throw new FailedToLoadException("Could not create ID Authority");
        _signingProvider = new HMACSHA512(_data.Key);
    }
    //Public Functions
    public uint GetAuthorityIdentifier() => _data.Authority;
    
    public bool SignId(HrtId id)
    {
        if (!IsInMyAuthority(id))
            return false;
        id.Signature = CalcSignature(id);
        return true;
    }
    
    public HrtId CreateId(HrtId.IdType type) =>
        new(_data.Authority, type, CreateUniqueSequence(type));
    
    public bool VerifySignature(HrtId id)
    {
        if (!IsInMyAuthority(id))
            return false;
        if (id.Signature.Length != SIGNATURE_SIZE)
            return false;
        byte[] correctSig = CalcSignature(id);
        return correctSig.SequenceEqual(id.Signature);

    }
    
    private bool IsInMyAuthority(HrtId id) => id.Authority == _data.Authority;
    
    private ulong CreateUniqueSequence(HrtId.IdType type) => type switch
    {
        HrtId.IdType.Gear        => _dataManager.GearDb.GetNextSequence(),
        HrtId.IdType.Character   => _dataManager.CharDb.GetNextSequence(),
        HrtId.IdType.Player      => _dataManager.PlayerDb.GetNextSequence(),
        HrtId.IdType.Group       => _dataManager.RaidGroupDb.GetNextSequence(),
        HrtId.IdType.RaidSession => _dataManager.RaidSessionDb.GetNextSequence(),
        _                        => _data.Counter++,
    };
    
    private byte[] CalcSignature(HrtId id)
    {
        byte[] input = Encoding.UTF8.GetBytes(id.ToString());
        return _signingProvider.ComputeHash(input);
    }
    
    private static void SecureRandom<T>(ref T num) where T : IBinaryInteger<T>
    {
        int byteCount = num.GetByteCount();
        byte[] buffer = new byte[byteCount];
        _numberGenerator.GetBytes(buffer);
        num = T.ReadLittleEndian(buffer, true);
    }

    public class ConfigData : IHrtConfigData
    {
        [JsonProperty] public uint Authority;
        [JsonProperty] public ulong Counter = 1;
        [JsonProperty] public byte[] Key = new byte[KEY_SIZE_BYTES];

        public void AfterLoad(HrtDataManager dataManager) { }

        public void BeforeSave() { }
    }
}