Software requirement specification for the application "Groopr".

The purpose is to automate the process of dividing students into groups, based on various criteria.

Students provide various optional information:
- positive wishes, i.e. other students they want to be in group with
- negative wishes, i.e. other students they do not want to group with
- DISC profile
- Physical location

A GroupComposition is a list of groups, each with a list of students. This `GroupComposition` is eventually scored by the application, and the teacher will select the best one, after many iterations.

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

### 3. The Generation & Search Loop (The "Dinner" Engine)
- Because your scale is capped around 45 students and you are happy to let a random search run for a few minutes, a Monte Carlo / Random Sampling with Elitism approach fits your workflow perfectly:
- The Generator: Randomly shuffles the student pool and slots them into the structural template blueprint.
- The Gatekeeper (Hard Rejects): Before spending time calculating a score, the candidate composition passes through all active hard constraints (e.g., "Are any blacklisted students in the same group?"). If it fails, it is immediately thrown out.
- The Evaluator (Scoring Pipeline): If it passes validation, it runs through your chain of active scoring rules (mutuals, partials, personality variety, etc.) to produce a final fitness score.
- The Keeper (Elitism): The engine maintains a rolling "Top 5" list. If a newly generated valid composition beats the lowest score on the top-list, it replaces it. This runs continuously in a loop until you stop it.


## Tasks

### Task 1 Restructuring the Student Model for Extensibility
To make your Student record future-proof against negative wishes, DISC profiles, or hard constraints, you want to shift away from hardcoding fields like List<Student> positiveWishes. Instead, treat the student data model as a container for properties/traits.
Conceptually, a student could look like this:

Identity: ID, Name

Preferences Container:
- Positive wishes (List of IDs/Names)
- Negative wishes (List of IDs/Names)

Attributes Container:
- Personality profile (e.g., DISC color: Red, Green, etc.)
- Custom metadata flags if needed later

By separating Identity from Preferences and Attributes, you can pass the whole student object into any rule engine without changing the core record structure every time you invent a new rule.

### Task 2 Scoring Pipeline
Decoupling the Scoring Pipeline (Strategies)
You hit the nail on the head: mixing partial and full matches together makes the code fast, but rigid.

With a Strategy Pattern (or a rule pipeline), the loop shouldn't care how a score is calculated. It should look something like this conceptually:

1. The Evaluator Engine receives a proposed group composition.
2. It passes the composition through the Hard Reject Registry first. If any rule returns false, throw it out immediately.
3. If valid, it loops through the active Scoring Rules (e.g., MutualWishRule, PartialWishRule, PersonalityVarietyRule).
4. Each rule inspects the groups, calculates its own points/penalties, and returns a number.
5. The engine sums them up to get the final score for that configuration.

This means your mutual match logic and partial match logic live in completely separate classes, and you can easily turn one off or adjust its weight via a configuration UI.

### Task 3 Group composition Generation

Put this behind an Iterable interface, so I can swap out how the group compositions are generated.

#### Option 1: complete random
Randomly sort the list of student records, and pluck out a list of groups based on the group sizes.
Randomize the list for each iteration.


#### Option 2: Sliding window selection
Randomly sort the list, create groups.
	- Use above ordered list, but start with student x+1 for plucking groups
	- Use same ordered list, but start with student x+2 for plucking groups
	- ...
	- Use x+n. Then go back to first step: random sort list.

# prompt

## Ignore this part below, I use it to better type out long prompts to the AI

