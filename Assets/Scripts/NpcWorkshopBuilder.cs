using UnityEngine;

public static class NpcWorkshopBuilder
{
    struct NpcSetup
    {
        public string objectName;
        public string displayName;
        public string persona;
        public Color color;
        public Vector3 position;
    }

    static readonly NpcSetup[] DefaultNpcs =
    {
        new NpcSetup
        {
            objectName = "NPC_Mira",
            displayName = "Mira",
            persona = "You are Mira, a cheerful village merchant. You love haggling, gossip, and recommending odd trinkets. Stay warm, playful, and practical.",
            color = new Color(0.85f, 0.45f, 0.2f),
            position = new Vector3(0f, 1f, 5f)
        },
        new NpcSetup
        {
            objectName = "NPC_Grunk",
            displayName = "Grunk",
            persona = "You are Grunk, a gruff town guard. You speak in short, blunt sentences, distrust strangers, and care about rules and safety.",
            color = new Color(0.45f, 0.48f, 0.52f),
            position = new Vector3(-6f, 1f, 2f)
        },
        new NpcSetup
        {
            objectName = "NPC_Luna",
            displayName = "Luna",
            persona = "You are Luna, a dreamy scholar who reads old books in the plaza. You answer with poetic language, metaphors, and curious questions.",
            color = new Color(0.55f, 0.35f, 0.85f),
            position = new Vector3(6f, 1f, 2f)
        }
    };

    public static void BuildIfMissing()
    {
        CreateGroundIfMissing();
        var player = CreatePlayerIfMissing();
        MigrateLegacyNpc();
        EnsureNpcsExist();
        SetupCamera(player);
    }

    static void CreateGroundIfMissing()
    {
        if (GameObject.Find("Ground") != null)
            return;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        SetColor(ground, new Color(0.35f, 0.55f, 0.35f));
    }

    static GameObject CreatePlayerIfMissing()
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
            return existing;

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, -4f);
        SetColor(player, new Color(0.2f, 0.55f, 0.95f));

        Object.Destroy(player.GetComponent<CapsuleCollider>());

        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = Vector3.zero;

        player.AddComponent<PlayerMovement>();
        player.AddComponent<PlayerNpcInteraction>();

        return player;
    }

    static void MigrateLegacyNpc()
    {
        var legacy = GameObject.Find("NPC");
        if (legacy == null)
            legacy = GameObject.Find("GameObject");

        if (legacy == null || legacy.name == "NPC_Mira")
            return;

        legacy.name = "NPC_Mira";
        EnsureNpcVisual(legacy, DefaultNpcs[0].color);
    }

    static void EnsureNpcsExist()
    {
        NpcChat settingsSource = null;

        foreach (var setup in DefaultNpcs)
        {
            var npc = EnsureNpc(setup, ref settingsSource);
            if (npc == null)
                continue;

            var chat = npc.GetComponent<NpcChat>();
            if (chat != null && settingsSource == null)
                settingsSource = chat;
        }
    }

    static GameObject EnsureNpc(NpcSetup setup, ref NpcChat settingsSource)
    {
        var npc = GameObject.Find(setup.objectName);
        var isNew = npc == null;

        if (isNew)
        {
            npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = setup.objectName;
        }

        npc.transform.position = setup.position;
        EnsureNpcVisual(npc, setup.color);

        var chat = npc.GetComponent<NpcChat>() ?? npc.AddComponent<NpcChat>();

        if (isNew)
            chat.Configure(setup.displayName, setup.persona);

        if (settingsSource != null && settingsSource != chat)
            settingsSource.ShareSettingsWith(chat);
        else if (settingsSource == null)
            settingsSource = chat;

        return npc;
    }

    static void SetupCamera(GameObject player)
    {
        var camera = Camera.main;
        if (camera == null || player == null)
            return;

        var follow = camera.GetComponent<CameraFollow>() ?? camera.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);
        camera.transform.position = player.transform.position + new Vector3(0f, 6f, -8f);
        camera.transform.LookAt(player.transform.position + Vector3.up * 1.5f);
    }

    static void EnsureNpcVisual(GameObject npc, Color color)
    {
        var colorTarget = npc;
        var renderer = npc.GetComponent<MeshRenderer>();

        if (renderer == null)
        {
            var body = npc.transform.Find("Body");
            if (body != null)
            {
                renderer = body.GetComponent<MeshRenderer>();
                if (renderer != null)
                    colorTarget = body.gameObject;
            }
        }

        if (renderer == null)
        {
            var meshFilter = npc.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = npc.AddComponent<MeshFilter>();

            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");

            if (npc.GetComponent<MeshRenderer>() == null)
                npc.AddComponent<MeshRenderer>();

            if (npc.GetComponent<CapsuleCollider>() == null)
            {
                var collider = npc.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.5f;
                collider.center = Vector3.zero;
            }

            colorTarget = npc;
        }

        SetColor(colorTarget, color);
    }

    static void SetColor(GameObject target, Color color)
    {
        var renderer = target.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader);
        material.color = color;
        renderer.sharedMaterial = material;
    }
}
