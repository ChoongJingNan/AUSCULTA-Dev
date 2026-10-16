using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.Audio;
public class EntitySpot : MonoBehaviour
{
    public AudioMixerGroup sfxMixerGroup; 
    [Header("References")]
    public EncounterManager encounterManager;
    public GameObject entityMeshObject; 
    public Camera playerCamera;         
    public Image vignetteImage;         

    [Header("Chase 3D Audio Settings (Chase Entity Only)")]
    [Tooltip("Audio source attached to the entity or mesh object.")]
    public AudioSource chaseAudioSource;
    [Tooltip("Audio clip for footsteps/screams that loop during the chase.")]
    public AudioClip chaseFootstepsClip;
    [Tooltip("Distance at which the sound is at 100% full volume.")]
    public float minAudioDistance = 5.0f;
    [Tooltip("Maximum distance at which the sound becomes completely silent.")]
    public float maxAudioDistance = 60.0f;
    [Range(0f, 1f)] public float chaseAudioVolume = 1.0f;
    [Tooltip("Use Linear curve for better distant reach, or Logarithmic for realistic drop-off.")]
    public bool useLinearRolloff = true;

    [Header("Player Slowdown")]
    public FirstPersonController playerController;
    [Range(0f, 1f)] public float slowdownMultiplier = 0.4f;

    [Header("Run UI & Sprint Settings")]
    public TMP_Text runTextComponent;       
    public bool enableSprintDuringChase = true; 

    [Header("Spotting Settings")]
    public float visibleDuration = 3.0f; 
    public float maxSpottingDistance = 35.0f;

    [Header("Chase Settings (Entity #5 Only)")]
    public bool isChaseEntity = false; 
    public float delayBeforeChase = 5.0f;
    public float chaseSpeed = 4.5f;
    public bool despawnOnEscape = false; 
    public Animator entityAnimator;

    [Header("Chase Animations")]
    public string idleAnimationName = "Idle";
    public string runAnimationName = "Run Forward Left"; 
    public string runLeftAnimationName = "Run Forward Left";
    public string runRightAnimationName = "Run Forward Right";
    public float turnThresholdAngle = 15.0f;

    [Header("Attack Settings")]
    public string attackAnimationName = "Zombie Attack Left"; 
    public string attackLeftAnimationName = "Zombie Attack Left";
    public string attackRightAnimationName = "Zombie Attack Right";
    public float attackRange = 2.0f;
    public float attackWindupSpeed = 0.8f; 
    public float attackImpactTolerance = 0.6f; 
    public float attackCooldown = 1.5f;
    public float attackDuration = 1.4f; 
    public float attackImpactDelay = 0.5f;

    [Header("Atmosphere Settings")]
    public float zoomFOV = 45.0f;            
    public float zoomSpeed = 5.0f;           
    public float atmosphereFadeOutSpeed = 2.5f; 

    [Header("Darkness Settings")]
    [Range(0f, 1f)] public float targetVignetteAlpha = 0.35f; 
    public Color darkAmbientColor = new Color(0.25f, 0.25f, 0.25f, 1f); 
    public bool disableDirectionalLights = true; 

    [Header("Darkness / Exposure Settings (URP)")]
    public Volume globalVolume;
    public float darkExposure = -0.8f; 

    [Header("Filter & Material Settings")]
    public string targetObjectName = "CeilingLights"; 
    public Material darkCeilingMaterial;

    // Static Atmosphere Tracking & Material Backups
    private static List<EntitySpot> activeSpottedEntities = new List<EntitySpot>();
    private static List<GameObject> globalDisabledLightObjects = new List<GameObject>();
    private static List<Light> globalDisabledDirectionalLights = new List<Light>();
    private static Dictionary<Renderer, Material[]> originalMaterialCache = new Dictionary<Renderer, Material[]>();

