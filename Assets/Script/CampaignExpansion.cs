using UnityEngine;

/// <summary>Twelve individually authored routes; the three legacy maps have no annex.</summary>
public static partial class CampaignExpansion
{
    static float origin;
    static int climate;
    public static Vector3 AnnexSpawn => new Vector3(400, 2, 0); // Legacy API, never used by the new campaign.
    public static void Build(int stage, PlayerController player)
    {
        if (CampaignMap.Original(stage)) return;
        origin = 0;
        climate = CampaignMap.Region(stage);
        circuit = new GameObject("Regional route circuit").AddComponent<RouteCircuit>();
        switch (stage)
        {
            case 2: RootServiceLoop(); break;
            case 3: CanopyJunction(); break;
            case 4: PollinationCircuit(); break;
            case 5: BiodomeLockdown(); break;
            case 7: SlagReturn(); break;
            case 8: FurnaceExchange(); break;
            case 9: CrucibleTransit(); break;
            case 10: ReactorShutdown(); break;
            case 12: IceSpiral(); break;
            case 13: GlacierBalance(); break;
            case 14: AuroraDescent(); break;
            case 15: LastLight(); break;
            default: throw new System.ArgumentOutOfRangeException("stage");
        }
        var death = new GameObject("Expansion fall recovery");
        death.transform.position = P(120, -10);
        var trigger = death.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(600, 2);
        trigger.isTrigger = true;
        death.AddComponent<DeathZone>();
        player.transform.position = P(0, 2);
    }
    static Vector3 P(float x, float y) => new Vector3(origin + x, y, 0);
    static GameObject Body(string name, Vector3 position, Vector2 size, bool dynamic = false)
    {
        var go = new GameObject(name);
        go.layer = 6;
        go.transform.position = position;
        go.AddComponent<BoxCollider2D>().size = size;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        body.gravityScale = 1.8f;
        body.mass = 1;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        return go;
    }
    static GameObject Prop(string name, float x, float y, Vector2 size, int index, bool dynamic = false)
    {
        var go = Body(name, P(x, y), size, dynamic);
        var art = Art.Place("NewArt " + name, Art.Cell("Props", 3, 2, index), go.transform.position, size, 4, go.transform);
        if (index == 1 || index == 2) art.gameObject.AddComponent<GroundedFixture>().supportY = y - size.y*.5f;
        return go;
    }
    static void Surface(float left, float right, float top, float depth = 1)
    {
        var go = Body("RoomSurface", P((left + right) * .5f, top - depth * .5f), new Vector2(right - left, depth));
        go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        for (float x = left; x < right - .01f; x += .75f)
            for (float y = top; y > top - depth + .01f; y -= .75f)
            {
                float w = Mathf.Min(.75f, right - x), h = Mathf.Min(.75f, y - (top - depth));
                Art.Place("NewArt Deck", Art.Cell("TerrainV2", 3, 2, climate + (y < top - .1f ? 3 : 0), false), P(x + w * .5f, y - h * .5f), new Vector2(w, h), 1, go.transform);
            }
        if (top > 1)
            foreach (float x in new[] { left + .4f, right - .4f })
                WorldFixtures.DeckSupport(origin + x, top - depth);
    }
    static void Duct(float x, float floor, float width)
    {
        Surface(x, x + width, floor + 4, 2.88f);
        TutorialDirector.Point("duct", P(x - 1.5f, floor + 1));
        Supply(x - 4, floor + .6f, true, 3);
    }
    static void Supply(float x, float y, bool fresh, int count = 3, Transform parent = null)
    {
        for (int i = 0; i < count; i++)
        {
            var food = new GameObject("Annex supply " + (fresh ? "fresh" : "energy") + " " + i);
            food.tag = fresh ? "HealthyFood" : "JunkFood";
            food.transform.position = P(x + i * .8f, y);
            if (parent)
                food.transform.SetParent(parent, true);
            food.AddComponent<CircleCollider2D>().radius = .4f;
            food.GetComponent<CircleCollider2D>().isTrigger = true;
            food.AddComponent<FoodVisual>().fresh = fresh;
        }
    }
    static void Checkpoint(float x, float floor)
        => WorldFixtures.Checkpoint(P(x, floor + .55f));
    static void Gate(float x, float floor, int size)
    {
        var sprite = Art.Cell("CalibrationGate", 1, 1, 0);
        var dimensions = new Vector2(3.4f * sprite.rect.width / sprite.rect.height, 3.4f);
        var go = Body("Mass calibration gate", P(x, floor + 1.7f), dimensions);
        Art.Place("NewArt Calibration shutter", sprite, go.transform.position, dimensions, 4, go.transform);
        var gate = go.AddComponent<MassGate>();
        gate.requiredSize = size;
        // A continuous physical housing prevents a small-body jump from bypassing
        // the requested scanner shape. Its underside remains level with the door.
        Surface(x - 1.3f, x + 1.3f, floor + 5.3f, 1.9f);
        WorldFixtures.Rail(P(x, floor + 1.7f), 1.7f, 1.2f);
        TutorialDirector.Point("gate", P(x - 3, floor + 1));

    }
    static ButtonController CallLift(float x, float floor, float height, bool supplies = true)
    {
        var platform = Prop("Annex pressure lift", x, floor + height, new Vector2(2.5f, .32f), 0);
        var controller = platform.AddComponent<PlatformController>();
        controller.platformTransform = platform.transform;
        controller.downPosition = P(x, floor + .22f);
        controller.upPosition = P(x, floor + height);
        controller.speed = 2;
        var plate = Prop("Annex pressure plate", x - 3, floor + .12f, new Vector2(1.2f, .24f), 1);
        var button=plate.AddComponent<ButtonController>(); button.platformController = controller;
        WorldFixtures.Foundation("Pressure housing", 2, origin + x - 3, floor, 1.5f, 3);
        WorldFixtures.Rail(P(x, floor + height * .5f), height * .5f, 2.8f);
        if (supplies) Supply(x - 7, floor + .6f, false, 4);
        TutorialDirector.Point("pressure", P(x - 4, floor + 1));
        return button;
    }
    static void Lift(float x, float y, float travel = 6)
    {
        var go = Prop("Annex mass lift", x, y, new Vector2(3, .32f), 0);
        go.AddComponent<ElevatorController>().travel = travel;
        WorldFixtures.Rail(P(x, y), travel, 3.3f);
        Supply(x - .8f, y + .7f, true, 3, go.transform);
        Supply(x, y + 1.5f, false, 1, go.transform);
        TutorialDirector.Point("elevator", P(x - 1, y + 1));
    }
    static void Spring(float x, float floor)
    {
        var go = Prop("Annex spring", x, floor + .35f, new Vector2(1.2f, .7f), 2);
        var spring = go.AddComponent<SpringController>();
        spring.strongForce = 20;
        spring.mediumForce = 14;
        spring.weakForce = 8;
        WorldFixtures.Foundation("Spring housing", 2, origin + x, floor, 1.4f, 3);
        Supply(x - 4, floor + .6f, true, 3);
        TutorialDirector.Point("spring", P(x - 2, floor + 1));
    }
    static void Bridge(float x, float y, bool reverse = false)
    {
        var deck = Prop("Annex balance bridge", x, y, new Vector2(8, .28f), 0, true);
        var body = deck.GetComponent<Rigidbody2D>();
        body.mass = 2;
        body.angularDamping = 2;
        var joint = deck.AddComponent<HingeJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = P(x, y);
        joint.useLimits = true;
        joint.limits = new JointAngleLimits2D { min = -12, max = 12 };
        deck.AddComponent<BalanceBridge>().riderSide = reverse ? 1 : -1;
        var crate = Prop("Annex bridge counterweight", x + (reverse ? -2.9f : 2.9f), y + .73f, new Vector2(1.1f, 1.1f), 3, true);
        var friction = new PhysicsMaterial2D("Counterweight grip") { friction = .8f, bounciness = 0 };
        crate.GetComponent<Collider2D>().sharedMaterial = friction;
        body.GetComponent<Collider2D>().sharedMaterial = friction;
        WorldFixtures.BridgeSupport(P(x, y));
        TutorialDirector.Point("bridge", P(x - 5, y + 1));
    }
    static void Goal(float x, float floor)
    {
        var sprite = Art.Cell("Props", 3, 2, 4);
        var art = WorldFixtures.OnFloor("NewArt Annex Exit", sprite, origin+x, floor, new Vector2(2.8f*sprite.rect.width/sprite.rect.height, 2.8f), 8);
        art.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        art.gameObject.AddComponent<LevelGoal>();
        GamePresentation.WorldLabel("EXIT", P(x, floor + 3.1f), new Color(.95f, .78f, .45f));
    }
    static void Record(float x, float y, string title, string text, int index)
    {
        var art = Art.Place("NewArt Research note", Art.Cell("Icons", 2, 2, 2), P(x, y), new Vector2(.35f, .5f), 7);
        art.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        var record = art.gameObject.AddComponent<ResearchRecord>();
        record.heading = title;
        record.body = text;
        record.index = index;
    }
    static VectorAirwell Fan(float x, float floor, bool tiered, float high = 13.4f, float middle = 6.9f, float field = 18)
    {
        var go = Body(tiered ? "Tiered airflow turbine" : "Free-flight airflow turbine", P(x, floor + .45f), new Vector2(3.6f, .9f));
        var air = go.AddComponent<VectorAirwell>();
        air.tiered = tiered;
        air.highHeight = high;
        air.middleHeight = middle;
        air.height = field;
        TutorialDirector.Point(tiered ? "airwell" : "freeflight", P(x - 4, floor + 1));
        return air;
    }
    static BodyRelay Relay(float x, float floor, int size, float teachingLead = 3)
    {
        var go = new GameObject("Body route relay");
        go.transform.position = P(x, floor);
        var relay = go.AddComponent<BodyRelay>();
        relay.requiredSize = size;
        TutorialDirector.Point("relay", P(x - teachingLead, floor + 1));
        return relay;
    }
    static void LinkedDoor(float x, float floor, System.Func<bool> power, Component source)
    {
        var sprite = Art.Cell("CalibrationGate", 1, 1, 0);
        var size = new Vector2(5.3f * sprite.bounds.size.x / sprite.bounds.size.y, 5.3f);
        var go = Body("Linked circuit shutter", P(x, floor + 2.65f), size);
        Art.Place("NewArt Linked shutter", sprite, go.transform.position, size, 5, go.transform);
        var door=go.AddComponent<SignalBarrier>();door.Powered=power;door.Source=source;
        Surface(x - 1.3f, x + 1.3f, floor + 6.2f, .9f);
        WorldFixtures.Rail(P(x, floor + 3), 3, 1.2f);
    }
    static void Rations(float x, float floor, int count = 6)
    {
        // Two readable supply packs replace a wall of individually collected food.
        foreach (bool fresh in new[] { false, true })
        {
            var go=new GameObject(fresh?"Fresh supply pack":"Energy supply pack");
            go.transform.position=P(x+(fresh?1.6f:0),floor+.65f);
            go.tag=fresh?"HealthyFood":"JunkFood";
            var shape=go.AddComponent<CircleCollider2D>();shape.radius=.5f;shape.isTrigger=true;
            var food=go.AddComponent<FoodVisual>();food.fresh=fresh;food.quantity=count;
        }
    }
    static void LockStation(float x, float floor)
    {
        var go = new GameObject("Mass lock station");
        go.transform.position = P(x, floor);
        go.AddComponent<MassLockStation>();
        TutorialDirector.Point("masslock", P(x - 1.5f, floor + 1));
    }
}

public class ExtensionEntrance : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>())
            GameFlow.Instance.EnterAnnex();
    }
}
public class ResearchRecord : MonoBehaviour
{
    public string heading, body;
    public int index;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponent<PlayerController>())
            return;
        TutorialDirector.Show("record-" + GameFlow.Instance.Stage + "-" + index, heading, body, "Research record collected", true);
        Destroy(gameObject);
    }
}
