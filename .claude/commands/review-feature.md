---
description: Review a .NET API feature against the v2 generator patterns, scalability rules, and references. Reports PASS/FAIL with evidence-backed findings.
allowed-tools: Read, Glob, Grep, Bash
---

# /review-feature — .NET Feature Pattern Reviewer

You are invoking the **dotnet-feature-reviewer-v2** agent to audit a specific feature.

## Instructions

1. **Load the reviewer agent:** Read `.claude/agents/dotnet-feature-reviewer-v2.md` and follow its 7-step execution protocol exactly.

2. **Load the skill for reference criteria:** Read `.claude/skills/dotnet-feature-generator-v2/SKILL.md` for the 14 scalability rules and templates.

3. **Conditionally load references** based on what the feature uses (determined in agent Step 2).

4. **Execute Steps 1–7** in order. Do not skip the reference-specific checks (Step 6) if references apply.

5. **Target feature:** $ARGUMENTS

If no arguments provided, ask the user which feature to review.
