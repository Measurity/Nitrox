using System.Linq;
using NitroxClient.MonoBehaviours;
using NitroxModel.DataStructures;
using UnityEngine;

namespace NitroxClient.Debuggers.Drawer.Nitrox;

public class NitroxEntityDrawer : IDrawer<NitroxEntity>, IDrawer<NitroxId>
{
    private const float LABEL_WIDTH = 250;

    public NitroxEntity Draw(NitroxEntity nitroxEntity)
    {
        Draw(nitroxEntity.Id);

        GUILayout.Space(8);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("GameObject with IDs", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.TextField(NitroxEntity.GetGameObjects().Count().ToString());
        }

        return nitroxEntity;
    }

    public NitroxId Draw(NitroxId nitroxId)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("NitroxId", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.TextField(nitroxId == null ? "ID IS NULL!!!" : nitroxId.ToString());
        }

        return nitroxId;
    }
}
