# AGENTS.md

## System purpose

DoubleEntryHomeBookkeeping is home accounting software based on strict double-entry bookkeeping principles.

## Repository layout

- `backend/`: C#/.NET solution (`DoubleEntryHomeBookkeeping.slnx`).
- `frontend/`: TypeScript web client.
- `docs/`: Technical specifications, architecture decision records (ADRs), and AI-created artifacts.
- `references/`: Read-only source material, API specifications, and bank export examples.
- `infra/`: Docker Compose and local environment setup.

## Rules

- Always use the `GL Dev CSharp` plugin for implementation and review work in the backend C# solution.
- Put every AI-created artifact in `docs/`. Reuse existing artifacts when useful.
- Never add, edit, move, or delete files in `references/`. Treat it as read-only input.
- Perform only the actions explicitly requested in the prompt. Do not make unrelated changes such as refactoring or adding comments.
- Treat the prompt as permission for all actions needed to perform its requested activities. Do not ask for additional confirmation unless required by a higher-priority safety or platform rule.
- Keep explanations very short and simple. Use caveman style.
- The VM shared folder has two equivalent path forms: `\\MYLEGION\Shared` and `S:\Gena\Local\Work\_Drive\Shared`. A file under one path is the same file under the other. If one form is inaccessible, use the other.

## Requirements documents

- [BRD](docs/requirements/BRD.md): business requirements and open questions. Consult it when relevant to implementation, review, or further requirements work. Unresolved questions are not approved requirements.
- [TRD](docs/requirements/TRD.md): technical requirements. Consult it when relevant to implementation, review, or technical design.
- Keep requirements documents in `docs/requirements/`. Update the BRD during brainstorming and expand the TRD as requirements become stable.
