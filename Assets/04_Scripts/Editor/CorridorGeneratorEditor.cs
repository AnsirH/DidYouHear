using UnityEngine;
using UnityEditor;
using DidYouHear.Corridor;

namespace DidYouHear.Editor
{
    /// <summary>
    /// CorridorGenerator 전용 커스텀 에디터
    /// </summary>
    [CustomEditor(typeof(CorridorGenerator))]
    public class CorridorGeneratorEditor : UnityEditor.Editor
    {
        private CorridorGenerator corridorGenerator;
        
        private void OnEnable()
        {
            corridorGenerator = (CorridorGenerator)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 그리기
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // 구분선
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // 에디터 전용 버튼들
            DrawEditorButtons();
            
            // 복도 분포 정보 표시
            DrawCorridorInfo();
        }
        
        /// <summary>
        /// 에디터 전용 버튼들 그리기
        /// </summary>
        private void DrawEditorButtons()
        {
            EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 맵 재생성 버튼
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🔄 Regenerate Map", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    corridorGenerator.RegenerateCorridors();
                    Debug.Log("Map regenerated!");
                }
                else
                {
                    Debug.LogWarning("Map regeneration is only available in Play Mode!");
                }
            }
            
            GUI.backgroundColor = Color.white;
            
            // 복도 분포 로그 버튼
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("📊 Log Distribution", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    corridorGenerator.LogCorridorDistribution();
                }
                else
                {
                    Debug.LogWarning("Distribution logging is only available in Play Mode!");
                }
            }
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // 시드 재설정 버튼
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("🎲 Randomize Seed", GUILayout.Height(25)))
            {
                int newSeed = Random.Range(1, 999999);
                corridorGenerator.SetSeed(newSeed);
                Debug.Log($"Seed changed to: {newSeed}");
            }
            
            GUI.backgroundColor = Color.white;
            
            // 시드 0으로 리셋
            if (GUILayout.Button("🔄 Reset Seed", GUILayout.Height(25)))
            {
                corridorGenerator.SetSeed(0);
                Debug.Log("Seed reset to 0 (random)");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 복도 정보 표시
        /// </summary>
        private void DrawCorridorInfo()
        {
            if (!Application.isPlaying) return;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            
            // 복도 개수 정보
            var eventEligibleCorridors = corridorGenerator.GetEventEligibleCorridors();
            int totalCorridors = eventEligibleCorridors?.Count ?? 0;
            
            EditorGUILayout.LabelField($"Total Corridors: {totalCorridors}");
            EditorGUILayout.LabelField($"Event-Eligible: {eventEligibleCorridors?.Count ?? 0}");
            
            // 현재 시드 정보
            SerializedProperty seedProperty = serializedObject.FindProperty("seed");
            if (seedProperty != null)
            {
                EditorGUILayout.LabelField($"Current Seed: {seedProperty.intValue}");
            }
        }
    }
}
