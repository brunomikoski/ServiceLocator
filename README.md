# ServiceLocator

<p align="center">
    <a href="https://github.com/brunomikoski/ServiceLocator/blob/master/LICENSE.md">
		<img alt="GitHub license" src ="https://img.shields.io/github/license/Thundernerd/Unity3D-PackageManagerModules" />
	</a>

</p> 
<p align="center">
    <a href="https://openupm.com/packages/com.brunomikoski.servicelocator/">
        <img src="https://img.shields.io/npm/v/com.brunomikoski.servicelocator?label=openupm&amp;registry_uri=https://package.openupm.com" />
    </a>

  <a href="https://github.com/brunomikoski/ServiceLocator/issues">
     <img alt="GitHub issues" src ="https://img.shields.io/github/issues/brunomikoski/ServiceLocator" />
  </a>

  <a href="https://github.com/brunomikoski/ServiceLocator/pulls">
   <img alt="GitHub pull requests" src ="https://img.shields.io/github/issues-pr/brunomikoski/ServiceLocator" />
  </a>

  <img alt="GitHub last commit" src ="https://img.shields.io/github/last-commit/brunomikoski/ServiceLocator" />
</p>

<p align="center">
    	<a href="https://github.com/brunomikoski">
        	<img alt="GitHub followers" src="https://img.shields.io/github/followers/brunomikoski?style=social">
	</a>	
	<a href="https://twitter.com/brunomikoski">
		<img alt="Twitter Follow" src="https://img.shields.io/twitter/follow/brunomikoski?style=social">
	</a>
</p>

A lightweight service locator pattern implementation for Unity, designed to give you full control over service initialization order, lifetime, and access across your project.

## Features

- Register and resolve services by type at runtime
- Automatic dependency ordering via `[ServiceImplementation(DependsOn = ...)]`
- `ServiceReference<T>` for lazy, cached access with lifecycle events
- `ServicesReporterBase` for grouping service registration by scene/context
- Optional code generation for static access to services
- Conditional service registration via `IConditionalService`
- Lifecycle callbacks via `IOnServiceRegistered` / `IOnServiceUnregistered`
- UniTask support for async service waiting

## Quick Start

### 1. Define a Service

Any class can be a service. Use `[ServiceImplementation]` to enable code generation and declare dependencies:

```csharp
[ServiceImplementation(DependsOn = new[] { typeof(SettingsService) })]
public class AudioService : MonoBehaviour, IOnServiceRegistered, IOnServiceUnregistered
{
    private readonly ServiceReference<SettingsService> _settingsService = new();

    void IOnServiceRegistered.OnRegisteredOnServiceLocator(ServiceLocator serviceLocator)
    {
        // Safe to access dependencies here - they are guaranteed to be registered
        _settingsService.Reference.Settings.Audio.MasterVolume.OnChangedEvent += OnMasterVolumeChanged;
    }

    void IOnServiceUnregistered.OnUnregisteredFromServiceLocator(ServiceLocator serviceLocator)
    {
        if (_settingsService.HasCachedReference)
        {
            _settingsService.Reference.Settings.Audio.MasterVolume.OnChangedEvent -= OnMasterVolumeChanged;
        }
    }
}
```

### 2. Register Services with a Reporter

Create `ServicesReporterBase` subclasses to group services by context (bootstrap, gameplay, lobby, etc.). The reporter registers on `Awake` and unregisters on `OnDestroy`, tying service lifetime to the scene.

```csharp
public class BootstrapServiceReporter : ServicesReporterBase
{
    [SerializeField] private AudioService _audioService;
    [SerializeField] private SettingsService _settingsService;
    [SerializeField] private InputService _inputService;

    protected override void RegisterServices()
    {
        ServiceLocator.Instance.RegisterInstance(_settingsService);
        ServiceLocator.Instance.RegisterInstance(_audioService);    // waits for SettingsService via DependsOn
        ServiceLocator.Instance.RegisterInstance(_inputService);
    }

    protected override void UnregisterServices()
    {
        ServiceLocator.Instance.UnregisterInstance(_audioService);
        ServiceLocator.Instance.UnregisterInstance(_settingsService);
        ServiceLocator.Instance.UnregisterInstance(_inputService);
    }
}
```

Scene-scoped reporters let you register services that only live during a specific game state:

