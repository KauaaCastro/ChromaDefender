#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gera o Animator Controller do Player com TODOS os estados de golpes e movimentações.
/// O PlayerBehavior decide via código o que tocar, garantindo máxima fluidez e sem bugs de transição.
/// </summary>
[InitializeOnLoad]
public static class PlayerAnimatorBuilder
{
    private const string ClipsFolder = "Assets/2D Pixel Art Black Knight/Animations/Black_Knight/";
    private const string ControllerFolder = "Assets/Animations";
    private const string ControllerPath = ControllerFolder + "/Player.controller";

    // Versão do Animator para forçar atualização automática na compilação
    private const string BuildVersion = "PlayerAnim_v5_FullCombatAndMovement";

    static PlayerAnimatorBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing == null || System.Array.IndexOf(AssetDatabase.GetLabels(existing), BuildVersion) < 0)
            {
                BuildController();
            }
        };
    }

    [MenuItem("Chroma Defender/Criar Boneco de Treino (Alvo de Dano)")]
    public static void CreateTrainingDummyMenu()
    {
        CreateTrainingDummy();
    }

    public static GameObject CreateTrainingDummy()
    {
        var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Training Dummy.prefab");
        GameObject dummy = null;
        if (dummyPrefab != null)
        {
            dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
            dummy.name = "TrainingDummy";
        }
        else
        {
            dummy = new GameObject("TrainingDummy");
            var sr = dummy.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
        }

        dummy.transform.position = new Vector3(2.5f, 0f, 0f);

        var srComp = dummy.GetComponent<SpriteRenderer>();
        if (srComp != null) srComp.sortingOrder = 5;

        var col = dummy.GetComponent<BoxCollider2D>();
        if (col == null) col = dummy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 1.8f);
        col.offset = new Vector2(0f, 0.9f);

        var rb = dummy.GetComponent<Rigidbody2D>();
        if (rb == null) rb = dummy.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.mass = 3.5f;
        rb.linearDamping = 14f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var training = dummy.GetComponent<TrainingDummy>();
        if (training == null) training = dummy.AddComponent<TrainingDummy>();

        Selection.activeGameObject = dummy;
        Undo.RegisterCreatedObjectUndo(dummy, "Criar Boneco de Treino");
        Debug.Log("<color=green>[TrainingDummy]</color> Boneco de Treino pronto com visual autêntico e física de combo!");
        return dummy;
    }

    [MenuItem("Chroma Defender/Criar Animator do Player")]
    public static void BuildControllerMenu()
    {
        BuildController();
    }

    [MenuItem("Chroma Defender/Criar Prefab de Impacto (Explosao)")]
    public static void CreateImpactPrefabMenu()
    {
        CreateImpactPrefab();
    }

    public static GameObject CreateImpactPrefab()
    {
        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string prefabPath = folder + "/ImpactEffect.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        GameObject temp = new GameObject("ImpactEffect");
        var sr = temp.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10; // Fica na frente do cenário e personagens

        var impact = temp.AddComponent<ImpactEffect>();

        // Carrega os 5 frames de explosão da spritesheet (Black_Knight_All_Frame_With_Border_257 a 261)
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/2D Pixel Art Black Knight/Sprites/Black_Knight_All_Frame_With_Border.png");
        var framesList = new System.Collections.Generic.List<Sprite>();
        foreach (var obj in sprites)
        {
            if (obj is Sprite s && s.name.StartsWith("Black_Knight_All_Frame_With_Border_25"))
            {
                if (s.name == "Black_Knight_All_Frame_With_Border_257" ||
                    s.name == "Black_Knight_All_Frame_With_Border_258" ||
                    s.name == "Black_Knight_All_Frame_With_Border_259" ||
                    s.name == "Black_Knight_All_Frame_With_Border_260" ||
                    s.name == "Black_Knight_All_Frame_With_Border_261")
                {
                    framesList.Add(s);
                }
            }
        }

        // Ordena por nome
        framesList.Sort((a, b) => string.Compare(a.name, b.name));

        var so = new SerializedObject(impact);
        var framesProp = so.FindProperty("animationFrames");
        framesProp.arraySize = framesList.Count;
        for (int i = 0; i < framesList.Count; i++)
        {
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = framesList[i];
        }
        so.FindProperty("frameRate").floatValue = 0.05f;
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();
        Debug.Log("ImpactEffect prefab criado com sucesso em " + prefabPath);
        return prefab;
    }

    private static AnimationClip Clip(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipsFolder + name + ".anim");
        if (clip == null)
        {
            Debug.LogError("PlayerAnimatorBuilder: clip não encontrado: " + ClipsFolder + name + ".anim");
        }
        return clip;
    }

    private static void BuildController()
    {
        if (!AssetDatabase.IsValidFolder(ControllerFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            for (int i = controller.parameters.Length - 1; i >= 0; i--)
            {
                controller.RemoveParameter(i);
            }

            var oldSm = controller.layers[0].stateMachine;
            foreach (var child in oldSm.states)
            {
                oldSm.RemoveState(child.state);
            }
            foreach (var any in oldSm.anyStateTransitions)
            {
                oldSm.RemoveAnyStateTransition(any);
            }
        }

        var sm = controller.layers[0].stateMachine;

        // ---------- Movimentação Básica ----------
        var idle = AddState(sm, "Idle", "BK_unarmed_idle", new Vector3(250, 0));
        AddState(sm, "Run", "BK_unarmed_run", new Vector3(250, 60));
        AddState(sm, "Jump", "BK_unarmed_jump", new Vector3(250, 120));
        AddState(sm, "Fall", "BK_unarmed_midair", new Vector3(250, 180));
        AddState(sm, "Land", "BK_unarmed_land", new Vector3(250, 240));
        AddState(sm, "Roll", "BK_unarmed_roll", new Vector3(250, 300));

        // ---------- Quedas e Pulos Avançados ----------
        AddState(sm, "FallLight", "BK_unarmed_fall_light", new Vector3(250, 360));
        AddState(sm, "FallHeavy", "BK_unarmed_fall_heavy", new Vector3(250, 420));

        // ---------- Ataques Leves (Combo de 1 a 4) ----------
        AddState(sm, "Attack1", "BK_attack_1", new Vector3(500, 0));
        AddState(sm, "Attack2", "BK_attack_2", new Vector3(500, 60));
        AddState(sm, "Attack3", "BK_attack_3", new Vector3(500, 120));
        AddState(sm, "Attack4", "BK_attack_4", new Vector3(500, 180));

        // ---------- Ataques Pesados (Cadeia de 1 a 3) ----------
        AddState(sm, "Heavy1", "BK_heavy_attack_1", new Vector3(500, 240));
        AddState(sm, "Heavy2", "BK_heavy_attack_2", new Vector3(500, 300));
        AddState(sm, "Heavy3", "BK_heavy_attack_3", new Vector3(500, 360));

        // ---------- Ataque Supremo / Especial ----------
        AddState(sm, "WeaponArt", "BK_weapon_art", new Vector3(500, 420));

        // ---------- Utilidades (Defesa / Buff / Dano / Morte) ----------
        AddState(sm, "ParryStart", "BK_parry_start", new Vector3(750, 0));
        AddState(sm, "Parry", "BK_parry", new Vector3(750, 60));
        AddState(sm, "ParryMiss", "BK_parry_miss", new Vector3(750, 120));
        AddState(sm, "Buff", "BK_unarmed_buff", new Vector3(750, 180));
        AddState(sm, "Hurt", "BK_unarmed_hurt", new Vector3(750, 240));
        AddState(sm, "Death", "BK_death", new Vector3(750, 300));

        sm.defaultState = idle;

        AssetDatabase.SetLabels(controller, new[] { BuildVersion });
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PlayerAnimatorBuilder: Animator atualizado com todos os estados em " + ControllerPath);
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, string clipName, Vector3 position)
    {
        var state = sm.AddState(name, position);
        state.motion = Clip(clipName);
        state.writeDefaultValues = true;
        return state;
    }

    [MenuItem("Chroma Defender/Preparar Player da Cena")]
    public static void PreparePlayerInScene()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            BuildController();
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        }

        var players = Object.FindObjectsByType<PlayerBehavior>(FindObjectsSortMode.None);
        if (players.Length == 0)
        {
            Debug.LogWarning("Nenhum PlayerBehavior encontrado na cena atual.");
            return;
        }

        foreach (var player in players)
        {
            var go = player.gameObject;

            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = Undo.AddComponent<Animator>(go);
            Undo.RecordObject(animator, "Atribuir Animator Controller");
            animator.runtimeAnimatorController = controller;

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = Undo.AddComponent<Rigidbody2D>(go);
            Undo.RecordObject(rb, "Configurar Rigidbody2D");
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var sr = go.GetComponentInChildren<SpriteRenderer>();

            if (go.GetComponent<Collider2D>() == null)
            {
                var box = Undo.AddComponent<BoxCollider2D>(go);
                if (sr != null)
                {
                    box.size = new Vector2(sr.bounds.size.x * 0.3f, sr.bounds.size.y * 0.8f);
                    box.offset = (Vector2)(sr.bounds.center - go.transform.position);
                }
            }

            var checker = go.GetComponent<IsGroundedChecker>();
            if (checker == null) checker = Undo.AddComponent<IsGroundedChecker>(go);

            // Vincula o prefab de impacto na explosão do ataque especial
            var impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ImpactEffect.prefab");
            if (impactPrefab == null) impactPrefab = CreateImpactPrefab();
            var playerSo = new SerializedObject(player);
            var effectProp = playerSo.FindProperty("impactEffectPrefab");
            if (effectProp != null && effectProp.objectReferenceValue == null)
            {
                effectProp.objectReferenceValue = impactPrefab;
            }

            // Vincula os VFX dos cortes de fogo e gelo
            var fireSlash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Eric VFX Studio/Free Game VFX/Prefab/FX_Orange_Slash_1.prefab");
            var iceSlash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFXFantasyPackDEMO/Content/URP/Prefabs/Slash/FX_SlashLight_Blue.prefab");

            var fireProp = playerSo.FindProperty("fireSlashPrefab");
            if (fireProp != null && fireProp.objectReferenceValue == null)
            {
                fireProp.objectReferenceValue = fireSlash;
            }

            var iceProp = playerSo.FindProperty("iceSlashPrefab");
            if (iceProp != null && iceProp.objectReferenceValue == null)
            {
                iceProp.objectReferenceValue = iceSlash;
            }

            // Remove quaisquer auras antigas para manter o personagem 100% limpo
            Transform oldIce = go.transform.Find("IceAura");
            if (oldIce != null) Undo.DestroyObjectImmediate(oldIce.gameObject);
            Transform oldFire = go.transform.Find("FireAura");
            if (oldFire != null) Undo.DestroyObjectImmediate(oldFire.gameObject);

            var iceAuraProp = playerSo.FindProperty("iceAuraObject");
            if (iceAuraProp != null) iceAuraProp.objectReferenceValue = null;

            // Ajusta a hitbox de combate para garantir alcance nos combos
            var hitboxProp = playerSo.FindProperty("attackHitboxSize");
            if (hitboxProp != null)
            {
                hitboxProp.vector2Value = new Vector2(1.6f, 1.2f);
            }
            var hitboxOffsetProp = playerSo.FindProperty("attackHitboxOffset");
            if (hitboxOffsetProp != null)
            {
                hitboxOffsetProp.vector2Value = new Vector2(0.85f, 0.1f);
            }

            // Garante valores padrão para configuração de danos caso estejam zerados
            var d1Prop = playerSo.FindProperty("light1Damage");
            if (d1Prop != null && d1Prop.intValue == 0)
            {
                d1Prop.intValue = 20;
                var p2 = playerSo.FindProperty("light2Damage"); if (p2 != null) p2.intValue = 25;
                var p3 = playerSo.FindProperty("light3Damage"); if (p3 != null) p3.intValue = 35;
                var p4 = playerSo.FindProperty("light4Damage"); if (p4 != null) p4.intValue = 50;
                var h1 = playerSo.FindProperty("heavy1Damage"); if (h1 != null) h1.intValue = 50;
                var h2 = playerSo.FindProperty("heavy2Damage"); if (h2 != null) h2.intValue = 70;
                var h3 = playerSo.FindProperty("heavy3Damage"); if (h3 != null) h3.intValue = 95;
                var sf = playerSo.FindProperty("specialFlurryDamage"); if (sf != null) sf.intValue = 20;
                var sfin = playerSo.FindProperty("specialFinisherDamage"); if (sfin != null) sfin.intValue = 85;
            }

            // Vincula o shader de coloração seletiva da espada para o modo de gelo
            var tintShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/SpriteElementTint.shader");
            var tintProp = playerSo.FindProperty("swordTintShader");
            if (tintProp != null && tintProp.objectReferenceValue == null && tintShader != null)
            {
                tintProp.objectReferenceValue = tintShader;
            }

            playerSo.ApplyModifiedProperties();

            var so = new SerializedObject(checker);
            var posProp = so.FindProperty("checkerPosition");
            if (posProp.objectReferenceValue == null)
            {
                var child = new GameObject("GroundCheck");
                Undo.RegisterCreatedObjectUndo(child, "Criar GroundCheck");
                child.transform.SetParent(go.transform, false);
                var bottom = sr != null ? sr.bounds.min.y : go.transform.position.y;
                child.transform.position = new Vector3(go.transform.position.x, bottom, go.transform.position.z);
                posProp.objectReferenceValue = child.transform;
            }

            var sizeProp = so.FindProperty("checkerSize");
            if (sizeProp.vector2Value == Vector2.zero)
            {
                sizeProp.vector2Value = new Vector2(0.4f, 0.1f);
            }

            var layerProp = so.FindProperty("groundLayer");
            if (layerProp.intValue == 0)
            {
                layerProp.intValue = LayerMask.GetMask("Ground");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Player preparado com sucesso!");
    }
}
#endif
