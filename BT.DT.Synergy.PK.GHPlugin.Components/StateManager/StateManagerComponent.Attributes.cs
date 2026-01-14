using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace BT.DT.Synergy.PK.GHPlugin
{
    public class StateManagerAttributes : GH_ComponentAttributes
    {
        private const int CheckboxSize = 16;
        private const int Padding = 4;
        private const int RowHeight = 24;
        private const int ScrollbarWidth = 12;
        private const int VisibleRows = 8;
        private int scrollOffset = 0;
        private bool isScrolling = false;
        private int scrollStartY = 0;
        private int scrollStartOffset = 0;

        // Remove search bar related fields
        // private const int SearchBarHeight = 28;
        // private bool searchBarFocused = false;
        // private string searchText = "";
        // private RectangleF searchBarRect;

        private const int SearchBarHeight = 0; // No search bar
        private bool searchBarFocused = false; // Not used
        private string searchText = ""; // Not used
        private RectangleF searchBarRect = RectangleF.Empty; // Not used

        private RectangleF listRect;
        private RectangleF scrollbarRect;

        public StateManagerAttributes(StateManagerComponent owner) : base(owner)
        {
            if (!mouseWheelSubscribed && Grasshopper.Instances.ActiveCanvas != null)
            {
                Grasshopper.Instances.ActiveCanvas.MouseWheel += Canvas_MouseWheel;
                mouseWheelSubscribed = true;
            }
            activeAttributes = this;
        }

        protected override void Layout()
        {
            base.Layout();
            float listHeight = VisibleRows * RowHeight;
            // The custom UI should start below the default component area
            float customUiTop = base.Bounds.Bottom + Padding;
            float customUiHeight = listHeight + Padding; // No search bar
            // Expand the component bounds to fit the custom UI
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, base.Bounds.Height + customUiHeight + Padding);
            // Position UI elements just below the main component area
            // searchBarRect = new RectangleF(Bounds.X + Padding, customUiTop, Bounds.Width - 2 * Padding, SearchBarHeight); // Not used
            listRect = new RectangleF(Bounds.X + Padding, customUiTop, Bounds.Width - 2 * Padding, listHeight);
            scrollbarRect = new RectangleF(listRect.Right - ScrollbarWidth, listRect.Top, ScrollbarWidth, listRect.Height);
        }

        private List<IGH_DocumentObject> GetFilteredList(StateManagerComponent comp)
        {
            // No search bar, so always return all components
            return comp.AllComponents;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;

            // Clip drawing to component bounds
            var oldClip = graphics.Clip;
            graphics.SetClip(Bounds);
            var comp = Owner as StateManagerComponent;
            var font = GH_FontServer.Standard;

            var filtered = GetFilteredList(comp);
            int totalCount = filtered.Count;
            int maxOffset = Math.Max(0, totalCount - VisibleRows);
            scrollOffset = Math.Max(0, Math.Min(scrollOffset, maxOffset));
            // Draw list background
            graphics.FillRectangle(Brushes.WhiteSmoke, listRect);
            graphics.DrawRectangle(Pens.Gray, listRect.X, listRect.Y, listRect.Width, listRect.Height);
            // Draw items
            for (int i = 0; i < VisibleRows; i++)
            {
                int idx = i + scrollOffset;
                if (idx >= totalCount) break;
                var obj = filtered[idx];
                var guid = obj.InstanceGuid;
                var name = (obj as GH_Component)?.Name ?? obj.NickName;
                float y = listRect.Top + i * RowHeight;
                var itemRect = new RectangleF(listRect.X, y, listRect.Width, RowHeight);
                // Ensure itemRect is within Bounds
                if (itemRect.Bottom > Bounds.Bottom) break;
                // Highlight if hovered or selected
                if (comp.SelectedComponentGuids.Contains(guid))
                    graphics.FillRectangle(Brushes.LightBlue, itemRect);
                // Draw checkbox
                var checkboxRect = new RectangleF(itemRect.X + 4, itemRect.Y + (RowHeight - CheckboxSize) / 2, CheckboxSize, CheckboxSize);
                graphics.FillRectangle(Brushes.White, checkboxRect);
                graphics.DrawRectangle(Pens.Black, checkboxRect.X, checkboxRect.Y, checkboxRect.Width, checkboxRect.Height);
                if (comp.SelectedComponentGuids.Contains(guid))
                {
                    graphics.DrawLine(Pens.Black, checkboxRect.Left + 3, checkboxRect.Top + CheckboxSize / 2, checkboxRect.Right - 3, checkboxRect.Top + CheckboxSize / 2);
                    graphics.DrawLine(Pens.Black, checkboxRect.Left + CheckboxSize / 2, checkboxRect.Top + 3, checkboxRect.Left + CheckboxSize / 2, checkboxRect.Bottom - 3);
                }
                // Draw name
                graphics.DrawString(name, font, Brushes.Black, checkboxRect.Right + 8, itemRect.Top + (RowHeight - font.Height) / 2);
            }
            // Draw scrollbar if needed
            if (totalCount > VisibleRows)
            {
                float scrollbarHeight = listRect.Height;
                float thumbHeight = Math.Max(scrollbarHeight * VisibleRows / totalCount, 24);
                float thumbY = listRect.Top + (scrollbarHeight - thumbHeight) * scrollOffset / maxOffset;
                // Ensure scrollbar is within Bounds
                if (scrollbarRect.Bottom > Bounds.Bottom)
                    scrollbarRect.Height = Bounds.Bottom - scrollbarRect.Top;
                graphics.FillRectangle(Brushes.LightGray, scrollbarRect);
                graphics.FillRectangle(Brushes.Gray, scrollbarRect.X, thumbY, ScrollbarWidth, thumbHeight);
                graphics.DrawRectangle(Pens.Black, scrollbarRect.X, scrollbarRect.Y, scrollbarRect.Width, scrollbarRect.Height);
            }
            // Draw item count
            string countText = $"{filtered.Count} items";
            var countSize = graphics.MeasureString(countText, font);
            float countY = Math.Min(listRect.Bottom + 2, Bounds.Bottom - countSize.Height - 2);
            graphics.DrawString(countText, font, Brushes.Gray, listRect.Right - countSize.Width - 4, countY);
            // Restore previous clip
            graphics.SetClip(oldClip, System.Drawing.Drawing2D.CombineMode.Replace);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var comp = Owner as StateManagerComponent;
            var filtered = GetFilteredList(comp);
            int totalCount = filtered.Count;
            int maxOffset = Math.Max(0, totalCount - VisibleRows);
            // Search bar click
            if (searchBarRect.Contains(e.CanvasLocation))
            {
                searchBarFocused = true;
                Instances.RedrawCanvas();
                return GH_ObjectResponse.Handled;
            }
            searchBarFocused = false;
            // Scrollbar click
            if (totalCount > VisibleRows && scrollbarRect.Contains(e.CanvasLocation))
            {
                isScrolling = true;
                scrollStartY = (int)e.CanvasLocation.Y;
                scrollStartOffset = scrollOffset;
                return GH_ObjectResponse.Capture;
            }
            // List item/checkbox click
            if (listRect.Contains(e.CanvasLocation))
            {
                int idx = (int)((e.CanvasLocation.Y - listRect.Top) / RowHeight) + scrollOffset;
                if (idx >= 0 && idx < filtered.Count)
                {
                    var obj = filtered[idx];
                    var guid = obj.InstanceGuid;
                    // Only toggle if click is in checkbox area
                    float y = listRect.Top + (idx - scrollOffset) * RowHeight;
                    var checkboxRect = new RectangleF(listRect.X + 4, y + (RowHeight - CheckboxSize) / 2, CheckboxSize, CheckboxSize);
                    if (checkboxRect.Contains(e.CanvasLocation))
                    {
                        comp.ToggleComponentSelection(guid);
                        Instances.RedrawCanvas();
                        return GH_ObjectResponse.Handled;
                    }
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (isScrolling)
            {
                var comp = Owner as StateManagerComponent;
                var filtered = GetFilteredList(comp);
                int totalCount = filtered.Count;
                int maxOffset = Math.Max(0, totalCount - VisibleRows);
                float deltaY = e.CanvasLocation.Y - scrollStartY;
                // Increase scroll speed by a factor (e.g., 2.5)
                float scrollSpeed = 2.5f;
                int newOffset = scrollStartOffset + (int)(deltaY / RowHeight * scrollSpeed);
                scrollOffset = Math.Max(0, Math.Min(maxOffset, newOffset));
                Instances.RedrawCanvas();
                return GH_ObjectResponse.Capture;
            }
            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (isScrolling)
            {
                isScrolling = false;
                return GH_ObjectResponse.Release;
            }
            return base.RespondToMouseUp(sender, e);
        }

        public override GH_ObjectResponse RespondToKeyDown(GH_Canvas sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (searchBarFocused)
            {
                if (e.KeyCode == System.Windows.Forms.Keys.Back && searchText.Length > 0)
                {
                    searchText = searchText.Substring(0, searchText.Length - 1);
                    Instances.RedrawCanvas();
                    return GH_ObjectResponse.Handled;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Escape)
                {
                    searchBarFocused = false;
                    Instances.RedrawCanvas();
                    return GH_ObjectResponse.Handled;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Enter)
                {
                    searchBarFocused = false;
                    Instances.RedrawCanvas();
                    return GH_ObjectResponse.Handled;
                }
                else if (e.KeyCode >= System.Windows.Forms.Keys.A && e.KeyCode <= System.Windows.Forms.Keys.Z ||
                         e.KeyCode >= System.Windows.Forms.Keys.D0 && e.KeyCode <= System.Windows.Forms.Keys.D9 ||
                         e.KeyCode == System.Windows.Forms.Keys.Space)
                {
                    string key = e.Shift ? e.KeyCode.ToString() : e.KeyCode.ToString().ToLower();
                    if (key.Length == 1)
                        searchText += key;
                    else if (e.KeyCode == System.Windows.Forms.Keys.Space)
                        searchText += " ";
                    Instances.RedrawCanvas();
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToKeyDown(sender, e);
        }

        // Static event handler for mouse wheel
        private static bool mouseWheelSubscribed = false;
        private static StateManagerAttributes activeAttributes = null;

        public override void ExpireLayout()
        {
            base.ExpireLayout();
            activeAttributes = this;
        }

        private static void Canvas_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (activeAttributes == null) return;
            var comp = activeAttributes.Owner as StateManagerComponent;
            var filtered = activeAttributes.GetFilteredList(comp);
            int totalCount = filtered.Count;
            int maxOffset = Math.Max(0, totalCount - VisibleRows);
            // Convert mouse position to canvas coordinates
            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas == null) return;
            var mousePt = canvas.Viewport.ProjectPoint(e.Location);
            // Only scroll if mouse is over the list area
            if (activeAttributes.listRect.Contains(mousePt) && totalCount > VisibleRows)
            {
                int delta = e.Delta < 0 ? 1 : -1; // Down = positive, Up = negative
                activeAttributes.scrollOffset = Math.Max(0, Math.Min(maxOffset, activeAttributes.scrollOffset + delta));
                Grasshopper.Instances.RedrawCanvas();
            }
        }

        public void Dispose()
        {
            if (mouseWheelSubscribed && Grasshopper.Instances.ActiveCanvas != null)
            {
                Grasshopper.Instances.ActiveCanvas.MouseWheel -= Canvas_MouseWheel;
                mouseWheelSubscribed = false;
            }
        }
    }
}
