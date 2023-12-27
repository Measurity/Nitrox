using NitroxClient.Debuggers.Drawer.Unity;
using NitroxModel.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace NitroxClient.Debuggers.Drawer.UnityUI;

public class VisualDrawer : IDrawer<Selectable>, IDrawer<Button>, IDrawer<Dropdown>, IDrawer<Scrollbar>, IDrawer<InputField>, IDrawer<Slider>, IDrawer<Toggle>, IDrawer<Image>, IDrawer<RawImage>, IDrawer<Text>
{
    private const float LABEL_WIDTH = 150;
    private const float VALUE_WIDTH = 200;
    private readonly ColorDrawer colorDrawer;
    private readonly DimensionDrawer dimensionDrawer;
    private readonly IDebugObjectSelector objectSelector;
    private readonly UnityEventDrawer unityEventDrawer;

    public VisualDrawer(IDebugObjectSelector objectSelector, ColorDrawer colorDrawer, UnityEventDrawer unityEventDrawer, DimensionDrawer dimensionDrawer)
    {
        Validate.NotNull(objectSelector);
        Validate.NotNull(colorDrawer);
        Validate.NotNull(unityEventDrawer);
        Validate.NotNull(dimensionDrawer);
        this.objectSelector = objectSelector;
        this.colorDrawer = colorDrawer;
        this.unityEventDrawer = unityEventDrawer;
        this.dimensionDrawer = dimensionDrawer;
    }

