---
description: Create or update the project constitution - the foundational governance document
---
# Constitution Workflow
The Constitution is the foundational document that defines all non-negotiable rules, principles, and constraints for the project. All specifications, plans, and implementations must comply with this document.
## Usage
Invoke with: `/speckit.constitution`
Provide the constitution content after the command. The content should include:
1. **Product Identity** - Name, type, purpose, and target audience
2. **Core Principles** - Non-negotiable rules (tenant isolation, progression governance, etc.)
3. **Architectural Constraints** - Backend and frontend architecture requirements
4. **Auth & Identity Rules** - Authentication and role systems
5. **Data Management Rules** - Soft delete, audit, database constraints
6. **Non-Negotiable Prohibitions** - Absolute restrictions
7. **Acceptance Criteria** - Checklist for constitution approval
## Steps
1. Parse the constitution content from the user's message
2. Format the constitution into a clean markdown document
3. Save to `.specify/memory/constitution.md`
4. Confirm the constitution has been ratified
## Output Location
- **Constitution File:** `.specify/memory/constitution.md`
## Spec-Kit Workflow Integration
```
Constitution → Specify → Plan → Tasks
    ↑
  YOU ARE HERE
```
The Constitution is the first step. After ratification:
- Use `/speckit.specify` to create feature specifications
- Use `/speckit.plan` to create implementation plans
- Use `/speckit.tasks` to generate development tasks