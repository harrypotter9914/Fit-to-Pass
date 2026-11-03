using UnityEngine;

/// <summary>Authored room geometry. Every method defines a different traversal topology.</summary>
public static partial class CampaignExpansion
{
    static RouteCircuit circuit;
    static BodyRelay Link(string label, float x, float floor, int size, Transform parent = null, float teachingLead = 3)
    {
        var relay = Relay(x, floor, size, teachingLead);
        relay.name = "Route link " + (circuit.Links.Count + 1) + " / " + label;
        if (parent) relay.transform.SetParent(parent, true);
        circuit.Links.Add(relay); circuit.Labels.Add(label);
        relay.RouteNumber=circuit.Links.Count;relay.RouteLabel=label;
        GamePresentation.WorldLabel("R"+circuit.Links.Count + "  " + label.ToUpperInvariant() + " / " + new[] { "S", "M", "L" }[size], P(x, floor + 3.5f), new Color(.52f, .95f, .86f));
        return relay;
    }
    static void Interlock(string label, System.Func<bool> state, Component source=null) { circuit.Interlocks.Add(state); circuit.InterlockLabels.Add(label); circuit.InterlockSources.Add(source); }
    static void Camp(float x, float floor, int supplies = 5)
    {
        Checkpoint(x, floor); Rations(x + 2, floor, supplies);
    }
    static void Sign(string label, float x, float floor) => GamePresentation.WorldLabel(label, P(x, floor + 3), new Color(.95f, .8f, .5f));
    static RailShuttle Cart(float left, float right, float dock, float floor)
    {
        var go = Body("Regional rail shuttle " + left, P(left, floor + .45f), new Vector2(4, .8f));
        var rail = go.AddComponent<RailShuttle>(); rail.left = left; rail.right = right; rail.dock = dock; rail.speed = 4;
        OnboardSupply(go.transform, true, 16);
        OnboardSupply(go.transform, false, 12);
        TutorialDirector.Point("shuttle", P(left - 3, floor + 1));
        Interlock("BRAKE AT CYAN DOCK", () => rail.Docked,rail);
        return rail;
    }
    static void OnboardSupply(Transform carriage, bool fresh, int quantity)
    {
        var go = new GameObject(fresh ? "Carriage fresh reserve" : "Carriage energy reserve");
        go.transform.SetParent(carriage, false); go.transform.localPosition = new Vector3(0, fresh ? 1.35f : 1.95f);
        go.tag = fresh ? "HealthyFood" : "JunkFood"; go.AddComponent<CircleCollider2D>().radius = .5f; go.GetComponent<CircleCollider2D>().isTrigger = true;
        var food = go.AddComponent<FoodVisual>(); food.fresh = fresh; food.quantity = quantity;
    }
    static MassEcho Echo(float x, float floor, float duration = 24)
    {
        var go = new GameObject("Service echo " + x); go.transform.position = P(x, floor);
        var echo = go.AddComponent<MassEcho>(); echo.duration = duration;
        TutorialDirector.Point("echo", P(x - 3, floor + 1)); return echo;
    }
    static MassPrism Prism(float x, float floor)
    {
        var go = new GameObject("Optical interlock " + x); go.transform.position = P(x, floor);
        var prism = go.AddComponent<MassPrism>();
        TutorialDirector.Point("prism", P(x - 3, floor + 1));
        Interlock("LIGHT ALL OPTICAL RECEIVERS", () => prism.Complete,prism); return prism;
    }
    static KineticCapacitor EnergyBridge(float x, float floor, float start, float end)
    {
        var go = Body("Regional impact capacitor " + x, P(x, floor + .45f), new Vector2(2.8f, .9f));
        var core = go.AddComponent<KineticCapacitor>(); core.bridgeStart = P(start, floor); core.bridgeEnd = P(end, floor); core.lifetime = 12;
        TutorialDirector.Point("capacitor", P(x - 5, floor + 1));
        Sign("HEAVY IMPACT  →  LIGHT CROSSING", x - 4, floor); return core;
    }
    static void BalanceLink(string label, float x, float floor, bool reverse = false)
    {
        Bridge(x, floor + .3f, reverse);
        var bridge = GameObject.Find("Annex balance bridge");
        // Find the just-authored bridge explicitly; names do not identify earlier decks.
        foreach (var b in Object.FindObjectsByType<BalanceBridge>())
            if (Mathf.Abs(b.transform.position.x - x) < .1f && Mathf.Abs(b.transform.position.y - floor - .3f) < .1f) bridge = b.gameObject;
        Link(label, x + (reverse ? 2.8f : -2.8f), floor + .44f, 1, bridge.transform);
        bridge.name = "Regional balance bridge " + x;
    }
    static void Counterweight(float plateX, float stoneX, float floor)
    {
        var plate = Prop("Return pressure platform", plateX, floor + 2.6f, new Vector2(3, .32f), 0);
        var rock = Prop("Closing counterweight stone", stoneX, floor + 2.2f, new Vector2(4.4f, 4.4f), 3);
        var control = plate.AddComponent<PlatformAndRockController>();
        control.platformTransform = plate.transform; control.rockTransform = rock.transform;
        control.platformUpPosition = plate.transform.position; control.platformDownPosition = P(plateX, floor + .25f);
        control.rockDownPosition = rock.transform.position; control.rockUpPosition = P(stoneX, floor + 8.2f);
        control.platformMoveSpeed = 1.5f; control.rockMoveSpeed = 1.35f;
        WorldFixtures.Rail(P(plateX, floor + 1.5f), 1.6f, 3.4f);
        WorldFixtures.Rail(P(stoneX, floor + 5), 5, 4);
        Surface(stoneX - 2.1f, stoneX + 2.1f, floor + 12, 1);
        TutorialDirector.Point("counterweight", P(plateX - 4, floor + 1));
        Rations(plateX - 6, floor, 6);
        Sign("STEP OFF  →  SHRINK  →  RUN", plateX + 4, floor);
    }

