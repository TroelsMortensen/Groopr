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

### 3. Scoring rules configuration
- The user will be able to configure the scoring rules.
- Currently, there are two scoring rules:
    - Mutual matches
    - Partial matches

### 4. The Generation & Search Loop (The "Dinner" Engine)
- Because your scale is capped around 45 students and you are happy to let a random search run for a few minutes, a Monte Carlo / Random Sampling with Elitism approach fits your workflow perfectly:
- The Generator: Randomly shuffles the student pool and slots them into the structural template blueprint.
- The Gatekeeper (Hard Rejects): Before spending time calculating a score, the candidate composition passes through all active hard constraints (e.g., "Are any blacklisted students in the same group?"). If it fails, it is immediately thrown out.
- The Evaluator (Scoring Pipeline): If it passes validation, it runs through your chain of active scoring rules (mutuals, partials, personality variety, etc.) to produce a final fitness score.
- The Keeper (Elitism): The engine maintains a rolling "Top 5" list. If a newly generated valid composition beats the lowest score on the top-list, it replaces it. This runs continuously in a loop until you stop it.


## Tasks

### Task 2 setup group generation UI
- Start/stop button to start generating group compositions.
- InputConfiguration is used to start the generation. 
- The view keeps the top 5 group compositions, in five UI cards, stacked vertically. Each group composition shows its score.
- When a better group composition is generated, the lowest scoring group composition is removed, and the new one is added to its place in the vertical list, which remains sorted by score, highest score on top.

### Task 3 multithreading
- Maybe....


### Task 3 Group composition Generation

Put this behind an Iterable interface, so I can swap out how the group compositions are generated.

#### Option 1: complete random - Done
Randomly sort the list of student records, and pluck out a list of groups based on the group sizes.
Randomize the list for each iteration.


#### Option 2: Sliding window selection - TODO
Randomly sort the list, create groups.
	- Use above ordered list, but start with student x+1 for plucking groups
	- Use same ordered list, but start with student x+2 for plucking groups
	- ...
	- Use x+n. Then go back to first step: random sort list.

In case of duplicate random sorts, this will produce many duplicate group compositions.

# prompt

Next feature. This requires the fourth and last view: GroupCompositionGenerationView.

The "Next" button on the third view, ScorerSetup, should navigate to this new view, if all validation passes.

The fourth view will have a start/stop button at the top.

When navigating to the view, it will use the InputConfiguration to setup various things:
- Map the ScorerConfigurations to matching ScoringStrategies from the Logic project, these will put added to the GroupCompositionScorer.

When pressing the start button, the view will start generating group compositions using the IGroupCompositionProducer interface, with the RandomShuffleStrategy implementation. This class should be used as an IEnumerable, i.e. in a foreach loop.

The start button will switch to a stop button, which when clicked will stop the generation.

There should be a counter showing the number of group compositions generated and scored, which is updated in the UI, every 100 iterations.

The view retains a list of the top 5 group compositions, sorted by score, highest score on top.

When a better group composition is generated, the lowest scoring group composition is removed, and the new one is added to its place in the vertical list, which remains sorted by score, highest score on top.

This group generation will potentially run for a very long time, until the stop button is clicked.

It is important that the UI is responsive, and that the generation does not block the UI. I am unsure how to achieve this, so any suggestions are welcome. I imaging using async programming, and pausing the thread to let the UI update. Or use Task.run(..). Maybe with a CancellationTokenSource to cancel the task.

If clicking back, the group generation should be paused, and a confirmation dialog should be shown to the user, asking if they want to stop and clear the generation.

The UI shows a card for each group composition, showing the score and the groups, something like this:

Score: 95.7
Group 1
- 123456, Jan Jansen
- 654321, Pieter Janssen
- 321456, Jan Jansen

Group 2
- 986532, Per Person
- 546532, Ida Idadaughter
- 786545, John Doe

.... and so on.

