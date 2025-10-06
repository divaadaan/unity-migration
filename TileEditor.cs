using UnityEngine;
using UnityEditor;

namespace MiningGame
{
    [CustomEditor(typeof(DualGridSystem))]
    public class DualGridSystemEditor : Editor
    {
        private bool isEditing = false;
        private TerrainType paintType = TerrainType.Diggable;
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var system = (DualGridSystem)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tile Editing", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            isEditing = EditorGUILayout.Toggle("Enable Tile Editing (Alt+Click)", isEditing);
            
            if (isEditing)
            {
                paintType = (TerrainType)EditorGUILayout.EnumPopup("Paint Type", paintType);
                EditorGUILayout.HelpBox("Alt+Click: Cycle tile type\nAlt+Shift+Click: Paint selected type", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh All Visual Tiles"))
            {
                system.RefreshAllVisualTiles();
            }
            
            if (GUILayout.Button("Clear Grid"))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Reset all tiles to empty?", "Clear", "Cancel"))
                {
                    ClearGrid(system);
                }
            }
        }
        
        private void OnSceneGUI()
        {
            if (!isEditing) return;
            
            var system = (DualGridSystem)target;
            HandleTileEditing(system);
        }
        
        private void HandleTileEditing(DualGridSystem system)
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.alt && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 worldPoint = ray.origin - ray.direction * (ray.origin.z / ray.direction.z);
                
                Vector2Int gridPos = system.WorldToBaseGrid(worldPoint);
                
                if (gridPos.x >= 0 && gridPos.x < system.Width && 
                    gridPos.y >= 0 && gridPos.y < system.Height)
                {
                    if (e.shift)
                    {
                        // Paint mode
                        system.SetTileAt(gridPos.x, gridPos.y, new Tile(paintType));
                        Debug.Log($"Painted tile ({gridPos.x},{gridPos.y}) as {paintType}");
                    }
                    else
                    {
                        // Cycle mode
                        system.CycleTileAt(gridPos.x, gridPos.y);
                    }
                    
                    e.Use();
                }
            }
        }
        
        private void ClearGrid(DualGridSystem system)
        {
            for (int y = 0; y < system.Height; y++)
            {
                for (int x = 0; x < system.Width; x++)
                {
                    system.SetTileAt(x, y, new Tile(TerrainType.Empty));
                }
            }
            system.RefreshAllVisualTiles();
        }
    }
}