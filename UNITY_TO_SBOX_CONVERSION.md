# After Hours — Unity → s&box Conversion Guide

> Comprehensive migration reference for the pharmacy horror game.  
> Unity project: `C:\Users\JTSte\Unity\Pharmacy\Pharmacy\`  
> s&box project: `C:\Users\JTSte\OneDrive\Documents\s&box Projects\my_project\`

---

## Table of Contents

1. [Current Implementation Status](#1-current-implementation-status)
2. [Project Structure Migration](#2-project-structure-migration)
3. [Engine API Cheat Sheet](#3-engine-api-cheat-sheet)
4. [Asset Migration](#4-asset-migration)
5. [Networking Architecture Migration](#5-networking-architecture-migration)
6. [System-by-System Conversion](#6-system-by-system-conversion)
   - [6.1 Player Controls](#61-player-controls)
   - [6.2 Focus State Manager](#62-focus-state-manager)
   - [6.3 Object Pickup (Central Interaction Hub)](#63-object-pickup-central-interaction-hub)
   - [6.4 NPC System](#64-npc-system)
   - [6.5 Shift Manager](#65-shift-manager)
   - [6.6 Counter, ID Card & Cash Register](#66-counter-id-card--cash-register)
   - [6.7 Computer Screen & Information UI](#67-computer-screen--information-ui)
   - [6.8 Pill Counting Station](#68-pill-counting-station)
   - [6.9 Pill Filling Station](#69-pill-filling-station)
   - [6.10 Delivery Station](#610-delivery-station)
   - [6.11 Shelf System](#611-shelf-system)
   - [6.12 Dialogue System](#612-dialogue-system)
   - [6.13 Doppelganger Data (ScriptableObjects)](#613-doppelganger-data-scriptableobjects)
   - [6.14 Gun Case & Shooting](#614-gun-case--shooting)
   - [6.15 Score Manager](#615-score-manager)
   - [6.16 Blood Decal & Mop](#616-blood-decal--mop)
   - [6.17 Door](#617-door)
   - [6.18 Player Networking Utilities](#618-player-networking-utilities)
7. [UI Conversion (Canvas/TMP → Razor)](#7-ui-conversion-canvastmp--razor)
8. [Missing Systems (Build in s&box)](#8-missing-systems-build-in-sbox)
9. [Build Order & Priorities](#9-build-order--priorities)

---

## 1. Current Implementation Status

### Fully Implemented in Unity

| Script(s) | Description |
|---|---|
| `PlayerMovement.cs` | FPS CharacterController movement + sprint + jump |
| `MouseLook.cs` | FPS camera look + screen shake |
| `PlayerComponents.cs` | Central reference hub for player |
| `ObjectPickup.cs` | Interaction hub: pickup, throw, place, interact with all world objects |
| `HoldableItem.cs` | Per-item hold position/rotation overrides |
| `FocusStateManager.cs` | Camera transition between FPS and focus modes |
| `ShiftManager.cs` | Full day/night state machine (Dawn→Day→Transition→Night) |
| `ShiftScoreManager.cs` | Tracks money, customers served, kills, escapes |
| `GameStarter.cs` | Entry point routing to ShiftManager |
| `CashRegister.cs` | NPC checkout trigger |
| `CounterSlot.cs` | NPC item placement + player "bagging" |
| `IDCardSlot.cs` / `IDCardInteraction.cs` / `IDCardVisuals.cs` | Counter ID card system |
| `ComputerScreen.cs` / `ComputerScreenController.cs` | Focus mode + tab views |
| `NPCInfoDisplay.cs` / `NPCIdentityField.cs` | NPC identity panel |
| `PrescriptionDisplay.cs` / `PrescriptionField.cs` | Prescription data panel |
| `NPISearchPanel.cs` | NPI lookup in computer |
| `PillCountingStation.cs` + children | Full pill-counting mini-game |
| `PillFillingStation.cs` + children | Medication bottle filling station |
| `DeliveryStation.cs` | Spawns inventory boxes |
| `ShelfSection.cs` / `ShelfSlot.cs` / `InventoryBox.cs` | Shelf restocking system |
| `NPCInteractionController.cs` | 13-state NPC AI state machine |
| `NPCSpawnManager.cs` | Sequential NPC spawner from RoundConfig |
| `NPCAnimationController.cs` | NPC animation driver |
| `NPCIdentity.cs` / `PrescriptionData.cs` / `DoppelgangerProfile.cs` | NPC data assets |
| `PrescriberDatabase.cs` / `RoundConfig.cs` / `ItemCategory.cs` | Game data assets |
| `GunCase.cs` | Gun pickup + shooting NPCs |
| `BloodDecal.cs` / `BloodSplatterEffect.cs` | Blood visual system |
| `Mop.cs` | Blood cleanup tool |
| `Door.cs` | Interactable doors |
| `DialogueManager.cs` + children | NPC conversation overlay |
| `NPCDialogueTrigger.cs` / `NPCInfoTalkButton.cs` | Dialogue triggers |
| Networking utilities | `ClientNetworkTransform`, `CounterSlotNetwork`, `ShelfSlotNetwork`, `DisconnectHandler`, `PlayerRegistry`, `PlayerSetup`, `PlayerSpawnManager`, `QuickConnect` |

### Not Yet Implemented (Build in s&box from Scratch)

| System | Status |
|---|---|
| `MonsterController` | Missing — NavMesh AI with sound reaction |
| `NoiseSystem` | Missing — event bus for sound-based monster attraction |
| `IngredientData` / `RecipeGenerator` | Missing — crafting data and randomization rules |
| `RecipeNote` | Missing — world-space interactable note |
| `CraftingManager` | Missing — counter-slot crafting validation |
| `CraftedWeapon` (4 types) | Missing — vial, spray, trap, syringe |
| `MortarStation` | Missing — grinding station (mouse-rotation input) |
| `ShiftLighting` | Missing — phase-based lighting control |
| `IngredientSpawner` | Missing — night-shift ingredient placement |
| Carry limit (2 items) | Partial — current ObjectPickup holds 1 |

---

## 2. Project Structure Migration

### Unity → s&box Directory Map

```
Unity Assets/Scripts/          →    s&box /code/
Unity Assets/Scenes/           →    s&box /scenes/
Unity Assets/Prefab/           →    s&box /prefabs/
Unity Assets/Data/             →    s&box /data/
Unity Assets/Models/           →    s&box /models/
Unity Assets/Materials/        →    s&box /materials/
Unity Assets/Textures/         →    s&box /textures/
Unity Assets/Scripts/Dialogue/ →    s&box /code/Dialogue/
Unity Assets/Scripts/NPC/      →    s&box /code/NPC/
Unity Assets/Scripts/PillCounting/ → s&box /code/PillCounting/
Unity Assets/Scripts/PillFilling/  → s&box /code/PillFilling/
Unity Assets/Scripts/Shelf/    →    s&box /code/Shelf/
Unity Assets/Scripts/Counter/  →    s&box /code/Counter/
Unity Assets/Scripts/Delivery/ →    s&box /code/Delivery/
Unity Assets/Scripts/Networking/ →  s&box /code/Networking/
Unity Assets/UISprites/        →    s&box /ui/sprites/
Unity ScriptableObject assets  →    s&box /data/*.gameresource files
```

### s&box Project File Layout

```
my_project/
├── my_project.sbproj
├── code/
│   ├── Player/
│   │   ├── PlayerController.cs       (replaces PlayerMovement + MouseLook)
│   │   ├── PlayerInteraction.cs      (replaces ObjectPickup)
│   │   ├── FocusStateManager.cs
│   │   └── PlayerComponents.cs
│   ├── NPC/
│   │   ├── NPCController.cs          (replaces NPCInteractionController)
│   │   ├── NPCSpawnManager.cs
│   │   └── NPCAnimationController.cs
│   ├── Shift/
│   │   ├── ShiftManager.cs
│   │   └── ShiftScoreManager.cs
│   ├── Counter/
│   │   ├── CounterSlot.cs
│   │   ├── IDCardSlot.cs
│   │   ├── IDCardInteraction.cs
│   │   └── CashRegister.cs
│   ├── Computer/
│   │   ├── ComputerScreen.cs
│   │   └── ComputerScreenController.cs
│   ├── PillCounting/
│   ├── PillFilling/
│   ├── Shelf/
│   ├── Delivery/
│   ├── Dialogue/
│   ├── Crafting/                     (new — night-shift systems)
│   ├── Monster/                      (new)
│   └── Weapons/                      (new)
├── scenes/
│   └── pharmacy.scene
├── prefabs/
│   ├── npc_customer.prefab
│   ├── mop.prefab
│   └── ...
├── data/
│   ├── npcs/                         (GameResource files — replaces ScriptableObjects)
│   ├── prescriptions/
│   ├── ingredients/
│   └── round_configs/
└── ui/
    ├── HudPanel.razor
    ├── HudPanel.razor.scss
    ├── DialoguePanel.razor
    ├── NPCInfoPanel.razor
    ├── ComputerScreen.razor
    └── ...
```

---

## 3. Engine API Cheat Sheet

### Core Class Changes

| Unity | s&box |
|---|---|
| `MonoBehaviour` | `Component` |
| `NetworkBehaviour` | `Component` (networking is built-in) |
| `ScriptableObject` | `GameResource` |
| `using UnityEngine;` | `using Sandbox;` |
| `using Unity.Netcode;` | *(remove — not needed)* |
| `using TMPro;` | *(remove — use Razor UI)* |

### Lifecycle Methods

| Unity | s&box |
|---|---|
| `Awake()` | `protected override void OnAwake()` |
| `OnEnable()` | `protected override void OnEnabled()` |
| `Start()` | `protected override void OnStart()` |
| `Update()` | `protected override void OnUpdate()` |
| `FixedUpdate()` | `protected override void OnFixedUpdate()` |
| `LateUpdate()` | `protected override void OnPreRender()` |
| `OnDisable()` | `protected override void OnDisabled()` |
| `OnDestroy()` | `protected override void OnDestroy()` |
| `Time.deltaTime` | `Time.Delta` |
| `Time.time` | `Time.Now` |

### Serialization

| Unity | s&box |
|---|---|
| `[SerializeField] private float x;` | `[Property] public float X { get; set; }` |
| `[Header("Combat")]` | `[Group("Combat")]` |
| `[Tooltip("...")]` | XML doc comment or `[Title("...")]` |
| `[Range(0, 10)]` | `[Range(0, 10)]` *(same)* |
| `[HideInInspector]` | Omit `[Property]` |

### GameObject & Component

| Unity | s&box |
|---|---|
| `gameObject` | `GameObject` |
| `transform` | `Transform` or `WorldPosition` / `WorldRotation` |
| `transform.position` | `WorldPosition` |
| `transform.rotation` | `WorldRotation` |
| `transform.forward` | `WorldRotation.Forward` |
| `gameObject.SetActive(false)` | `GameObject.Enabled = false` |
| `gameObject.activeInHierarchy` | `GameObject.Active` |
| `Destroy(gameObject)` | `GameObject.Destroy()` |
| `Destroy(gameObject, 2f)` | `await Task.DelaySeconds(2f); go.Destroy();` |
| `go.IsValid()` | s&box equivalent of Unity's `go != null` after destroy |
| `GetComponent<T>()` | `GetComponent<T>()` *(same)* |
| `GetComponentInChildren<T>()` | `GetComponentInChildren<T>()` *(same)* |
| `FindObjectsOfType<T>()` | `Scene.GetAllComponents<T>()` |
| `GameObject.FindWithTag("x")` | `Scene.FindAllWithTag("x").FirstOrDefault()` |
| `Instantiate(prefab, pos, rot)` | `SceneUtility.Instantiate(prefab, new Transform(pos, rot, scale))` |
| `go.tag = "Enemy"` | `go.Tags.Add("enemy")` |
| `go.CompareTag("Enemy")` | `go.Tags.Has("enemy")` |

### Networking

| Unity (Netcode for GameObjects) | s&box |
|---|---|
| `: NetworkBehaviour` | `: Component` (no special base needed) |
| `NetworkVariable<T>` | `[Sync] public T Prop { get; set; }` |
| `NetworkVariable<T>` (host-only write) | `[Sync(SyncFlags.FromHost)] public T Prop { get; set; }` |
| `[ServerRpc]` | `[Rpc.Host]` |
| `[ClientRpc]` | `[Rpc.Broadcast]` |
| Owner-only ClientRpc | `[Rpc.Owner]` |
| `IsOwner` | `!IsProxy` |
| `IsServer` | `Networking.IsHost` |
| `IsClient` | `Networking.IsClient` |
| `NetworkManager.Singleton.IsServer` | `Networking.IsHost` |
| `NetworkManager.Singleton.IsListening` | `Networking.IsActive` |
| `NetworkManager.Singleton.LocalClientId` | `Connection.Local.Id` (or compare `GameObject.Network.OwnerId`) |
| `NetworkObject.Spawn()` | `go.NetworkSpawn()` |
| `NetworkObject.Despawn()` | `go.Destroy()` |
| `ulong.MaxValue` for "no owner" sentinel | Use `Guid.Empty` or `-1` as a sentinel on a `[Sync] int` |
| `NetworkObject.NetworkObjectId` | `GameObject.Id` (Guid) |
| `[RequireOwnership = false]` on ServerRpc | `[Rpc.Host]` methods are always callable by any client |
| `OnNetworkSpawn()` | `protected override void OnStart()` + check `Networking.IsActive` |

### Physics

| Unity | s&box |
|---|---|
| `Physics.Raycast(ray, out hit, dist)` | `Scene.PhysicsWorld.Trace.Ray(start, end).Run()` |
| `hit.point` | `tr.HitPosition` |
| `hit.normal` | `tr.Normal` |
| `hit.collider.gameObject` | `tr.Body?.GameObject` |
| `Physics.SphereCast(...)` | `.Sphere(radius, from, to).Run()` |
| `Physics.RaycastAll(...)` | `.Ray(start, end).RunAll()` |
| `rb.velocity = v` | `rb.Velocity = v` |
| `rb.AddForce(f)` | `rb.ApplyForce(f)` |
| `rb.AddForce(f, ForceMode.Impulse)` | `rb.ApplyImpulse(f)` |
| `rb.mass = x` | `rb.MassOverride = x` |
| `rb.drag = x` | `rb.LinearDamping = x` |
| `rb.useGravity = false` | `rb.Gravity = false` |
| `void OnCollisionEnter(Collision c)` | Implement `Component.ICollisionListener` interface |

### Input

| Unity | s&box |
|---|---|
| `Input.GetKeyDown(KeyCode.E)` | `Input.Pressed("Use")` |
| `Input.GetKey(KeyCode.LeftShift)` | `Input.Down("Run")` |
| `Input.GetMouseButtonDown(0)` | `Input.Pressed("Attack1")` |
| `Input.GetMouseButton(0)` | `Input.Down("Attack1")` |
| `Input.GetAxisRaw("Horizontal")` | `Input.AnalogMove.y` |
| `Input.GetAxisRaw("Vertical")` | `Input.AnalogMove.x` |
| `Input.GetAxis("Mouse X")` | `Input.AnalogLook.yaw` |
| `Input.GetAxis("Mouse Y")` | `Input.AnalogLook.pitch` |
| `Input.GetButtonDown("Jump")` | `Input.Pressed("Jump")` |

### Audio

| Unity | s&box |
|---|---|
| `AudioSource.PlayOneShot(clip)` | `Sound.Play("sound/name.vsnd")` |
| `AudioSource.Play()` | `Sound.Play(...)` with a handle |
| `AudioSource` on GameObject | `SoundPointComponent` or `Sound.Play(...)` at position |

### Coroutines

| Unity | s&box |
|---|---|
| `StartCoroutine(MyRoutine())` | `_ = MyAsync()` |
| `yield return null` | `await Task.Yield()` |
| `yield return new WaitForSeconds(n)` | `await Task.DelaySeconds(n)` |
| `yield return new WaitUntil(() => cond)` | `while (!cond) await Task.Yield()` |
| `StopCoroutine(...)` | `CancellationToken` |

---

## 4. Asset Migration

### 3D Models (FBX → s&box)

The existing `Interior1.fbx` and all NPC/item models must be compiled for Source 2.

**Two paths:**

**Option A — ModelDoc (recommended for production):**
1. Export each FBX from the Unity project.
2. Create a `.vmdl` (ModelDoc) file for each mesh that references the FBX source.
3. Compile in the s&box editor — it handles collision, LODs, and material binding.

**Option B — Auto-import (fast prototype):**
- Drop FBX files into `/models/` — s&box auto-imports them with defaults.
- You lose precise collision control; edit the ModelDoc afterward.

**Material binding:**
- Each sub-mesh material maps to a `.vmat` file.
- Existing Unity Standard materials → create Source 2 `.vmat` files with equivalent texture references.
- Texture files (PNG/TGA) drop into `/materials/textures/` and are auto-compiled.

### ScriptableObjects → GameResource

Every Unity `ScriptableObject` becomes an s&box `GameResource`. This is the direct equivalent.

**Pattern:**

```csharp
// Unity
[CreateAssetMenu(menuName = "NPC/Identity")]
public class NPCIdentity : ScriptableObject
{
    public string npcName;
    public int age;
    public Sprite photo;
}

// s&box
[GameResource("NPC Identity", "npcid", "Defines an NPC's personal data")]
public class NPCIdentity : GameResource
{
    [Property] public string NpcName { get; set; }
    [Property] public int Age { get; set; }
    [Property] public Texture Photo { get; set; }  // Texture instead of Sprite
}

// Loading at runtime
var id = ResourceLibrary.Get<NPCIdentity>("data/npcs/customer_01.npcid");
// Or load all
var all = ResourceLibrary.GetAll<NPCIdentity>();
```

**ScriptableObjects to convert:**

| Unity Asset | s&box GameResource | Extension |
|---|---|---|
| `NPCIdentity.cs` | `NPCIdentity.cs` | `.npcid` |
| `PrescriptionData.cs` | `PrescriptionData.cs` | `.prescription` |
| `DoppelgangerProfile.cs` | `DoppelgangerProfile.cs` | `.doppelganger` |
| `PrescriberDatabase.cs` | `PrescriberDatabase.cs` | `.npidatabase` |
| `ItemCategory.cs` | `ItemCategory.cs` | `.itemcategory` |
| `RoundConfig.cs` | `RoundConfig.cs` | `.roundconfig` |
| `MedicationData.cs` | `MedicationData.cs` | `.medication` |
| *(new)* `IngredientData.cs` | `IngredientData.cs` | `.ingredient` |

**Sprite → Texture:**
- Unity `Sprite` fields become `Texture` in s&box.
- In UI (Razor), use `<div style="background-image: url(@Photo?.ResourcePath)">` to display textures.

### Dialogue JSON Files

The dialogue system uses JSON files (`Assets/Data/Dialogues/*.json`). These migrate directly — copy them into `data/dialogues/`. The loading code changes from `Resources.Load<TextAsset>()` to `FileSystem.Mounted.ReadAllText()`:

```csharp
// Unity
var ta = Resources.Load<TextAsset>("Dialogues/npc_customer_01");
var data = JsonUtility.FromJson<DialogueData>(ta.text);

// s&box
var json = FileSystem.Mounted.ReadAllText("data/dialogues/npc_customer_01.json");
var data = Json.Deserialize<DialogueData>(json);
```

---

## 5. Networking Architecture Migration

### Overall Model — No Change Required

Both Unity Netcode for GameObjects and s&box use a **host-authoritative** model. The architectural decisions (server validates outcomes, clients run local visuals, NetworkVariables drive UI) are all valid in s&box. You are not redesigning — you are retranslating.

### Key Translation Points

**NetworkVariable → [Sync]**

```csharp
// Unity
public NetworkVariable<int> CurrentPhase { get; } =
    new NetworkVariable<int>(0, ReadPermission.Everyone, WritePermission.Server);

// s&box
[Sync(SyncFlags.FromHost)] public int CurrentPhase { get; set; }
```

For the `OnValueChanged` callback on `NetworkVariable`, s&box has no direct equivalent. Instead:
- Override `OnRefresh()` — called after a network snapshot update. Check your synced values there.
- Or poll values in `OnUpdate()`.

```csharp
// Unity OnValueChanged equivalent in s&box
protected override void OnRefresh()
{
    // Called after network snapshot — check if phase changed
    OnPhaseChanged(CurrentPhase);
}
```

**ServerRpc → [Rpc.Host]**

```csharp
// Unity
[ServerRpc(RequireOwnership = false)]
private void ProcessCheckoutServerRpc() { /* runs on server */ }