    // GREENHOUSE 2: a long lower duct route, freight ascent, then a westward canopy return.
    static void RootServiceLoop()
    {
        Surface(-4, 98, 0); Camp(1, 0); Duct(12, 0, 9);
        Link("ROOT VALVE", 30, 0, 0); Rations(36, 0); Gate(48, 0, 1);
        Duct(61, 0, 11); Camp(79, 0); Link("FREIGHT CONTROL", 85, 0, 2);
        CallLift(96, 0, 7); Surface(14, 93, 6.8f);
        Camp(83, 6.8f); Gate(68, 6.8f, 1); Link("CANOPY RETURN", 53, 6.8f, 1);
        Duct(30, 6.8f, 10); Rations(45, 6.8f); Goal(18, 6.8f);
        Sign("ROOT SERVICE  →", 8, 0); Sign("←  RETURN THROUGH THE CANOPY", 76, 6.8f);
    }
    // GREENHOUSE 3: hub with two opposite upper branches and a low relay; no linear repeated room chain.
    static void CanopyJunction()
    {
        Surface(-4, 104, 0); Camp(0, 0, 7); Link("LOW IRRIGATION", 11, 0, 2);
        Fan(27, 0, true); Surface(30, 78, 6.5f); Surface(-38, 24, 13);
        Camp(36, 6.5f); Gate(49, 6.5f, 1); Link("EAST VENTILATION", 71, 6.5f, 1);
        Camp(12, 13); Duct(-9, 13, 10); Link("WEST CANOPY", -30, 13, 0); Rations(-24, 13, 6);
        LinkedDoor(86, 0, () => circuit.Complete,circuit); Duct(92, 0, 6); Goal(102, 0);
        Sign("USE THE SHAFT TO RETURN BETWEEN BRANCHES", 27, 2);
    }
    // GREENHOUSE 4: eastbound rail plus a separate upper return; exit sits west of the start.
    static void PollinationCircuit()
    {
        Surface(-4, 15, 0); Camp(1, 0, 8); Cart(16, 76, 45, 0);
        Surface(77, 106, 0); Camp(81, 0); Link("EAST POLLINATION", 89, 0, 2);
        CallLift(104, 0, 7); Surface(-24, 101, 6.8f);
        Camp(94, 6.8f); var echo = Echo(88, 6.8f, 10); Duct(73, 6.8f, 8);
        LinkedDoor(63, 6.8f, () => echo.Powered,echo); Link("RETURN AIRLOCK", 54, 6.8f, 0);
        Camp(42, 6.8f); Gate(25, 6.8f, 1); Duct(-5, 6.8f, 13);
        Link("WEST RELEASE", -14, 6.8f, 0); Goal(-21, 6.8f);
        Sign("→  BRAKE AT THE CYAN DOCK", 6, 0); Sign("←  WEST RETURN / EXIT", 35, 6.8f);
    }
    // GREENHOUSE 5: three-storey atrium, full east/west sweep, then a capacitor-powered transfer.
    static void BiodomeLockdown()
    {
        Surface(-4, 94, 0); Camp(0, 0, 8); Duct(13, 0, 12); Link("IRRIGATION INTAKE", 40, 0, 0);
        Gate(57, 0, 2); Camp(71, 0); CallLift(92, 0, 7);
        Surface(5, 89, 6.8f); Camp(78, 6.8f); var echo = Echo(72, 6.8f, 9);
        Duct(52, 6.8f, 11); LinkedDoor(42, 6.8f, () => echo.Powered,echo); Link("VENTILATION RETURN", 31, 6.8f, 0);
        Rations(18, 6.8f, 7); CallLift(7, 6.8f, 7); Surface(10, 111, 13.6f);
        Camp(17, 13.6f); LockStation(29, 13.6f); Gate(45, 13.6f, 1);
        Link("CANOPY ISOLATION", 61, 13.6f, 1); Duct(75, 13.6f, 10); Camp(96, 13.6f);
        EnergyBridge(108, 13.6f, 112, 134); Surface(134, 155, 13.6f);
        Link("BIODOME RELEASE", 141, 13.6f, 0); Goal(151, 13.6f);
        Sign("→  IRRIGATION", 6, 0); Sign("←  VENTILATION", 81, 6.8f); Sign("→  CANOPY / FOUNDRY TRANSFER", 68, 13.6f);
    }