```csharp
public class LobbyServiceReporter : ServicesReporterBase
{
    [SerializeField] private CameraService _cameraService;
    [SerializeField] private LobbyService _lobbyService;

    protected override void RegisterServices()
    {
        ServiceLocator.Instance.RegisterInstance(_cameraService);
        ServiceLocator.Instance.RegisterInstance(_lobbyService);
    }

    protected override void UnregisterServices()
    {
        ServiceLocator.Instance.UnregisterInstance(_cameraService);
        ServiceLocator.Instance.UnregisterInstance(_lobbyService);
    }
}
```

### 3. Access Services

#### ServiceReference (recommended)

`ServiceReference<T>` is the primary way to access services. It provides lazy resolution, caching, and automatic cache invalidation when services are registered/unregistered:

```csharp
public class InGameMenuController : MonoBehaviour
{
    private readonly ServiceReference<GameplayService> _gameplayService = new();
    private readonly ServiceReference<SessionService> _sessionService = new();

    private void OnEnable()
    {
        // .Reference resolves and caches the service on first access
        _lobbyButton.gameObject.SetActive(_sessionService.Reference.IsHost);
    }
}
```

#### Checking availability

```csharp
// Check if the service is registered (does not cache)
if (_audioService.Exists) { ... }

// Check if the service is registered AND we have a valid cached reference
if (_audioService.HasCachedReference) { ... }
```

#### Reacting to service registration

```csharp
// One-shot callback: fires immediately if already registered, then unsubscribes
_audioService.WhenServiceBecomesAvailable(() =>
{
    // Service is now available
});

// Persistent events: fires on every register/unregister
_audioService.OnWhenServiceGetsRegistered += OnAudioAvailable;
_audioService.OnWhenServiceGetsUnregistered += OnAudioRemoved;
```

#### Waiting for services (coroutine)

```csharp
private IEnumerator Start()
{
    yield return _audioService.WaitForServiceBeAvailableEnumerator();
    // AudioService is now available
}
```

#### Waiting for services (UniTask)

Requires the `UNITASK_ENABLED` scripting define:

```csharp
private async UniTask InitializeAsync(CancellationToken token)
{
    await _audioService.WaitForServiceBeAvailableAsync();
    // or directly:
    await ServiceLocator.Instance.WaitForServiceAsync<AudioService>(token);
}
```

#### Direct access

For cases where you know the service is registered:

```csharp
var audio = ServiceLocator.Instance.GetInstance<AudioService>();

// Safe variant
if (ServiceLocator.Instance.TryGetInstance<AudioService>(out var audio))
{
    audio.PlaySound(...);
}
```

### 4. Interface Registration

You can register a concrete type under an interface or base class, allowing consumers to depend on abstractions:

```csharp
// Register the concrete type under its base class
ServiceLocator.Instance.RegisterInstance<PhysicsService>(networkedPhysicsService);

// Consumers only know about the base type
private readonly ServiceReference<PhysicsService> _physics = new();
```

## Dependency Ordering

Services decorated with `[ServiceImplementation(DependsOn = ...)]` are automatically held in a waiting queue until all their dependencies are registered. This means you can register services in any order and the locator will resolve them correctly:

```csharp
[ServiceImplementation(DependsOn = new[] { typeof(SaveDataService), typeof(GraphicsService) })]
public class SettingsService : MonoBehaviour { ... }
```

If `SettingsService` is registered before `SaveDataService`, it will be queued and automatically registered once `SaveDataService` becomes available.

## Lifecycle Interfaces

| Interface | When it fires |
|---|---|
| `IOnServiceRegistered` | Immediately after the service is added to the locator |
| `IOnServiceUnregistered` | Immediately before the service is removed from the locator |
| `IConditionalService` | Called during registration to decide if the service should be registered at all |

## Code Generation

Classes with `[ServiceImplementation]` are picked up by the code generator, creating a static file for direct access:

```csharp
[ServiceImplementation(Category = "Game")]
public class GameplayService : MonoBehaviour { ... }

// Generated static access:
Services.Game.Gameplay            // returns the service instance
Services.Game.Ref.Gameplay        // returns the ServiceReference<T>
```

Configure code generation in **Project Settings > Service Locator**.

## How to Install

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.brunomikoski.servicelocator

To add the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.brunomikoski
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.brunomikoski.servicelocator`
- click <kbd>Add</kbd>
</details>

<details>
<summary>Add from GitHub</summary>

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/brunomikoski/ServiceLocator.git`
- click <kbd>Add</kbd>
</details>
