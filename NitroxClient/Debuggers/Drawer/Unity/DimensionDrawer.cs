using System;
using NitroxModel_Subnautica.DataStructures;
using NitroxModel.DataStructures.Unity;
using UnityEngine;

namespace NitroxClient.Debuggers.Drawer.Unity;

public class DimensionDrawer : IDrawer<Vector2>, IDrawer<Vector3>, IDrawer<NitroxVector3>, IDrawer<Vector4>, IDrawer<NitroxVector4>, IDrawer<Quaternion>, IDrawer<Int3>, IDrawer<RectTransform>, IDrawer<Rect>
{
    private const float LABEL_WIDTH = 120;
    private const float VECTOR_MAX_WIDTH = 405;
    private const float MAX_WIDTH = 400;

    private const float FLOAT_TOLERANCE = 0.0001f;

    public static Tuple<int, int, int, int> DrawInt4(int item1, int item2, int item3, int item4, float maxWidth = MAX_WIDTH)
    {
        float valueWidth = maxWidth / 4 - 6;
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth)))
        {
            item1 = NitroxGUILayout.IntField(item1, valueWidth);
            NitroxGUILayout.Separator();
            item2 = NitroxGUILayout.IntField(item2, valueWidth);
            NitroxGUILayout.Separator();
            item4 = NitroxGUILayout.IntField(item4, valueWidth);
            NitroxGUILayout.Separator();
            item4 = NitroxGUILayout.IntField(item4, valueWidth);
            return new Tuple<int, int, int, int>(item1, item2, item3, item4);
        }
    }

    public Rect Draw(Rect rect, float valueWidth, float maxWidth)
    {
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth)))
        {
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("X:", NitroxGUILayout.DrawerLabel);
                    rect.x = NitroxGUILayout.FloatField(rect.x, valueWidth);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Y:", NitroxGUILayout.DrawerLabel);
                    rect.y = NitroxGUILayout.FloatField(rect.y, valueWidth);
                }
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("W:", NitroxGUILayout.DrawerLabel);
                    rect.width = NitroxGUILayout.FloatField(rect.width, valueWidth);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("H:", NitroxGUILayout.DrawerLabel);
                    rect.height = NitroxGUILayout.FloatField(rect.height, valueWidth);
                }
            }
        }

        return rect;
    }

    public Rect Draw(Rect rect)
    {
        return Draw(rect, 100, 215);
    }

    public Vector2 Draw(Vector2 vector2, float maxWidth)
    {
        float valueWidth = maxWidth / 2 - 5;
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth)))
        {
            vector2.x = NitroxGUILayout.FloatField(vector2.x, valueWidth);
            NitroxGUILayout.Separator();
            vector2.y = NitroxGUILayout.FloatField(vector2.y, valueWidth);
            return vector2;
        }
    }

    public Vector2 Draw(Vector2 vector2)
    {
        return Draw(vector2, MAX_WIDTH);
    }

    public Vector3 Draw(Vector3 vector3, float maxWidth)
    {
        float valueWidth = maxWidth / 3 - 5;
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth)))
        {
            vector3.x = NitroxGUILayout.FloatField(vector3.x, valueWidth);
            NitroxGUILayout.Separator();
            vector3.y = NitroxGUILayout.FloatField(vector3.y, valueWidth);
            NitroxGUILayout.Separator();
            vector3.z = NitroxGUILayout.FloatField(vector3.z, valueWidth);
            return vector3;
        }
    }

    public Vector3 Draw(Vector3 vector3)
    {
        return Draw(vector3, MAX_WIDTH);
    }

    public Vector4 Draw(Vector4 vector4, float maxWidth)
    {
        float valueWidth = maxWidth / 4 - 6;
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth)))
        {
            vector4.x = NitroxGUILayout.FloatField(vector4.x, valueWidth);
            NitroxGUILayout.Separator();
            vector4.y = NitroxGUILayout.FloatField(vector4.y, valueWidth);
            NitroxGUILayout.Separator();
            vector4.z = NitroxGUILayout.FloatField(vector4.z, valueWidth);
            NitroxGUILayout.Separator();
            vector4.w = NitroxGUILayout.FloatField(vector4.w, valueWidth);
            return vector4;
        }
    }

    public Vector4 Draw(Vector4 vector4)
    {
        return Draw(vector4, MAX_WIDTH);
    }

    public Quaternion Draw(Quaternion quaternion, float maxWidth)
    {
        Vector4 vector4 = new(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        vector4 = Draw(vector4, maxWidth);
        return new Quaternion(vector4.x, vector4.y, vector4.z, vector4.w);
    }

    public Quaternion Draw(Quaternion quaternion)
    {
        return Draw(quaternion, MAX_WIDTH);
    }

    public Int3 Draw(Int3 int3)
    {
        return Draw(int3, MAX_WIDTH);
    }

    public Int3 Draw(Int3 int3, float maxWidth)
    {
        float valueWidth = maxWidth / 3 - 5;
        using (new GUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth)))
        {
            int3.x = NitroxGUILayout.IntField(int3.x, valueWidth);
            NitroxGUILayout.Separator();
            int3.y = NitroxGUILayout.IntField(int3.y, valueWidth);
            NitroxGUILayout.Separator();
            int3.z = NitroxGUILayout.IntField(int3.z, valueWidth);
            return int3;
        }
    }

    public NitroxVector3 Draw(NitroxVector3 target)
    {
        return Draw(target.ToUnity()).ToDto();
    }

    public NitroxVector4 Draw(NitroxVector4 target)
    {
        return Draw(target.ToUnity()).ToDto();
    }

    public RectTransform Draw(RectTransform rectTransform)
    {
        using (new GUILayout.VerticalScope())
        {
            //TODO: Implement position display like the Unity editor
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Anchored Position", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                rectTransform.anchoredPosition = Draw(rectTransform.anchoredPosition, VECTOR_MAX_WIDTH);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Local Position", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                rectTransform.localPosition = Draw(rectTransform.localPosition, VECTOR_MAX_WIDTH);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Local  Rotation", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                rectTransform.localRotation = Quaternion.Euler(Draw(rectTransform.localRotation.eulerAngles, VECTOR_MAX_WIDTH));
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Local  Scale", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                rectTransform.localScale = Draw(rectTransform.localScale, VECTOR_MAX_WIDTH);
            }

            GUILayout.Space(20);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Size", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                rectTransform.sizeDelta = Draw(rectTransform.sizeDelta, VECTOR_MAX_WIDTH);
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Anchor", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                AnchorMode anchorMode = VectorToAnchorMode(rectTransform.anchorMin, rectTransform.anchorMax);

                if (anchorMode == AnchorMode.NONE)
                {
                    Draw(rectTransform.anchorMin, VECTOR_MAX_WIDTH * 0.5f);
                    Draw(rectTransform.anchorMax, VECTOR_MAX_WIDTH * 0.5f);
                }
                else
                {
                    anchorMode = NitroxGUILayout.EnumPopup(anchorMode, VECTOR_MAX_WIDTH);
                    if (anchorMode != AnchorMode.NONE)
                    {
                        Vector2[] anchorVectors = AnchorModeToVector(anchorMode);
                        rectTransform.anchorMin = anchorVectors[0];
                        rectTransform.anchorMax = anchorVectors[1];
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Pivot", NitroxGUILayout.DrawerLabel, GUILayout.Width(LABEL_WIDTH));
                NitroxGUILayout.Separator();
                rectTransform.pivot = Draw(rectTransform.pivot, VECTOR_MAX_WIDTH);
            }
        }

        return rectTransform;
    }

    private static AnchorMode VectorToAnchorMode(Vector2 min, Vector2 max)
    {
        bool minXNull = min.x == 0f;
        bool minXHalf = Math.Abs(min.x - 0.5f) < FLOAT_TOLERANCE;
        bool minXFull = Math.Abs(min.x - 1f) < FLOAT_TOLERANCE;

        bool minYNull = min.y == 0f;
        bool minYHalf = Math.Abs(min.y - 0.5f) < FLOAT_TOLERANCE;
        bool minYFull = Math.Abs(min.y - 1f) < FLOAT_TOLERANCE;

        bool maxXNull = max.x == 0f;
        bool maxXHalf = Math.Abs(max.x - 0.5f) < FLOAT_TOLERANCE;
        bool maxXFull = Math.Abs(max.x - 1f) < FLOAT_TOLERANCE;

        bool maxYNull = max.y == 0f;
        bool maxYHalf = Math.Abs(max.y - 0.5f) < FLOAT_TOLERANCE;
        bool maxYFull = Math.Abs(max.y - 1f) < FLOAT_TOLERANCE;

        if (minYFull && maxYFull)
        {
            if (minXNull && maxXNull)
                return AnchorMode.TOP_LEFT;
            if (minXHalf && maxXHalf)

                return AnchorMode.TOP_CENTER;
            if (minXFull && maxXFull)
                return AnchorMode.TOP_RIGHT;
            if (minXNull && maxXFull)
                return AnchorMode.TOP_STRETCH;
        }

        if (minYHalf && maxYHalf)
        {
            if (minXNull && maxXNull)
                return AnchorMode.MIDDLE_LEFT;
            if (minXHalf && maxXHalf)
                return AnchorMode.MIDDLE_CENTER;
            if (minXFull && maxXFull)
                return AnchorMode.MIDDLE_RIGHT;
            if (minXNull && maxXFull)
                return AnchorMode.MIDDLE_STRETCH;
        }

        if (minYNull && maxYNull)
        {
            if (minXNull && maxXNull)
                return AnchorMode.BOTTOM_LEFT;
            if (minXHalf && maxXHalf)
                return AnchorMode.BOTTOM_CENTER;
            if (minXFull && maxXFull)
                return AnchorMode.BOTTOM_RIGHT;
            if (minXNull && maxXFull)
                return AnchorMode.BOTTOM_STRETCH;
        }

        if (minYNull && maxYFull)
        {
            if (minXNull && maxXNull)
                return AnchorMode.STRETCH_LEFT;
            if (minXHalf && maxXHalf)
                return AnchorMode.STRETCH_CENTER;
            if (minXFull && maxXFull)
                return AnchorMode.STRETCH_RIGHT;
            if (minXNull && maxXFull)
                return AnchorMode.STRETCH_STRETCH;
        }

        return AnchorMode.NONE;
    }

    private static Vector2[] AnchorModeToVector(AnchorMode anchorMode)
    {
        return anchorMode switch
        {
            AnchorMode.TOP_LEFT => new[] { new Vector2(0, 1), new Vector2(0, 1) },
            AnchorMode.TOP_CENTER => new[] { new Vector2(0.5f, 1), new Vector2(0.5f, 1) },
            AnchorMode.TOP_RIGHT => new[] { new Vector2(1, 1), new Vector2(1, 1) },
            AnchorMode.TOP_STRETCH => new[] { new Vector2(0, 1), new Vector2(1, 1) },
            AnchorMode.MIDDLE_LEFT => new[] { new Vector2(0, 0.5f), new Vector2(0, 0.5f) },
            AnchorMode.MIDDLE_CENTER => new[] { new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f) },
            AnchorMode.MIDDLE_RIGHT => new[] { new Vector2(1, 0.5f), new Vector2(1, 0.5f) },
            AnchorMode.MIDDLE_STRETCH => new[] { new Vector2(0, 0.5f), new Vector2(1, 0.5f) },
            AnchorMode.BOTTOM_LEFT => new[] { new Vector2(0, 0), new Vector2(0, 0) },
            AnchorMode.BOTTOM_CENTER => new[] { new Vector2(0.5f, 0), new Vector2(0.5f, 0) },
            AnchorMode.BOTTOM_RIGHT => new[] { new Vector2(1, 0), new Vector2(1, 0) },
            AnchorMode.BOTTOM_STRETCH => new[] { new Vector2(0, 0), new Vector2(1, 0) },
            AnchorMode.STRETCH_LEFT => new[] { new Vector2(0, 0), new Vector2(0, 1) },
            AnchorMode.STRETCH_CENTER => new[] { new Vector2(0.5f, 0), new Vector2(0.5f, 1) },
            AnchorMode.STRETCH_RIGHT => new[] { new Vector2(1, 0), new Vector2(1, 1) },
            AnchorMode.STRETCH_STRETCH => new[] { new Vector2(0, 0), new Vector2(1, 1) },
            AnchorMode.NONE => throw new ArgumentOutOfRangeException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private enum AnchorMode
    {
        TOP_LEFT,
        TOP_CENTER,
        TOP_RIGHT,
        TOP_STRETCH,
        MIDDLE_LEFT,
        MIDDLE_CENTER,
        MIDDLE_RIGHT,
        MIDDLE_STRETCH,
        BOTTOM_LEFT,
        BOTTOM_CENTER,
        BOTTOM_RIGHT,
        BOTTOM_STRETCH,
        STRETCH_LEFT,
        STRETCH_CENTER,
        STRETCH_RIGHT,
        STRETCH_STRETCH,
        NONE
    }
}
