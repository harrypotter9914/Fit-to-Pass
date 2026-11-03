using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Linq;

public static class GamePresentation
{
    public static void Apply(int stage, PlayerController player)
    {
        foreach (var collider in Object.FindObjectsByType<TilemapCollider2D>()) collider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        int climate = CampaignMap.Region(stage) + 1;
        foreach (var c in Object.FindObjectsByType<Canvas>())
            c.gameObject.SetActive(false);
        var camera = Camera.main;
        if (camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.backgroundColor = new Color(.025f, .055f, .075f);
            camera.gameObject.AddComponent<Backdrop>().stage = climate;
        }
        var legacyAnimator = player.GetComponent<Animator>();
        if (legacyAnimator)
            legacyAnimator.enabled = false;
        var oldPlayer = player.GetComponent<SpriteRenderer>();
        if (oldPlayer)
            oldPlayer.enabled = false;
        if (!player.GetComponent<CameraFollow>())
            player.gameObject.AddComponent<CameraFollow>();
        player.gameObject.AddComponent<CharacterVisual>();
        foreach (var map in Object.FindObjectsByType<Tilemap>())
            ReskinMap(map, climate);
        foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>())
        {
            if (renderer.gameObject == player.gameObject || renderer.name.StartsWith("NewArt"))
                continue;
            if (renderer.GetComponentInParent<PlayerController>())
            {
                renderer.enabled = false;
                continue;
            }
            var go = renderer.gameObject;
            var bounds = renderer.bounds;
            // Bridge crate sprites are much larger than their physics shapes.
            if (go.TryGetComponent<Collider2D>(out var shape) && !shape.isTrigger)
                bounds = shape.bounds;
            if (go.CompareTag("HealthyFood") || go.CompareTag("JunkFood"))
            {
                var foodAnimator = go.GetComponent<Animator>();
                if (foodAnimator)
                    foodAnimator.enabled = false;
                renderer.enabled = false;
                var food = go.AddComponent<FoodVisual>();
                food.fresh = go.CompareTag("HealthyFood");
                continue;
            }
            string name = go.name.ToLowerInvariant();
            if (name.Contains("endpoint") || name == "end" || name.StartsWith("end ("))
            {
                renderer.enabled = false;
                foreach (var c in go.GetComponents<Collider2D>())
                    c.enabled = false;
                continue;
            }
            if (name.Contains("startpoint"))
            {
                renderer.enabled = false;
                Art.Place("NewArt Entry platform", Art.Cell("Props", 3, 2, 0), bounds.center, bounds.size, 1, go.transform);
                float entryFloor = WorldFixtures.FloorAt(go.transform.position, go.transform.position.y - .55f);
                var entrySprite = Art.Cell("Props", 3, 2, 5);
                WorldFixtures.OnFloor("NewArt Entry", entrySprite, go.transform.position.x, entryFloor,
                    new Vector2(entrySprite.rect.width / entrySprite.rect.height, 1), 4);
                continue;
            }
            bool spring = go.GetComponent<SpringController>();
            bool button = go.GetComponent<ButtonController>();
            bool platform = go.GetComponent<PlatformController>() || go.GetComponent<PlatformAndRockController>() || go.GetComponent<ElevatorController>() || name.Contains("platform") || bounds.size.x > bounds.size.y * 2.5f;
            int index = spring ? 2 : button ? 1 : platform ? 0 : 3;
            var animator = go.GetComponent<Animator>();
            if (animator)
                animator.enabled = false;
            renderer.enabled = false;
            // Keep the authored collision geometry; replace only its visible skin.
            var size = new Vector2(Mathf.Max(.25f, bounds.size.x), Mathf.Max(.16f, bounds.size.y));
            if (button)
            {
                float floor = WorldFixtures.FloorAt(bounds.center, bounds.min.y);
                size = new Vector2(Mathf.Max(.8f, bounds.size.x), .26f);
                bounds.center = new Vector3(bounds.center.x, Mathf.Max(floor + .12f, bounds.max.y - .13f), 0);
            }
            var skin = go.GetComponent<DeathZone>() ? Art.Cell("Hazard", 1, 1, 0) : Art.Cell("Props", 3, 2, index);
            var art = Art.Place("NewArt " + go.name, skin, bounds.center, size, 4, go.transform);
            if (spring || button)
            {
                float floor = WorldFixtures.FloorAt(bounds.center, bounds.min.y);
                art.transform.position += Vector3.up * (floor - art.bounds.min.y);
                art.gameObject.AddComponent<GroundedFixture>().supportY = floor;
                var visual = go.AddComponent<MechanismVisual>();
                visual.art = art.transform;
            }
        }
        foreach (var lift in Object.FindObjectsByType<ElevatorController>())
        {
            if (stage == 11)
                lift.travel = 20;
        }
        foreach (var spring in Object.FindObjectsByType<SpringController>())
        {
            spring.strongForce = 20;
            spring.mediumForce = 14;
            spring.weakForce = 8;
        }
        if (CampaignMap.Original(stage))
            AddRoute(CampaignMap.Region(stage) + 1);
        WorldFixtures.Install();
        CampaignExpansion.Build(stage, player);
        TutorialDirector.Install(stage);
    }
    static void AddRoute(int stage)
    {
        Vector3[] exits = { new Vector3(2, 10.6f, 0), new Vector3(49, 10, 0), new Vector3(73, -3.5f, 0) };
        var position = exits[stage - 1];
        float floor = WorldFixtures.FloorAt(position, position.y - 1.35f);
        var sprite = Art.Cell("Props", 3, 2, 4);
        var exit = WorldFixtures.OnFloor("NewArt Campaign Exit", sprite, position.x, floor, new Vector2(2.7f * sprite.rect.width / sprite.rect.height, 2.7f), 8);
        var hit = exit.gameObject.AddComponent<BoxCollider2D>();
        hit.isTrigger = true;
        exit.gameObject.AddComponent<LevelGoal>();
        WorldLabel("CONTINUE THROUGH THE " + CampaignMap.Regions[stage - 1], new Vector3(position.x, floor + 3.15f), new Color(1, .8f, .45f));
        Vector3[][] points = { new[] { new Vector3(30, .9f, 0), new Vector3(84, .9f, 0), new Vector3(70, 10.7f, 0) }, new[] { new Vector3(-7, 1.2f, 0), new Vector3(18, 1.1f, 0) }, new[] { new Vector3(11, .9f, 0), new Vector3(49, 5.1f, 0) } };
        foreach (var point in points[stage - 1])
        {
            WorldFixtures.Checkpoint(point);
        }
        // The source contained orphan prototype props far above the actual final map.
        if (stage == 3)
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>())
                if (r.transform.position.y > 35 && r.sortingOrder >= 0)
                    r.gameObject.SetActive(false);
        if (stage == 1)
        {
            var orphan = Object.FindObjectsByType<Transform>().FirstOrDefault(t => t.name.Contains("Missing Prefab"));
            if (orphan)
                orphan.gameObject.SetActive(false);
            Supply(new Vector3(43, .65f, 0), true);
            Supply(new Vector3(45, .65f, 0), true);
        }
        if (stage == 3)
        {
            for (int i = 0; i < 6; i++)
                Supply(new Vector3(49.5f + i, 4.65f, 0), i < 2);
        }
    }
    static void Supply(Vector3 position, bool fresh)
    {
        var go = new GameObject("Campaign supply " + (fresh ? "fresh" : "energy"));
        go.transform.position = position;
        go.tag = fresh ? "HealthyFood" : "JunkFood";
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = .45f;
        go.AddComponent<FoodVisual>().fresh = fresh;
    }
    static void ReskinMap(Tilemap original, int stage)
    {
        var renderer = original.GetComponent<TilemapRenderer>();
        if (!renderer)
            return;
        var go = new GameObject("NewArt Terrain " + original.name);
        go.transform.SetParent(original.transform.parent, false);
        go.transform.localPosition = original.transform.localPosition;
        go.transform.localRotation = original.transform.localRotation;
        go.transform.localScale = original.transform.localScale;
        var map = go.AddComponent<Tilemap>();
        var newRenderer = go.AddComponent<TilemapRenderer>();
        newRenderer.sharedMaterial = Art.Material;
        newRenderer.sortingOrder = renderer.sortingOrder;
        renderer.enabled = false;
        float ppu = Art.Texture("TerrainV2").width / 3f / original.layoutGrid.cellSize.x;
        var top = ScriptableObject.CreateInstance<Tile>();
        top.sprite = Art.Cell("TerrainV2", 3, 2, stage - 1, false, ppu);
        top.colliderType = Tile.ColliderType.None;
        var fill = ScriptableObject.CreateInstance<Tile>();
        fill.sprite = Art.Cell("TerrainV2", 3, 2, stage + 2, false, ppu);
        fill.colliderType = Tile.ColliderType.None;
        foreach (var pos in original.cellBounds.allPositionsWithin)
            if (original.HasTile(pos))
                map.SetTile(pos, original.HasTile(pos + Vector3Int.up) ? fill : top);
        if (original.CompareTag("Death"))
        {
            map.color = new Color(1, .36f, .2f);
            newRenderer.sortingOrder = 1;
        }
    }
    public static void WorldLabel(string text, Vector3 position, Color color)
    {
        var go = new GameObject("NewArt Sign");
        go.transform.position = position;
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 44;
        mesh.characterSize = .045f;
        mesh.color = color;
        go.GetComponent<MeshRenderer>().sortingOrder = 12;
    }
}
public class FoodVisual : MonoBehaviour
{
    public bool fresh;
    public int quantity = 1;
    public string PersistentId
    {
        get; private set;
    }
    Transform art; Vector3 home;
    void Awake()
    {
        PersistentId = name + ":" + transform.position.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + transform.position.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
    void Start()
    {
        art = Art.Place("NewArt Food", Art.Cell("Icons", 2, 2, fresh ? 1 : 0), transform.position, new Vector2(.62f, .62f), 9, transform).transform;
        home = art.localPosition;
        if (quantity > 1)
        {
            var tag = new GameObject("Supply quantity"); tag.transform.SetParent(transform, false); tag.transform.localPosition = new Vector3(.55f, .05f);
            var label = tag.AddComponent<TextMesh>(); label.text = "x" + quantity; label.fontSize = 36; label.characterSize = .04f;
            label.anchor = TextAnchor.MiddleLeft; label.color = new Color(.9f, 1, .9f); tag.GetComponent<MeshRenderer>().sortingOrder = 12;
        }
    }
    void Update()
    {
        if (art && PlayerPrefs.GetInt("Fit.ReducedMotion", 0) == 0)
            art.localPosition = home + Vector3.up * (Mathf.Sin(Time.time * 2.2f + transform.position.x) * .065f);
    }
}
public class LevelGoal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            var circuit = Object.FindAnyObjectByType<RouteCircuit>();
            if (circuit && !circuit.Complete) { GameFlow.Instance?.Toast(circuit.MissingReason);FirstEncounterGuide.Instance?.Request("exit",this); return; }
            GameFlow.Instance?.Finish();
        }
    }
}
public class CheckpointBeacon : MonoBehaviour
{
    public float floorY;
    bool activated;
    public bool Activated => activated;
    void OnTriggerStay2D(Collider2D other) { OnTriggerEnter2D(other); }
    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerController>();
        if (p && !activated)
        {
            int size = Mathf.Max(1, p.Size);
            if (!RespawnSafety.Find(p, new Vector2(transform.position.x, floorY), size, out var position)) return;
            activated = true;
            // A small-body touch still requires a medium-clear retry area.
            if (p.Size == 0) position = RespawnSafety.Root(p, new Vector2(position.x, floorY), 0);
            GameFlow.Instance?.Checkpoint(position);
        }
    }
}
[DefaultExecutionOrder(200)]
public class Backdrop : MonoBehaviour
{
    public int stage;
    SpriteRenderer art;
    void Start()
    {
        string[] names = { "Lab", "Heat", "Cold" };
        var texture = Art.Texture(names[stage - 1]);
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
        art = Art.Place("NewArt Backdrop", sprite, transform.position + Vector3.forward * 20, new Vector2(24, 14), -100);
    }
    void LateUpdate()
    {
        if (!art)
            return;
        var cam = GetComponent<Camera>();
        float height = cam.orthographicSize * 2.16f, width = Mathf.Max(height * cam.aspect, height * 1.777f);
        art.transform.position = new Vector3(transform.position.x, transform.position.y, 10);
        art.transform.localScale = new Vector3(width / art.sprite.bounds.size.x, height / art.sprite.bounds.size.y, 1);
    }
}
public class CharacterVisual : MonoBehaviour
{
    PlayerController player; SpriteRenderer art; Sprite[] frames;
    float step, landTime; bool wasGrounded; int previousSize;
    void Start()
    {
        player = GetComponent<PlayerController>();
        frames = new Sprite[8];
        for (int i = 0; i < 8; i++)
            frames[i] = Art.Cell("Subject", 4, 2, i, false, 300);
        art = Art.Place("NewArt Subject", frames[0], transform.position, new Vector2(1.28f, 1.707f), 10, transform);
        art.transform.localScale = Vector3.one;
        art.transform.localPosition = GetComponent<Collider2D>().offset;
        previousSize = player.Size;
    }
    void LateUpdate()
    {
        if (!art)
            return;
        int frame = 0;
        if (!player.Grounded)
            frame = player.Body.linearVelocity.y > 0 ? 5 : 6;
        else if (Mathf.Abs(player.HorizontalInput) > .1f)
        {
            step += Time.deltaTime * 10;
            frame = 1 + (int)step % 3;
        }
        if (player.Grounded && !wasGrounded)
            landTime = .12f;
        wasGrounded = player.Grounded;
        if (landTime > 0)
        {
            landTime -= Time.deltaTime;
            frame = 7;
        }
        art.sprite = frames[frame];
        art.flipX = !player.FacingRight;
        if (previousSize != player.Size)
        {
            landTime = .14f;
            previousSize = player.Size;
        }
    }
}
