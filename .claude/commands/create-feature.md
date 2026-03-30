---
description: Generate a complete .NET API feature using the multi-agent architecture (v2). Supports simple CRUD, hierarchical entities, batch operations, and distributed locking.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, Agent
---

# /create-feature — .NET API Feature Generator v2

You are invoking the **dotnet-feature-generator-v2** multi-agent pipeline.

## Instructions

1. **Load the agent definition:** Read `.claude/agents/dotnet-feature-generator-v2.md` and follow its 8-step execution protocol exactly.

2. **Load the skill:** Read `.claude/skills/dotnet-feature-generator-v2/SKILL.md` for templates and scalability rules.

3. **Conditionally load references** based on user requirements:
   - Hierarchical entity? → `.claude/skills/dotnet-feature-generator-v2/references/hierarchical-entities.md`
   - Batch operations? → `.claude/skills/dotnet-feature-generator-v2/references/batch-operations.md`
   - Distributed locking? → `.claude/skills/dotnet-feature-generator-v2/references/distributed-locking.md`
   - Custom caching? → `.claude/skills/dotnet-feature-generator-v2/references/caching-patterns.md`
   - High-growth entity? → `.claude/skills/dotnet-feature-generator-v2/references/high-growth-indexing.md`
   - Enhanced auditing? → `.claude/skills/dotnet-feature-generator-v2/references/auditing-patterns.md`

4. **Execute the agent's steps 1–8** in order. Do not skip the self-validation checklist (Step 5) or build validation (Step 6).

5. **User input:** $ARGUMENTS

If no arguments provided, begin with Step 1 (Requirements Gathering) and ask the user what feature they want to create.
