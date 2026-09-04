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
- The Generator: Randomly shuffles the student pool and slots them into the structural template blueprint.
- The Gatekeeper (Hard Rejects): Before spending time calculating a score, the candidate composition passes through all active hard constraints (e.g., "Are any blacklisted students in the same group?"). If it fails, it is immediately thrown out.
- The Evaluator (Scoring Pipeline): If it passes validation, it runs through your chain of active scoring rules (mutuals, partials, personality variety, etc.) to produce a final fitness score.
- The Keeper (Elitism): The engine maintains a rolling "Top 5" list. If a newly generated valid composition beats the lowest score on the top-list, it replaces it. If the composition is a duplicate of one already on the top-list (same student-number partition), it is rejected and not inserted—even if its score is higher. This runs continuously in a loop until you stop it.


## Tasks

### Task 2 setup group generation UI updates
- show last five timestamps for accepted group compositions

### Task 3 multithreading
- Maybe....


### Task 3 Group composition Generation

#### Option 2: Sliding window selection - TODO
Randomly sort the list, create groups.
	- Use above ordered list, but start with student x+1 for plucking groups
	- Use same ordered list, but start with student x+2 for plucking groups
	- ...
	- Use x+n. Then go back to first step: random sort list.

In case of duplicate random sorts, this will produce many duplicate group compositions.

# prompt
...