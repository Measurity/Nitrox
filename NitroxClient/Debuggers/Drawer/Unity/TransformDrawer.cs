using NitroxModel.Helper;
using UnityEngine;

namespace NitroxClient.Debuggers.Drawer.Unity;

public class TransformDrawer : IDrawer<Transform>
{
    private readonly IDebugObjectSelector objectSelector;
    private readonly DimensionDrawer dimensionDrawer;
    private const float LABEL_WIDTH = 100;
    private const float VECTOR_MAX_WIDTH = 405;

    private bool showGlobal;

    public TransformDrawer(IDebugObjectSelector objectSelector, DimensionDrawer dimensionDrawer)
    {
        Validate.NotNull(objectSelector);
        Validate.NotNull(dimensionDrawer);
        this.objectSelector = objectSelector;
        this.dimensionDrawer = dimensionDrawer;
    }

    public Transform Draw(Transform transform)
    {
        using (new GUILayout.VerticalScope())
        {
            if (showGlobal)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Global Position", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                    NitroxGUILayout.Separator();
                    dimensionDrawer.Draw(transform.position, VECTOR_MAX_WIDTH);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Global Rotation", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                    NitroxGUILayout.Separator();
                    dimensionDrawer.Draw(transform.rotation.eulerAngles, VECTOR_MAX_WIDTH);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Lossy Scale", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                    NitroxGUILayout.Separator();
                    dimensionDrawer.Draw(transform.lossyScale, VECTOR_MAX_WIDTH);
                }
            }
            else
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Local  Position", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                    NitroxGUILayout.Separator();
                    transform.localPosition = dimensionDrawer.Draw(transform.localPosition, VECTOR_MAX_WIDTH);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Local  Rotation", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                    NitroxGUILayout.Separator();
                    transform.localRotation = Quaternion.Euler(dimensionDrawer.Draw(transform.localRotation.eulerAngles, VECTOR_MAX_WIDTH));
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Local  Scale", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                    NitroxGUILayout.Separator();
                    transform.localScale = dimensionDrawer.Draw(transform.localScale, VECTOR_MAX_WIDTH);
                }
            }

            GUILayout.Space(5);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Toggle Local/Global", GUILayout.MaxWidth(125)))
                {
                    showGlobal = !showGlobal;
                }
                if (GUILayout.Button("Destroy GameObject", GUILayout.MaxWidth(150)))
                {
                    if (transform)
                    {
                        if (transform.parent)
                        {
                            objectSelector.JumpToComponent(transform.parent);
                        }
                        Object.Destroy(transform.gameObject);
                    }
                }
            }
        }

        return transform;
    }
}
