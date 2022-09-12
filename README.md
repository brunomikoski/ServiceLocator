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


Simple service locator for Unity3D

## Features
 - Code generation for easy access
 - Easy mockup setup
 - Simple replace system
 - Simple and Fast

## FAQ (WIP)

### Setup 
The idea of the `ServiceLocator` is that you can have full control of initialization and lifetime of all your project services.
The way I suggest doing this is creating different places for registering services, for instance you might have your Main services that are available everywhere, and you also have your gameplay Services and maybe Meta Services.
So you would have 3 classes that extends `ServicesReporterBase` like `MainServiceReporter`, `GameplayServiceReporter` and `MetaServiceReporter` and they would report services on `RegisterServices()` and `UnregisterServices()`

If you have a service that depends on other services, you can use the `IDependsOnServices` interface and allow the ServiceLocator take care of their initialization

### Resolving Dependencies
The system provides 2 ways of dealing with dependencies. 
You can use both `IDependsOnServices` or `IDependsOnExplicitServices` interfaces to define dependencies

#### IDependsOnServices
This interface will try to find dependencies on your code and store this into a json file everytime you have script changes. So when you want the dependencies to be resolver you can use the `ServiceLocator.Instance.ResolveDependencies()` to make sure the class is initialized with all the dependencies resolved.

#### IDependsOnExplicitServices
Its almost the same as the `IDependsOnServices`, but this one allows you explicit state the dependencies on your classes.

### Code Generation
If you are using the code generation for services, any class that implements the `[ServiceImplementation]` will be added to the static `Service` file, so a class like this: 

```csharp
[ServiceImplementation(Category = "Main", Type = typeof(IFoo))]
public class FooService : MonoBehaviour, IFoo
{
    
}

```

Can be accessed by:
`Services.Main.Foo`

You can also directly access the ServiceReference that is a weak reference to check if the service is still exist or not

`Services.Main.Ref.Foo`

### How I can register a service to be available
 You can register any service by using `ServiceLocator.Instance.RegisterInstance(serviceInstance);`

### How I can access one service.
There's multiple ways of accessing it, you can use `ServiceLocator.Instance.GetInstance<T>` or you can use the `ServiceReference<T>` to access it as well, or if you are using the code generation you can use the quick access like `Services.YourService`

## How to install

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
<summary>Add from GitHub | <em>not recommended, no updates :( </em></summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/brunomikoski/ServiceLocator.git`
- click <kbd>Add</kbd>
</details>
