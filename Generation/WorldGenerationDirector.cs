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
        
        [Tooltip("Phase 1b: Distant Decorations")]
        [SerializeField] private DistantDecorationMapGenerator distantDecorationGenerator;

        [Tooltip("BG")]
        [SerializeField] private BackgroundMapGenerator BGGenerator;

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
            Debug.Log($"--- World Generation Director: Starting DistantBG");

            if (distantBgGenerator != null)
            {
                distantBgGenerator.Generate(globalSeed);
            }

            Debug.Log($"--- World Generation Director: Starting DistantBG Decorations");

            if (distantDecorationGenerator != null)
            {
                // We don't really need a seed since it's a clone, but we pass it for consistency
                distantDecorationGenerator.Generate(globalSeed);
            }

            Debug.Log($"--- World Generation Director: Starting BG");
            if (BGGenerator != null)
            {
                BGGenerator.Generate(globalSeed);
            }
            
            Debug.Log($"--- World Generation Director: Starting MG");
            if (miningGenerator != null)
            {
                miningGenerator.GenerateMap(globalSeed);
            }
            else
            {
                Debug.LogError("Director: Critical Error - Mining Generator is missing!");
            }

            Debug.Log($"--- World Generation Director: Starting MG Decoration");
            if (decorationGenerator != null)
            {
                decorationGenerator.Generate(globalSeed);
            }
            
            Debug.Log($"--- World Generation Director: Starting FG");
            if (foregroundGenerator != null)
            {
                foregroundGenerator.Generate(globalSeed);
            }
            
            Debug.Log("--- World Generation Director: Complete ---");
        }
    }
}