using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.DataManagement;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Security;

internal class LocalIdProvider : IIdProvider
{
    private static readonly RandomNumberGenerator _numberGenerator = RandomNumberGenerator.Create();
    private const string INTERNAL_NAME = "LocalIDProvider";
    private readonly HMACSHA512 _signingProvider;
    private const int SIGNATURE_SIZE = HMACSHA512.HashSizeInBytes;
    private const int KEY_SIZE = 1024;
    private const int KEY_SIZE_BYTES = KEY_SIZE / 8;
    private readonly HrtDataManager _dataManager;
    private readonly ConfigData _data = new();
    internal LocalIdProvider(HrtDataManager dataManager)
    {
        _dataManager = dataManager;
        _signingProvider = new HMACSHA512();
        dataManager.ModuleConfigurationManager.LoadConfiguration(INTERNAL_NAME, ref _data);
        if (_data.Authority == 0)
        {
            SecureRandom(ref _data.Authority);
            _numberGenerator.GetBytes(_data.Key);
            if (!dataManager.ModuleConfigurationManager.SaveConfiguration(INTERNAL_NAME, _data))
                throw new FailedToLoadException("Could not create ID Authority");
        }
    }
    //Public Funtions
    public uint GetAuthorityIdentifier() => _data.Authority;
    public bool SignId(HrtId id)
    {
        if (!IsinMyAuthority(id))
            return false;
        id.Signature = CalcSignature(id);
        return true;
    }
    public HrtId CreateId(HrtId.IdType type) =>
        new(_data.Authority, type, CreateUniqueSequence(type));
    public HrtId CreateGearId(ulong seq) => new(_data.Authority, HrtId.IdType.Gear, seq);
    internal HrtId CreateCharId(ulong seq) => new(_data.Authority, HrtId.IdType.Character, seq);
    public bool VerifySignature(HrtId id)
    {
        if (!IsinMyAuthority(id))
            return false;
        if (id.Signature.Length != SIGNATURE_SIZE)
            return false;
        byte[] correctSig = CalcSignature(id);
        return correctSig.SequenceEqual(id.Signature);

    }
    private bool IsinMyAuthority(HrtId id) => id.Authority == _data.Authority;
    private ulong CreateUniqueSequence(HrtId.IdType type)
    {
        if (type == HrtId.IdType.Gear)
            return _dataManager.GearDb.GetNextSequence();
        else if (type == HrtId.IdType.Character)
            return _dataManager.CharDb.GetNextSequence();
        else
            return _data.Counter++;
    }
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

    public class ConfigData
    {
        [JsonProperty] public uint Authority = 0;
        [JsonProperty] public byte[] Key = new byte[KEY_SIZE_BYTES];
        [JsonProperty] public ulong Counter = 1;
    }
}