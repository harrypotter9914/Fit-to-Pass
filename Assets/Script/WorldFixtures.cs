using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Visible engineering and grounded scenery. Never changes original terrain collision.</summary>
public static class WorldFixtures
{
    static bool IsGround(Collider2D c) => c.GetComponent<Tilemap>() || c.name.StartsWith("RoomSurface") || c.name.ToLowerInvariant().Contains("startpoint");
    public static float FloorAt(Vector2 near, float fallback)
    {
        Physics2D.SyncTransforms();
        float floor = fallback, best = float.PositiveInfinity;
        // A ray starting INSIDE terrain reports a zero-distance hit and a fake
        // upward normal. Sample around the requested footing and reject those
        // hits; choose the exposed surface closest to the intended floor.
        for (int i = -6; i <= 16; i++)
            foreach (var h in Physics2D.RaycastAll(near + Vector2.up * (i * .5f + .6f), Vector2.down, 40, 1 << 6))
                if (h.collider && !h.collider.isTrigger && h.distance > .001f && h.normal.y > .65f &&
                    !h.collider.CompareTag("Death") && IsGround(h.collider))
                {
                    float difference = Mathf.Abs(h.point.y - fallback);
                    if (difference < best) { best = difference; floor = h.point.y; }
                }
        return floor;
    }
    public static SpriteRenderer OnFloor(string name, Sprite sprite, float x, float floor, Vector2 size, int order = 5, Transform parent = null)
    {
        var art = Art.Place(name, sprite, new Vector3(x, floor + size.y * .5f), size, order, parent);
        art.gameObject.AddComponent<GroundedFixture>().supportY = floor;
        return art;
    }
    public static void Install()
    {
        foreach (var bridge in Object.FindObjectsByType<BalanceBridge>())
            BridgeSupport(bridge.transform.position);
        foreach (var lift in Object.FindObjectsByType<ElevatorController>())
            Rail(lift.transform.position, lift.travel, 2.5f);
        foreach (var lift in Object.FindObjectsByType<PlatformAndRockController>())
        {
            var center = (lift.platformUpPosition + lift.platformDownPosition) * .5f;
            Rail(center, Mathf.Abs(lift.platformUpPosition.y - lift.platformDownPosition.y) * .5f, 2.6f);
        }
        foreach (var lift in Object.FindObjectsByType<PlatformController>())
            Rail((lift.upPosition + lift.downPosition) * .5f, Vector3.Distance(lift.upPosition, lift.downPosition) * .5f, 2.5f);
        foreach (var button in Object.FindObjectsByType<ButtonController>())
        {
            var b = button.GetComponent<Collider2D>().bounds;
            float y = FloorAt(b.center, b.max.y);
                Foundation("Pressure plate housing", 2, b.center.x, y, 1.4f, 3);
        }
        foreach (var body in Object.FindObjectsByType<Rigidbody2D>())
            if (body.bodyType == RigidbodyType2D.Dynamic && !body.GetComponent<PlayerController>())
            {
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
    }
    public static void BridgeSupport(Vector3 pivot)
    {
        float floor = FloorAt(pivot - Vector3.up, pivot.y - 3);
        if (!HasTerrainContact(pivot.x, floor)) floor = DeepFooting(pivot.x);
        var sprite = Art.Cell("Fixtures", 2, 2, 0);
        float height = 1.4f * sprite.rect.height / sprite.rect.width;
        float bottom = pivot.y - height * .87f;
        Art.Place("NewArt Bridge pivot", sprite, new Vector3(pivot.x, bottom + height * .5f, 0), new Vector2(1.4f, height), 2);
        if (bottom > floor)
            Fixture("Bridge column", 1, new Vector3(pivot.x, (bottom + floor) * .5f, 0), new Vector2(.55f, bottom - floor), -2);
        Foundation("Bridge footing", 3, pivot.x, floor, 1.65f, 2);
    }
    public static bool HasTerrainContact(float x, float y)
    {
        Physics2D.SyncTransforms();
        return Physics2D.RaycastAll(new Vector2(x, y + .04f), Vector2.down, .08f, 1 << 6)
            .Any(h => h.distance > .001f && h.normal.y > .65f && !h.collider.isTrigger &&
                IsGround(h.collider));
    }
    static float DeepFooting(float x)
    {
        // Below fall recovery: structural support, never a new playable route.
        const float floor = -14;
        var go = new GameObject("RoomSurface structural foundation");
        go.layer = 6;
        go.transform.position = new Vector3(x, floor - .5f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(2, 1);
        Art.Place("NewArt Structural foundation", Art.Cell("Props", 3, 2, 0), new Vector3(x, floor-.15f), new Vector2(2,.3f), 1, go.transform);
        return floor;
    }
    public static void DeckSupport(float x, float underside)
    {
        float floor = FloorAt(new Vector2(x, underside-.1f), 0);
        if (floor >= underside - .05f || !HasTerrainContact(x, floor)) floor = DeepFooting(x);
        Fixture("Deck column", 1, new Vector3(x, (floor+underside)*.5f), new Vector2(.35f, underside-floor), -2);
        Foundation("Deck footing", 3, x, floor, .8f, -1);
    }
    public static void Rail(Vector3 center, float travel, float width)
    {
        foreach (float side in new[] { -1f, 1f })
            Fixture("Lift guide", 1, center + Vector3.right * side * width * .5f, new Vector2(.15f, travel * 2 + .8f), -3);
    }
    public static SpriteRenderer Fixture(string name, int index, Vector3 position, Vector2 size, int order)
    {
        var sprite = Art.Cell("Fixtures", 2, 2, index);
        if (index == 2 || index == 3)
        {
            float height = size.x * sprite.rect.height / sprite.rect.width;
            position.y -= (height - size.y) * .5f;
            size.y = height;
        }
        return Art.Place("NewArt " + name, sprite, position, size, order);
    }
    public static SpriteRenderer Foundation(string name, int index, float x, float floor, float width, int order)
    {
        var sprite = Art.Cell("Fixtures", 2, 2, index);
        return OnFloor("NewArt " + name, sprite, x, floor, new Vector2(width, width * sprite.rect.height / sprite.rect.width), order);
    }

    public static CheckpointBeacon Checkpoint(Vector3 near)
    {
        float floor = FloorAt(near, near.y - .55f);
        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player)
        {
            if (!RespawnSafety.Find(player, new Vector2(near.x, floor), 2, out var safe) &&
                !RespawnSafety.Find(player, new Vector2(near.x, floor), 1, out safe))
            {
                Debug.LogWarning("Checkpoint has no supported medium-body clearance: " + near);
                return null;
            }
            near.x = safe.x;
        }
        var plinth = Foundation("Checkpoint plinth", 3, near.x, floor, .8f, 4);
        var sprite = Art.Cell("Props", 3, 2, 5);
        float width = 1.05f * sprite.rect.width / sprite.rect.height;
        var art = OnFloor("NewArt Checkpoint", sprite, near.x, plinth.bounds.max.y, new Vector2(width, 1.05f), 5);
        var collider = art.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        var beacon = art.gameObject.AddComponent<CheckpointBeacon>();
        beacon.floorY = floor;
        return beacon;
    }
}
