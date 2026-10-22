# Codex Rules for EasyBluetoothAudio

This root `AGENTS.md` is the project instruction entry point for Codex. Apply the behavioral guidelines below together with the existing workflow and WPF/MVVM requirements throughout this repository.

## Karpathy-Inspired Coding Guidelines

Adapted in full for Codex from [multica-ai/andrej-karpathy-skills: CLAUDE.md](https://github.com/multica-ai/andrej-karpathy-skills/blob/main/CLAUDE.md), reviewed on 2026-10-22 (upstream file blob `daced9bd64f25908ebedeb4701fb406985dc8366`). The upstream companion skill expresses the same four principles. The guidance is included here so it applies as project instructions without a separate skill invocation.

Favor careful decisions over rushed implementation, while scaling the process to the task: an obvious typo or one-line correction does not need the same ceremony as an architectural change.

### 1. Think Before Coding

- Make assumptions visible before implementation. If uncertainty affects correctness or scope, ask a focused question before doing dependent work.
- Explain materially different interpretations of a request instead of silently choosing one.
- Describe relevant tradeoffs, suggest a simpler solution when one exists, and challenge an approach when there is a concrete reason to do so.
- When requirements or evidence are confusing, stop the affected work, identify the specific gap, and seek clarification. Continue independent work when possible.

### 2. Simplicity First

- Implement only the requested behavior; avoid speculative additions.
- Keep one-off logic direct rather than introducing a reusable abstraction with no current need. The mandatory interfaces for hardware and external services below still apply.
- Add configuration or extension points only when the task requires them.
- Handle real failure modes at actual boundaries; do not add defensive branches for states the program cannot reach.
- Review the solution for unnecessary size and indirection. If a 200-line implementation can be expressed clearly in 50 lines, simplify it.
- Before finishing, consider whether a senior engineer would find the solution overcomplicated, and reduce it if so.

### 3. Surgical Changes

- Limit edits to code, documentation, and tests needed for the requested outcome. Every changed line must have a clear connection to that outcome.
- Leave adjacent formatting, comments, and unrelated code alone. Do not refactor working code just because it is nearby.
- Follow the surrounding style while honoring the explicit project requirements below.
- Report unrelated dead code when relevant rather than deleting it without a request.
- Remove imports, variables, and functions that become unused because of your changes. Leave pre-existing unused code outside the task untouched.
- Apply the Boy Scout rule within the task's scope: necessary cleanup is welcome; opportunistic cleanup must not expand the diff.

### 4. Goal-Driven Execution

- Define observable acceptance criteria before editing, then iterate until the relevant checks demonstrate success.
- For validation changes, first write tests for invalid inputs, then implement the validation and verify the expected rejection behavior.
- For a bug fix, first reproduce the failure with a regression test when feasible, then verify that the fix makes it pass.
- For a refactor, run relevant checks before and after to demonstrate preserved behavior.
- For multi-step work, record a short plan in `tasks/todo.md` that pairs each step with its verification:

  ```text
  1. Establish the failing behavior -> verify: a focused reproduction fails as expected.
  2. Apply the smallest complete fix -> verify: the reproduction and relevant tests pass.
  3. Review the final diff -> verify: acceptance criteria are met and unrelated edits are absent.
  ```

- Use concrete success conditions to work autonomously instead of repeatedly asking what "done" means. If success cannot be defined from the request and repository evidence, resolve that ambiguity first.
- Match verification to the change. For instructions-only changes, check content coverage, consistency, and the diff; application builds and tests are needed when application changes warrant them.

Evaluate these guidelines by the resulting work: focused diffs, simple solutions that avoid unnecessary rewrites, and consequential questions resolved before implementation.

## Workflow Orchestration

### Planning
- For ANY non-trivial task (3+ steps or architectural decisions), write a plan before implementation; use plan mode when the active Codex environment provides it, otherwise record the plan in `tasks/todo.md` and share it in a progress update
- If something goes sideways, STOP and re-plan immediately — don't keep pushing
- Include verification steps in the plan, not just implementation
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

- **Boy Scout Rule**: Leave changed code clear and maintainable. Limit refactoring to what the requested change requires, and remove unused code introduced by your changes; do not expand the task into unrelated cleanup.
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
