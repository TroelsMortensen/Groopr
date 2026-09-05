Software requirement specification for the application "Groopr".

The purpose is to automate the process of dividing students into groups, based on various criteria.

Students provide various optional information:
- positive wishes, i.e. other students they want to be in group with
- negative wishes, i.e. other students they do not want to group with
- DISC profile
- Physical location

A GroupComposition is a list of groups, each with a list of students. This `GroupComposition` is eventually scored by the application, and the teacher will select the best one, after many iterations.

### Valid GroupComposition (structural rules)

A structurally valid GroupComposition is an exact partition of the student pool into the size blueprint:

- The number of groups and each group's size must match the blueprint exactly (e.g. blueprint `[4, 4, 3]` → three groups of those sizes).
- The sum of the blueprint sizes must equal the number of students.
- Every student from the input pool appears in exactly one group (no missing students, no duplicates across groups).
- Group members are students from that pool.
- A freshly generated composition is unscored (`TotalScore` starts at 0) until it passes hard rejects and is evaluated by the scoring pipeline.

These structural rules are distinct from configurable invalidation rules (hard rejects): a composition can be structurally valid and still be rejected before scoring (e.g. too many students from a previous group).

### Equality of GroupCompositions

Two GroupCompositions are considered the same if they contain the same groups of students by student number only (names, wishes, and score are ignored). Order of groups and order of students within each group do not matter.

## Tech stack

- .NET 10
- Avalonia UI for the desktop application
- Eventually Blazor wasm for a web application

## Implementation strategy

As much as possible, the code in the Logic project is implemented using test-driven development. That means one agent will first write unit tests. Then a second agent will implement the code, **without looking at the tests**. I do not want the implemented behaviour to be influenced by what the tests look like, in case the first agent misinterpreted the requirements. If tests and implementation match, all seem good. If the tests fail, one agent misunderstood something.

This means tests are not to be skipped or disabled.

## Workflow

Conceptual Architecture of the Grouping Engine

### 1. The Ingestion & Configuration Layer
- Data Ingestion: Reads the raw student input (names, positive lists, and eventually personality profiles or blacklists) into a clean internal data model.
- The Rule Registry: A modular configuration where teachers can toggle rules on/off and adjust weights. This houses both your Hard Rejects and your Scoring Pipelines.

### 2. The Partition Engine (Group Sizing)
- Takes the total student count and your ranked group-size preferences (e.g., 4, 4, 3) to establish the exact blueprint of the classroom (e.g., three 4-person groups and two 3-person groups). This blueprint acts as the structural template for every generated trial.

### 3. Scoring rules configuration
- The user will be able to configure the scoring rules.
- Currently, there are three scoring rules:
    - Mutual matches
    - Partial matches
    - Negative matches (subtracts points when students who listed each other as negative wishes (not necessarily a mutual negative wish) are placed in the same group)

### 4. Invalidation rules configuration
- The user will be able to configure the invalidation rules. These are rules that will reject a group composition before it is scored.
- Currently, there is one invalidation rule:
    - Max number of students from previous group

### 5. The Generation & Search Loop (The "Dinner" Engine)
- Because your scale is capped around 45 students and you are happy to let a random search run for a few minutes, a Monte Carlo / Random Sampling with Elitism approach fits your workflow perfectly:
- The Generator: Produces candidate compositions via pluggable generation strategies (see below). Each strategy yields an infinite stream of structurally valid, unscored compositions matching the size blueprint.
- The Gatekeeper (Hard Rejects): Before spending time calculating a score, the candidate composition passes through all active hard constraints (e.g., "Are any blacklisted students in the same group?"). If it fails, it is immediately thrown out.
- The Evaluator (Scoring Pipeline): If it passes validation, it runs through your chain of active scoring rules (mutuals, partials, personality variety, etc.) to produce a final fitness score.
- The Keeper (Elitism): The engine maintains a rolling "Top 5" list. If a newly generated valid composition beats the lowest score on the top-list, it replaces it. If the composition is a duplicate of one already on the top-list (same student-number partition), it is rejected and not inserted—even if its score is higher. This runs continuously in a loop until you stop it.

### 6. Generation strategies

All strategies implement the same producer interface and return structurally valid partitions. Wish-aware strategies typically shuffle (or otherwise randomize ties) once per composition so the Monte Carlo loop explores variety.

- **RandomShuffleStrategy** — Shuffles the student pool and slices it sequentially into the blueprint sizes. No use of wishes while generating.
- **BreadthFirstGreedyStrategy** — Seeds each group from the shuffled pool, then repeatedly expands by taking an available positive wish from the earliest group member who still has one (BFS-style over join order); falls back to the next unassigned student when stuck.
- **DepthFirstGreedyStrategy** — Like BFS greedy, but always expands from the most recently added student (a chain walk). When that tip has no available wishes, picks a random unassigned student and pivots the chain to them.
- **MutualPairFirstStrategy** — Pre-detects mutual positive-wish pairs, seeds groups with a mutual pair when size allows, then fills remaining seats with BFS greedy expansion (or a single random seed if no pair is available).
- **OrphanFirstStrategy** — Orders students by incoming positive-wish count (least wished-for first), seeds each group with the most isolated remaining student, then fills with BFS greedy expansion.
- **TriadFirstStrategy** — Pre-detects directed wish triangles (A→B→C→A), seeds with a triangle when group size ≥ 3, otherwise falls back to mutual-pair then random seeding, then BFS greedy fill.
- **IslandFirstStrategy** — Finds connected components in the undirected positive-wish graph (“friend islands”), prefers seeding with the largest island that still fits the target group size, then BFS greedy fill; if none fit, seeds with a single student.
- **RoundRobinStrategy** — Composite producer: runs a list of child strategies in alternating phases (default: MutualPairFirst then OrphanFirst, 1000 compositions each) so the search budget is shared across approaches.
- **HillClimbingWrapper** — Refinement wrapper (default inner: MutualPairFirst). After each baseline composition, repeatedly proposes random swaps of two students in different groups and keeps a swap only when the score improves (default ~75 iterations). Yields the polished composition still unscored for the outer pipeline.
- **SlidingWindowStrategy** — Planned sliding-window selection over shuffled orderings; not implemented yet.

## Tasks

### Task 1 Add Hill Climber finisher
After generation is complete, ask if the user wants to run the Hill Climber finisher, in an attempt to improve the group compositions.


### Task 2 setup group generation UI updates
- show last five timestamps for accepted group compositions


### Task 3 multithreading
- Maybe....

# prompt
...