    // FOUNDRY 2: timed closing stone with a physical low reset lane, followed by an upper loop.
    static void SlagReturn()
    {
        Surface(-4, 79, 0); Camp(0, 0, 8); Counterweight(16, 29, 0);
        Link("SLAG INTAKE", 43, 0, 0); Camp(49, 0); Gate(60, 0, 2); CallLift(77, 0, 7);
        Surface(8, 74, 6.8f); Camp(65, 6.8f); Duct(46, 6.8f, 10);
        Link("EXHAUST RETURN", 35, 6.8f, 0); Rations(23, 6.8f); Gate(14, 6.8f, 1);
        Surface(-24, 5, 0); Link("SLAG RELEASE", -10, 0, 2); Goal(-20, 0);
        Sign("MISSED IT? RETURN LEFT AND RESET", 8, 0); Sign("←  EXHAUST RETURN", 60, 6.8f);
    }
    // FOUNDRY 3: diagonal lift climb, light energy crossing, lower service return, final lift.
    static void FurnaceExchange()
    {
        Surface(-4, 13, 0); Camp(0, 0, 8); Lift(15, 1, 7); Lift(21, 3.2f, 7); Lift(27, 5.5f, 7);
        Surface(30, 64, 9); Camp(34, 9); Link("UPPER EXCHANGER", 47, 9, 1);
        EnergyBridge(60, 9, 65, 88); Surface(88, 117, 9); Camp(94, 9); Link("HEAT SINK", 107, 9, 0);
        // A separate route drops below the first bridge, turns east and climbs at the far end.
        Surface(115, 155, 2); Camp(122, 2); Duct(130, 2, 10); Link("LOW COOLANT", 148, 2, 0);
        Surface(155, 184, -3); Camp(164, -3); Gate(176, -3, 1);
        Lift(187, -2, 18); Surface(86, 184, 16); Camp(174, 16);
        Link("EXCHANGER RETURN", 154, 16, 1); Duct(125, 16, 14); Rations(112, 16);
        Gate(100, 16, 2); Goal(90, 16);
        Sign("←  HIGH EXCHANGER RETURN", 168, 16);
        Sign("SET A HEIGHT / BECOME MEDIUM / JUMP", 8, 0); Sign("DROP TO THE COOLANT CHANNEL  →", 108, 9);
    }
    // FOUNDRY 4: two transport spurs at different heights with an optical overhead detour.
    static void CrucibleTransit()
    {
        Surface(-4, 14, 0); Camp(0, 0, 8); Cart(15, 63, 39, 0);
        Surface(64, 95, 0); Camp(69, 0); var optics = Prism(77, 0); Link("EAST CRUCIBLE", 89, 0, 2, null, 0);
        var call = CallLift(93, 0, 10, false); OpticalLiftPower.Connect(optics, call, 0);
        Surface(48, 90, 9.8f); Camp(77, 9.8f);
        Gate(64, 9.8f, 1); Duct(49, 9.8f, 8); Link("OVERHEAD BYPASS", 50, 9.8f, 0);
        Surface(36, 47, 4); Camp(39, 4); Cart(46, 115, 81, 4).requireReturn = true;
        Surface(116, 158, 4); Camp(122, 4); Duct(133, 4, 9); Link("OUTLET REGULATOR", 149, 4, 0); Goal(155, 4);
        Sign("FIRST LINE → / OVERHEAD RETURN ← / SECOND LINE →", 6, 0);
    }
    // FOUNDRY 5: broad service sweep, rail crossing, optical interlock, then an elevated westward crown.
    static void ReactorShutdown()
    {
        Surface(-4, 47, 0); Camp(0, 0, 9); var echo = Echo(10, 0, 8); Duct(19, 0, 10);
        LinkedDoor(36, 0, () => echo.Powered,echo); Link("SERVICE OVERRIDE", 42, 0, 0);
        Cart(48, 102, 76, 0); Surface(103, 152, 0); Camp(109, 0, 8);
        var optics = Prism(120, 0); LinkedDoor(132, 0, () => optics.Complete,optics); Link("REACTOR OPTICS", 139, 0, 1); Gate(148, 0, 2);
        Surface(152, 158, 0); Fan(156, 0, true, 14.4f, 7.9f, 20); Surface(159, 189, 14);
        Camp(166, 14); Link("COOLING CROWN", 180, 14, 0);
        EnergyBridge(186, 14, 190, 212); Surface(212, 242, 14); Camp(219, 14);
        CallLift(239, 14, 7); Surface(82, 210, 20.8f); Surface(218, 236, 20.8f); Camp(226, 20.8f, 7);
        Sign("LIGHT BODY / CROSS THE BROKEN CROWN", 225, 20.8f);
        Gate(204, 20.8f, 1); Duct(175, 20.8f, 13); Link("CROWN RETURN", 154, 20.8f, 0);
        LockStation(140, 20.8f); Gate(123, 20.8f, 2); Rations(114, 20.8f);
        // Finish with a different task from the rail exchange: descend from the
        // crown, brake in free flight and enter a side vent before the ground stop.
        Surface(56,81,10); Rations(59,10,8); Fan(79,10,false,14,7,15); Surface(56,76.8f,15);
        Camp(71,15); Link("FINAL VENT",67,15,1); Goal(60,15);
        Sign("DESCEND / BRAKE INTO THE SIDE VENT",87,20.8f);
        Sign("RETURN TO AIRFLOW  →",64,10);
        Sign("SERVICE → / COOLING ↑ / CROWN ←", 4, 0);
    }

