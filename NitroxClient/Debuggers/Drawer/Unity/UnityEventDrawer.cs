using UnityEngine;
using UnityEngine.Events;

namespace NitroxClient.Debuggers.Drawer.Unity;

public class UnityEventDrawer : IDrawer<UnityEvent>, IDrawer<UnityEvent<bool>>, IDrawer<UnityEventBase>
{
    private const float LABEL_WIDTH = 250;

    public UnityEvent Draw(UnityEvent unityEvent)
    {
        return Draw(unityEvent, "NoName");
    }

    public UnityEvent Draw(UnityEvent unityEvent, string name)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(name, NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Invoke All", GUILayout.Width(100)))
            {
                unityEvent.Invoke();
            }
        }

        Draw((UnityEventBase)unityEvent);

        return unityEvent;
    }

    public UnityEvent<bool> Draw(UnityEvent<bool> target)
    {
        return Draw(target, "NoName");
    }

    public UnityEvent<bool> Draw(UnityEvent<bool> unityEvent, string name)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(name, NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Invoke All (true)", GUILayout.Width(100)))
            {
                unityEvent.Invoke(true);
            }
            if (GUILayout.Button("Invoke All (false)", GUILayout.Width(100)))
            {
                unityEvent.Invoke(false);
            }
        }

        Draw((UnityEventBase)unityEvent);

        return unityEvent;
    }

    public UnityEventBase Draw(UnityEventBase unityEventBase)
    {
        for (int index = 0; index < unityEventBase.GetPersistentEventCount(); index++)
        {
            using (new GUILayout.HorizontalScope())
            {
                NitroxGUILayout.Separator();
                GUILayout.Label(unityEventBase.GetPersistentMethodName(index), NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
            }
        }

        return unityEventBase;
    }
}