// Call site: ProcessCheckoutServerRpc();

// s&box
[Rpc.Host]
public void ProcessCheckout() { /* runs on host */ }

// Call site: ProcessCheckout(); — same call, routes to host automatically
```

**ClientRpc → [Rpc.Broadcast]**

```csharp
// Unity
[ClientRpc]
private void PlayHitEffectClientRpc() { /* runs on all clients */ }

// Call site: PlayHitEffectClientRpc();

// s&box
[Rpc.Broadcast]
public void PlayHitEffect() { /* runs on all clients */ }

// Call site: PlayHitEffect();
```

**Exclusive Access Locks (ulong NetworkVariable)**

The Unity code uses `NetworkVariable<ulong> _currentUserId` with `ulong.MaxValue` as a sentinel for "no user". In s&box:

```csharp
// Unity
private NetworkVariable<ulong> _currentUserId = new NetworkVariable<ulong>(
    ulong.MaxValue, ReadPermission.Everyone, WritePermission.Server);

public bool IsInUse => IsSpawned && _currentUserId.Value != ulong.MaxValue;

// s&box — use -1 as sentinel; Guid is clunkier for comparison
[Sync(SyncFlags.FromHost)] public long CurrentUserId { get; set; } = -1;

public bool IsInUse => Networking.IsActive && CurrentUserId != -1;

