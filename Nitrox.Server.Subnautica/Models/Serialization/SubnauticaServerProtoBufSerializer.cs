using NitroxModel_Subnautica.DataStructures.Surrogates;
using NitroxModel.DataStructures.Unity;
using NitroxServer.Serialization;
using UnityEngine;

namespace Nitrox.Server.Subnautica.Models.Serialization;

public class SubnauticaServerProtoBufSerializer : ServerProtoBufSerializer
{
    public SubnauticaServerProtoBufSerializer() : base("Assembly-CSharp", "Assembly-CSharp-firstpass", "NitroxModel", "NitroxModel-Subnautica")
    {
        RegisterHardCodedTypes();
    }

    // Register here all hard coded types, that come from NitroxModel-Subnautica or Nitrox.Server.Subnautica
    private void RegisterHardCodedTypes()
    {
        Model.Add(typeof(Light), true);
        Model.Add(typeof(BoxCollider), true);
        Model.Add(typeof(SphereCollider), true);
        Model.Add(typeof(MeshCollider), true);
        Model.Add(typeof(Vector3), false).SetSurrogate(typeof(Vector3Surrogate));
        Model.Add(typeof(NitroxVector3), false).SetSurrogate(typeof(Vector3Surrogate));
        Model.Add(typeof(Quaternion), false).SetSurrogate(typeof(QuaternionSurrogate));
        Model.Add(typeof(NitroxQuaternion), false).SetSurrogate(typeof(QuaternionSurrogate));
        Model.Add(typeof(Transform), false).SetSurrogate(typeof(NitroxTransform));
        Model.Add(typeof(GameObject), false).SetSurrogate(typeof(NitroxServer.UnityStubs.GameObject));
    }
}
