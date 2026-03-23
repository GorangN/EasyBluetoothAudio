# Claude Code Rules for EasyBluetoothAudio

## Workflow Orchestration

### Planning
- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately — don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity

### Subagent Strategy
- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

### Self-Improvement Loop
- After ANY correction from the user: update `tasks/lessons.md` with the pattern
- Write rules that prevent the same mistake from recurring
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start for relevant context

### Verification Before Done
- Never mark a task complete without proving it works
- Diff behavior between main and your changes when relevant
- Ask: "Would a staff engineer approve this?"
- Run tests, check logs, demonstrate correctness

### Elegance (Balanced)
- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: implement the elegant solution instead
- Skip for simple, obvious fixes — don't over-engineer

### Autonomous Bug Fixing
- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests — then resolve them
- Zero context switching required from the user

---

## Task Management

1. **Plan First**: Write plan to `tasks/todo.md` with checkable items
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step
5. **Document Results**: Add review section to `tasks/todo.md`
6. **Capture Lessons**: Update `tasks/lessons.md` after corrections

---

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.

---

## WPF MVVM & Clean Code Design Guide

### 1. General Principles

- **Microsoft Guidelines**: Follow official C# and .NET design guidelines (naming, coding style).
- **Zero Warnings**: Code must compile with zero errors and zero warnings.
  - Do not suppress warnings unless absolutely necessary (justify via pragma comment).
  - Fix warnings about missing comments, unused variables, or unreachable code through refactoring.
- **Commit Rules**:
  - Never commit broken or non-compiling code.
  - Commit messages must be meaningful and linked to the respective DevOps item or ticket.

### 2. Refactoring & Clean Code

- **Boy Scout Rule**: Always leave the code cleaner than you found it. Refactor whenever you touch code.
- **SOLID Principles**: Classes must have a single responsibility; program against interfaces, not concrete implementations.
- **Coordination**: Changes affecting cross-domain interfaces, database structures, or global configs must be coordinated with the team before implementation.

### 3. Formatting & Comments

- **Curly Brackets — Mandatory**: Every loop or conditional (if/else) must use curly brackets, even for single lines.
  - Correct: `if (a == b) { DoSomething(); }`
  - Incorrect: `if (a == b) DoSomething();`
- **XML Documentation — Strict Rule**:
  - Every public class, method, variable, property, and enum must have a detailed `/// <summary>`.
  - For methods: document all `<param>`, `<returns>`, and `<exception>` tags.
- **Inline Comments**:
  - Forbidden for obvious code. Code must be self-explanatory through excellent naming.
  - Only allowed to explain highly complex, business-specific workarounds or algorithms (explain the "Why", not the "What").
- **File Structure**:
  - Every class, interface, and enum must reside in its own separate file.
  - Files exceeding ~300 lines usually indicate an SRP violation and should be split.

### 4. Naming Conventions

- **PascalCase**: Namespaces, Classes, Interfaces (prefix `I`), Methods, Properties, Public Members.
- **camelCase**: Parameters and local variables.
- **Private Backing Fields**: Underscore + camelCase (e.g., `_myVariable`). No Hungarian Notation (use `_isActive`, not `_bIsActive`).
- **Events**: Use `Closing`/`Closed` instead of `BeforeClose`/`AfterClose`. Event handlers follow the `On[EventName]` pattern.

### 5. Interfaces & Dependency Injection

- Interfaces are **mandatory** for: hardware integrations, external API calls, database access, all external services.
- Access to modules implementing an interface must occur **exclusively** through the interface (IoC).
- **Forbidden**: Casting an interface to its concrete class to bypass the abstraction (e.g., `(module as MyConcreteService).DoSomethingSecret()`).
- **DI**: Use `Microsoft.Extensions.DependencyInjection`; inject services via Constructor Injection.

### 6. Architecture & Layer Separation

Strict Clean Architecture layers. Circular dependencies are strictly forbidden.

| Layer | Contents | Dependencies |
|-------|----------|--------------|
| **Core / Domain** | Abstract domain models, enums, global interfaces | None |
| **Application / Services** | Business logic, abstractions, ViewModels | Core only |
| **Infrastructure / Data** | DB access, external APIs, hardware implementations | Core + Application |
| **UI / Presentation** | WPF Views and XAML | Application (via ViewModels) |

### 7. WPF & MVVM

- **Separation of Concerns**: Strict separation between GUI and business logic.
- **DataBindings**: UI communicates with application logic exclusively via DataBindings. GUI controls handle visual states only.
- **Commands**: User actions (Buttons, Menus, Toolbars) must trigger `ICommand` / `RelayCommand` in the ViewModel.
- **Code-Behind**: `.xaml.cs` files must remain completely empty except for `InitializeComponent()`. **No business logic in code-behind.**