// Lock: (host only)
[Rpc.Host]
public void RequestActivation()
{
    if (CurrentUserId != -1) return;
    CurrentUserId = (long)Rpc.Caller.SteamId;
    // ... activate
}

[Rpc.Host]
public void ReleaseActivation()
{
    CurrentUserId = -1;
}
```

**IsOwner / IsServer Check Pattern**

```csharp
// Unity
void Update()
{
    if (!IsOwner) return;   // skip non-owner clients
    if (!IsServer) return;  // skip non-server
}

// s&box
protected override void OnUpdate()
{
    if (IsProxy) return;          // replaces !IsOwner
    if (!Networking.IsHost) return; // replaces !IsServer
}
```

**NetworkObject.Spawn() → NetworkSpawn()**

```csharp
// Unity
var npc = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);
var netObj = npc.GetComponent<NetworkObject>();
netObj.Spawn();

// s&box
var prefab = ResourceLibrary.Get<PrefabFile>("prefabs/npc_customer.prefab");
var npc = SceneUtility.Instantiate(prefab, new Transform(spawnPoint.WorldPosition, Rotation.Identity, 1f));
npc.NetworkSpawn();
```

---

## 6. System-by-System Conversion

### 6.1 Player Controls

**Scripts:** `PlayerMovement.cs` + `MouseLook.cs` + `PlayerComponents.cs`

**What to keep:** All the logic — walk speed, sprint, jump with coyote time + jump buffering, mouse look with smoothing, screen shake. The design is sound.

**What to change:**

```csharp
// Unity PlayerMovement.cs
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    void Update()
    {
        if (!IsOwner) return;
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        bool isSprinting = Input.GetKey(sprintKey) && inputZ > 0;
        controller.Move(_currentMoveVelocity * Time.deltaTime);
    }
}

// s&box PlayerController.cs
public sealed class PlayerController : Component
{
    [Property] public float WalkSpeed { get; set; } = 200f;
    [Property] public float SprintSpeed { get; set; } = 350f;

    private CharacterController _cc;

    protected override void OnAwake()
    {
        _cc = GetComponent<CharacterController>();
    }

    protected override void OnFixedUpdate()
    {
        if (IsProxy) return;

        var wishDir = Input.AnalogMove.Normal;
        bool isSprinting = Input.Down("Run") && wishDir.x > 0;
        float speed = isSprinting ? SprintSpeed : WalkSpeed;

        // Apply as velocity — CharacterController handles collision
        var velocity = WorldRotation * wishDir * speed;
        _cc.Move(velocity * Time.Delta);
    }

    protected override void OnUpdate()
    {
        if (IsProxy) return;

        // Camera look
        EyeAngles += Input.AnalogLook;
        EyeAngles = EyeAngles.WithPitch(EyeAngles.pitch.Clamp(-89f, 89f));
        Scene.Camera.WorldRotation = EyeAngles.ToRotation();

        // Jump
        if (Input.Pressed("Jump") && _cc.IsOnGround)
            _cc.Punch(Vector3.Up * JumpStrength);
    }
}
```

**PlayerComponents equivalent:** In s&box, the central-hub pattern is less necessary because `GetComponentInParent<T>()` is cheap. You can keep it as a convenience, but there is no `Local` singleton problem — each connected player is a separate `GameObject`. Use a `[RequireComponent]` attribute or find the local player via tag: `Scene.FindAllWithTag("localplayer").FirstOrDefault()`.

**Screen shake:** Replace `Camera.main` references with `Scene.Camera`. For shake, animate `Scene.Camera.LocalPosition` offset or use `Scene.Camera.WorldPosition += shakeOffset * Time.Delta`.

---

### 6.2 Focus State Manager

**Script:** `FocusStateManager.cs`

**What to keep:** The concept of switching between FPS look and a fixed camera aimed at a station. The `IsFocused` flag that blocks other input.

**Key change — FocusStateManager is client-local, no networking needed.** In the Unity version it's already a non-networked MonoBehaviour. Keep it that way — each client has their own focus state.

```csharp
// s&box
public sealed class FocusStateManager : Component
{
    [Property] public bool IsFocused { get; private set; }

