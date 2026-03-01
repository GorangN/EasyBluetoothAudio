# WPF MVVM & Clean Code Design Guide

## 1. General Principles

* **Microsoft Guidelines:** Coding conventions must follow the official C# and .NET design guidelines (naming conventions, coding style).
* **Compiler Warnings:** Code must compile with **zero errors and zero warnings**.
* Do not suppress warnings unless it is an absolute technical necessity (and if so, justify it via a pragma comment).
* Fix warnings regarding missing comments, unused variables, or unreachable code through refactoring.
* **Commit Rules:**
* Never check in broken or non-compiling code.
* Commit messages must be meaningful and linked to the respective DevOps item or ticket.

## 2. Refactoring & Clean Code

* **Boy Scout Rule:** Always leave the code cleaner than you found it. Refactor code whenever you touch it to align with these guidelines.
* **SOLID Principles:** Classes must have a single responsibility and program against interfaces rather than concrete implementations.
* **Coordination:** Changes affecting cross-domain interfaces, database structures, or global configurations must be coordinated with the team **before** implementation.

## 3. Formatting & Comments

* **Curly Brackets:**
* **Mandatory:** Every loop or conditional statement (If/Else) must use curly brackets, even if it is a single line.
* *Correct:* `if (a == b) { DoSomething(); }`
* *Incorrect:* `if (a == b) DoSomething();`
* **XML Documentation (Strict Rule):**
* **Every** public class, method, variable, property, and enum **must** have a detailed XML summary (`/// <summary>`).
* For methods, all parameters (`<param>`), return values (`<returns>`), and potential exceptions (`<exception>`) must be strictly documented.
* **Inline Comments:**
* **Forbidden for obvious code.** The code must be self-explanatory through excellent naming.
* Inline comments are *only* allowed to explain highly complex, business-specific workarounds or algorithms (explain the "Why", not the "What").
* **File Structure:**
* Every class, interface, and enum must reside in its own separate file.
* Files exceeding ~300 lines usually indicate a violation of the Single Responsibility Principle and should be split.

## 4. Naming Conventions

* **Casing:**
* **PascalCase:** Namespaces, Classes, Interfaces (must use an `I` prefix), Methods, Properties, Public Members (`MyNamespace`, `DoWork`, `SomeProperty`).
* **camelCase:** Parameters and local variables (`someParameter`, `myVariable`).
* **Private Backing Fields:**
* Private class-level fields must begin with an underscore followed by camelCase (e.g., `_myVariable`).
* Do not use type prefixes (no Hungarian Notation; use `_isActive` instead of `_bIsActive`).
* **Events:**
* Use `Closing` and `Closed` instead of `BeforeClose` and `AfterClose`.
* Event handlers should follow the `On[EventName]` pattern.

## 5. Interfaces & Dependency Injection

* Interfaces are **mandatory** for hardware integrations, external API calls, database access, and all external services.
* **Strict Access:** Access to modules implementing an interface must occur **exclusively** through the interface (Inversion of Control).
* *Forbidden:* Casting an interface to its concrete class to bypass the abstraction (`(module as MyConcreteService).DoSomethingSecret()`).
* **Dependency Injection:** Use an IoC container (e.g., `Microsoft.Extensions.DependencyInjection`) to inject services into ViewModels or other services via Constructor Injection.

## 6. Architecture & Library Structure

A strict layer separation (e.g., Clean Architecture) must be maintained. Circular dependencies are strictly forbidden.

* **Layer 1: Core / Domain:** Contains abstract domain models, enums, and global interfaces. References no other layers.
* **Layer 2: Application / Services:** Contains business logic, abstractions, and ViewModels.
* **Layer 3: Infrastructure / Data:** Concrete implementations of database access, external APIs, and hardware integrations.
* **Layer 4: UI / Presentation:** WPF Views and XAML (strictly bound to ViewModels).

## 7. WPF & MVVM Application Structure

* **Separation of Concerns:** Strict separation between GUI and business logic is required.
* **DataBindings:** The UI communicates with the application logic exclusively via DataBindings. GUI controls should only handle visual states.
* **Commands:** User actions (Buttons, Menus, Toolbars) must trigger `ICommand` or `RelayCommand` implementations in the ViewModel.
* **Code-Behind:** The `.xaml.cs` files must remain completely empty except for `InitializeComponent()`. **No business logic is allowed in the code-behind.**