    public Selectable Draw(Selectable selectable)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Interactable", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.interactable = NitroxGUILayout.BoolField(selectable.interactable);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Transition", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.transition = NitroxGUILayout.EnumPopup(selectable.transition);
        }

        switch (selectable.transition)
        {
            case Selectable.Transition.ColorTint:
                DrawTransitionColorTint(selectable);
                break;
            case Selectable.Transition.SpriteSwap:
                DrawTransitionSpriteSwap(selectable);
                break;
            case Selectable.Transition.Animation:
                DrawTransitionAnimation(selectable);
                break;
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Navigation", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            NitroxGUILayout.EnumPopup(selectable.navigation.mode);
        }

        return selectable;
    }

    public Button Draw(Button button)
    {
        Draw((Selectable)button);
        GUILayout.Space(10);
        unityEventDrawer.Draw(button.onClick, "OnClick()");
        return button;
    }

    public Dropdown Draw(Dropdown dropdown)
    {
        Draw((Selectable)dropdown);

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Template", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(dropdown.template);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Caption Text", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(dropdown.captionText);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Caption Image", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(dropdown.captionImage);
            }
        }

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Item Text", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(dropdown.itemText);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Item Image", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(dropdown.itemImage);
            }
        }

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Value", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            dropdown.value = NitroxGUILayout.IntField(dropdown.value);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Alpha Fade Speed", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            dropdown.alphaFadeSpeed = NitroxGUILayout.FloatField(dropdown.alphaFadeSpeed);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Options", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.Button("Unsupported", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("On Value Changed", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.Button("Unsupported", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
        }

        return dropdown;
    }

    public Scrollbar Draw(Scrollbar scrollbar)
    {
        Draw((Selectable)scrollbar);

        GUILayout.Space(10);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Handle Rect", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(scrollbar.handleRect);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Direction", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            scrollbar.direction = NitroxGUILayout.EnumPopup(scrollbar.direction);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Value", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            scrollbar.value = NitroxGUILayout.SliderField(scrollbar.value, 0f, 1f);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Size", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            scrollbar.size = NitroxGUILayout.SliderField(scrollbar.size, 0f, 1f);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Number Of Steps", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            scrollbar.numberOfSteps = NitroxGUILayout.SliderField(scrollbar.numberOfSteps, 0, 11);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("On Value Changed", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.Button("Unsupported", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
        }

        return scrollbar;
    }

    public InputField Draw(InputField inputField)
    {
        Draw((Selectable)inputField);

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Text Component", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(inputField.textComponent);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Text", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.text = GUILayout.TextArea(inputField.text, GUILayout.MaxHeight(100));
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Character Limit", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.characterLimit = NitroxGUILayout.IntField(inputField.characterLimit);
        }

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Content Type", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.contentType = NitroxGUILayout.EnumPopup(inputField.contentType);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Line Type", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.lineType = NitroxGUILayout.EnumPopup(inputField.lineType);
        }

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Placeholder", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(inputField.placeholder);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Caret Blink Rate", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.caretBlinkRate = NitroxGUILayout.SliderField(inputField.caretBlinkRate, 0f, 4f);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Caret Width", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.caretWidth = NitroxGUILayout.SliderField(inputField.caretWidth, 1, 5);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Custom Caret Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.customCaretColor = NitroxGUILayout.BoolField(inputField.customCaretColor);
        }

        if (inputField.customCaretColor)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Caret Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
                NitroxGUILayout.Separator();
                inputField.caretColor = colorDrawer.Draw(inputField.caretColor);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Selection Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.selectionColor = colorDrawer.Draw(inputField.selectionColor);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Hide Mobile Input", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.shouldHideMobileInput = NitroxGUILayout.BoolField(inputField.shouldHideMobileInput);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Read Only", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            inputField.readOnly = NitroxGUILayout.BoolField(inputField.readOnly);
        }

        GUILayout.Space(NitroxGUILayout.DEFAULT_SPACE);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("On Value Changed", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.Button("Unsupported", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("On End Edit", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.Button("Unsupported", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
        }

        return inputField;
    }

    public Slider Draw(Slider slider)
    {
        Draw((Selectable)slider);

        GUILayout.Space(10);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Fill Rect", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(slider.fillRect);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Handle Rect", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.JumpToComponent(slider.handleRect);
            }
        }

        GUILayout.Space(10);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Direction", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            slider.direction = NitroxGUILayout.EnumPopup(slider.direction);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Min Value", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            slider.minValue = NitroxGUILayout.FloatField(slider.minValue);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Max Value", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            slider.maxValue = NitroxGUILayout.FloatField(slider.maxValue);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Whole Numbers", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            slider.wholeNumbers = NitroxGUILayout.BoolField(slider.wholeNumbers);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Value", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (slider.wholeNumbers)
            {
                slider.value = NitroxGUILayout.SliderField((int)slider.value, (int)slider.minValue, (int)slider.maxValue);
            }
            else
            {
                slider.value = NitroxGUILayout.SliderField(slider.value, slider.minValue, slider.maxValue);
            }
        }

        GUILayout.Space(10);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("On Value Changed", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.Button("Unsupported", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
        }

        return slider;
    }

    public Toggle Draw(Toggle toggle)
    {
        Draw((Selectable)toggle);
        GUILayout.Space(10);

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Is On", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            toggle.isOn = NitroxGUILayout.BoolField(toggle.isOn);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Toggle Transition", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            toggle.toggleTransition = NitroxGUILayout.EnumPopup(toggle.toggleTransition);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Graphic", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.UpdateSelectedObject(toggle.graphic.gameObject);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Group", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.UpdateSelectedObject(toggle.group.gameObject);
            }
        }

        unityEventDrawer.Draw(toggle.onValueChanged, "OnClick()");

        return toggle;
    }

    public Image Draw(Image image)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Image", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            DrawTexture(image.mainTexture);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            image.color = colorDrawer.Draw(image.color);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Material", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            image.material = Draw(image.material);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Raycast Target", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            image.raycastTarget = NitroxGUILayout.BoolField(image.raycastTarget);
        }

        return image;
    }

    public RawImage Draw(RawImage rawImage)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Image", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            DrawTexture(rawImage.mainTexture);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            rawImage.color = colorDrawer.Draw(rawImage.color);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Material", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            rawImage.material = Draw(rawImage.material);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Raycast Target", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            rawImage.raycastTarget = NitroxGUILayout.BoolField(rawImage.raycastTarget);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("UV Rect", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            rawImage.uvRect = dimensionDrawer.Draw(rawImage.uvRect);
        }

        return rawImage;
    }

    public Material Draw(Material material)
    {
        // TODO: Implement Material picker
        GUILayout.Box(material.name, GUILayout.Width(150), GUILayout.Height(20));
        return material;
    }

    public Text Draw(Text text)
    {
        GUILayout.Label("Text");
        text.text = GUILayout.TextArea(text.text, GUILayout.MaxHeight(100));

        GUILayout.Space(25);
        GUILayout.Label("Character:", "bold");
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Font", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.TextField(text.font ? text.font.name : "NoFont", GUILayout.Width(VALUE_WIDTH));
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Font Style", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            GUILayout.TextField(text.fontStyle.ToString(), GUILayout.Width(VALUE_WIDTH));
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Font Size", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.fontSize = NitroxGUILayout.IntField(text.fontSize, VALUE_WIDTH);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Line Spacing", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.lineSpacing = NitroxGUILayout.FloatField(text.lineSpacing, VALUE_WIDTH);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Rich Text", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.supportRichText = NitroxGUILayout.BoolField(text.supportRichText, VALUE_WIDTH);
        }

        GUILayout.Space(25);
        GUILayout.Label("Paragraph:", "bold");
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Alignment", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.alignment = NitroxGUILayout.EnumPopup(text.alignment, VALUE_WIDTH);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Align By Geometry", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.alignByGeometry = NitroxGUILayout.BoolField(text.alignByGeometry, VALUE_WIDTH);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Horizontal Overflow", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.horizontalOverflow = NitroxGUILayout.EnumPopup(text.horizontalOverflow, VALUE_WIDTH);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Vertical Overflow", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.verticalOverflow = NitroxGUILayout.EnumPopup(text.verticalOverflow, VALUE_WIDTH);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Best Fit", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.resizeTextForBestFit = NitroxGUILayout.BoolField(text.resizeTextForBestFit, VALUE_WIDTH);
        }

        GUILayout.Space(25);
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Color", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.color = colorDrawer.Draw(text.color);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Material", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.material = Draw(text.material);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Raycast Target", GUILayout.Width(LABEL_WIDTH));
            NitroxGUILayout.Separator();
            text.raycastTarget = NitroxGUILayout.BoolField(text.raycastTarget, VALUE_WIDTH);
        }

        return text;
    }

    private static void DrawTexture(Texture texture)
    {
        GUIStyle style = new("box") { fixedHeight = texture.height * (250f / texture.width), fixedWidth = 250 };
        GUILayout.Box(texture, style);
    }

    private static void DrawTransitionAnimation(Selectable selectable)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Normal Trigger", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.animationTriggers.normalTrigger = GUILayout.TextField(selectable.animationTriggers.normalTrigger);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Highlighted Trigger", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.animationTriggers.highlightedTrigger = GUILayout.TextField(selectable.animationTriggers.highlightedTrigger);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Pressed Trigger", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.animationTriggers.pressedTrigger = GUILayout.TextField(selectable.animationTriggers.pressedTrigger);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Selected Trigger", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.animationTriggers.selectedTrigger = GUILayout.TextField(selectable.animationTriggers.selectedTrigger);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Disabled Trigger", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectable.animationTriggers.disabledTrigger = GUILayout.TextField(selectable.animationTriggers.disabledTrigger);
        }
    }

    private void DrawTransitionSpriteSwap(Selectable selectable)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Target Graphic", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH)))
            {
                objectSelector.UpdateSelectedObject(selectable.targetGraphic.gameObject);
            }
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Highlighted Sprite", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            DrawTexture(selectable.spriteState.highlightedSprite.texture);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Pressed Sprite", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            DrawTexture(selectable.spriteState.pressedSprite.texture);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Selected Sprite", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            DrawTexture(selectable.spriteState.selectedSprite.texture);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Disabled Sprite", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            DrawTexture(selectable.spriteState.disabledSprite.texture);
        }
    }

    private void DrawTransitionColorTint(Selectable selectable)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Target Graphic", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            if (GUILayout.Button("Jump to", GUILayout.Width(NitroxGUILayout.VALUE_WIDTH)))
            {
                objectSelector.UpdateSelectedObject(selectable.targetGraphic.gameObject);
            }
        }

        Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor;
        float colorMultiplier, fadeDuration;

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Normal Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            normalColor = colorDrawer.Draw(selectable.colors.normalColor);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Highlighted Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            highlightedColor = colorDrawer.Draw(selectable.colors.highlightedColor);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Pressed Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            pressedColor = colorDrawer.Draw(selectable.colors.pressedColor);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Selected Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            selectedColor = colorDrawer.Draw(selectable.colors.selectedColor);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Disabled Color", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            disabledColor = colorDrawer.Draw(selectable.colors.disabledColor);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Color Multiplier", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            colorMultiplier = NitroxGUILayout.SliderField(selectable.colors.colorMultiplier, 1, 5);
        }

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Fader Duration", NitroxGUILayout.DrawerLabel, GUILayout.Width(NitroxGUILayout.DEFAULT_LABEL_WIDTH));
            NitroxGUILayout.Separator();
            fadeDuration = NitroxGUILayout.SliderField(selectable.colors.fadeDuration, 1, 5);
        }

        selectable.colors = new ColorBlock
        {
            normalColor = normalColor,
            highlightedColor = highlightedColor,
            pressedColor = pressedColor,
            selectedColor = selectedColor,
            disabledColor = disabledColor,
            colorMultiplier = colorMultiplier,
            fadeDuration = fadeDuration
        };
    }
}
