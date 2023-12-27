using UnityEngine;

namespace NitroxClient.Debuggers;

public interface IDebugObjectSelector
{
    void UpdateSelectedObject(GameObject item);
    void JumpToComponent(Component item);
}
