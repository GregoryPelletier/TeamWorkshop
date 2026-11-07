using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Editor only helper. Adds Tools > Dragon's Toll menu items.
public static class SkeletonSetupTool
{
    private const string skeletonFolder = "Assets/Kevin Iglesias/Skeleton Animations";
    private const string controllerPath = "Assets/Prefabs/Enemies/SkeletonEnemy.controller";

    [MenuItem("Tools/Dragon's Toll/Fit And Animate Skeleton")]
    public static void FitAndAnimateSkeleton()
    {
        GameObject skeletonObject = GameObject.Find("Skeleton");
        GameObject playerObject = GameObject.Find("Player");
        if (skeletonObject == null || playerObject == null)
        {
            Debug.LogError("Need a Skeleton and a Player in the open scene.");
            return;
        }

        Undo.RecordObject(skeletonObject.transform, "Fit Skeleton");

        // Stand on the same depth as the 2D world, facing the player.
        Vector3 playerPosition = playerObject.transform.position;
        Vector3 skeletonPosition = skeletonObject.transform.position;
        float faceAngle = playerPosition.x < skeletonPosition.x ? -90f : 90f;
        skeletonObject.transform.rotation = Quaternion.Euler(0f, faceAngle, 0f);
        skeletonObject.transform.localScale = Vector3.one;
        skeletonObject.transform.position = new Vector3(skeletonPosition.x, skeletonPosition.y, playerPosition.z);

        // Measure the skeleton by its bones, since skinned mesh bounds are padded and too big.
        Animator boneAnimator = skeletonObject.GetComponent<Animator>();
        // The player's collider matches its visible body; its sprite has lots of empty padding.
        Collider2D playerCollider = playerObject.GetComponent<Collider2D>();
        Bounds playerBounds = playerCollider != null ? playerCollider.bounds : GetBounds(playerObject);
        float skeletonHeight = MeasureBoneHeight(boneAnimator);
        float targetHeight = playerBounds.size.y * 1.2f;
        float scaleFactor = targetHeight / skeletonHeight;
        skeletonObject.transform.localScale = Vector3.one * scaleFactor;

        // Drop the skeleton's feet onto the ground below it.
        float footLevel = MeasureFootLevel(boneAnimator) - targetHeight * 0.04f;
        float groundLevel = playerBounds.min.y;
        Vector2 rayStart = new Vector2(skeletonObject.transform.position.x, footLevel + targetHeight + 2f);
        RaycastHit2D[] groundHits = Physics2D.RaycastAll(rayStart, Vector2.down, 100f);
        foreach (RaycastHit2D groundHit in groundHits)
        {
            if (groundHit.collider.gameObject != playerObject && !groundHit.collider.isTrigger)
            {
                groundLevel = groundHit.point.y;
                break;
            }
        }
        float footOffset = groundLevel - footLevel;
        skeletonObject.transform.position += new Vector3(0f, footOffset, 0f);

        Animator skeletonAnimator = skeletonObject.GetComponent<Animator>();
        if (skeletonAnimator != null)
        {
            Undo.RecordObject(skeletonAnimator, "Animate Skeleton");
            skeletonAnimator.runtimeAnimatorController = BuildController();
            skeletonAnimator.applyRootMotion = false;
            skeletonAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        EditorSceneManager.MarkSceneDirty(skeletonObject.scene);
        Selection.activeGameObject = skeletonObject;
        Debug.Log("Skeleton scaled to " + scaleFactor.ToString("0.###") + " and given SkeletonEnemy controller.");
    }

    [MenuItem("Tools/Dragon's Toll/Create Spell Prototype Scene")]
    public static void CreateSpellPrototypeScene()
    {
        string scenePath = "Assets/Prototypes/SpellSwitching/SpellPrototype.unity";
        if (System.IO.File.Exists(scenePath))
        {
            Debug.Log("SpellPrototype scene already exists.");
            return;
        }
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
        EditorSceneManager.SaveScene(newScene, scenePath);
        EditorSceneManager.CloseScene(newScene, true);
        AssetDatabase.Refresh();
        Debug.Log("Created " + scenePath);
    }

    [MenuItem("Tools/Dragon's Toll/Set Up Spell Manager")]
    public static void SetUpSpellManager()
    {
        // Remove the stray empty GameObject from Dragons Toll, if it is there.
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject rootObject in activeScene.GetRootGameObjects())
        {
            if (rootObject.name == "GameObject" && rootObject.GetComponents<Component>().Length == 1 && rootObject.transform.childCount == 0)
            {
                Object.DestroyImmediate(rootObject);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("Removed empty GameObject from " + activeScene.name);
                break;
            }
        }
        EditorSceneManager.SaveOpenScenes();

        Scene prototypeScene = EditorSceneManager.OpenScene("Assets/Prototypes/SpellSwitching/SpellPrototype.unity", OpenSceneMode.Single);
        GameObject managerObject = GameObject.Find("SpellManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("SpellManager");
        }
        SpellInput spellInput = managerObject.GetComponent<SpellInput>();
        if (spellInput == null)
        {
            spellInput = managerObject.AddComponent<SpellInput>();
        }

        InputActionReference[] actionReferences = AssetDatabase
            .LoadAllAssetsAtPath("Assets/Prototypes/SpellSwitching/SpellControls.inputactions")
            .OfType<InputActionReference>().ToArray();
        SerializedObject serializedInput = new SerializedObject(spellInput);
        AssignAction(serializedInput, actionReferences, "castAction", "Cast");
        AssignAction(serializedInput, actionReferences, "spellOneAction", "SelectSpell1");
        AssignAction(serializedInput, actionReferences, "spellTwoAction", "SelectSpell2");
        AssignAction(serializedInput, actionReferences, "spellThreeAction", "SelectSpell3");
        AssignAction(serializedInput, actionReferences, "scrollAction", "ScrollSpell");
        serializedInput.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(prototypeScene);
        Selection.activeGameObject = managerObject;
        Debug.Log("SpellManager set up in SpellPrototype with all five actions.");
    }

