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

### Task 1 setup scoring UI

In this third view of the wizard, the user will be able to configure the scoring rules.

Current scoring rules:
- Mutual wishes
- Partial wishes


### Task 2 setup group generation UI

### Task 3 multithreading

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


We will start work on the third view: seting up scoring. The view will consists of two columns of cards, each card contains data for a scoring rule. Current scoring rules:
- Mutual matches
- Partial matches

Each card will have a title at the top, preferably as a pill box right on the top edge of the card. There will also be a checkbox to enable/disable the rule.

For the mutual matches, the user must input a decimal value above 0.

For the partial matches, the user must input a decimal value above 0.

At the top left (like the other views), there will be a button to go back to the previous view. No data is cleared when going back.

At the top right, there will be a button to go to the next view. Eventually, the button will take the user to the fourth view, but for now, this will be disabled.

When the user clicks the "next" button, the data from all enabled scoring rule cards will be collected, and put into the shared InputConfiguration object, that has been used by the previous two views. See records below.

The data from each card will be collected into dedicated simple records. I want a record type for each scoring rule. They all inherit from the same base class, something like this:

```csharp
// The base type stored in your InputConfiguration
public abstract record ScorerConfigurationRecord;

// Specific configuration payloads for each scorer
public record MutualMatchConfiguration(double Weight) : ScorerConfigurationRecord;
public record PartialMatchConfiguration(double Weight) : ScorerConfigurationRecord;
```

Put these records into the AvaloniaUI/Data directory.

More scoring configuration records may be added in the future.

Update the `InputConfiguration` class to have a list of `ScorerConfigurationRecord` objects.

I want validation on the scoring records, using smart constructors. Clicking the "next" button collects all the scoring rule cards data into records, and catches validation errors. In case of errors, the user is presented with a message using the existing ErrorDialog, which has been used in the previous views.

Below is the description from my brainstorming session with another AI for inspiration:

Task: Implement Step 3 Scorer Setup and Polymorphic Configuration Architecture

We are building a multi-step Avalonia wizard for a C# "Group Matcher" application. We need to implement Step 3 (Scorer Setup), which uses a card-based UI (toggles/checkboxes per scorer) and serializes the selections into a shared, state-holding InputConfiguration object using a type-safe, polymorphic record architecture.

Requirements:
Polymorphic Configuration Records:
Create a base abstract record for scorer configurations and specific subclasses for each strategy. Do not use magic strings or a monolithic enum that requires modification for future scorers.

C#
public abstract record ScorerConfigurationRecord;
public record MutualMatchConfiguration(double Weight) : ScorerConfigurationRecord;
public record PartialMatchConfiguration(double Weight) : ScorerConfigurationRecord;
Update the Shared State Container (InputConfiguration):
Ensure the shared wizard configuration class includes a collection for these configurations:

C#
public class InputConfiguration
{
    // ... existing fields (Students, GroupSizes, etc.)
    public List<ScorerConfigurationRecord> EnabledScorers { get; set; } = [];
}
Step 3 ViewModel & UI Card Model:

Create a ScorerCardViewModel tracking UI state (e.g., IsEnabled boolean, Weight double, Title, Description).

In the Step 3 ViewModel (ScorerSetupViewModel), maintain an ObservableCollection<ScorerCardViewModel> populated with the available scorers.

Implement a "Next" command that loops through enabled cards, maps them to their respective ScorerConfigurationRecord subclasses, and updates the shared InputConfiguration.