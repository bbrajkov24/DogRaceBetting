# Dog Race Betting Simulation

This project implements a console-based dog race betting simulation, demonstrating a decoupled architecture with a shared data store for persistence. It consists of a core class library and two interactive console applications.

## Architecture

The solution is composed of three main parts:

* **`DogRace` (Class Library):**
    * Contains the core business logic and services for managing races, bets, and players.
    * Includes interfaces and their implementations for various functionalities, such as `IRaceService`, `IBetService`, and `IPlayerService`.
    * Defines the domain models (`Race`, `Bet`, `Player`, `RaceParticipant`, etc.) and their behaviors.

* **`RaceManager` (Console Application):**
    * Acts as the central orchestrator for the race simulation.
    * Starts a timed loop to manage the lifecycle of races, including:
        * Creating new races to maintain a minimum number of active races.
        * Announcing upcoming races.
        * Completing races when their start time is reached.
        * Resolving bets for finished races, determining winners/losers and payouts.
    * Interacts with the SQLite database to store and retrieve race and bet data.

* **`PlayerClient` (Console Application):**
    * Provides a command-line interface for users (players) to interact with the simulation.
    * Allows players to:
        * View their wallet balance.
        * Browse active and upcoming races.
        * Place bets.
        * View their historical bets and overall betting results.
    * Reads and writes player data (including wallet balances) from the shared SQLite database.

## Key Features

* **Automated Race Simulation:** Races are automatically generated and progressed by the `RaceManager`.
* **Real-time Betting:** Players can place bets on upcoming races.
* **Persistent Data Storage:** All race, bet, and player data is stored persistently in an SQLite database.

## Getting Started

To run the simulation:

1.  **Build the Solution:** Ensure all projects (`DogRace`, `RaceManager`, `PlayerClient`) are built successfully.

2.  **Start `RaceManager`:**
    * The Race Manager will initialize the SQLite database and begin creating and simulating races. **It is crucial to run this application first.**

3.  **Start `PlayerClient`:**`
    * The Player Client will connect to the same SQLite database and allow you to interact with the simulation.

## Data Persistence

* **SQLite** is used for data persistence.
* Player (wallet) data was transitioned from in-memory storage to the database to ensure synchronized access and persistence across both applications.
* Note that all data (races, bets, and players) is cleared and reinitialized each time **`RaceManager`** starts.