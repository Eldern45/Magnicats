using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MagnetVFX : MonoBehaviour
{
    [Header("VFX Settings")]
    [Tooltip("Multiplier for the influence range (length)")]
    public float radiusMultiplier = 1.0f;
    [Tooltip("Multiplier for the beam width (only for FixedDirectional)")]
    public float widthMultiplier = 1.1f; // Default 1.1 for a slight overlap
    public float speed = 2.0f;
    [Range(0f, 1f)]
    public float opacity = 0.5f;

    private SpriteRenderer _sr;
    private Material _matInstance;
    private MagnetPolarity _myPolarity;
    private PlayerMagnetController _player;

    public void Initialize(Bounds magnetBounds, float influenceRange, MagnetPolarity polarity, MagnetFieldMode mode, Vector2 direction)
    {
        _myPolarity = polarity;
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();

        // Ensure sprite exists
        if (_sr.sprite == null) _sr.sprite = CreateWhiteSprite();
        
        // Sorting Order
        if (transform.parent != null)
        {
            var parentRenderer = transform.parent.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                _sr.sortingLayerID = parentRenderer.sortingLayerID;
                _sr.sortingOrder = parentRenderer.sortingOrder - 1; 
            }
        }

        if (mode == MagnetFieldMode.Radial)
        {
            SetupRadial(magnetBounds, influenceRange, polarity);
        }
        else // FixedDirectional
        {
            SetupDirectional(magnetBounds, influenceRange, polarity, direction);
        }
    }

    private void SetupRadial(Bounds magnetBounds, float influenceRange, MagnetPolarity polarity)
    {
        float visualRange = influenceRange * radiusMultiplier;
        transform.position = magnetBounds.center;
        transform.rotation = Quaternion.identity;

        Vector3 magSize = magnetBounds.size;
        Vector3 totalSize = magSize + new Vector3(visualRange * 2, visualRange * 2, 0f);
        
        transform.localScale = new Vector3(totalSize.x, totalSize.y, 1f);

        // Ratio
        float ratioX = magSize.x / totalSize.x;
        float ratioY = magSize.y / totalSize.y;

        Material mat = GetMaterial("Custom/MagnetField");
        if (mat)
        {
            ConfigureCommonMaterial(mat, polarity);
            mat.SetFloat("_Density", 5.0f * (totalSize.x / 5f)); 
            mat.SetVector("_CoreSize", new Vector4(ratioX, ratioY, 0, 0));
            _matInstance = mat;
            _sr.material = mat;
        }
    }

    private void SetupDirectional(Bounds magnetBounds, float influenceRange, MagnetPolarity polarity, Vector2 dir)
    {
        float beamLength = influenceRange * radiusMultiplier;
        
        // Determine rotation
        // Default sprite is Up (Y+). We rotate it to match 'dir'.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Determine Width based on magnet size perpendicular to direction
        // Simple approx: use max dimension of bounds, or project?
        // Let's take the diagonal for safety or just average size.
        // Better: Project bounds onto perpendicular vector.
        Vector2 perp = new Vector2(-dir.y, dir.x);
        Vector3 extents = magnetBounds.extents;
        // Project corners? Or just take max size to be safe.
        // Let's assume magnets are mostly axis-aligned boxes.
        // If dir is (1,0) [Right], width is bounds.size.y.
        // If dir is (0,1) [Up], width is bounds.size.x.
        float width = Mathf.Abs(dir.x) * magnetBounds.size.y + Mathf.Abs(dir.y) * magnetBounds.size.x;
        // If diagonal, this simple logic mixes them.
        if (width < 0.1f) width = Mathf.Max(magnetBounds.size.x, magnetBounds.size.y);

        // Apply width multiplier
        width *= widthMultiplier;

        // Position: Start at center, shift forward by half length so the "base" of the sprite is at center
        // Sprite pivot is Center. So pos = Center + Dir * (Length/2).
        // BUT we want it to start at the EDGE of the magnet.
        // Distance to edge along dir?
        // Simple approx: Center + Dir * (Length/2)
        transform.position = magnetBounds.center + (Vector3)dir.normalized * (beamLength * 0.5f);
        
        transform.localScale = new Vector3(width, beamLength, 1f);

        Material mat = GetMaterial("Custom/MagnetFieldDirectional");
        if (mat)
        {
            ConfigureCommonMaterial(mat, polarity);
            mat.SetFloat("_Density", 5.0f * (beamLength / 5f));
            _matInstance = mat;
            _sr.material = mat;
        }
    }

    private Material GetMaterial(string shaderName)
    {
        Shader s = Shader.Find(shaderName);
        if (s == null) return null;
        return new Material(s);
    }

    private void ConfigureCommonMaterial(Material mat, MagnetPolarity polarity)
    {
        Color color = (polarity == MagnetPolarity.Red) ? Color.red : Color.blue;
        color.a = opacity; 
        mat.SetColor("_MainColor", color);
        mat.SetFloat("_Alpha", opacity);
    }

    // --- Update Logic remains similar, works for both shaders if property names match ---


    private void Update()
    {
        if (_matInstance == null) return;
        
        // Dynamic properties
        _matInstance.SetFloat("_Alpha", opacity);
        _matInstance.SetFloat("_Speed", speed);

        // Try to find player if we haven't yet
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMagnetController>();
        }

        if (_player != null)
        {
            // Use VisualPolarity to see what the polarity WOULD be if enabled
            int playerPol = _player.VisualPolarity; // Returns +1 or -1 directly

            int myPol = (_myPolarity == MagnetPolarity.Red) ? 1 : -1;

            // Same polarity = Repel (Outwards = 1)
            // Different polarity = Attract (Inwards = -1)
            float flowDir = (playerPol == myPol) ? 1.0f : -1.0f;
            
            _matInstance.SetFloat("_FlowDirection", flowDir);
        }
    }

    private Sprite CreateWhiteSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size); // PPU = size => 1 unit world size
    }

    private void OnDestroy()
    {
        if (_matInstance != null) Destroy(_matInstance);
    }
}