    private static bool isOriginalLightingSaved = false;
    private static Color originalAmbientColor;
    private static AmbientMode originalAmbientMode;
    private static float originalReflectionIntensity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        activeSpottedEntities.Clear();
        globalDisabledLightObjects.Clear();
        globalDisabledDirectionalLights.Clear();
        originalMaterialCache.Clear();
        isOriginalLightingSaved = false;
    }

    private bool playerInTrigger, isSpotted, sequenceFinished, isChasing, isAttacking;
    private float lastAttackTime = -999f;
    private string currentPlayingAnim = "";

    private float defaultFOV, targetFOV, currentVignetteAlpha, currentTextAlpha;
    private ColorAdjustments colorAdjustments;
    private float originalWalkSpeed, originalSprintSpeed;

    private NavMeshAgent navAgent;
    private PlayerHealth cachedPlayerHealth;

    private void OnEnable() => ResetExposureImmediate();

    private void Start()
    {
        if (attackImpactTolerance <= 0f) attackImpactTolerance = 0.6f;

        if (!isOriginalLightingSaved && !MenuBackgroundLoader.IsBackdrop(this))
        {
            originalAmbientColor = RenderSettings.ambientLight;
            originalAmbientMode = RenderSettings.ambientMode;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;
            isOriginalLightingSaved = true;
        }

        FetchColorAdjustments();
        ResetExposureImmediate();
        EnsurePlayerReference();

        if (playerController != null)
        {
            cachedPlayerHealth = playerController.GetComponent<PlayerHealth>();
            playerController.enableSprint = false;
        }

        if (runTextComponent != null)
        {
            runTextComponent.gameObject.SetActive(true);
            SetAlpha(runTextComponent, 0f);
        }

        if (entityMeshObject != null)
        {
            entityMeshObject.SetActive(false);
            navAgent = entityMeshObject.GetComponent<NavMeshAgent>();
            if (navAgent != null) navAgent.enabled = false;
        }

        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null) targetFOV = defaultFOV = playerCamera.fieldOfView;

        if (vignetteImage != null)
        {
            vignetteImage.transform.SetAsFirstSibling();
            SetAlpha(vignetteImage, 0f);
        }

        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (!isChaseEntity) return;

        GameObject targetObj = entityMeshObject != null ? entityMeshObject : gameObject;

        if (chaseAudioSource == null)
        {
            if (sfxMixerGroup != null) chaseAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            chaseAudioSource = targetObj.GetComponent<AudioSource>();
            if (chaseAudioSource == null)
            {
                chaseAudioSource = targetObj.AddComponent<AudioSource>();
            }
        }

        if (chaseAudioSource != null)
        {
            chaseAudioSource.playOnAwake = false;
            chaseAudioSource.loop = true;
            chaseAudioSource.spatialBlend = 1.0f; // 100% 3D Sound
            chaseAudioSource.rolloffMode = useLinearRolloff ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
            chaseAudioSource.minDistance = minAudioDistance;
            chaseAudioSource.maxDistance = maxAudioDistance;
            chaseAudioSource.volume = chaseAudioVolume;
            if (chaseFootstepsClip != null) chaseAudioSource.clip = chaseFootstepsClip;
        }
    }

    private void EnsurePlayerReference()
    {
        if (playerController == null) playerController = Object.FindFirstObjectByType<FirstPersonController>();
        if (playerController != null)
        {
            if (originalWalkSpeed <= 0f && playerController.walkSpeed > 0f) originalWalkSpeed = playerController.walkSpeed;
            if (originalSprintSpeed <= 0f && playerController.sprintSpeed > 0f) originalSprintSpeed = playerController.sprintSpeed;
        }
    }

    public static void ResetBackroomsZone()
    {
        activeSpottedEntities.Clear();
        RestoreGlobalAtmosphere();

        FirstPersonController fpc = Object.FindFirstObjectByType<FirstPersonController>();
        if (fpc != null && fpc.TryGetComponent<CharacterController>(out var cc)) cc.enabled = true;

        foreach (var mgr in Object.FindObjectsByType<EncounterManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            mgr?.ResetEncounterState();

        foreach (var entity in Object.FindObjectsByType<EntitySpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            entity?.ResetEntityState();
    }

    public void ResetEntityState()
    {
        StopAllCoroutines();
        if (activeSpottedEntities.Contains(this)) activeSpottedEntities.Remove(this);

        playerInTrigger = isSpotted = sequenceFinished = isChasing = isAttacking = false;
        currentVignetteAlpha = currentTextAlpha = 0f;

        if (chaseAudioSource != null && chaseAudioSource.isPlaying)
            chaseAudioSource.Stop();

        if (vignetteImage != null) SetAlpha(vignetteImage, 0f);
        if (runTextComponent != null) SetAlpha(runTextComponent, 0f);

        if (playerCamera != null && defaultFOV > 0f) playerCamera.fieldOfView = targetFOV = defaultFOV;

        if (navAgent != null)
        {
            if (navAgent.enabled) { navAgent.isStopped = true; navAgent.velocity = Vector3.zero; }
            navAgent.enabled = false;
        }

        if (entityMeshObject != null) entityMeshObject.SetActive(false);

        RestorePlayerSpeed();
        ResetExposureImmediate();
    }

    private void ResetExposureImmediate()
    {
        if (MenuBackgroundLoader.IsBackdrop(this)) return;
        FetchColorAdjustments();
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 0f;
            colorAdjustments.postExposure.overrideState = false;
        }
        RestoreGlobalAtmosphere();
    }

    private void FetchColorAdjustments()
    {
        if (colorAdjustments == null && globalVolume != null && globalVolume.profile != null)
            globalVolume.profile.TryGet(out colorAdjustments);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other) && !sequenceFinished && !playerInTrigger)
        {
            playerInTrigger = true;
            cachedPlayerHealth = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponent<PlayerHealth>();
            if (entityMeshObject != null) entityMeshObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other) && playerInTrigger && !isSpotted && !sequenceFinished)
            playerInTrigger = false;
    }

    private bool IsPlayer(Collider col) => col.CompareTag("Player") || (col.transform.root != null && col.transform.root.CompareTag("Player")) || col.GetComponentInParent<FirstPersonController>() != null;

    private void Update()
    {
        if (playerInTrigger && !sequenceFinished)
        {
            if (!isChasing) FacePlayer();

            if (!isSpotted && IsPlayerLookingAtEntity())
            {
                isSpotted = true;
                if (!activeSpottedEntities.Contains(this)) activeSpottedEntities.Add(this);

                targetFOV = zoomFOV;        
                currentVignetteAlpha = targetVignetteAlpha; 
                currentTextAlpha = 1f; 

                RenderSettings.ambientMode = AmbientMode.Flat;

                // Turn off lights and swap materials for ALL entities upon spotting
                DisableAllCeilingFixturesInstantly();
                if (disableDirectionalLights) DisableDirectionalLights();

                ApplyPlayerSlowdown();

                if (encounterManager != null && !encounterManager.introStarted && !isChaseEntity)
                    encounterManager.StartEncounter();

                StartCoroutine(SpottedTimerRoutine());
            }
        }

        if (isChasing)
        {
            DisableAllCeilingFixturesInstantly();
            if (disableDirectionalLights) DisableDirectionalLights();

            if (!isAttacking && navAgent != null && navAgent.enabled && playerCamera != null)
            {
                Vector3 entityPos = Vector3.Scale(entityMeshObject.transform.position, new Vector3(1,0,1));
                Vector3 playerPos = Vector3.Scale(playerCamera.transform.position, new Vector3(1,0,1));

                if (Vector3.Distance(entityPos, playerPos) <= attackRange)
                {
                    FacePlayer();
                    if (Time.time >= lastAttackTime + attackCooldown) StartCoroutine(PerformAttackRoutine());
                    else
                    {
                        navAgent.isStopped = true;
                        navAgent.velocity = Vector3.zero;
                        PlayAnimation(idleAnimationName);
                    }
                }
                else
                {
                    navAgent.isStopped = false;
                    navAgent.speed = chaseSpeed;
                    navAgent.SetDestination(playerCamera.transform.position);
                    UpdateDirectionalRunAnimations();
                }
            }
        }

        UpdateAtmosphereAndUI();
    }

    private void UpdateAtmosphereAndUI()
    {
        if (MenuBackgroundLoader.IsBackdrop(this)) return;
        float lerpSpeed = (currentVignetteAlpha > 0f) ? zoomSpeed : atmosphereFadeOutSpeed;
        bool isSpottingActive = activeSpottedEntities.Exists(e => e.isSpotted && !e.isChasing);

        if (isSpottingActive || isChasing)
        {
            FetchColorAdjustments();
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = Mathf.Lerp(colorAdjustments.postExposure.value, darkExposure, Time.deltaTime * lerpSpeed);
            }
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, darkAmbientColor, Time.deltaTime * lerpSpeed);
            RenderSettings.reflectionIntensity = Mathf.Lerp(RenderSettings.reflectionIntensity, 0.1f, Time.deltaTime * lerpSpeed);
        }
        else
        {
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Lerp(colorAdjustments.postExposure.value, 0f, Time.deltaTime * lerpSpeed);
                if (Mathf.Abs(colorAdjustments.postExposure.value) < 0.05f)
                {
                    colorAdjustments.postExposure.value = 0f;
                    colorAdjustments.postExposure.overrideState = false;
                }
            }

            if (isOriginalLightingSaved)
            {
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, originalAmbientColor, Time.deltaTime * lerpSpeed);
                RenderSettings.reflectionIntensity = Mathf.Lerp(RenderSettings.reflectionIntensity, originalReflectionIntensity, Time.deltaTime * lerpSpeed);
            }
        }

        if (playerCamera != null) playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * lerpSpeed);
        if (vignetteImage != null) SetAlpha(vignetteImage, Mathf.Lerp(vignetteImage.color.a, currentVignetteAlpha, Time.deltaTime * lerpSpeed));
        if (runTextComponent != null) SetAlpha(runTextComponent, Mathf.Lerp(runTextComponent.color.a, currentTextAlpha, Time.deltaTime * lerpSpeed));
    }

    private void UpdateDirectionalRunAnimations()
    {
        if (navAgent == null || entityAnimator == null || entityMeshObject == null) return;
        Vector3 moveDir = navAgent.desiredVelocity;

        if (moveDir.sqrMagnitude > 0.1f)
        {
            float angle = Vector3.SignedAngle(entityMeshObject.transform.forward, moveDir, Vector3.up);
            if (angle < -turnThresholdAngle) PlayAnimation(runLeftAnimationName);
            else if (angle > turnThresholdAngle) PlayAnimation(runRightAnimationName);
            else PlayAnimation(runAnimationName);
        }
        else PlayAnimation(idleAnimationName);
    }

    private void PlayAnimation(string animName, float crossFade = 0.1f, bool forceReplay = false)
    {
        if (entityAnimator != null && !string.IsNullOrEmpty(animName) && (currentPlayingAnim != animName || forceReplay))
        {
            currentPlayingAnim = animName;
            entityAnimator.CrossFade(animName, crossFade, -1, 0f);
        }
    }

    private IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        FacePlayer();

        string anim = attackAnimationName;
        if (currentPlayingAnim == runLeftAnimationName) anim = attackLeftAnimationName;
        else if (currentPlayingAnim == runRightAnimationName) anim = attackRightAnimationName;

        PlayAnimation(anim, 0.05f, true);

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = attackWindupSpeed <= 0f;
            if (attackWindupSpeed > 0f)
            {
                navAgent.speed = attackWindupSpeed;
                if (playerCamera != null) navAgent.SetDestination(playerCamera.transform.position);
            }
        }

        float elapsed = 0f;
        while (elapsed < attackImpactDelay)
        {
            if (sequenceFinished) yield break;
            FacePlayer();
            if (navAgent != null && navAgent.enabled && attackWindupSpeed > 0f && playerCamera != null)
                navAgent.SetDestination(playerCamera.transform.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (navAgent != null && navAgent.enabled) { navAgent.isStopped = true; navAgent.velocity = Vector3.zero; }

        if (entityMeshObject != null)
        {
            Vector3 ePos = Vector3.Scale(entityMeshObject.transform.position, new Vector3(1,0,1));
            Vector3 tPos = Vector3.Scale((playerController != null ? playerController.transform.position : playerCamera.transform.position), new Vector3(1,0,1));

            if (Vector3.Distance(ePos, tPos) <= attackRange + attackImpactTolerance)
            {
                cachedPlayerHealth ??= Object.FindFirstObjectByType<PlayerHealth>();
                cachedPlayerHealth?.TakeDamage(1);
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackImpactDelay));
        isAttacking = false;
    }

    private void FacePlayer()
    {
        if (entityMeshObject == null || playerCamera == null) return;
        Vector3 dir = playerCamera.transform.position - entityMeshObject.transform.position;
        dir.y = 0;
        if (dir != Vector3.zero) entityMeshObject.transform.rotation = Quaternion.LookRotation(dir);
    }

    private bool IsPlayerLookingAtEntity()
    {
        if (playerCamera == null || entityMeshObject == null) return false;
        Vector3 dir = entityMeshObject.transform.position - playerCamera.transform.position;
        return dir.magnitude <= maxSpottingDistance && Vector3.Angle(playerCamera.transform.forward, dir) < 45.0f;
    }

    private void ApplyPlayerSlowdown()
    {
        EnsurePlayerReference();
        if (playerController == null) return;
        playerController.walkSpeed = originalWalkSpeed * slowdownMultiplier;
        playerController.sprintSpeed = originalSprintSpeed * slowdownMultiplier;
    }

    private void RestorePlayerSpeed()
    {
        EnsurePlayerReference();
        if (playerController == null) return;
        if (originalWalkSpeed > 0f) playerController.walkSpeed = originalWalkSpeed;
        if (originalSprintSpeed > 0f) playerController.sprintSpeed = originalSprintSpeed;
    }

    private void DisableAllCeilingFixturesInstantly()
    {
        string targetLower = string.IsNullOrEmpty(targetObjectName) ? "ceilinglights" : targetObjectName.ToLower();

        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            string name = obj.name.ToLower();

            if (name.Contains("ceiling_light") || name == "ceilinglights" || name.StartsWith("ceilinglight"))
            {
                if (obj.activeSelf) obj.SetActive(false);
                if (!globalDisabledLightObjects.Contains(obj)) globalDisabledLightObjects.Add(obj);

                if (darkCeilingMaterial != null)
                {
                    Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer rend in renderers)
                    {
                        if (rend == null) continue;
                        if (!originalMaterialCache.ContainsKey(rend))
                        {
                            originalMaterialCache[rend] = rend.sharedMaterials;
                        }

                        Material[] darkMats = new Material[rend.sharedMaterials.Length];
                        for (int i = 0; i < darkMats.Length; i++) darkMats[i] = darkCeilingMaterial;
                        rend.sharedMaterials = darkMats;
                    }
                }
            }
        }
    }

    private void DisableDirectionalLights()
    {
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l != null && l.type == LightType.Directional)
            {
                if (l.enabled) l.enabled = false;
                if (!globalDisabledDirectionalLights.Contains(l)) globalDisabledDirectionalLights.Add(l);
            }
        }
    }

    public static void RestoreGlobalAtmosphere()
    {
        foreach (GameObject obj in globalDisabledLightObjects) if (obj != null) obj.SetActive(true);
        globalDisabledLightObjects.Clear();

        foreach (Light l in globalDisabledDirectionalLights) if (l != null) l.enabled = true;
        globalDisabledDirectionalLights.Clear();

        foreach (var kvp in originalMaterialCache)
        {
            if (kvp.Key != null) kvp.Key.sharedMaterials = kvp.Value;
        }
        originalMaterialCache.Clear();

        if (isOriginalLightingSaved)
        {
            RenderSettings.ambientLight = originalAmbientColor;
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.reflectionIntensity = originalReflectionIntensity;
        }
    }

    private IEnumerator SpottedTimerRoutine()
    {
        if (isChaseEntity)
        {
            float timer = delayBeforeChase;
            while (timer > 0)
            {
                if (runTextComponent != null) runTextComponent.text = $"RUN! ({Mathf.CeilToInt(timer)})";
                timer -= Time.deltaTime;
                yield return null;
            }

            currentVignetteAlpha = currentTextAlpha = 0f;
            targetFOV = defaultFOV;

            if (activeSpottedEntities.Contains(this)) activeSpottedEntities.Remove(this);

            RestorePlayerSpeed();

            if (playerController != null && enableSprintDuringChase) playerController.enableSprint = true;

            if (navAgent != null)
            {
                navAgent.enabled = true;
                navAgent.speed = chaseSpeed;
            }

            // Start 3D Chase Audio
            EnsureAudioSource();
            if (chaseAudioSource != null && chaseFootstepsClip != null)
            {
                chaseAudioSource.clip = chaseFootstepsClip;
                chaseAudioSource.Play();
            }

            if (encounterManager != null) encounterManager.StartChaseMusic();
            isChasing = true;
        }
        else
        {
            yield return new WaitForSeconds(visibleDuration);
            if (entityMeshObject != null) entityMeshObject.SetActive(false);
            targetFOV = defaultFOV;
            currentVignetteAlpha = currentTextAlpha = 0f;

            RestorePlayerSpeed();

            if (activeSpottedEntities.Contains(this)) activeSpottedEntities.Remove(this);
            RestoreGlobalAtmosphere();
            if (encounterManager != null) encounterManager.AdvanceMusicStage();
            sequenceFinished = true;
        }
    }

    public void CompleteChaseEscape(bool forceDespawn = false)
    {
        if (!isChaseEntity || sequenceFinished) return;

        sequenceFinished = true;
        isChasing = false;

        if (chaseAudioSource != null && chaseAudioSource.isPlaying)
            chaseAudioSource.Stop();

        if (activeSpottedEntities.Contains(this)) activeSpottedEntities.Remove(this);
        if (playerController != null) playerController.enableSprint = false;

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            navAgent.enabled = false;
        }

        PlayAnimation(idleAnimationName);
        if ((despawnOnEscape || forceDespawn) && entityMeshObject != null) entityMeshObject.SetActive(false);

        targetFOV = defaultFOV;
        currentVignetteAlpha = currentTextAlpha = 0f;

        RestorePlayerSpeed();
        ResetExposureImmediate();
        RestoreGlobalAtmosphere();

        if (encounterManager != null) { encounterManager.StopChaseAndPlayOut(); }
    }

    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    private void OnDisable()
    {
        if (chaseAudioSource != null && chaseAudioSource.isPlaying)
            chaseAudioSource.Stop();

        if (activeSpottedEntities.Contains(this)) activeSpottedEntities.Remove(this);
        ResetExposureImmediate();
        RestorePlayerSpeed();
    }

    private void OnDestroy() => OnDisable();
}