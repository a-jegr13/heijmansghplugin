using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Attributes;

using BT.DT.Synergy.PK.GHPlugin;
namespace BT.DT.Synergy.PK.GHPlugin
{
    public class StateManagerComponent : GH_Component
    {
        // --- Members ---
        internal HashSet<Guid> SelectedComponentGuids = new HashSet<Guid>();
        internal List<IGH_DocumentObject> AllComponents
        {
            get
            {
                if (ghdoc == null || componentHandlers == null)
                    return new List<IGH_DocumentObject>();
                return ghdoc.Objects.Where(obj => componentHandlers.Any(h => h.CanHandle(obj))).ToList();
            }
        }
        private GH_Document ghdoc => Grasshopper.Instances.ActiveCanvas?.Document;
        private string InputStateGuid = null;
        private const string CsvDelimiter = "|";
        private int SaveCounter;
        private int RestoreCounter;
        private int InvalidCounter;
        private string docName;
        private string directory;
        private string filePath;
        private List<IComponentHandler> componentHandlers;


                // --- Constructor ---
                public StateManagerComponent()
                    : base("State Manager", "StateMgr",
                        "Save and restore Grasshopper input states (sliders, toggles, etc.) to file.",
                        "Heijmans Synergy", "Utils")
                {
                }
        // --- Custom Attributes for Checkbox UI ---
        public override void CreateAttributes()
        {
            m_attributes = new StateManagerAttributes(this);
        }

        internal void ToggleComponentSelection(Guid guid)
        {
            if (SelectedComponentGuids.Contains(guid))
                SelectedComponentGuids.Remove(guid);
            else
                SelectedComponentGuids.Add(guid);
            ExpireSolution(true);
        }


        // --- Register Inputs ---
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("SaveInputs", "Save", "Save all input states to file", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("RestoreInputs", "Restore", "Restore all input states from file", GH_ParamAccess.item, false);
            pManager.AddTextParameter("StateGuid", "Guid", "Optional: Specify a state GUID to restore", GH_ParamAccess.item, "");
        }

        // --- Register Outputs ---
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        // --- SolveInstance ---
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool saveInputs = false;
            bool restoreInputs = false;
            string stateGuid = "";
            DA.GetData(0, ref saveInputs);
            DA.GetData(1, ref restoreInputs);
            DA.GetData(2, ref stateGuid);

            bool invalidInputs = saveInputs && restoreInputs;
            RestoreCounter = restoreInputs ? RestoreCounter + 1 : 0;
            SaveCounter = saveInputs ? SaveCounter + 1 : 0;
            InvalidCounter = invalidInputs ? InvalidCounter + 1 : 0;
            InitializeHandlers();
            InputStateGuid = string.IsNullOrWhiteSpace(stateGuid) ? null : stateGuid.Trim();

            string status = "";
            if (invalidInputs)
            {
                if (InvalidCounter == 1)
                {
                    status = "Error: SaveInputs and RestoreInputs cannot be active at the same time.";
                }
                SaveCounter = 10;
                RestoreCounter = 10;
            }
            else if (SaveCounter == 1)
            {
                status = SaveComponentStates();
            }
            else if (RestoreCounter == 1)
            {
                status = RestoreComponentStates(InputStateGuid);
            }
        }

        // --- Exposure ---
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // --- Icon ---
        protected override System.Drawing.Bitmap Icon => null;

        // --- Guid ---
        public override Guid ComponentGuid => new Guid("7ba64de5-086a-446f-9c32-252b8199d27d");

        // --- Helper Methods ---
        void InitializeHandlers()
        {
            componentHandlers = new List<IComponentHandler>
            {
                new SliderHandler(),
                new KnobHandler(),
                new MultiSliderHandler(),
                new ToggleHandler(),
                new PanelHandler(),
                new ValueListHandler(),
                new ColorSwatchHandler(),
                new ColorPickerHandler()
            };
        }

        string SaveComponentStates()
        {
            GetFileInfo();
            if (string.IsNullOrEmpty(filePath))
            {
                return "Error: Unable to determine file path.";
            }
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                return "Error deleting existing file: " + ex.Message;
            }

            Dictionary<IComponentHandler, Dictionary<Guid, string>> allNames = new Dictionary<IComponentHandler, Dictionary<Guid, string>>();
            Dictionary<IComponentHandler, Dictionary<Guid, string>> allValues = new Dictionary<IComponentHandler, Dictionary<Guid, string>>();
            Dictionary<IComponentHandler, int> maxNameLengths = new Dictionary<IComponentHandler, int>();

            foreach (var handler in componentHandlers)
            {
                allNames[handler] = new Dictionary<Guid, string>();
                allValues[handler] = new Dictionary<Guid, string>();
                maxNameLengths[handler] = 0;
            }

            // Only save selected components
            foreach (var obj in ghdoc.Objects)
            {
                if (!SelectedComponentGuids.Contains(obj.InstanceGuid))
                    continue;
                IComponentHandler matchedHandler = null;
                foreach (var handler in componentHandlers)
                {
                    if (handler.CanHandle(obj))
                    {
                        matchedHandler = handler;
                        break;
                    }
                }
                if (matchedHandler != null)
                {
                    int currentMaxLength = maxNameLengths[matchedHandler];
                    matchedHandler.SaveComponent(obj, allNames[matchedHandler], allValues[matchedHandler], ref currentMaxLength);
                    maxNameLengths[matchedHandler] = currentMaxLength;
                }
            }

