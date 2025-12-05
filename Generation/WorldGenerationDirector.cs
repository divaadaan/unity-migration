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
        [Tooltip("DistantBG")]
        [SerializeField] private BackgroundMapGenerator distantBgGenerator;

        [Tooltip("BG")]
        [SerializeField] private BackgroundMapGenerator midBgGenerator;

        [Tooltip("MG")]
        [SerializeField] private MapGenerator miningGenerator;

        [Tooltip("MGDecoration")]
        [SerializeField] private DecorationMapGenerator decorationGenerator;

        [Tooltip("FG")]
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
            
            if (decorationGenerator != null)
            {
                decorationGenerator.Generate(globalSeed);
            }
            
            if (foregroundGenerator != null)
            {
                foregroundGenerator.Generate(globalSeed);
            }
            
            Debug.Log("--- World Generation Director: Complete ---");
        }
    }
}