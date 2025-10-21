using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Handles all player visual rendering including body, direction indicator, shadow,
    /// bobbing animation, and dig preview highlight.
    /// </summary>
    public class PlayerRenderer : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.3f); // Default green
        [SerializeField] private Color directionIndicatorColor = Color.white;

        private Player player;
        private DualGridSystem gridSystem;

        // Child GameObjects for visual elements
        private GameObject bodyObject;
        private GameObject directionIndicatorObject;
        private GameObject shadowObject;
        private GameObject digPreviewObject;

        // Sprite Renderers
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer directionRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer digPreviewRenderer;

        // Animation state
        private float bobTimer;

        public void Initialize(Player playerController)
        {
            player = playerController;
            gridSystem = player.GridSystem;

            CreateVisualElements();
        }

        private void CreateVisualElements()
        {
            // Create body (colored circle)
            bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform);
            bodyObject.transform.localPosition = Vector3.zero;
            bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = CreateCircleSprite(SharedConstants.PLAYER_BODY_RADIUS);
            bodyRenderer.color = playerColor;
            bodyRenderer.sortingOrder = 10; // Above tiles

            // Create shadow (below player)
            shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(transform);
            shadowObject.transform.localPosition = new Vector3(0, SharedConstants.PLAYER_SHADOW_OFFSET_Y, 0);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = CreateCircleSprite(SharedConstants.PLAYER_BODY_RADIUS * 0.8f);
            shadowRenderer.color = new Color(0, 0, 0, SharedConstants.PLAYER_SHADOW_ALPHA);
            shadowRenderer.sortingOrder = 8; // Below player, above tiles

            // Create direction indicator (arrow/triangle)
            directionIndicatorObject = new GameObject("DirectionIndicator");
            directionIndicatorObject.transform.SetParent(bodyObject.transform);
            directionIndicatorObject.transform.localPosition = Vector3.zero;
            directionRenderer = directionIndicatorObject.AddComponent<SpriteRenderer>();
            directionRenderer.sprite = CreateTriangleSprite();
            directionRenderer.color = directionIndicatorColor;
            directionRenderer.sortingOrder = 11; // Above body

            // Create dig preview highlight
            digPreviewObject = new GameObject("DigPreview");
            digPreviewObject.transform.SetParent(transform);
            digPreviewObject.transform.localPosition = Vector3.zero;
            digPreviewRenderer = digPreviewObject.AddComponent<SpriteRenderer>();
            digPreviewRenderer.sprite = CreateSquareSprite();
            digPreviewRenderer.color = SharedConstants.DIG_PREVIEW_COLOR;
            digPreviewRenderer.sortingOrder = 7; // Below player, above tiles
            digPreviewObject.SetActive(false); // Hidden by default
        }

        private void Update()
        {
            if (player == null) return;

            UpdateBobAnimation();
            UpdateDirectionIndicator();
            UpdateDigPreview();
        }

        private void UpdateBobAnimation()
        {
            bobTimer += Time.deltaTime * SharedConstants.PLAYER_BOB_SPEED;
            float bobOffset = Mathf.Sin(bobTimer) * SharedConstants.PLAYER_BOB_AMOUNT;

            // Apply bobbing to body (shadow stays in place)
            if (bodyObject != null)
            {
                bodyObject.transform.localPosition = new Vector3(0, bobOffset, 0);
            }
        }

        private void UpdateDirectionIndicator()
        {
            if (directionIndicatorObject == null) return;

            Vector2Int facing = player.FacingDirection;

            // Position the indicator in the facing direction
            Vector3 indicatorOffset = new Vector3(
                facing.x * SharedConstants.PLAYER_DIRECTION_INDICATOR_OFFSET,
                facing.y * SharedConstants.PLAYER_DIRECTION_INDICATOR_OFFSET,
                0
            );
            directionIndicatorObject.transform.localPosition = indicatorOffset;

            // Rotate the indicator to point in the facing direction
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            directionIndicatorObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90); // -90 because triangle points up by default
        }

        private void UpdateDigPreview()
        {
            if (digPreviewObject == null) return;

            Vector2Int digTarget = player.GridPosition + player.FacingDirection;

            // Check if we can dig at the target position
            var digging = player.GetComponent<PlayerDigging>();
            if (digging != null && digging.CanDigAt(digTarget))
            {
                // Show preview at dig target
                digPreviewObject.SetActive(true);
                digPreviewObject.transform.position = new Vector3(digTarget.x, digTarget.y, 0);
            }
            else
            {
                // Hide preview
                digPreviewObject.SetActive(false);
            }
        }

        /// <summary>
        /// Creates a circular sprite for the player body or shadow.
        /// </summary>
        private Sprite CreateCircleSprite(float radius)
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];

            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float radiusPixels = (resolution / 2f) * (radius / SharedConstants.PLAYER_BODY_RADIUS);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    if (distance <= radiusPixels)
                    {
                        pixels[y * resolution + x] = Color.white; // Will be tinted by SpriteRenderer
                    }
                    else
                    {
                        pixels[y * resolution + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution / 2f // Pixels per unit
            );
        }

        /// <summary>
        /// Creates a triangle sprite for the direction indicator.
        /// </summary>
        private Sprite CreateTriangleSprite()
        {
            int resolution = 32;
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];

            // Triangle points up: (center-top), (left-bottom), (right-bottom)
            Vector2 p1 = new Vector2(resolution / 2f, resolution * 0.75f); // Top
            Vector2 p2 = new Vector2(resolution * 0.3f, resolution * 0.25f); // Bottom-left
            Vector2 p3 = new Vector2(resolution * 0.7f, resolution * 0.25f); // Bottom-right

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 point = new Vector2(x, y);

                    if (IsPointInTriangle(point, p1, p2, p3))
                    {
                        pixels[y * resolution + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * resolution + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution * 2f // Smaller sprite
            );
        }

        /// <summary>
        /// Creates a square sprite for the dig preview highlight.
        /// </summary>
        private Sprite CreateSquareSprite()
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white; // Will be tinted by SpriteRenderer
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution // 1 unit = 1 tile
            );
        }

        /// <summary>
        /// Helper function to check if a point is inside a triangle.
        /// Uses barycentric coordinates.
        /// </summary>
        private bool IsPointInTriangle(Vector2 p, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float area = 0.5f * (-p2.y * p3.x + p1.y * (-p2.x + p3.x) + p1.x * (p2.y - p3.y) + p2.x * p3.y);
            float s = 1 / (2 * area) * (p1.y * p3.x - p1.x * p3.y + (p3.y - p1.y) * p.x + (p1.x - p3.x) * p.y);
            float t = 1 / (2 * area) * (p1.x * p2.y - p1.y * p2.x + (p1.y - p2.y) * p.x + (p2.x - p1.x) * p.y);

            return s >= 0 && t >= 0 && (s + t) <= 1;
        }

        /// <summary>
        /// Public method to change player color (for future customization).
        /// </summary>
        public void SetPlayerColor(Color color)
        {
            playerColor = color;
            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }
        }
    }
}