    private static void AssignAction(SerializedObject serializedInput, InputActionReference[] actionReferences, string fieldName, string actionName)
    {
        InputActionReference matchingReference = actionReferences.First(foundReference => foundReference.action.name == actionName);
        serializedInput.FindProperty(fieldName).objectReferenceValue = matchingReference;
    }

    private static float MeasureFootLevel(Animator boneAnimator)
    {
        Transform leftFoot = boneAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = boneAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        return Mathf.Min(leftFoot.position.y, rightFoot.position.y);
    }

    private static float MeasureBoneHeight(Animator boneAnimator)
    {
        Transform headBone = boneAnimator.GetBoneTransform(HumanBodyBones.Head);
        // The head bone sits at the neck line, so add room for the skull and feet.
        return (headBone.position.y - MeasureFootLevel(boneAnimator)) * 1.18f;
    }

    private static Bounds GetBounds(GameObject rootObject)
    {
        Renderer[] childRenderers = rootObject.GetComponentsInChildren<Renderer>();
        Bounds totalBounds = childRenderers[0].bounds;
        foreach (Renderer childRenderer in childRenderers)
        {
            totalBounds.Encapsulate(childRenderer.bounds);
        }
        return totalBounds;
    }

    private static AnimationClip FindClip(string fileName, string clipName)
    {
        string assetPath = AssetDatabase.FindAssets(fileName + " t:Model", new[] { skeletonFolder })
            .Select(AssetDatabase.GUIDToAssetPath).First();
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>()
            .Where(foundClip => !foundClip.name.StartsWith("__preview__"))
            .First(foundClip => foundClip.name.Contains(clipName));
    }

    private static AnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController enemyController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        enemyController.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
        enemyController.AddParameter("attack", AnimatorControllerParameterType.Trigger);
        enemyController.AddParameter("hit", AnimatorControllerParameterType.Trigger);
        enemyController.AddParameter("die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine baseMachine = enemyController.layers[0].stateMachine;
        AnimatorState idleState = baseMachine.AddState("Idle", new Vector3(300, 0));
        idleState.motion = FindClip("Skeleton@Idle01", "Skeleton@Idle01");
        AnimatorState walkState = baseMachine.AddState("Walk", new Vector3(300, 120));
        walkState.motion = FindClip("Skeleton@Walk01", "Forward");
        AnimatorState attackState = baseMachine.AddState("Attack", new Vector3(560, 0));
        attackState.motion = FindClip("Skeleton@Attack01", "Attack01");
        AnimatorState hitState = baseMachine.AddState("Hit", new Vector3(560, 120));
        hitState.motion = FindClip("Skeleton@Damage01", "Damage01");
        AnimatorState deathState = baseMachine.AddState("Death", new Vector3(560, 240));
        deathState.motion = FindClip("Skeleton@Death01", "Death01_A");
        baseMachine.defaultState = idleState;

        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");

        AddReturnTrigger(baseMachine, attackState, idleState, "attack");
        AddReturnTrigger(baseMachine, hitState, idleState, "hit");

        AnimatorStateTransition toDeath = baseMachine.AddAnyStateTransition(deathState);
        toDeath.hasExitTime = false;
        toDeath.duration = 0.1f;
        toDeath.canTransitionToSelf = false;
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "die");

        AssetDatabase.SaveAssets();
        return enemyController;
    }

    private static void AddReturnTrigger(AnimatorStateMachine baseMachine, AnimatorState actionState, AnimatorState idleState, string triggerName)
    {
        AnimatorStateTransition toAction = baseMachine.AddAnyStateTransition(actionState);
        toAction.hasExitTime = false;
        toAction.duration = 0.1f;
        toAction.canTransitionToSelf = false;
        toAction.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        AnimatorStateTransition backToIdle = actionState.AddTransition(idleState);
        backToIdle.hasExitTime = true;
        backToIdle.exitTime = 0.9f;
        backToIdle.duration = 0.15f;
    }
}
