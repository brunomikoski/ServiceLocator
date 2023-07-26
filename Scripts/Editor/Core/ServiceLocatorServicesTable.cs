using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class AvailableServiceViewItem : TreeViewItem
    {
        public ServiceImplementationAttribute ServiceAttribute { get; set; }
        public string GroupName = "";
        private string cachedDisplayDependencies;

        public bool IsEnabled
        {
            get => ServiceLocatorSettings.Instance.IsServiceEnabled(ServiceAttribute);
            set => ServiceLocatorSettings.Instance.SetServiceEnabled(ServiceAttribute, value);
        } 
        
        public AvailableServiceViewItem(int id) : base(id)
        {

        }

        public string GetDependencies()
        {
            if (string.IsNullOrEmpty(cachedDisplayDependencies))
            {
                if (ServiceAttribute.DependsOn == null || ServiceAttribute.DependsOn.Length == 0)
                {
                    cachedDisplayDependencies = "None";
                }
                else
                {
                    cachedDisplayDependencies =
                        string.Join(",", ServiceAttribute.DependsOn.Select(type => type.Name).ToArray());
                }
            }
            return cachedDisplayDependencies;
        }
    }
    
    public class AvailableServiceTreeView : TreeView
    {
        private const string SORTED_COLUMN_INDEX_STATE_KEY = "AvailableServiceTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItem> CurrentBindingItems;

        public AvailableServiceTreeView()
            : this(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Enabled"), width = 3},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Group Name"), width = 5},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Name"), width = 10},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Implementation Type"), width = 10},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Dependencies"), width = 20},
            })))
        {
        }

        AvailableServiceTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(SORTED_COLUMN_INDEX_STATE_KEY, 1);
        }

        public void ReloadAndSort()
        {
            var currentSelected = this.state.selectedIDs;
            Reload();
            Header_sortingChanged(this.multiColumnHeader);
            this.state.selectedIDs = currentSelected;
        }

        private void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(SORTED_COLUMN_INDEX_STATE_KEY, multiColumnHeader.sortedColumnIndex);
            var index = multiColumnHeader.sortedColumnIndex;
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var items = rootItem.children.Cast<AvailableServiceViewItem>();

            IOrderedEnumerable<AvailableServiceViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                {
                    orderedEnumerable = ascending ? items.OrderBy(item => item.IsEnabled) : items.OrderByDescending(item => item.IsEnabled);
                    break;
                }
                case 1:
                {
                    orderedEnumerable = ascending ? items.OrderBy(item => item.GroupName) : items.OrderByDescending(item => item.GroupName);
                    break;
                }
                case 2:
                {
                    orderedEnumerable = ascending ? items.OrderBy(item => item.ServiceAttribute.Name) : items.OrderByDescending(item => item.ServiceAttribute.Name);
                    break;
                }
                case 3:
                {
                    orderedEnumerable = ascending ? items.OrderBy(item => item.ServiceAttribute.Type.Name) : items.OrderByDescending(item => item.ServiceAttribute.Type.Name);
                    break;
                }
                case 4:
                {
                    orderedEnumerable = ascending ? items.OrderBy(item => item.GetDependencies().Length) : items.OrderByDescending(item => item.GetDependencies().Length);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem { depth = -1 };
            List<TreeViewItem> children = new List<TreeViewItem>();



            Dictionary<string, List<ServiceImplementationAttribute>> items = ServiceLocatorCodeGenerator.GetAvailableServices(false);

            foreach (var groupToServiceList in items)
            {
                for (int i = 0; i < groupToServiceList.Value.Count; i++)
                {
                    ServiceImplementationAttribute attribute = groupToServiceList.Value[i];
                    children.Add(new AvailableServiceViewItem(children.Count)
                    {
                        GroupName = groupToServiceList.Key,
                        ServiceAttribute = attribute
                    });
                }
            }

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItem>;
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as AvailableServiceViewItem;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect rect = args.GetCellRect(visibleColumnIndex);
                int columnIndex = args.GetColumn(visibleColumnIndex);

                GUIStyle labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;

                GUIStyle toggleStyle = EditorStyles.toggle;
                toggleStyle.alignment = TextAnchor.MiddleCenter;
                switch (columnIndex)
                {
                    case 0:
                    {
                        item.IsEnabled = EditorGUI.Toggle(rect, item.IsEnabled);
                        break;
                    }
                    case 1:
                    {
                        EditorGUI.LabelField(rect, item.GroupName, labelStyle);
                        break;
                    }
                    case 2:
                    {
                        EditorGUI.LabelField(rect, item.ServiceAttribute.Name, labelStyle);
                        break;
                    }
                    case 3:
                    {
                        EditorGUI.LabelField(rect, item.ServiceAttribute.Type.Name, labelStyle);
                        break;
                    }
                    case 4:
                    {
                        EditorGUI.LabelField(rect, item.GetDependencies(), labelStyle);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
    }
    
    public class ServiceLocatorServicesTable 
    {
        private static readonly GUILayoutOption[] EMPTY_LAYOUT_OPTION = Array.Empty<GUILayoutOption>();

        private static AvailableServiceTreeView TREE_VIEW;
        private static GUIStyle TABLE_LIST_STYLE;
        private static Vector2 TABLE_SCROLL;
        private static readonly GUIContent GenerateContent = EditorGUIUtility.TrTextContent("Generate", "Generate Services File");

        public static void DrawGeneratorWindow()
        {
            // Head
            RenderHeadPanel();

            // Column Tabble
            RenderTable();
            
            RenderBottomPanel();

        }

        private static void RenderBottomPanel()
        {
            EditorGUILayout.BeginVertical(EMPTY_LAYOUT_OPTION);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EMPTY_LAYOUT_OPTION);

            if (GUILayout.Button(GenerateContent))
            {
                ServiceLocatorCodeGenerator.GenerateServicesClass();
            }
            

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        static void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(EMPTY_LAYOUT_OPTION);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EMPTY_LAYOUT_OPTION);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private static void RenderTable()
        {
            if (TREE_VIEW == null)
                TREE_VIEW = new AvailableServiceTreeView();
            
            if (TABLE_LIST_STYLE == null)
            {
                TABLE_LIST_STYLE = new GUIStyle("CN Box");
                TABLE_LIST_STYLE.margin.top = 0;
                TABLE_LIST_STYLE.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(TABLE_LIST_STYLE, EMPTY_LAYOUT_OPTION);

            TABLE_SCROLL = EditorGUILayout.BeginScrollView(TABLE_SCROLL, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(2000f));
            Rect controlRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));


            TREE_VIEW?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public static void Reload()
        {
            TREE_VIEW.ReloadAndSort();
        }
    }
}