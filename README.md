# Syncomania-Solver
A solver for syncomania game

## About
Syncomania is a puzzle game and this project allow to solve game levels.

The solver uses BFS or A-star algorithms to find solutions.

## Rules
4 Actors controlled by player. Their movement is permanently synchronous.

Level is won when all actors, one after another, step into an 'exit' tile.

### Level tiles
There are 'block' tiles on the map where actor can't move in (so the actor just stays where it is).

There are 'trap' tiles. If actor steps into such tile, the level considered lost.

There are 4 'pusher' types of tiles corresponding to each direction. Pusher pushes anything farther to the adjacent tile.

An actor can be pushed only once per turn.

## Input for the solver
Game level should be rectangle and its area not greater than 256 tiles. Such area constraint allows usage of non-collision hash 
for game states.