    private Transform _focusTarget;
    private Angles _savedEyeAngles;
    private PlayerController _player;

    protected override void OnAwake()
    {
        _player = GetComponent<PlayerController>();
    }

    public void EnterFocus(Transform cameraTarget)
    {
        if (IsFocused) return;
        IsFocused = true;
        _focusTarget = cameraTarget;
        _savedEyeAngles = _player.EyeAngles;
        Input.MouseCursorVisible = true;
    }

    public void ExitFocus()
    {
        if (!IsFocused) return;
        IsFocused = false;
        _player.EyeAngles = _savedEyeAngles;
        Input.MouseCursorVisible = false;
    }

    protected override void OnUpdate()
    {
        if (!IsFocused || _focusTarget == null) return;
        Scene.Camera.WorldPosition = Vector3.Lerp(
            Scene.Camera.WorldPosition,
            _focusTarget.WorldPosition,
            Time.Delta * 10f
        );
        Scene.Camera.LookAt(_focusTarget.WorldPosition);
    }
}
```

---

### 6.3 Object Pickup (Central Interaction Hub)

**Script:** `ObjectPickup.cs`

This is the most complex conversion. The Unity version does everything: raycast detection, pickup/throw/place, station activation routing, network position sync for held items.

**Key architectural changes:**

1. **No `ClientNetworkTransform` needed.** In s&box, set `GameObject.NetworkMode = NetworkMode.Object` on pickable items. The owner (whoever holds it) syncs the transform automatically when they have ownership. Transfer ownership on pickup.

2. **`NetworkObject.NetworkObjectId` → `GameObject.Id` (Guid).** Use Guid for object identification in RPCs.

3. **Camera reference:** Replace `Camera _playerCamera` with `Scene.Camera` for the local player.

4. **Interaction raycasting:**

```csharp
// Unity
Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayerMask);

// s&box
var tr = Scene.PhysicsWorld.Trace
    .Ray(Scene.Camera.WorldPosition, Scene.Camera.WorldPosition + Scene.Camera.WorldRotation.Forward * PickupRange)
    .IgnoreGameObjectHierarchy(GameObject)
    .Run();
if (tr.Hit)
{
    var interactable = tr.Body?.GameObject?.GetComponent<IInteractable>();
}
```

5. **Held item position update (replaces ClientNetworkTransform manually-set positions):**

```csharp
// s&box — hold item at offset from camera
protected override void OnUpdate()
{
    if (_heldObject == null || IsProxy) return;

    var holdPos = Scene.Camera.WorldPosition
        + Scene.Camera.WorldRotation * _holdOffset;
    var holdRot = Scene.Camera.WorldRotation * Rotation.From(_holdRotation);

    _heldObject.WorldPosition = holdPos;
    _heldObject.WorldRotation = holdRot;
}
```

6. **Pickup ownership transfer:**

```csharp
[Rpc.Host]
public void PickupNetworkObject(Guid objectId)
{
    var go = Scene.Directory.FindByGuid(objectId);
    if (go == null) return;
    // Transfer ownership to the requesting client
    go.Network.AssignOwnership(Rpc.Caller);
    go.GetComponent<Rigidbody>()?.SetKinematic(true);
}
```

**Interface translations:**
- `IInteractable` → keep as C# interface, no change needed
- `IPlaceable` → keep as C# interface, no change needed

---

### 6.4 NPC System

**Scripts:** `NPCInteractionController.cs` (13 states, 1224 lines), `NPCSpawnManager.cs`, `NPCAnimationController.cs`

**What to keep:** The entire state machine logic — all 13 states, transitions, item picking behavior, counter placement, checkout flow. The AI logic is engine-agnostic.

**Key changes:**

**NavMesh → s&box NavMesh Agent**

```csharp
// Unity
[RequireComponent(typeof(NavMeshAgent))]
public class NPCInteractionController : NetworkBehaviour
{
    private NavMeshAgent _agent;
    void Awake() { _agent = GetComponent<NavMeshAgent>(); }
    void MoveToTarget(Vector3 pos) { _agent.SetDestination(pos); }
    bool IsAtDestination() => !_agent.pathPending && _agent.remainingDistance < 0.5f;
}

// s&box
public sealed class NPCController : Component
{
    private NavMeshAgent _agent;

    protected override void OnAwake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void MoveToTarget(Vector3 pos)
    {
        _agent.MoveTo(pos);
    }

    bool IsAtDestination()
    {
        return !_agent.IsMoving || _agent.TargetPosition.Distance(WorldPosition) < 30f;
    }
}
```

**Animator → SkinnedModelRenderer + AnimationGraph**

```csharp
// Unity
private Animator _animator;
_animator.SetBool("IsWalking", true);

// s&box — use AnimatedModelComponent and set parameters
private AnimatedModelComponent _model;
_model.Set("b_walking", true);
_model.Set("f_speed", _agent.Speed);
```

**Server-only AI check:**

```csharp
// Unity
void Update()
{
    if (!IsServer) return;  // AI only runs on server
    RunStateMachine();
}

// s&box
protected override void OnFixedUpdate()
{
    if (!Networking.IsHost) return;  // AI only runs on host
    RunStateMachine();
}
```

**Finding objects in scene:**

```csharp
// Unity — FindObjectsOfType (slow)
CounterSlot[] slots = FindObjectsOfType<CounterSlot>();

// s&box — Scene-wide component query
var slots = Scene.GetAllComponents<CounterSlot>().ToList();
```

**NPCSpawnManager — Coroutine → async:**

```csharp
// Unity
private IEnumerator SpawnCoroutine()
{
    yield return new WaitForSeconds(initialDelay);
    while (_spawnQueue.Count > 0)
    {
        SpawnNextNPC();
        yield return new WaitUntil(() => _activeNPC == null);
        yield return new WaitForSeconds(delayAfterExit);
    }
    OnAllNPCsFinished?.Invoke();
}

// s&box
private async Task SpawnLoop()
{
    await Task.DelaySeconds(InitialDelay);
    while (_spawnQueue.Count > 0)
    {
        SpawnNextNPC();
        while (_activeNPC.IsValid()) await Task.Yield();
        await Task.DelaySeconds(DelayAfterExit);
    }
    OnAllNPCsFinished?.Invoke();
}
```

---

### 6.5 Shift Manager

**Script:** `ShiftManager.cs`

The full state machine (Dawn → DayShift → Transition → NightShift → Dawn) is already implemented. This is one of the cleanest conversions.

**Changes summary:**

```csharp
// Unity header
public class ShiftManager : NetworkBehaviour
{
    public NetworkVariable<int> CurrentPhase { get; } =
        new NetworkVariable<int>((int)ShiftPhase.Dawn,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> EscapedDoppelgangers { get; } =
        new NetworkVariable<int>(0, ReadPermission.Everyone, WritePermission.Server);

    public NetworkVariable<int> CurrentNight { get; } =
        new NetworkVariable<int>(1, ReadPermission.Everyone, WritePermission.Server);
}

// s&box equivalent
public sealed class ShiftManager : Component
{
    [Sync(SyncFlags.FromHost)] public int CurrentPhase { get; set; } = (int)ShiftPhase.Dawn;
    [Sync(SyncFlags.FromHost)] public int EscapedDoppelgangers { get; set; } = 0;
    [Sync(SyncFlags.FromHost)] public int CurrentNight { get; set; } = 1;

