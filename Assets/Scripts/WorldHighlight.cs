using UnityEngine;

public class WorldHighlight : MonoBehaviour
{
    [Header("Colour")]
    [Tooltip("RGB sets the glow tint. Alpha is ignored — driven by pulse settings below.")]
    [SerializeField] private Color glowColor = new Color(1f, 0.75f, 0.1f, 1f);

    [Header("Size")]
    [Tooltip("Scale multiplier for the glow sprite relative to the source sprite.")]
    [SerializeField] private float glowScale = 1.25f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float minAlpha = 0.12f;
    [SerializeField] private float maxAlpha = 0.60f;

    private SpriteRenderer _source;
    private SpriteRenderer _glow;
    private float _phaseOffset;

    private void Awake()
    {
        _source = GetComponent<SpriteRenderer>();
        _phaseOffset = (GetInstanceID() & 0xFF) * 0.0245f;
        BuildGlowChild();
    }

    private void Update()
    {
        if (_glow == null) return;

        if (_source != null && _glow.sprite != _source.sprite)
            _glow.sprite = _source.sprite;

        float t = (Mathf.Sin(Time.time * pulseSpeed + _phaseOffset) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        _glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
    }

    private void BuildGlowChild()
    {
        Transform existing = transform.Find("__Glow__");
        if (existing != null)
        {
            _glow = existing.GetComponent<SpriteRenderer>();
            return;
        }

        var go = new GameObject("__Glow__");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * glowScale;

        _glow = go.AddComponent<SpriteRenderer>();

        if (_source != null)
        {
            _glow.sprite = _source.sprite;
            _glow.sortingLayerID = _source.sortingLayerID;
            _glow.sortingOrder = _source.sortingOrder - 1;
        }

        _glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        _glow.material = new Material(Shader.Find("Sprites/Default"));

        go.transform.SetAsFirstSibling();
    }
}