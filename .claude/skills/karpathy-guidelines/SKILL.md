---
name: karpathy-guidelines
description: Apply Karpathy-inspired coding behavior guidelines: think before coding, simplicity first, surgical changes, and goal-driven execution. Use when users request implementation, refactoring, debugging, or code review quality improvements, or mention overengineering, unnecessary diffs, ambiguity, or verification criteria. Triggers: karpathy, guidelines, think before coding, simplicity first, surgical changes, goal-driven execution, 先思考再编码, 简单优先, 外科手术式修改, 目标驱动执行.
---

# Karpathy Guidelines

Use this skill to keep coding behavior minimal, correct, and verifiable.

## 1) Think Before Coding / 先思考再编码

- State key assumptions explicitly before implementation.
- If intent is ambiguous, ask short clarification questions.
- If multiple interpretations exist, list options briefly and pick one only with evidence.
- Push back when a simpler approach can meet the same goal.

Quick check:
- Do I understand the exact success target?
- Am I assuming hidden requirements?

## 2) Simplicity First / 简单优先

- Implement the smallest change that satisfies the request.
- Avoid speculative abstractions, configurability, or future-proofing unless requested.
- Prefer straightforward control flow and existing project patterns.
- If a solution feels overbuilt, reduce it.

Quick check:
- Can this be done with fewer moving parts?
- Is each new type/function justified by current needs?

## 3) Surgical Changes / 外科手术式修改

- Touch only files and lines directly related to the user's task.
- Do not refactor adjacent code unless required for correctness.
- Preserve existing style and conventions in touched files.
- Remove only dead code introduced by your own change.

Quick check:
- Can every changed line be traced to the task?
- Did I avoid opportunistic cleanup outside scope?

## 4) Goal-Driven Execution / 目标驱动执行

For non-trivial tasks, define a short verify loop:

1. Implement minimal change
2. Run the smallest relevant verification (tests/build/lint/manual check)
3. Fix issues and re-verify until pass

Use explicit criteria:
- "Bug fixed" means reproducible case no longer fails.
- "Refactor complete" means behavior unchanged and checks pass.
- "Validation added" means invalid inputs are demonstrably rejected.

## Response Pattern / 输出模式

When acting under this skill, structure work as:

1. **Intent & assumptions** (1-3 bullets)
2. **Plan** (short, concrete steps)
3. **Minimal implementation**
4. **Verification result**
5. **What changed and why** (scope-limited)

## Anti-Patterns to Avoid / 避免事项

- Silent assumption jumps
- Overgeneralized architecture for small tasks
- Drive-by formatting/refactors
- "Looks good" without verification evidence
