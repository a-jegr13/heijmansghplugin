using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;

namespace BT.DT.Synergy.PK.GHPlugin
{
    internal interface IComponentHandler
    {
        string SectionName { get; }
        bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj);
        void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength);
        void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values);
    }

    internal class SliderHandler : IComponentHandler
    {
        public string SectionName => "[Sliders]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_NumberSlider;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_NumberSlider slider = obj as GH_NumberSlider;
            double value = (double)slider.Slider.Value;
            string name = !string.IsNullOrEmpty(slider.NickName) ? slider.NickName : "Slider";
            values[slider.InstanceGuid] = value.ToString();
            names[slider.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_NumberSlider slider = obj as GH_NumberSlider;
            if (values.TryGetValue(slider.InstanceGuid, out string valueString))
            {
                if (decimal.TryParse(valueString, out decimal valueDecimal))
                {
                    slider.Slider.Value = valueDecimal;
                }
            }
        }
    }
    internal class KnobHandler : IComponentHandler
    {
        public string SectionName => "[Control Knobs]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_DialKnob;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_DialKnob knob = obj as GH_DialKnob;
            double value = (double)knob.Value;
            string name = !string.IsNullOrEmpty(knob.NickName) ? knob.NickName : "Knob";
            values[knob.InstanceGuid] = value.ToString();
            names[knob.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_DialKnob knob = obj as GH_DialKnob;
            if (values.TryGetValue(knob.InstanceGuid, out string valueString))
            {
                if (decimal.TryParse(valueString, out decimal valueDecimal))
                {
                    knob.Value = valueDecimal;
                }
            }
        }
    }
    internal class MultiSliderHandler : IComponentHandler
    {
        public string SectionName => "[Multidimensional Sliders]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_MultiDimensionalSlider;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_MultiDimensionalSlider multiSlider = obj as GH_MultiDimensionalSlider;
            string valueString = multiSlider.Value.X + "," + multiSlider.Value.Y + "," + multiSlider.Value.Z;
            string name = !string.IsNullOrEmpty(multiSlider.NickName) ? multiSlider.NickName : "MultiSlider";
            values[multiSlider.InstanceGuid] = valueString;
            names[multiSlider.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_MultiDimensionalSlider multiSlider = obj as GH_MultiDimensionalSlider;
            if (values.TryGetValue(multiSlider.InstanceGuid, out string valueString))
            {
                string[] parts = valueString.Split(',');
                if (parts.Length == 3 && double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y) && double.TryParse(parts[2], out double z))
                {
                    multiSlider.Value = new Point3d(x, y, z);
                }
            }
        }
    }
    internal class ToggleHandler : IComponentHandler
    {
        public string SectionName => "[Boolean Toggles]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_BooleanToggle;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_BooleanToggle toggle = obj as GH_BooleanToggle;
            bool value = toggle.Value;
            string name = !string.IsNullOrEmpty(toggle.NickName) ? toggle.NickName : "Toggle";
            values[toggle.InstanceGuid] = value.ToString();
            names[toggle.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_BooleanToggle toggle = obj as GH_BooleanToggle;
            if (values.TryGetValue(toggle.InstanceGuid, out string valueString))
            {
                if (bool.TryParse(valueString, out bool valueBool))
                {
                    toggle.Value = valueBool;
                }
            }
        }
    }
    internal class PanelHandler : IComponentHandler
    {
        public string SectionName => "[Panels]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_Panel;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_Panel panel = obj as GH_Panel;
            string value = panel.UserText;
            string name = !string.IsNullOrEmpty(panel.NickName) ? panel.NickName : "Panel";
            values[panel.InstanceGuid] = value;
            names[panel.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_Panel panel = obj as GH_Panel;
            if (values.TryGetValue(panel.InstanceGuid, out string valueString))
            {
                panel.UserText = valueString.Replace("<lf>", "\n").Replace("<cr>", "\r");
            }
        }
    }
    internal class ValueListHandler : IComponentHandler
    {
        public string SectionName => "[Value Lists]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_ValueList;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_ValueList valueList = obj as GH_ValueList;
            List<int> selectedIndices = new List<int>();
            for (int i = 0; i < valueList.ListItems.Count; i++)
            {
                if (valueList.ListItems[i].Selected)
                {
                    selectedIndices.Add(i);
                }
            }
            string name = !string.IsNullOrEmpty(valueList.NickName) ? valueList.NickName : "ValueList";
            string selectedIndicesString = string.Join(",", selectedIndices);
            values[valueList.InstanceGuid] = selectedIndicesString;
            names[valueList.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_ValueList valueList = obj as GH_ValueList;
            if (values.TryGetValue(valueList.InstanceGuid, out string valueString))
            {
                List<int> selectedIndices = new List<int>();
                if (!string.IsNullOrEmpty(valueString))
                {
                    string[] indicesArray = valueString.Split(',');
                    foreach (string indexString in indicesArray)
                    {
                        if (int.TryParse(indexString, out int index))
                        {
                            selectedIndices.Add(index);
                        }
                    }
                }
                for (int i = 0; i < valueList.ListItems.Count; i++)
                {
                    valueList.ListItems[i].Selected = selectedIndices.Contains(i);
                }
            }
        }
    }
    internal class ColorSwatchHandler : IComponentHandler
    {
        public string SectionName => "[Color Swatches]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_ColourSwatch;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_ColourSwatch colorSwatch = obj as GH_ColourSwatch;
            Color currentColor = colorSwatch.SwatchColour;
            string name = !string.IsNullOrEmpty(colorSwatch.NickName) ? colorSwatch.NickName : "ColorSwatch";
            string rgbaValue = currentColor.R + "," + currentColor.G + "," + currentColor.B + "," + currentColor.A;
            values[colorSwatch.InstanceGuid] = rgbaValue;
            names[colorSwatch.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_ColourSwatch colorSwatch = obj as GH_ColourSwatch;
            if (values.TryGetValue(colorSwatch.InstanceGuid, out string valueString))
            {
                string[] rgbaValues = valueString.Split(',');
                if (rgbaValues.Length == 4 && int.TryParse(rgbaValues[0], out int r) && int.TryParse(rgbaValues[1], out int g) && int.TryParse(rgbaValues[2], out int b) && int.TryParse(rgbaValues[3], out int a))
                {
                    Color newColor = Color.FromArgb(a, r, g, b);
                    colorSwatch.SwatchColour = newColor;
                }
            }
        }
    }
    internal class ColorPickerHandler : IComponentHandler
    {
        public string SectionName => "[Color Pickers]";
        public bool CanHandle(Grasshopper.Kernel.IGH_DocumentObject obj) => obj is GH_ColourPickerObject;
        public void SaveComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> names, Dictionary<Guid, string> values, ref int maxNameLength)
        {
            GH_ColourPickerObject colorPicker = obj as GH_ColourPickerObject;
            Color currentColor = colorPicker.Colour;
            string name = !string.IsNullOrEmpty(colorPicker.NickName) ? colorPicker.NickName : "ColorPicker";
            string rgbaValue = currentColor.R + "," + currentColor.G + "," + currentColor.B + "," + currentColor.A;
            values[colorPicker.InstanceGuid] = rgbaValue;
            names[colorPicker.InstanceGuid] = name;
            maxNameLength = Math.Max(maxNameLength, name.Length);
        }
        public void RestoreComponent(Grasshopper.Kernel.IGH_DocumentObject obj, Dictionary<Guid, string> values)
        {
            GH_ColourPickerObject colorPicker = obj as GH_ColourPickerObject;
            if (values.TryGetValue(colorPicker.InstanceGuid, out string valueString))
            {
                string[] rgbaValues = valueString.Split(',');
                if (rgbaValues.Length == 4 && int.TryParse(rgbaValues[0], out int r) && int.TryParse(rgbaValues[1], out int g) && int.TryParse(rgbaValues[2], out int b) && int.TryParse(rgbaValues[3], out int a))
                {
                    Color newColor = Color.FromArgb(a, r, g, b);
                    colorPicker.Colour = newColor;
                    colorPicker.TriggerAutoSave();
                    colorPicker.ExpireSolution(true);
                }
            }
        }
    }
}
