using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Automacao das partes mecanicas dos tutoriais 03 e 04 e das correcoes
/// encontradas na revisao dos tutoriais 01 a 03.
///
/// Cada item de menu e independente, idempotente e registrado no Undo:
/// se algo sair errado, Ctrl+Z desfaz.
///
/// O que NAO da para automatizar (precisa ser feito na mao, no Editor):
///   - importar o Barn Door Asset Pack da Asset Store (precisa da conta Unity);
///   - posicionar visualmente a porta de correr, as paredes e os BoxColliders.
/// </summary>
public static class SetupTrabalhoVR
{
    private const string MENU = "Tools/Trabalho VR/";

    // ---------------------------------------------------------------- helpers

    private static readonly List<string> _log = new List<string>();

    private static void Ok(string msg) { _log.Add("  OK   " + msg); }
    private static void Skip(string msg) { _log.Add("  --   " + msg + " (ja estava certo)"); }
    private static void Warn(string msg) { _log.Add("  !!   " + msg); }

    private static void Flush(string titulo)
    {
        Debug.Log("[" + titulo + "]\n" + string.Join("\n", _log));
        _log.Clear();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    /// <summary>Procura um GameObject pelo nome em toda a cena, inclusive inativos.</summary>
    private static GameObject Find(params string[] nomes)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var n in nomes)
                {
                    if (t.name == n)
                        return t.gameObject;
                }
            }
        }
        return null;
    }

    private static IEnumerable<GameObject> FindAll(string nome)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nome)
                    yield return t.gameObject;
            }
        }
    }

    private static void SetInteractionLayers(Object comp, string layerName)
    {
        var so = new SerializedObject(comp);
        var p = so.FindProperty("m_InteractionLayers");
        if (p == null) { Warn(comp.name + ": sem m_InteractionLayers"); return; }
        var bits = p.FindPropertyRelative("m_Bits");
        int mask = InteractionLayerMask.GetMask(layerName);
        try { bits.uintValue = (uint)mask; }
        catch { bits.intValue = mask; }
        so.ApplyModifiedProperties();
    }

    private static uint GetInteractionLayers(Object comp)
    {
        var so = new SerializedObject(comp);
        var bits = so.FindProperty("m_InteractionLayers").FindPropertyRelative("m_Bits");
        try { return bits.uintValue; } catch { return (uint)bits.intValue; }
    }

    private static void SetIntProp(Object comp, string path, int value)
    {
        var so = new SerializedObject(comp);
        var p = so.FindProperty(path);
        if (p == null) { Warn(comp.name + ": propriedade " + path + " nao encontrada"); return; }
        p.intValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetColliderList(Object comp, Collider col)
    {
        var so = new SerializedObject(comp);
        var list = so.FindProperty("m_Colliders");
        if (list == null) { Warn(comp.name + ": sem m_Colliders"); return; }
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = col;
        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------ 0 - diagnostico da cena

    [MenuItem(MENU + "0 - Diagnostico da cena", false, 0)]
    public static void Diagnostico()
    {
        var sb = new List<string>();

        var espelho = Find("Espelho");
        if (espelho == null) sb.Add("  !! Espelho (porta) nao encontrado");
        else
        {
            sb.Add("  Espelho.layer = " + LayerMask.LayerToName(espelho.layer) +
                   (espelho.layer == LayerMask.NameToLayer("Obstaculos") ? "  OK" : "  <-- deveria ser Obstaculos"));
            var hj = espelho.GetComponent<HingeJoint>();
            if (hj == null) sb.Add("  !! Espelho sem HingeJoint");
            else
            {
                sb.Add("  HingeJoint.anchor = " + hj.anchor + "  (esperado (0, 0.05, -0.45))");
                sb.Add("  HingeJoint.axis   = " + hj.axis + "  (esperado (0, 1, 0))");
                sb.Add("  HingeJoint.limits = " + hj.limits.min + " .. " + hj.limits.max + "  (esperado -120 .. 1)");
            }
            sb.Add("  Espelho tem EventosPorta? " + (espelho.GetComponent<EventosPorta>() != null));
        }

        foreach (var rack in FindAll("Test_tube_rack").Concat(FindAll("Test_tube_rack (1)")))
        {
            if (rack.GetComponent<Rigidbody>() != null || rack.GetComponent<TestTubeLock>() != null)
                sb.Add("  !! " + rack.name + " tem Rigidbody/TestTubeLock indevidos (o rack vai cair no Play)");
        }

        var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        if (origin != null)
        {
            foreach (var poke in origin.GetComponentsInChildren<XRPokeInteractor>(true))
            {
                uint bits = GetInteractionLayers(poke);
                string path = poke.transform.parent != null ? poke.transform.parent.name + "/" + poke.name : poke.name;
                sb.Add("  Poke " + path + " layers=" + bits +
                       (bits == (uint)InteractionLayerMask.GetMask("PokeOnly") ? "  OK" : "  <-- deveria ser so PokeOnly"));
            }
            var so = new SerializedObject(origin);
            var mode = so.FindProperty("m_RequestedTrackingOriginMode");
            if (mode != null)
                sb.Add("  XROrigin.TrackingOriginMode = " + mode.enumDisplayNames[mode.enumValueIndex] + "  (Oculus pede Device)");
        }

        foreach (var area in Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(area);
            int n = so.FindProperty("m_Colliders").arraySize;
            sb.Add("  " + area.name + ": TeleportationArea enabled=" + area.enabled + ", colliders atribuidos=" + n);
        }

        bool mainNaBuild = EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("MainScene.unity"));
        sb.Add("  MainScene na Build Scene List? " + mainNaBuild +
               "   (lista atual: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileName(s.path))) + ")");

        sb.Add("  Barn Door Asset Pack importado? " +
               (AssetDatabase.IsValidFolder("Assets/BarnDoorAssetPack") ||
                AssetDatabase.FindAssets("BarnDoor").Length > 0));

        Debug.Log("[Diagnostico Trabalho VR]\n" + string.Join("\n", sb));
    }

    // ---------------------------------- 1 - correcoes dos tutoriais 01 a 03

    [MenuItem(MENU + "1 - Corrigir erros encontrados (tutoriais 01-03)", false, 20)]
    public static void CorrigirErros()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Trabalho VR: corrigir erros 01-03");
        int group = Undo.GetCurrentGroup();

        // 1.1 - racks nao podem ter Rigidbody nem TestTubeLock
        foreach (var rack in FindAll("Test_tube_rack").Concat(FindAll("Test_tube_rack (1)")).ToList())
        {
            var lockComp = rack.GetComponent<TestTubeLock>();
            var rb = rack.GetComponent<Rigidbody>();
            bool mexeu = lockComp != null || rb != null;

            // a ordem importa: TestTubeLock exige Rigidbody (RequireComponent nao,
            // mas o Start() dele usa), entao tira o script primeiro
            if (lockComp != null) { Undo.DestroyObjectImmediate(lockComp); Ok(rack.name + ": TestTubeLock removido"); }
            if (rb != null) { Undo.DestroyObjectImmediate(rb); Ok(rack.name + ": Rigidbody removido (o rack nao pode cair)"); }

            if (!mexeu) Skip(rack.name);
        }

        // 1.2 - Espelho da porta no layer Obstaculos (so ele, nao os filhos)
        int obstaculos = LayerMask.NameToLayer("Obstaculos");
        var espelho = Find("Espelho");
        if (espelho == null) Warn("Espelho (porta) nao encontrado");
        else if (espelho.layer != obstaculos)
        {
            Undo.RecordObject(espelho, "layer");
            espelho.layer = obstaculos;
            Ok("Espelho: layer -> Obstaculos (evita colidir com os caixilhos)");
        }
        else Skip("Espelho.layer");

        // 1.3 - HingeJoint: anchor no eixo da porta, axis no Y
        if (espelho != null)
        {
            var hj = espelho.GetComponent<HingeJoint>();
            if (hj == null) Warn("Espelho sem HingeJoint");
            else
            {
                var anchorEsperado = new Vector3(0f, 0.05f, -0.45f);
                var axisEsperado = new Vector3(0f, 1f, 0f);
                if (hj.anchor != anchorEsperado || hj.axis != axisEsperado)
                {
                    Undo.RecordObject(hj, "hinge");
                    hj.anchor = anchorEsperado;
                    hj.axis = axisEsperado;
                    Ok("HingeJoint: anchor=(0, 0.05, -0.45), axis=(0, 1, 0) — a porta girava no eixo X");
                }
                else Skip("HingeJoint anchor/axis");

                if (!hj.useLimits || hj.limits.min != -120f || hj.limits.max != 1f)
                {
                    Undo.RecordObject(hj, "hinge limits");
                    hj.useLimits = true;
                    var l = hj.limits; l.min = -120f; l.max = 1f; hj.limits = l;
                    Ok("HingeJoint: Use Limits ligado, Min=-120 Max=1");
                }
                else Skip("HingeJoint limits");
            }
        }

        // 1.4 - Poke Interactors devem enxergar somente a interaction layer PokeOnly
        var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        if (origin == null) Warn("XR Origin nao encontrado");
        else
        {
            uint esperado = (uint)InteractionLayerMask.GetMask("PokeOnly");
            foreach (var poke in origin.GetComponentsInChildren<XRPokeInteractor>(true))
            {
                if (GetInteractionLayers(poke) != esperado)
                {
                    Undo.RecordObject(poke, "interaction layers");
                    SetInteractionLayers(poke, "PokeOnly");
                    string pai = poke.transform.parent != null ? poke.transform.parent.name : "?";
                    Ok(pai + "/Poke Interactor: Interaction Layer Mask -> so PokeOnly");
                }
                else Skip((poke.transform.parent != null ? poke.transform.parent.name : "?") + "/Poke Interactor");
            }
        }

        // 1.5 - TeleportationArea deve ter o MeshCollider atribuido
        foreach (var area in Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(area);
            if (so.FindProperty("m_Colliders").arraySize == 0)
            {
                var col = area.GetComponent<MeshCollider>() ?? area.GetComponent<Collider>();
                if (col == null) { Warn(area.name + ": sem Collider"); continue; }
                Undo.RecordObject(area, "colliders");
                SetColliderList(area, col);
                Ok(area.name + ": MeshCollider atribuido em Teleportation Area > Colliders");
            }
            else Skip(area.name + " colliders");
        }

        Undo.CollapseUndoOperations(group);
        Flush("Correcoes tutoriais 01-03");
    }

    // ------------------------------- 2 - tutorial 03 sec.3 + tutorial 04 sec.2.3

    [MenuItem(MENU + "2 - Porta de batente: grab no trinco + EventosPorta", false, 40)]
    public static void PortaDeBatente()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Trabalho VR: porta de batente");
        int group = Undo.GetCurrentGroup();

        var espelho = Find("Espelho");
        if (espelho == null) { Warn("Espelho nao encontrado"); Flush("Porta de batente"); return; }

        var trinco = Find("Trinco1", "Trinco01");
        if (trinco == null) { Warn("Trinco1 nao encontrado"); Flush("Porta de batente"); return; }

        // 2.1 - o grab sai do Espelho e vai para o Trinco (tutorial 04, sec. 2.3)
        var grabEspelho = espelho.GetComponent<XRGrabInteractable>();
        if (grabEspelho != null)
        {
            Undo.DestroyObjectImmediate(grabEspelho);
            Ok("Espelho: XR Grab Interactable removido");
        }
        else Skip("Espelho ja estava sem XR Grab Interactable");

        var grabTrinco = trinco.GetComponent<XRGrabInteractable>();
        if (grabTrinco == null)
        {
            grabTrinco = Undo.AddComponent<XRGrabInteractable>(trinco);
            Ok(trinco.name + ": XR Grab Interactable adicionado");
        }
        else Skip(trinco.name + " ja tinha XR Grab Interactable");

        SetIntProp(grabTrinco, "m_MovementType", 0); // 0 = Velocity Tracking
        Ok(trinco.name + ": Movement Type = Velocity Tracking");

        var rbTrinco = trinco.GetComponent<Rigidbody>(); // XRGrabInteractable ja cria
        if (rbTrinco == null) rbTrinco = Undo.AddComponent<Rigidbody>(trinco);
        Undo.RecordObject(rbTrinco, "rb");
        rbTrinco.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Ok(trinco.name + ": Collision Detection = Continuous Dynamic");

        var rbEspelho = espelho.GetComponent<Rigidbody>();
        if (rbEspelho == null) Warn("Espelho sem Rigidbody — o Fixed Joint precisa dele");
        else
        {
            var fj = trinco.GetComponent<FixedJoint>();
            if (fj == null) { fj = Undo.AddComponent<FixedJoint>(trinco); Ok(trinco.name + ": Fixed Joint adicionado"); }
            Undo.RecordObject(fj, "fj");
            fj.connectedBody = rbEspelho;
            Ok(trinco.name + ": Fixed Joint > Connected Body = Espelho");
        }

        // 2.2 - EventosPorta no Espelho, ligado a area de teleporte da sala 2
        var ev = espelho.GetComponent<EventosPorta>();
        if (ev == null) { ev = Undo.AddComponent<EventosPorta>(espelho); Ok("Espelho: EventosPorta adicionado"); }
        else Skip("Espelho ja tinha EventosPorta");

        var chao2 = Find("Chao2");
        if (chao2 == null) Warn("Chao2 nao encontrado — arraste a area da sala 2 no campo Teleporte");
        else
        {
            var area = chao2.GetComponent<TeleportationArea>();
            if (area == null) Warn("Chao2 sem Teleportation Area");
            else
            {
                Undo.RecordObject(ev, "teleporte");
                ev.teleporte = area;
                area.enabled = false; // fechado ate a porta abrir
                Ok("EventosPorta > Teleporte = Chao2 (Teleportation Area comeca desligada)");
            }
        }

        Warn("Falta ligar EventosPorta > Grab Porta no BarnDoor_nn depois de fazer a porta de correr (tutorial 04, sec. 2.4).");

        Undo.CollapseUndoOperations(group);
        Flush("Porta de batente");
    }

    // ---------------------------------------------- 3 - tutorial 04 sec.3 (UI)

    [MenuItem(MENU + "3 - Tela de vitoria (Canvas + ShowMessageNaArea no Chao3)", false, 60)]
    public static void TelaDeVitoria()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Trabalho VR: tela de vitoria");
        int group = Undo.GetCurrentGroup();

        var chao3 = Find("Chao3");
        if (chao3 == null) { Warn("Chao3 nao encontrado"); Flush("Tela de vitoria"); return; }

        // 3.1 - Canvas com o texto, desativado
        var canvasGO = Find("CanvasVitoria");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("CanvasVitoria", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "canvas");
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var textGO = new GameObject("Text (TMP)", typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(textGO, "texto");
            textGO.transform.SetParent(canvasGO.transform, false);

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Voce venceu !";
            tmp.fontSize = 72;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 200f);

            canvasGO.SetActive(false);
            Ok("CanvasVitoria criado com o texto 'Voce venceu !' (desativado)");
        }
        else Skip("CanvasVitoria ja existia");

        // 3.2 - ShowMessageNaArea no Chao3, com todas as referencias
        var show = chao3.GetComponent<ShowMessageNaArea>();
        if (show == null) { show = Undo.AddComponent<ShowMessageNaArea>(chao3); Ok("Chao3: ShowMessageNaArea adicionado"); }
        else Skip("Chao3 ja tinha ShowMessageNaArea");

        Undo.RecordObject(show, "refs");
        show.messageObject = canvasGO;

        var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        if (origin == null) Warn("XR Origin nao encontrado — preencha as referencias na mao");
        else
        {
            var camOffset = Find("Camera Offset");
            show.playerCamera = camOffset != null ? camOffset.transform : origin.Camera.transform;
            Ok("Player Camera = " + show.playerCamera.name);

            show.locomotionSystem = origin.GetComponentInChildren<LocomotionMediator>(true);
            Ok("Locomotion System = " + (show.locomotionSystem != null ? show.locomotionSystem.name : "NAO ENCONTRADO"));

            foreach (var lado in new[] { "Left Controller", "Right Controller" })
            {
                var ctrl = Find(lado);
                if (ctrl == null) { Warn(lado + " nao encontrado"); continue; }
                var ray = ctrl.GetComponentsInChildren<XRRayInteractor>(true)
                              .FirstOrDefault(r => r.name.Contains("Teleport"));
                if (ray == null) { Warn(lado + ": Teleport Interactor nao encontrado"); continue; }
                if (lado.StartsWith("Left")) show.leftRay = ray; else show.rightRay = ray;
                Ok(lado + " > Teleport Interactor ligado");
            }
        }
        EditorUtility.SetDirty(show);

        Warn("O Canvas esta em Screen Space - Overlay (como no tutorial). No Oculus ele nao aparece: " +
             "para o build, troque para World Space e posicione na frente da camera.");

        Undo.CollapseUndoOperations(group);
        Flush("Tela de vitoria");
    }

    // --------------------------------------------- 4 - tutorial 04 sec.4 (build)

    [MenuItem(MENU + "4 - Preparar build (MainScene na lista + Tracking Origin = Device)", false, 80)]
    public static void PrepararBuild()
    {
        var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        if (origin == null) Warn("XR Origin nao encontrado");
        else
        {
            var so = new SerializedObject(origin);
            var mode = so.FindProperty("m_RequestedTrackingOriginMode");
            if (mode == null) Warn("m_RequestedTrackingOriginMode nao encontrado");
            else if (mode.intValue != 1)
            {
                Undo.RecordObject(origin, "tracking origin");
                mode.intValue = 1; // Device
                so.ApplyModifiedProperties();
                Ok("XR Origin: Tracking Origin Mode = Device (senao o personagem fica rente ao chao no Oculus)");
            }
            else Skip("Tracking Origin Mode");
        }

        const string main = "Assets/MainScene.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(main) == null)
            Warn(main + " nao encontrada");
        else
        {
            var atual = EditorBuildSettings.scenes;
            bool jaOk = atual.Length == 1 && atual[0].enabled && atual[0].path == main;
            if (!jaOk)
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(main, true) };
                Ok("Build Scene List = somente " + main + "  (estava: " +
                    string.Join(", ", atual.Select(s => System.IO.Path.GetFileName(s.path))) + ")");
            }
            else Skip("Build Scene List");
        }

        Flush("Preparar build");
    }
}