    // Static accessor (replaces singleton pattern)
    public static ShiftManager Instance { get; private set; }
    protected override void OnAwake() { Instance = this; }
}
```

**Phase change callbacks (replaces OnValueChanged):**

```csharp
// Unity — subscribing to NetworkVariable change
CurrentPhase.OnValueChanged += (oldVal, newVal) => OnPhaseChanged((ShiftPhase)newVal);

// s&box — use OnRefresh() instead
private int _lastPhase = -1;

protected override void OnRefresh()
{
    if (CurrentPhase != _lastPhase)
    {
        _lastPhase = CurrentPhase;
        OnPhaseChanged((ShiftPhase)CurrentPhase);
    }
}
```

**ClientRpc for one-shot effects (lights flicker, audio):**

```csharp
// Unity
[ClientRpc]
private void TriggerLightsFlickerClientRpc() { /* all clients play flicker */ }

// s&box
[Rpc.Broadcast]
public void TriggerLightsFlicker() { /* all clients play flicker */ }
```

**`IsDebugUIActive` static bool:** Remove the `OnGUI()` debug overlay — use s&box's built-in DevCamera / console commands or a `[ConVar]` for debug flags instead.

```csharp
// s&box debug flag
[ConVar("shift_debug")]
public static bool IsDebugUIActive { get; set; } = false;
```

---

### 6.6 Counter, ID Card & Cash Register

**Scripts:** `CounterSlot.cs`, `IDCardSlot.cs`, `IDCardInteraction.cs`, `IDCardVisuals.cs`, `CashRegister.cs`

These are relatively self-contained. Main changes are networking pattern.

**CounterSlot — NetworkVariable for occupant:**

```csharp
// Unity
private NetworkVariable<ulong> _occupantObjectId = new NetworkVariable<ulong>(
    0, ReadPermission.Everyone, WritePermission.Server);

// s&box
[Sync(SyncFlags.FromHost)] public Guid OccupantId { get; set; } = Guid.Empty;

public bool IsOccupied => OccupantId != Guid.Empty;
```

**CashRegister — `FindObjectsOfType` removal:**

```csharp
// Unity
NPCInteractionController[] allNPCs = FindObjectsOfType<NPCInteractionController>();

// s&box
var allNPCs = Scene.GetAllComponents<NPCController>().ToArray();
```

**Doppelganger check in ProcessCheckout:**

```csharp
// s&box CashRegister
[Rpc.Host]
public void ProcessCheckout()
{
    var npc = FindClosestNPCAtCounter();
    if (npc == null) return;

    if (npc.IsDoppelganger)
    {
        ShiftManager.Instance.EscapedDoppelgangers++;
        ShiftScoreManager.Instance.RecordWrongApproval();
    }
    else
    {
        ShiftScoreManager.Instance.RecordCorrectApproval();
    }

    npc.Checkout();
}
```

---

### 6.7 Computer Screen & Information UI

**Scripts:** `ComputerScreen.cs`, `ComputerScreenController.cs`, `NPCInfoDisplay.cs`, `NPCIdentityField.cs`, `PrescriptionDisplay.cs`, `PrescriptionField.cs`, `NPISearchPanel.cs`

The most significant change here is the **UI layer** — everything that was TextMeshPro + Unity Canvas components becomes Razor panels. The C# logic (tab switching, field binding, NPI search) stays the same.

**ComputerScreen activation — focus mode remains the same:**

```csharp
// s&box ComputerScreen
public sealed class ComputerScreen : Component
{
    [Property] public Transform FocusCameraTarget { get; set; }
    [Sync(SyncFlags.FromHost)] public long CurrentUserId { get; set; } = -1;

    public bool IsActive => _isActive;
    private bool _isActive;

    public void Activate()
    {
        if (IsActive || (Networking.IsActive && CurrentUserId != -1)) return;
        RequestActivation();
    }

    [Rpc.Host]
    private void RequestActivation()
    {
        if (CurrentUserId != -1) return;
        CurrentUserId = (long)Rpc.Caller.SteamId;
        ActivateForUser(Rpc.Caller);
    }

    [Rpc.Owner]
    private void ActivateForUser(Connection conn)
    {
        _isActive = true;
        GetComponent<FocusStateManager>()?.EnterFocus(FocusCameraTarget);
        // Show computer UI panel
    }
}
```

**NPCIdentityField / PrescriptionField — bind to Razor instead of TMP:**

These Unity components find a `TextMeshProUGUI` by name and set its `.text`. In s&box, the Razor panel reads directly from data:

```razor
@* NPCInfoPanel.razor *@
@inherits PanelComponent

<root>
    @if (CurrentNPC != null)
    {
        <div class="npc-info">
            <img src="@CurrentNPC.Identity.Photo?.ResourcePath" />
            <label class="name">@CurrentNPC.Identity.NpcName</label>
            <label class="dob">DOB: @CurrentNPC.Identity.DateOfBirth</label>
            <label class="address">@CurrentNPC.Identity.Address</label>
        </div>
    }
</root>
```

The `NPCIdentityField` and `PrescriptionField` component classes can be removed entirely — the Razor template replaces them.

---

### 6.8 Pill Counting Station

**Scripts:** `PillCountingStation.cs`, `PillSpawner.cs`, `PillScraper.cs`, `PillCountingChute.cs`, `PillCountUI.cs`, `FocusStateManager.cs`

**What to keep:** All mini-game logic. Pills are spawned as local (non-networked) physics objects, scraped into a chute, counted via trigger. The server-authoritative exclusive-access lock pattern.

**Key changes:**

```csharp
// Unity — lock with NetworkVariable<ulong>
private readonly NetworkVariable<ulong> _currentUserId = new NetworkVariable<ulong>(
    ulong.MaxValue, ReadPermission.Everyone, WritePermission.Server);

[ServerRpc]
private void RequestActivationServerRpc(ulong clientId) { ... }

// s&box
[Sync(SyncFlags.FromHost)] public long CurrentUserId { get; set; } = -1;

[Rpc.Host]
public void RequestActivation()
{
    if (CurrentUserId != -1) return;
    CurrentUserId = (long)Rpc.Caller.SteamId;
    ActivateForCaller(Rpc.Caller);
}

[Rpc.Owner]
private void ActivateForCaller(Connection conn)
{
    DoActivate(); // spawn pills, enable scraper/chute locally
}
```

**PillScraper — mouse input:**

```csharp
// Unity
void Update()
{
    if (!_isActive) return;
    float mx = Input.GetAxis("Mouse X");
    float my = Input.GetAxis("Mouse Y");
    // apply movement to scraper transform
}

// s&box
protected override void OnUpdate()
{
    if (!_isActive || IsProxy) return;
    var delta = Input.MouseDelta;
    // delta.x is horizontal, delta.y is vertical
    WorldPosition += new Vector3(delta.x, 0, delta.y) * ScraperSensitivity * Time.Delta;
}
```

**PillCountUI — replace WorldSpace Canvas with WorldPanel:**

The world-space pill count display (showing "15 / 30" above the tray) was a Unity World Space Canvas. Replace with a `WorldPanel` component:
- Add `WorldPanel` component to a child GameObject above the tray.
- Create `PillCountUI.razor` with the counter display.
- The `WorldPanel` replaces the World Space Canvas — no other architectural changes.

---

### 6.9 Pill Filling Station

**Scripts:** `PillFillingStation.cs`, `RotatingHopper.cs`, `DispensingController.cs`, `FillCounterUI.cs`, `MedicationBottle.cs`, `MedicationData.cs`

Same pattern as PillCountingStation — exclusive lock, focus mode, local physics.

**MedicationData (ScriptableObject → GameResource):**

```csharp
// s&box
[GameResource("Medication", "medication", "Defines a medication type")]
public class MedicationData : GameResource
{
    [Property] public string MedicationName { get; set; }
    [Property] public Color PillColor { get; set; }
    [Property] public Model PillModel { get; set; }
}
```

**MedicationBottle (NetworkObject):**

```csharp
// s&box — pickable networked item
public sealed class MedicationBottle : Component
{
    [Property] public MedicationData Data { get; set; }
    [Sync] public bool IsFilled { get; set; }
    [Sync] public int CurrentPillCount { get; set; }
}
```

---

### 6.10 Delivery Station

**Script:** `DeliveryStation.cs`

Spawns inventory boxes on player interaction.

```csharp
// Unity
[ServerRpc(RequireOwnership = false)]
public void SpawnBoxServerRpc() { /* spawn and NetworkObject.Spawn() */ }

// s&box
[Rpc.Host]
public void SpawnBox()
{
    var prefab = ResourceLibrary.Get<PrefabFile>("prefabs/inventory_box.prefab");
    var box = SceneUtility.Instantiate(prefab, SpawnPoint);
    box.NetworkSpawn();
}
```

---

### 6.11 Shelf System

**Scripts:** `ShelfSection.cs`, `ShelfSlot.cs`, `InventoryBox.cs`, `ItemPlacementManager.cs`, `BoxItemPreview.cs`

**ShelfSection / ShelfSlot** — non-networked logic stays the same. For networked slot occupancy (previously `ShelfSlotNetwork.cs`):

```csharp
// s&box ShelfSlot
public sealed class ShelfSlot : Component
{
    [Sync(SyncFlags.FromHost)] public Guid OccupantId { get; set; } = Guid.Empty;
    public bool IsOccupied => OccupantId != Guid.Empty;
}
```

**ItemPlacementManager** — ghost preview using s&box physics traces:
- The ghost preview system (showing where an item will land on the shelf) remains the same concept.
- Replace `Physics.Raycast` with the s&box trace API.
- Replace `Renderer.material` with `ModelRenderer.MaterialOverride` to show a ghost material.

---

### 6.12 Dialogue System

**Scripts:** `DialogueManager.cs`, `DialogueData.cs`, `DialogueHistory.cs`, `NPCDialogueTrigger.cs`, `NPCInfoTalkButton.cs`

**DialogueManager** — replace the entire Canvas-based UI with a Razor panel. The core logic (node graph traversal, response buttons, camera lerp toward NPC) stays the same.

```csharp
// s&box DialoguePanel.cs (PanelComponent)
public sealed class DialoguePanel : PanelComponent
{
    [Sync] public string SpeakerName { get; set; }
    [Sync] public string DialogueBody { get; set; }

