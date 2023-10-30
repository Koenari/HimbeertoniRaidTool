using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.DataManagement;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.Security;

internal class LocalIDProvider : IIDProvider
{
    private static readonly RandomNumberGenerator NumberGenerator = RandomNumberGenerator.Create();
    private const string InternalName = "LocalIDProvider";
    private readonly HMACSHA512 SigningProvider;
    private const int SignatureSize = HMACSHA512.HashSizeInBytes;
    private const int KeySize = 1024;
    private const int KeySizeBytes = KeySize / 8;
    private readonly HrtDataManager DataManager;
    private readonly ConfigData Data = new();
    internal LocalIDProvider(HrtDataManager dataManager)
    {
        DataManager = dataManager;
        SigningProvider = new HMACSHA512();
        dataManager.ModuleConfigurationManager.LoadConfiguration(InternalName, ref Data);
        if (Data.Authority == 0)
        {
            SecureRandom(ref Data.Authority);
            NumberGenerator.GetBytes(Data.Key);
            if (!dataManager.ModuleConfigurationManager.SaveConfiguration(InternalName, Data))
                throw new FailedToLoadException("Could not create ID Authority");
        }
    }
    //Public Funtions
    public uint GetAuthorityIdentifier() => Data.Authority;
    public bool SignID(HrtId id)
    {
        if (!IsinMyAuthority(id))
            return false;
        id.Signature = CalcSignature(id);
        return true;
    }
    public HrtId CreateID(HrtId.IdType type) =>
        new(Data.Authority, type, CreateUniqueSequence(type));
    public HrtId CreateGearID(ulong seq) => new(Data.Authority, HrtId.IdType.Gear, seq);
    internal HrtId CreateCharID(ulong seq) => new(Data.Authority, HrtId.IdType.Character, seq);
    public bool VerifySignature(HrtId id)
    {
        if (!IsinMyAuthority(id))
            return false;
        if (id.Signature.Length != SignatureSize)
            return false;
        byte[] correctSig = CalcSignature(id);
        return correctSig.SequenceEqual(id.Signature);

    }
    private bool IsinMyAuthority(HrtId id) => id.Authority == Data.Authority;
    private ulong CreateUniqueSequence(HrtId.IdType type)
    {
        if (type == HrtId.IdType.Gear)
            return DataManager.GearDB.GetNextSequence();
        else if (type == HrtId.IdType.Character)
            return DataManager.CharDB.GetNextSequence();
        else
            return Data.Counter++;
    }
    private byte[] CalcSignature(HrtId id)
    {
        byte[] input = Encoding.UTF8.GetBytes(id.ToString());
        return SigningProvider.ComputeHash(input);
    }
    private static void SecureRandom<T>(ref T num) where T : IBinaryInteger<T>
    {
        int byteCount = num.GetByteCount();
        byte[] buffer = new byte[byteCount];
        NumberGenerator.GetBytes(buffer);
        num = T.ReadLittleEndian(buffer, true);
    }

    public class ConfigData
    {
        [JsonProperty] public uint Authority = 0;
        [JsonProperty] public byte[] Key = new byte[KeySizeBytes];
        [JsonProperty] public ulong Counter = 1;
    }
}