    // OBSERVATORY 2: an alternating left/right spring spiral followed by an external stepped descent.
    static void IceSpiral()
    {
        Surface(-4, 14, 0); Camp(0, 0, 7); Spring(11, 0);
        Surface(18, 43, 7); Camp(23, 7); Link("EAST LENS", 29, 7, 1); Spring(39, 7);
        Surface(-12, 23, 14); Camp(16, 14); Link("WEST LENS", 5, 14, 1); Spring(-7, 14);
        Surface(9, 49, 22); Camp(16, 22); Link("SUMMIT ALIGNMENT", 38, 22, 0);
        Surface(51, 60, 17); Surface(63, 73, 12); Camp(66, 12);
        Surface(76, 87, 6); Gate(82, 6, 1); Surface(90, 118, 0); Link("OUTER STAIR", 104, 0, 2); Goal(114, 0);
        Sign("SPRING EAST  →", 5, 0); Sign("←  SECOND SHELF", 34, 7); Sign("→  SUMMIT", 1, 14);
    }
    // OBSERVATORY 3: two separated balance crossings, lower eastward path and upper return over the chasm.
    static void GlacierBalance()
    {
        Surface(-4, 14, 0); Camp(0, 0, 8); Spring(10, 0); Surface(18, 39, 7);
        Camp(24, 7); BalanceLink("WEST BALANCE", 43, 7); Surface(47, 78, 7);
        Camp(53, 7); Gate(65, 7, 1); BalanceLink("EAST BALANCE", 82, 7, true); Surface(86, 112, 7);
        Sign("COUNTERWEIGHT LEFT / BALANCE FROM THE RIGHT", 77, 7);
        Camp(94, 7); Duct(98, 7, 9); Surface(112, 142, 0); Camp(119, 0);
        Link("GLACIER BASE", 133, 0, 2); Lift(145, 1, 17);
        Surface(64, 142, 17); Camp(130, 17); LockStation(115, 17); Gate(102, 17, 1);
        Duct(82, 17, 10); Link("SUMMIT RETURN", 72, 17, 0); Goal(67, 17);
        Sign("BALANCE BOTH GALLERIES / RETURN ABOVE", 27, 7);
    }
    // OBSERVATORY 4: ascent on the west, brake at a middle side port, then descend and climb the east tower.
    static void AuroraDescent()
    {
        Surface(-4, 23, 0); Camp(0, 0, 9); Fan(20, 0, true, 18.4f, 9.4f, 23);
        Surface(23, 62, 18); Camp(28, 18); Link("AURORA INTAKE", 45, 18, 0); LockStation(55, 18);
        Fan(66, 0, false, 20, 10, 24); Surface(68.2f, 106, 9);
        Camp(77, 9); Gate(89, 9, 1); Link("MIDDLE BRAKE", 101, 9, 1); Rations(73, 9, 8);
        Surface(64, 131, 0); Camp(79, 0); Duct(92, 0, 13); Link("LOW RETURN", 118, 0, 2);
        Spring(128, 0); Surface(136, 166, 8); Camp(143, 8); BalanceLink("EAST ALIGNMENT", 170, 8);
        Surface(174, 192, 8); Spring(187, 8); Surface(133, 177, 16);
        Camp(158, 16); Link("CROWN EXIT", 144, 16, 0); Goal(136, 16);
        Sign("RISE WEST / DESCEND THROUGH THE NEXT SHAFT", 29, 18);
        Sign("←  RETURN TO AIRFLOW / HEAVY TO DESCEND", 85, 9);
    }
    // OBSERVATORY 5: five unique sectors, no repeated mini-room; final descent leads outside.
    static void LastLight()
    {
        Surface(-4, 14, 0); Camp(0, 0, 10); Spring(10, 0); Surface(18, 42, 8);
        Camp(23, 8); Link("ENTRY ALIGNMENT", 31, 8, 0); BalanceLink("BALANCE GALLERY", 46, 8);
        Surface(50, 73, 8); Camp(56, 8); Cart(74, 122, 98, 8);
        Surface(123, 169, 8); Camp(130, 8, 8); var optics = Prism(141, 8); LinkedDoor(154, 8, () => optics.Complete,optics); Link("OPTICAL CROWN", 161, 8, 1);
        Surface(169, 175, 8); Fan(173, 8, true, 13.4f, 6.9f, 18); Surface(176, 208, 21); Camp(182, 21);
        LockStation(197, 21); Link("HIGH INTAKE", 203, 21, 0);
        Fan(212, 0, false, 25, 12, 29); Surface(214.2f, 245, 12); Camp(221, 12);
        Link("MIDDLE RELEASE", 238, 12, 1); Rations(218, 12, 8);
        Surface(209, 267, 0); Camp(226, 0); Link("FINAL GROUND LINK", 246, 0, 2);
        EnergyBridge(264, 0, 268, 291); Surface(291, 321, 0); Camp(297, 0);
        Duct(304, 0, 7); Goal(317, 0);
        Sign("FIVE SECTORS / ONE WAY OUT", 5, 0); Sign("→  OUTSIDE", 300, 0);
    }
}