    public List<DialogueResponse> Responses { get; set; } = new();

    protected override int BuildHash()
        => HashCode.Combine(SpeakerName, DialogueBody, Responses.Count);
}
```

```razor
@* DialoguePanel.razor *@
@inherits PanelComponent

<root>
    <div class="dialogue-overlay">
        <div class="speaker-name">@SpeakerName</div>
        <div class="dialogue-body">@DialogueBody</div>
        <div class="responses">
            @foreach (var r in Responses)
            {
                <button onclick="@(() => OnResponse(r.Key))">@r.Text</button>
            }
        </div>
    </div>
</root>
```

**DialogueData.cs** — the C# data classes and JSON loading stay the same. Replace `JsonUtility.FromJson<T>` with `Json.Deserialize<T>`.

**NPCDialogueTrigger / NPCInfoTalkButton** — same logic; remove `[SerializeField] TextMeshProUGUI` refs and replace with Razor panel bindings.

**Multiplayer dialogue lock** — `NetworkVariable<ulong> _dialogueOwnerId` → `[Sync(SyncFlags.FromHost)] public long DialogueOwnerId { get; set; } = -1`.

---

### 6.13 Doppelganger Data (ScriptableObjects)

**Scripts:** `NPCIdentity.cs`, `PrescriptionData.cs`, `DoppelgangerProfile.cs`, `PrescriberDatabase.cs`, `RoundConfig.cs`, `ItemCategory.cs`

All of these are ScriptableObjects with no logic — pure data containers. Convert directly to `GameResource`:

```csharp
// s&box NPCIdentity.cs
[GameResource("NPC Identity", "npcid", "NPC personal data for ID verification")]
public class NPCIdentity : GameResource
{
    [Property] public string NpcName { get; set; }
    [Property] public string DateOfBirth { get; set; }
    [Property] public string Address { get; set; }
    [Property] public string IdNumber { get; set; }
    [Property] public Texture Photo { get; set; }
}

// s&box PrescriptionData.cs
[GameResource("Prescription Data", "rxdata", "Prescription details for verification")]
public class PrescriptionData : GameResource
{
    [Property] public string MedicationName { get; set; }
    [Property] public int Quantity { get; set; }
    [Property] public string Dosage { get; set; }
    [Property] public string PrescriberName { get; set; }
    [Property] public string PrescriberNPI { get; set; }
    [Property] public string PrescriberSpecialty { get; set; }
    [Property] public string PrescriberAddress { get; set; }
    [Property] public List<string> FillHistory { get; set; } = new();
}

// s&box DoppelgangerProfile.cs
public enum DiscrepancyType
{
    PhotoMismatch, InvalidNPI, NoFillHistory,
    WrongPrescriberSpecialty, DoseJump, NonStandardQuantity, PrescriberOutsideArea
}

[GameResource("Doppelganger Profile", "doppelganger", "Fake data overrides for doppelgangers")]
public class DoppelgangerProfile : GameResource
{
    [Property] public DiscrepancyType[] Discrepancies { get; set; }
    [Property] public Texture FakePhoto { get; set; }
    [Property] public string FakeDOB { get; set; }
    [Property] public string FakeAddress { get; set; }
    [Property] public string FakePrescriberNPI { get; set; }
    [Property] public string FakePrescriberSpecialty { get; set; }
    [Property] public string FakeDosage { get; set; }
    [Property] public int FakeQuantity { get; set; }
}

// s&box RoundConfig.cs
[System.Serializable]
public class QueueEntry
{
    [Property] public bool IsFixed { get; set; }
    [Property] public FileReference<PrefabFile> FixedNpcPrefab { get; set; }
    [Property] public bool ForceDoppelganger { get; set; }
    [Property] public DoppelgangerProfile FixedProfile { get; set; }
}

[GameResource("Round Config", "roundconfig", "NPC queue configuration for a round")]
public class RoundConfig : GameResource
{
    [Property] public List<QueueEntry> Queue { get; set; } = new();
    [Property] public int DoppelgangerCount { get; set; } = 2;
}
```

**PrescriberDatabase** — list of valid NPI records, loaded once. Same pattern.

---

### 6.14 Gun Case & Shooting

**Script:** `GunCase.cs`

The gun pickup / put-back / exclusivity pattern is identical to any other exclusive-access station. Shooting uses a raycast that checks for NPC components.

```csharp
// s&box GunCase
public sealed class GunCase : Component
{
    [Property] public GameObject GunCaseModel { get; set; }
    [Property] public FileReference<PrefabFile> HeldGunPrefab { get; set; }
    [Property] public float ShootRange { get; set; } = 3000f;

    [Sync(SyncFlags.FromHost)] public long HolderSteamId { get; set; } = -1;

    public bool IsHeld => HolderSteamId != -1;
    public bool IsHeldByMe => !IsProxy && HolderSteamId == (long)Connection.Local.SteamId;

    protected override void OnUpdate()
    {
        if (!IsHeldByMe) return;
        if (Input.Pressed("Attack1")) Shoot();
    }

    private void Shoot()
    {
        var tr = Scene.PhysicsWorld.Trace
            .Ray(Scene.Camera.WorldPosition,
                 Scene.Camera.WorldPosition + Scene.Camera.WorldRotation.Forward * ShootRange)
            .Run();

        if (!tr.Hit) return;

        var npc = tr.Body?.GameObject?.GetComponent<NPCController>();
        if (npc == null) return;

        ShootNPC(npc.GameObject.Id, tr.HitPosition, tr.Normal);
    }

    [Rpc.Host]
    private void ShootNPC(Guid npcId, Vector3 hitPos, Vector3 hitNormal)
    {
        var go = Scene.Directory.FindByGuid(npcId);
        var npc = go?.GetComponent<NPCController>();
        if (npc == null) return;

        if (npc.IsDoppelganger)
            ShiftScoreManager.Instance.RecordCorrectKill();
        else
            ShiftScoreManager.Instance.RecordWrongKill();

        npc.Kill();
        SpawnBloodEffect(hitPos, hitNormal);
    }

    [Rpc.Broadcast]
    private void SpawnBloodEffect(Vector3 pos, Vector3 normal)
    {
        // Spawn blood splatter particle / decal locally on all clients
    }
}
```

---

### 6.15 Score Manager

**Script:** `ShiftScoreManager.cs`

Pure data — only `NetworkVariable` → `[Sync]` changes needed.

```csharp
// s&box
public sealed class ShiftScoreManager : Component
{
    public static ShiftScoreManager Instance { get; private set; }
    protected override void OnAwake() { Instance = this; }

    [Sync(SyncFlags.FromHost)] public int Money { get; set; } = 0;
    [Sync(SyncFlags.FromHost)] public int CustomersServed { get; set; }
    [Sync(SyncFlags.FromHost)] public int DoppelgangersCaught { get; set; }
    [Sync(SyncFlags.FromHost)] public int DoppelgangersEscaped { get; set; }
    [Sync(SyncFlags.FromHost)] public int InnocentsKilled { get; set; }

