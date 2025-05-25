using NitroxModel.DataStructures;
using NitroxModel.DataStructures.GameLogic;
using NitroxModel.DataStructures.Util;
using NitroxModel.Networking;
using TestHelper.Faker;

namespace NitroxModel;

[TestClass]
public class Main
{
    [AssemblyInitialize]
    public static void MyTestInitialize(TestContext testContext)
    {
        NitroxFaker.RegisterFakerForType<NitroxTechType>(f => new NitroxTechType(f.PickRandom<TechType>().ToString()));
        NitroxFaker.RegisterFakerForType<NitroxId>(f => new NitroxId(f.Random.Guid()));
        NitroxFaker.RegisterFakerForType<SessionId>(f => (SessionId)f.Random.UShort());
        NitroxFaker.RegisterFakerForType<PeerId>(f => (PeerId)f.Random.UInt());

        NitroxFaker.RegisterGenericType(typeof(Nullable<>));
        NitroxFaker.RegisterGenericType(typeof(Optional<>));
    }

    [AssemblyCleanup]
    public static void TearDown()
    {
    }
}