            List<string> iniContent = new List<string>();
            Dictionary<string, int> contentMap = new Dictionary<string, int>();

            foreach (var handler in componentHandlers)
            {
                var values = allValues[handler];
                var names = allNames[handler];
                int maxLength = maxNameLengths[handler];
                if (values.Count > 0)
                {
                    iniContent.Add(handler.SectionName);
                    foreach (var entry in values)
                    {
                        string name = names.ContainsKey(entry.Key) ? names[entry.Key] : "Unknown";
                        iniContent.Add(name.PadRight(maxLength) + " | " + entry.Key + " = " + entry.Value);
                    }
                    contentMap[handler.SectionName] = values.Count;
                }
            }

            try
            {
                System.IO.File.WriteAllLines(filePath, iniContent);
            }
            catch (Exception ex)
            {
                return "Error saving file: " + ex.Message + "\nFile path: " + filePath;
            }

            // --- CSV Export ---
            try
            {
                string csvFilePath = null;
                string modelStateId = null;
                if (!string.IsNullOrEmpty(directory) && !string.IsNullOrEmpty(docName))
                {
                    string statesFolder = System.IO.Path.Combine(directory, docName + ".States");
                    csvFilePath = System.IO.Path.Combine(statesFolder, docName + ".States.csv");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var fileName = System.IO.Path.GetFileName(filePath);
                        var parts = fileName.Split('.');
                        if (parts.Length >= 3 && parts[parts.Length - 2].Length == 6)
                        {
                            modelStateId = parts[parts.Length - 2];
                        }
                    }
                }
                if (!string.IsNullOrEmpty(csvFilePath))
                {
                    Dictionary<string, string> flatNameValue = new Dictionary<string, string>();
                    foreach (var handler in componentHandlers)
                    {
                        var names = allNames[handler];
                        var values = allValues[handler];
                        foreach (var entry in values)
                        {
                            string name = names.ContainsKey(entry.Key) ? names[entry.Key] : "Unknown";
                            flatNameValue[name] = entry.Value;
                        }
                    }

                    List<string> csvLines = new List<string>();
                    List<string> headers = new List<string>();
                    Dictionary<string, int> headerCounts = new Dictionary<string, int>();
                    bool hasModelStateColumn = false;
                    if (System.IO.File.Exists(csvFilePath))
                    {
                        csvLines.AddRange(System.IO.File.ReadAllLines(csvFilePath));
                        if (csvLines.Count > 0)
                        {
                            headers.AddRange(csvLines[0].Split(new string[] { CsvDelimiter }, StringSplitOptions.None));
                            foreach (var h in headers)
                            {
                                string baseHeader = h;
                                int n = 1;
                                while (headerCounts.ContainsKey(baseHeader))
                                {
                                    baseHeader = h + n.ToString();
                                    n++;
                                }
                                headerCounts[baseHeader] = 1;
                            }
                            hasModelStateColumn = headers.Contains("ModelState");
                        }
                    }
                    if (!hasModelStateColumn)
                    {
                        headers.Insert(0, "ModelState");
                    }
                    foreach (var key in flatNameValue.Keys)
                    {
                        string baseKey = key;
                        int suffix = 1;
                        string uniqueKey = baseKey;
                        while (headers.Contains(uniqueKey))
                        {
                            uniqueKey = baseKey + suffix.ToString();
                            suffix++;
                        }
                        if (!headers.Contains(uniqueKey))
                        {
                            headers.Add(uniqueKey);
                        }
                    }
                    List<string> row = new List<string>();
                    row.Add(modelStateId ?? "");
                    foreach (var header in headers)
                    {
                        if (header == "ModelState")
                            continue;
                        string value = "";
                        if (flatNameValue.TryGetValue(header, out value))
                        {
                            string safeValue = value.Replace(CsvDelimiter, ",").Replace("\r", "\\r").Replace("\n", "\\n");
                            row.Add(safeValue);
                        }
                        else
                        {
                            row.Add("");
                        }
                    }
                    for (int i = row.Count - 1; i > 0; i--)
                    {
                        if (string.IsNullOrEmpty(row[i]))
                            row.RemoveAt(i);
                        else
                            break;
                    }
                    if (csvLines.Count == 0)
                    {
                        csvLines.Add(string.Join(CsvDelimiter, headers));
                    }
                    csvLines.Add(string.Join(CsvDelimiter, row));
                    System.IO.File.WriteAllLines(csvFilePath, csvLines);
                }
            }
            catch (Exception ex)
            {
                return "Error appending to CSV: " + ex.Message;
            }
            return "Saved state to: " + filePath;
        }

        string RestoreComponentStates(string guidOverride = null)
        {
            docName = null;
            directory = null;
            if (Rhino.RhinoDoc.ActiveDoc?.Name != null)
            {
                docName = Rhino.RhinoDoc.ActiveDoc.Name;
                directory = System.IO.Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);
            }
            else if (!string.IsNullOrEmpty(ghdoc?.FilePath))
            {
                docName = ghdoc.DisplayName.TrimEnd('*');
                directory = System.IO.Path.GetDirectoryName(ghdoc.FilePath);
            }
            else
            {
                return "Error: Unable to determine file path.";
            }
            string statesFolder = System.IO.Path.Combine(directory, docName + ".States");
            if (!System.IO.Directory.Exists(statesFolder))
            {
                return "States folder not found: " + statesFolder;
            }
            string targetFile = null;
            if (!string.IsNullOrEmpty(guidOverride))
            {
                string pattern = docName + ".State." + guidOverride + ".txt";
                targetFile = System.IO.Path.Combine(statesFolder, pattern);
                if (!System.IO.File.Exists(targetFile))
                {
                    return "State file with GUID '" + guidOverride + "' not found: " + targetFile;
                }
            }
            else
            {
                var files = System.IO.Directory.GetFiles(statesFolder, docName + ".State.*.txt");
                if (files.Length == 0)
                {
                    return "No state files found in: " + statesFolder;
                }
                targetFile = files.OrderByDescending(f => System.IO.File.GetCreationTime(f)).First();
            }
            string[] iniLines;
            try
            {
                iniLines = System.IO.File.ReadAllLines(targetFile);
            }
            catch (Exception ex)
            {
                return "Error reading file: " + ex.Message + "\nFile path: " + targetFile;
            }
            Dictionary<IComponentHandler, Dictionary<Guid, string>> allRestoredValues = new Dictionary<IComponentHandler, Dictionary<Guid, string>>();
            foreach (var handler in componentHandlers)
            {
                allRestoredValues[handler] = new Dictionary<Guid, string>();
            }
            IComponentHandler currentHandler = null;
            foreach (string line in iniLines)
            {
                if (line.StartsWith("["))
                {
                    currentHandler = null;
                    foreach (var handler in componentHandlers)
                    {
                        if (line.Equals(handler.SectionName))
                        {
                            currentHandler = handler;
                            break;
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line) && currentHandler != null)
                {
                    string[] parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string nameAndGuid = parts[0].Trim();
                        int firstPipeIndex = nameAndGuid.IndexOf('|');
                        if (firstPipeIndex > 0)
                        {
                            string guidString = nameAndGuid.Substring(firstPipeIndex + 1).Trim();
                            if (!Guid.TryParse(guidString, out Guid guid))
                            {
                                continue;
                            }
                            string valueString = parts[1].Trim();
                            allRestoredValues[currentHandler][guid] = valueString;
                        }
                    }
                }
            }
            foreach (var obj in ghdoc.Objects)
            {
                IComponentHandler matchedHandler = null;
                foreach (var handler in componentHandlers)
                {
                    if (handler.CanHandle(obj))
                    {
                        matchedHandler = handler;
                        break;
                    }
                }
                if (matchedHandler != null)
                {
                    matchedHandler.RestoreComponent(obj, allRestoredValues[matchedHandler]);
                }
            }
            if (ghdoc != null)
            {
                ghdoc.ScheduleSolution(5, OnScheduledSolution);
            }
            return "Restored state from: " + targetFile;
        }

        void GetFileInfo()
        {
            docName = null;
            directory = null;
            if (Rhino.RhinoDoc.ActiveDoc?.Name != null)
            {
                docName = Rhino.RhinoDoc.ActiveDoc.Name;
                directory = System.IO.Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);
            }
            else if (!string.IsNullOrEmpty(ghdoc?.FilePath))
            {
                docName = ghdoc.DisplayName.TrimEnd('*');
                directory = System.IO.Path.GetDirectoryName(ghdoc.FilePath);
            }
            else
            {
                docName = null;
            }
            if (docName != null)
            {
                string statesFolder = System.IO.Path.Combine(directory, docName + ".States");
                if (!System.IO.Directory.Exists(statesFolder))
                {
                    System.IO.Directory.CreateDirectory(statesFolder);
                }
                string shortGuid = Guid.NewGuid().ToString("N").Substring(0, 6);
                string fileName = docName + ".State." + shortGuid + ".txt";
                filePath = System.IO.Path.Combine(statesFolder, fileName);
            }
        }

        void OnScheduledSolution(GH_Document grhdoc)
        {
            grhdoc.NewSolution(true);
        }

        // --- Document Event Subscription for Layout Updates ---
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            if (document != null)
            {
                document.ObjectsAdded += Document_ObjectsChanged;
                document.ObjectsDeleted += Document_ObjectsChanged;
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (document != null)
            {
                document.ObjectsAdded -= Document_ObjectsChanged;
                document.ObjectsDeleted -= Document_ObjectsChanged;
            }
            base.RemovedFromDocument(document);
        }

        private void Document_ObjectsChanged(object sender, GH_DocObjectEventArgs e)
        {
            // Invalidate layout and force redraw when objects are added/removed
            if (Attributes != null)
            {
                Attributes.ExpireLayout();
            }
            ExpireSolution(true);
        }
    }
}