    // Only called on host — all methods add server-authoritative changes
    public void RecordCorrectApproval()  { Money += 50; CustomersServed++; }
    public void RecordWrongApproval()    { DoppelgangersEscaped++; }
    public void RecordCorrectKill()      { DoppelgangersCaught++; Money += 25; }
    public void RecordWrongKill()        { InnocentsKilled++; Money -= 100; }
}
```

---

### 6.16 Blood Decal & Mop

**Scripts:** `BloodDecal.cs`, `BloodSplatterEffect.cs`, `Mop.cs`

**BloodDecal** — Unity Decal Projectors → s&box `DecalRenderer` component.

```csharp
// s&box — spawn a decal at hit point
var decalGo = new GameObject("BloodDecal");
decalGo.WorldPosition = hitPos;
decalGo.WorldRotation = Rotation.LookAt(hitNormal);
var decal = decalGo.AddComponent<DecalRenderer>();
decal.Material = Material.Load("materials/blood_decal.vmat");
decal.Size = new Vector3(32f, 32f, 8f);
```

**Mop** — cleanup logic is identical. Replace `Input.GetMouseButton(0)` with `Input.Down("Attack1")`. The ServerRpc → ClientRpc broadcast pattern for cleaning decals becomes `[Rpc.Host]` → `[Rpc.Broadcast]`:

```csharp
// s&box Mop
[Rpc.Host]
private void CleanNearby(Vector3 cleanPoint, float radius)
{
    // server validates clean request, then broadcast to all clients
    DoBroadcastClean(cleanPoint, radius);
}

[Rpc.Broadcast]
private void DoBroadcastClean(Vector3 cleanPoint, float radius)
{
    foreach (var decal in BloodDecal.Active.ToList())
    {
        if (decal.WorldPosition.Distance(cleanPoint) <= radius)
            decal.Remove();
    }
}
```

---

### 6.17 Door

**Script:** `Door.cs`

Doors are typically animated. In s&box:
- Use an `AnimatedModelComponent` with open/close animation clips.
- Or use a `Rigidbody` with a `HingeJoint` for physics-based doors.
- Interaction (press E) follows the same pattern as other interactables.

```csharp
public sealed class Door : Component
{
    [Property] public bool IsOpen { get; set; }
    private AnimatedModelComponent _model;

    protected override void OnAwake() { _model = GetComponent<AnimatedModelComponent>(); }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        _model.Set("b_open", IsOpen);
    }
}
```

---

### 6.18 Player Networking Utilities

**Scripts:** `PlayerRegistry.cs`, `PlayerSetup.cs`, `PlayerSpawnManager.cs`, `QuickConnect.cs`, `ClientNetworkTransform.cs`, `CounterSlotNetwork.cs`, `ShelfSlotNetwork.cs`, `DisconnectHandler.cs`

**PlayerSpawnManager** — s&box handles player spawning natively. When a client connects, the host calls `go.NetworkSpawn(connection)` to assign them their player object:

```csharp
// s&box — listen for connections
public sealed class GameNetworkManager : Component
{
    [Property] public FileReference<PrefabFile> PlayerPrefab { get; set; }
    [Property] public List<Transform> SpawnPoints { get; set; }

    protected override void OnStart()
    {
        if (!Networking.IsHost) return;
        Networking.OnConnect += OnPlayerConnected;
        Networking.OnDisconnect += OnPlayerDisconnected;
    }

    private void OnPlayerConnected(Connection conn)
    {
        var spawnPt = SpawnPoints[Random.Int(SpawnPoints.Count - 1)];
        var prefab = ResourceLibrary.Get<PrefabFile>(PlayerPrefab.ResourcePath);
        var player = SceneUtility.Instantiate(prefab, spawnPt);
        player.NetworkSpawn(conn);
    }

    private void OnPlayerDisconnected(Connection conn)
    {
        // Release any exclusive locks held by this client
        foreach (var station in Scene.GetAllComponents<PillCountingStation>())
        {
            if (station.CurrentUserId == (long)conn.SteamId)
                station.ForceReleaseLock();
        }
        // Repeat for ComputerScreen, GunCase, etc.
    }
}
```

**ClientNetworkTransform** — Remove entirely. s&box handles transform sync automatically for `NetworkMode.Object` objects when the owner updates position.

**CounterSlotNetwork / ShelfSlotNetwork** — These Unity scripts existed to work around NGO limitations on non-NetworkObject slots. In s&box, add `[Sync(SyncFlags.FromHost)]` properties directly to `CounterSlot` and `ShelfSlot` — no separate networking component needed.

**DisconnectHandler** — Replace with the `Networking.OnDisconnect` callback in `GameNetworkManager` above.

**QuickConnect** — s&box uses Steam lobbies. Replace with:

```csharp
// Host a game
await Networking.StartServer();

// Join a game
await Networking.Connect(lobbyId);
```

**PlayerSetup** — s&box doesn't need per-client prefab activation. The `NetworkSpawn(connection)` assignment already establishes ownership. Use `IsProxy` checks instead of `enabled = true/false` on components.

---

## 7. UI Conversion (Canvas/TMP → Razor)

Every UI element needs to be rebuilt as Razor panels. The visual design can be preserved exactly — only the implementation changes.

### Panel Inventory

| Unity Panel | s&box Razor File | Type |
|---|---|---|
| Dialogue overlay | `DialoguePanel.razor` | `ScreenPanel` |
| NPC info panel | `NPCInfoPanel.razor` | `ScreenPanel` |
| Prescription panel | `PrescriptionPanel.razor` | `ScreenPanel` |
| Computer screen | `ComputerScreenUI.razor` | `ScreenPanel` |
| HUD (money, phase, score) | `HudPanel.razor` | `ScreenPanel` |
| Pill count display | `PillCountUI.razor` | `WorldPanel` |
| Fill count display | `FillCountUI.razor` | `WorldPanel` |
| ID card visuals | `IDCardVisuals.razor` | `WorldPanel` |
| Shift end summary | `ShiftSummaryPanel.razor` | `ScreenPanel` |

### Razor Panel Template

Every Razor panel requires three files:

```
/ui/HudPanel.cs           ← C# PanelComponent — exposes data and BuildHash
/ui/HudPanel.razor        ← Razor markup — HTML-like structure
/ui/HudPanel.razor.scss   ← SCSS stylesheet — CSS flexbox layout
```

### HUD Panel Example

```csharp
// HudPanel.cs
public sealed class HudPanel : PanelComponent
{
    ShiftManager _shift;
    ShiftScoreManager _score;

    protected override void OnStart()
    {
        _shift = Scene.GetAllComponents<ShiftManager>().FirstOrDefault();
        _score = Scene.GetAllComponents<ShiftScoreManager>().FirstOrDefault();
    }

    string PhaseName => ((ShiftManager.ShiftPhase)(_shift?.CurrentPhase ?? 0)).ToString();
    int Money => _score?.Money ?? 0;
    int Night => _shift?.CurrentNight ?? 1;

    protected override int BuildHash()
        => HashCode.Combine(PhaseName, Money, Night);
}
```

```razor
@* HudPanel.razor *@
@inherits PanelComponent

<root>
    <div class="hud">
        <div class="top-left">
            <label class="phase">Night @Night — @PhaseName</label>
        </div>
        <div class="top-right">
            <label class="money">$@Money</label>
        </div>
    </div>
</root>
```

```scss
/* HudPanel.razor.scss */
.hud {
    position: absolute;
    top: 0; left: 0; right: 0;
    padding: 16px;
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    pointer-events: none;
}
.phase, .money {
    color: white;
    font-size: 18px;
    text-shadow: 1px 1px 2px black;
}
```

### WorldPanel for In-World Text

For the pill count display above the pill counting tray (previously a World Space Canvas):

```csharp
// PillCountUI.cs
public sealed class PillCountUI : PanelComponent
{
    [Property] public int CurrentCount { get; set; }
    [Property] public int TargetCount { get; set; }

    protected override int BuildHash() => HashCode.Combine(CurrentCount, TargetCount);
}
```

The GameObject needs a `WorldPanel` component instead of a Canvas. Set `WorldPanel.PanelSize` to match the desired world-space size.

---

## 8. Missing Systems (Build in s&box)

These systems have no Unity implementation yet. Build them directly in s&box. The design spec is in `IMPLEMENTATION_PLAN.md` sections 7–15.

### 8.1 Monster AI (`MonsterController.cs`)

```csharp
public sealed class MonsterController : Component
{
    public enum MonsterState { Patrol, Investigating, Chasing, Stunned, Dying, Dead }

    [Sync(SyncFlags.FromHost)] public MonsterState State { get; set; }

    private NavMeshAgent _agent;
    private List<Transform> _patrolWaypoints;

    protected override void OnFixedUpdate()
    {
        if (!Networking.IsHost) return;  // AI server-only
        RunStateMachine();
    }

