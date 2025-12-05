using UnityEngine;

namespace DigDigDiner
{
    public class PlayerRenderer : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color directionIndicatorColor = Color.white;

        private Player player;
        private DualGridSystem gridSystem;

        private GameObject bodyObject;
        private GameObject directionIndicatorObject;
        private GameObject shadowObject;
        private GameObject digPreviewObject;

        private SpriteRenderer bodyRenderer;
        private SpriteRenderer directionRenderer;
        private SpriteRenderer shadowRenderer;
        private SpriteRenderer digPreviewRenderer;

        private float bobTimer;

        public void Initialize(Player playerController)
        {
            player = playerController;
            gridSystem = player.GridSystem;
            CreateVisualElements();
        }

        private void CreateVisualElements()
        {
            // Body
            bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform);
            bodyObject.transform.localPosition = Vector3.zero;
            bodyRenderer = bodyObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = CreateCircleSprite(SharedConstants.PLAYER_BODY_RADIUS);
            bodyRenderer.color = playerColor;
            bodyRenderer.sortingOrder = 10; 
            bodyRenderer.sortingLayerName = "MG";

            // Shadow
            shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(transform);
            shadowObject.transform.localPosition = new Vector3(0, SharedConstants.PLAYER_SHADOW_OFFSET_Y, 0);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sortingLayerName = "MG";
            shadowRenderer.sprite = CreateCircleSprite(SharedConstants.PLAYER_BODY_RADIUS * 0.8f);
            shadowRenderer.color = new Color(0, 0, 0, SharedConstants.PLAYER_SHADOW_ALPHA);
            shadowRenderer.sortingOrder = 8; 

            // Direction Indicator
            directionIndicatorObject = new GameObject("DirectionIndicator");
            directionIndicatorObject.transform.SetParent(bodyObject.transform);
            directionIndicatorObject.transform.localPosition = Vector3.zero;
            directionRenderer = directionIndicatorObject.AddComponent<SpriteRenderer>();
            directionRenderer.sprite = CreateTriangleSprite();
            directionRenderer.color = directionIndicatorColor;
            bodyRenderer.sortingLayerName = "MG";
            directionRenderer.sortingOrder = 11; 

            // Dig Preview
            digPreviewObject = new GameObject("DigPreview");
            digPreviewObject.transform.SetParent(transform);
            digPreviewObject.transform.localPosition = Vector3.zero;
            digPreviewRenderer = digPreviewObject.AddComponent<SpriteRenderer>();
            digPreviewRenderer.sprite = CreateSquareSprite();
            digPreviewRenderer.color = SharedConstants.DIG_PREVIEW_COLOR;
            digPreviewRenderer.sortingOrder = 7; 
            digPreviewObject.SetActive(false);
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
            if (bodyObject != null)
            {
                bodyObject.transform.localPosition = new Vector3(0, bobOffset, 0);
            }
        }

        private void UpdateDirectionIndicator()
        {
            if (directionIndicatorObject == null) return;
            Vector2Int facing = player.FacingDirection;
            
            Vector3 indicatorOffset = new Vector3(
                facing.x * SharedConstants.PLAYER_DIRECTION_INDICATOR_OFFSET,
                facing.y * SharedConstants.PLAYER_DIRECTION_INDICATOR_OFFSET,
                0
            );
            directionIndicatorObject.transform.localPosition = indicatorOffset;
            
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            directionIndicatorObject.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }

        private void UpdateDigPreview()
        {
            if (digPreviewObject == null) return;
            Vector2Int digTarget = player.GridPosition + player.FacingDirection;
            var digging = player.GetComponent<PlayerDigging>();
            if (digging != null && digging.CanDigAt(digTarget))
            {
                digPreviewObject.SetActive(true);
                // Preview should be relative to player if parented, but here we want grid snap.
                // Since digPreviewObject is child of Player (which moves smoothly), 
                // we must offset it counter to the player's sub-tile movement if we wanted it locked to grid.
                // However, simpler is to just unparent it or handle it differently.
                // For now, let's keep it simple: It highlights the RELATIVE target.
                // Local position 1 unit away in facing direction.
                digPreviewObject.transform.localPosition = new Vector3(player.FacingDirection.x, player.FacingDirection.y, 0);
            }
            else
            {
                digPreviewObject.SetActive(false);
            }
        }

        /// <summary>
        /// Creates a circle sprite scaled exactly to the requested world radius.
        /// </summary>
        private Sprite CreateCircleSprite(float worldRadius)
        {
            int resolution = 64; // High res texture
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];
            
            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float pixelRadius = (resolution / 2f) - 1; // Fill texture with 1px padding

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    // Simple anti-aliasing edge
                    float alpha = 1f - Mathf.Clamp01(dist - pixelRadius);
                    pixels[y * resolution + x] = new Color(1, 1, 1, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Calculate PPU so that the Sprite (resolution pixels wide) equals (worldRadius * 2) units
            float targetDiameter = worldRadius * 2f;
            float ppu = resolution / targetDiameter;

            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                ppu
            );
        }

        private Sprite CreateTriangleSprite()
        {
            int resolution = 32;
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];
            Vector2 p1 = new Vector2(resolution / 2f, resolution * 0.75f);
            Vector2 p2 = new Vector2(resolution * 0.3f, resolution * 0.25f);
            Vector2 p3 = new Vector2(resolution * 0.7f, resolution * 0.25f);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    if (IsPointInTriangle(new Vector2(x, y), p1, p2, p3))
                        pixels[y * resolution + x] = Color.white;
                    else
                        pixels[y * resolution + x] = Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution * 2f);
        }

        private Sprite CreateSquareSprite()
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        private bool IsPointInTriangle(Vector2 p, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float area = 0.5f * (-p2.y * p3.x + p1.y * (-p2.x + p3.x) + p1.x * (p2.y - p3.y) + p2.x * p3.y);
            float s = 1 / (2 * area) * (p1.y * p3.x - p1.x * p3.y + (p3.y - p1.y) * p.x + (p1.x - p3.x) * p.y);
            float t = 1 / (2 * area) * (p1.x * p2.y - p1.y * p2.x + (p1.y - p2.y) * p.x + (p2.x - p1.x) * p.y);
            return s >= 0 && t >= 0 && (s + t) <= 1;
        }

        public void SetPlayerColor(Color color)
        {
            playerColor = color;
            if (bodyRenderer != null) bodyRenderer.color = color;
        }
    }
}