using UnityEngine;

namespace DigDigDiner
{
    public class WorldGenerationDirector : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private int globalSeed = 12345;
        [SerializeField] private bool randomizeSeedOnStart = true;
        [SerializeField] private bool generateOnStart = true;

        [Header("Layer Generators (Order of Execution)")]
        [Tooltip("Phase 1: The furthest background layer (Parallax)")]
        [SerializeField] private BackgroundMapGenerator distantBgGenerator;

        [Tooltip("Phase 2: The middle background layer (Decoration)")]
        [SerializeField] private BackgroundMapGenerator midBgGenerator;

        [Tooltip("Phase 3: The main gameplay layout")]
        [SerializeField] private MapGenerator miningGenerator;

        [Tooltip("Phase 4: The foreground occlusion based on Mining layer")]
        [SerializeField] private ForegroundMapGenerator foregroundGenerator;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateWorld();
            }
        }

        [ContextMenu("Generate World")]
        public void GenerateWorld()
        {
            if (randomizeSeedOnStart)
            {
                globalSeed = Random.Range(0, 100000);
            }

            Debug.Log($"--- World Generation Director: Starting (Seed: {globalSeed}) ---");

            if (distantBgGenerator != null)
            {
                distantBgGenerator.Generate(globalSeed);
            }

            if (midBgGenerator != null)
            {
                midBgGenerator.Generate(globalSeed);
            }

            if (miningGenerator != null)
            {
                miningGenerator.GenerateMap(globalSeed);
            }
            else
            {
                Debug.LogError("Director: Critical Error - Mining Generator is missing!");
            }

            if (foregroundGenerator != null)
            {
                foregroundGenerator.Generate(globalSeed);
            }
            
            Debug.Log("--- World Generation Director: Complete ---");
        }
    }
}