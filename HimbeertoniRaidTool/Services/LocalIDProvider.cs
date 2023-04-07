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
    public bool SignID(HrtID id)
    {
        if (!IsinMyAuthority(id))
            return false;
        id.Signature = CalcSignature(id);
        return true;
    }
    public HrtID CreateID(HrtID.IDType type) =>
        new(Data.Authority, type, CreateUniqueSequence(type));
    public HrtID CreateGearID(ulong seq) => new(Data.Authority, HrtID.IDType.Gear, seq);
    internal HrtID CreateCharID(ulong seq) => new(Data.Authority, HrtID.IDType.Character, seq);
    public bool VerifySignature(HrtID id)
    {
        if (!IsinMyAuthority(id))
            return false;
        if (id.Signature.Length != SignatureSize)
            return false;
        byte[] correctSig = CalcSignature(id);
        return correctSig.SequenceEqual(id.Signature);

    }
    private bool IsinMyAuthority(HrtID id) => id.Authority == Data.Authority;
    private ulong CreateUniqueSequence(HrtID.IDType type)
    {
        if (type == HrtID.IDType.Gear)
            return DataManager.GearDB.GetNextSequence();
        else if (type == HrtID.IDType.Character)
            return DataManager.CharDB.GetNextSequence();
        else
            return Data.Counter++;
    }
    private byte[] CalcSignature(HrtID id)
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
