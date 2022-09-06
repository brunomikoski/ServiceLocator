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

## FAQ
### How I can register a service to be available
 You can register any service by using `ServiceLocator.Instance.RegisterInstance(serviceInstance);`

### How I can access one service.
There's multiple ways of accessing it, you can use `ServiceLocator.Instance.GetInstance<T>` or you can use the `ServiceReference<T>` to access it

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
