# Blue Heeler wiki

Everything a developer, human or AI, needs to pick up this project from the repository
alone. The code says *what*; these pages say *why*, *what's next* and *how we work*.

## 1 Reading order

| # | Page | Type | Read it to learn |
|---|---|---|---|
| 1 | [Product](product.md) | Explanation | Who it's for, goals, constraints, scope |
| 2 | [Roadmap](roadmap.md) | Reference | Phases, gates, what ships when, Phase 1 items and status |
| 3 | [Decisions and open questions](decisions.md) | Reference | Product decisions already made, assumed defaults, what's still open |
| 4 | [Architecture](architecture.md) | Explanation | How the parts fit, request and sign-in flow, hosting |
| 5 | [Domain](domain.md) | Reference | Glossary, the model today, the model planned |
| 6 | [Development](development.md) | How-to | Run, test, gate, add a feature, workflow and PR rules |
| 7 | [Operations](operations.md) | Reference | Environments, configuration, deploys, backups, what's owed before go-live |

Elsewhere in `docs/`:

- [`adr/`](../adr/): architecture decisions with their reasoning. The wiki links them
  rather than re-arguing them.
- [`specs/`](../specs/): one tech spec per work item, written before the code.
- [`runbooks/`](../runbooks/): step-by-step operational procedures.

## 2 For AI sessions

Start with [`CLAUDE.md`](../../CLAUDE.md) (the project contract and gate), then this
page, then the page for the task at hand. Rules of thumb:

- **Product questions** (should X exist, who sees Y): [decisions.md](decisions.md)
  first. If it isn't there, ask the maintainer instead of guessing, and record the
  answer there in the same PR.
- **Design questions**: the ADRs, then the spec for the item, then
  [architecture.md](architecture.md).
- **"What's next"**: [roadmap.md §4](roadmap.md#4-phase-1-work-items) lists Phase 1
  items and their status.
- Precedence when sources disagree: code > ADR > spec > wiki. A disagreement is a bug;
  fix the wiki in the same PR.

## 3 Keeping it current

- Update the wiki **in the same PR** as the change it describes. The devflow `wiki`
  step does this automatically because `CLAUDE.md` names this folder.
- Describe the system as it is now, not how it got there. History lives in git and ADRs.
- One page, one type (explanation, how-to or reference). Number sections `## 1`,
  `### 1.1`; cross-reference as `§1.1`.
- Diagrams are Mermaid in fenced blocks, so they diff and GitHub renders them.
- **The repository is public.** No member data, real names, emails, connection strings,
  hostnames or keys. Use `example.org` addresses in examples.