    private void RunStateMachine()
    {
        switch (State)
        {
            case MonsterState.Patrol:       DoPatrol(); break;
            case MonsterState.Investigating: DoInvestigate(); break;
            case MonsterState.Chasing:      DoChase(); break;
        }
    }
}
```

Subscribe to `NoiseSystem.OnNoiseEmitted` on the host. Use `Scene.GetAllComponents<NavMeshAgent>()` to reuse the existing nav mesh paths from daytime NPC patrols.

### 8.2 Noise System (`NoiseSystem.cs`)

```csharp
// Static event bus — no MonoBehaviour/Component needed
public static class NoiseSystem
{
    public static event Action<Vector3, float> OnNoiseEmitted;

    public static void EmitNoise(Vector3 position, float loudness)
    {
        if (!Networking.IsHost) return;  // only emit on host
        OnNoiseEmitted?.Invoke(position, loudness);
    }
}
```

Wire noise emissions into existing s&box code at these points:
- Sprint: `OnFixedUpdate` in `PlayerController` when sprint input is active
- Item drop: in `PlayerInteraction` when releasing a held object
- Mop: in `Mop.CleanNearby` call
- Door: in `Door.Toggle`
- Gun shot: in `GunCase.ShootNPC` after confirming hit

### 8.3 Recipe & Crafting System

```csharp
// IngredientData.cs — GameResource
public enum IngredientRole { Base, Catalyst, Vessel }
public enum ProcessingType { None, Grinding, Measuring }

[GameResource("Ingredient", "ingredient", "Crafting ingredient data")]
public class IngredientData : GameResource
{
    [Property] public string IngredientName { get; set; }
    [Property] public IngredientRole Role { get; set; }
    [Property] public ProcessingType Processing { get; set; }
    [Property] public FileReference<PrefabFile> WorldPrefab { get; set; }
    [Property] public FileReference<PrefabFile> ProcessedPrefab { get; set; }
}

// CraftingManager.cs — validates counter-slot crafting
public sealed class CraftingManager : Component
{
    [Sync(SyncFlags.FromHost)] public bool RecipeActive { get; set; }

    [Rpc.Host]
    public void ValidateCraft(Guid slot1Id, Guid slot2Id, Guid slot3Id)
    {
        // Check ingredients against current recipe
        // Success: despawn ingredients, spawn weapon
        // Failure: despawn ingredients, notify client
    }
}
```

### 8.4 Mortar Station (`MortarStation.cs`)

```csharp
public sealed class MortarStation : Component
{
    [Sync(SyncFlags.FromHost)] public long CurrentUserId { get; set; } = -1;
    [Property] public Transform FocusCameraTarget { get; set; }

    private float _totalRotation;
    private Vector2 _lastMousePos;
    private const float GrindsNeeded = 7f;
    private const float DegreesPerGrind = 360f;

    protected override void OnUpdate()
    {
        if (IsProxy || CurrentUserId != (long)Connection.Local.SteamId) return;

        // Track circular mouse motion
        var delta = Input.MouseDelta;
        var center = Screen.Size * 0.5f;
        var prev = _lastMousePos - center;
        var curr = (Input.MousePosition) - center;

        float angle = Vector2.SignedAngle(prev, curr);
        _totalRotation += Math.Abs(angle);
        _lastMousePos = Input.MousePosition;

        float progress = (_totalRotation % (GrindsNeeded * DegreesPerGrind)) / (GrindsNeeded * DegreesPerGrind);

        if (_totalRotation >= GrindsNeeded * DegreesPerGrind)
        {
            _totalRotation = 0;
            CompleteGrinding();
        }
    }

    [Rpc.Host]
    private void CompleteGrinding()
    {
        // Transform held ingredient into processed version
        // Spawn processed item prefab, despawn raw item
    }
}
```

### 8.5 Shift Lighting (`ShiftLighting.cs`)

```csharp
// Client-local, reads synced ShiftManager.CurrentPhase
public sealed class ShiftLighting : Component, DontExecuteOnServer
{
    [Property] public List<Light> PharmacyLights { get; set; }
    [Property] public DirectionalLight SunLight { get; set; }

    private int _lastPhase = -1;

    protected override void OnUpdate()
    {
        int phase = ShiftManager.Instance?.CurrentPhase ?? 0;
        if (phase == _lastPhase) return;
        _lastPhase = phase;
        ApplyLighting((ShiftManager.ShiftPhase)phase);
    }

    private void ApplyLighting(ShiftManager.ShiftPhase phase)
    {
        switch (phase)
        {
            case ShiftManager.ShiftPhase.DayShift:
                foreach (var l in PharmacyLights) l.LightColor = Color.White;
                break;
            case ShiftManager.ShiftPhase.NightShift:
                foreach (var l in PharmacyLights) l.LightColor = new Color(0.2f, 0.2f, 0.4f);
                _ = GradualDimming();
                break;
        }
    }

    private async Task GradualDimming()
    {
        float elapsed = 0f;
        while (ShiftManager.Instance?.CurrentPhase == (int)ShiftManager.ShiftPhase.NightShift)
        {
            elapsed += Time.Delta;
            float t = (elapsed / 120f).Clamp(0f, 1f);
            foreach (var l in PharmacyLights)
                l.Brightness = MathX.Lerp(1f, 0.1f, t);
            await Task.Yield();
        }
    }
}
```

### 8.6 Recipe Note (`RecipeNote.cs`)

```csharp
public sealed class RecipeNote : Component
{
    [Property] public List<Transform> PossibleLocations { get; set; }

    // Set by ShiftManager when starting day shift
    [Sync(SyncFlags.FromHost)] public string RecipeJson { get; set; }

    public void Interact()
    {
        // Show recipe in a full-screen overlay panel
        // Read RecipeJson, deserialize, display ingredient names + roles + processing steps
    }
}
```

---

## 9. Build Order & Priorities

### Phase 1 — Core Playability (Port what exists)

In this order — each step unblocks the next:

1. **Project setup** — create `/code/`, `/scenes/`, `/data/`, `/ui/` directories; copy assets
2. **Player controller** — `PlayerController.cs` (movement + look + jump)
3. **Focus state manager** — `FocusStateManager.cs`
4. **Data assets** — all `GameResource` files (`NPCIdentity`, `ItemCategory`, `RoundConfig`, etc.)
5. **NPC controller** — `NPCController.cs` (state machine port from `NPCInteractionController`)
6. **NPC spawn manager** — `NPCSpawnManager.cs`
7. **Counter + Cash Register** — `CounterSlot.cs`, `CashRegister.cs`
8. **Shelf system** — `ShelfSection.cs`, `ShelfSlot.cs`, `InventoryBox.cs`
9. **Delivery station** — `DeliveryStation.cs`
10. **Shift manager** — `ShiftManager.cs` (tie phases to NPC spawning + score)
11. **Object pickup** — `PlayerInteraction.cs` (central interaction hub)
12. **Dialogue system** — `DialogueManager.cs` + Razor panel
13. **Computer screen** — `ComputerScreen.cs` + NPC info Razor panels

### Phase 2 — Mini-Games

14. **Pill counting station** — `PillCountingStation.cs` + world panel UI
15. **Pill filling station** — `PillFillingStation.cs`
16. **Doppelganger data** — `DoppelgangerProfile`, `PrescriptionData`, `NPCIdentityField` in Razor
17. **Gun case** — `GunCase.cs` (shooting + doppelganger outcomes)
18. **Score manager** — `ShiftScoreManager.cs` + HUD panel

### Phase 3 — Horror Phase (New Systems)

19. **Noise system** — `NoiseSystem.cs` static event bus
20. **Monster AI** — `MonsterController.cs` + NavMesh
21. **Shift lighting** — `ShiftLighting.cs`
22. **Ingredient system** — `IngredientData` GameResource + `IngredientSpawner`
23. **Mortar station** — `MortarStation.cs`
24. **Recipe system** — `RecipeGenerator.cs` + `RecipeNote.cs` + `CraftingManager.cs`
25. **Crafted weapons** — `CraftedWeapon` base + 4 subtypes (vial, spray, trap, syringe)

### Phase 4 — Polish

26. **Blood decals & mop** — `BloodDecal.cs` + `Mop.cs` with DecalRenderer
27. **Shift lighting transitions** — animated flicker, gradual dim
28. **Shift summary panel** — end-of-shift score display
29. **Carry limit** — extend `PlayerInteraction` to hold 2 items
30. **Audio** — replace all `AudioSource.PlayOneShot` with `Sound.Play(...)`

---

*This document covers the complete translation of all 40+ scripts from Unity Netcode to s&box, every ScriptableObject to GameResource, all Canvas UI to Razor panels, and all missing systems. Cross-reference with `IMPLEMENTATION_PLAN.md` for design spec details on missing systems.*
