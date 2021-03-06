﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SyncomaniaSolver
{
    public enum Direction
    {
        Left = 0,
        Up,
        Right,
        Down
    }

    public class MapTile
    {
        public enum TileType
        {
            Empty = 0,
            Block,
            Exit,
            Trap,
            PusherUp,
            PusherDown,
            PusherLeft,
            PusherRight,
        }

        public MapTile( TileType type, int x, int y, int index, GameMap gameMap )
        {
            this.type = type;
            position = new Pos{ x = x, y = y };
#if SYMMETRY_FOR_STATE_HASH_CHECK
            Index = gameMap.GetIndexBySymmetry( index, x, y );
#else
            Index = index;
#endif
        }

        public TileType type;
        public int distanceToExit = int.MaxValue;
        public Pos position;
        public readonly MapTile[] neighbours = new MapTile[4];
#if SYMMETRY_FOR_STATE_HASH_CHECK
        public int[] Index { get; private set; }
#else
        public int Index { get; private set; }
#endif
        public MovingObject obj;
    }

    public class MovingObject
    {
        public enum ObjectType
        {
            None = 0,
            Actor,
            AntiActor,
            Box
        }

        public ObjectType type;
        public MapTile position;
    }

    public struct StateHash
    {
        public long a;
        public long b;
        public long c;
        public long d;
        // Note that Int64.GetHashCode() is just xor of lower 32bits and upper 32bits
        public override int GetHashCode()
        {
            unchecked
            {
                const int s = 23;
                int hash = unchecked((int)(a));
                hash = hash * s + (int)(a >> 32);
                hash = hash * s + unchecked((int)(b));
                hash = hash * s + (int)(b >> 32);
                hash = hash * s + unchecked((int)(c));
                hash = hash * s + (int)(c >> 32);
                hash = hash * s + unchecked((int)(d));
                hash = hash * s + (int)(d >> 32);
                return hash;
            }

        }

        // TODO: Check correctness of this
        public void Add( int idx, int val)
        {
            int rem = idx % 32;
            //int div = Math.DivRem( idx, 32, out rem );
            int div = idx / 32;
            rem <<= 1;
            switch ( div )
            {
                case 0:
                    a |= (long)val << rem;
                    break;
                case 1:
                    b |= (long)val << rem;
                    break;
                case 2:
                    c |= (long)val << rem;
                    break;
                case 3:
                    d |= (long)val << rem;
                    break;
            }
        }
    }

    public class GameState : IComparable<GameState>
    {
        public GameState PrevState { get; private set; }
        public MapTile pos1;
        public MapTile pos2;
        public MapTile pos3;
        public MapTile pos4;
        public MapTile[] ContrActors { get; private set; }
        public MapTile[] Boxes { get; private set; }

        public Direction moveDir;

        public float weight;
        public int turn;

        public StateHash Hash { get; private set; }

        public GameState( GameState prevState, MapTile pos1, MapTile pos2, MapTile pos3, MapTile pos4, MapTile[] contrActors, Direction moveDir, StateHash hash )
        {
            this.PrevState = prevState;
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.pos3 = pos3;
            this.pos4 = pos4;
            this.ContrActors = contrActors;
            this.moveDir = moveDir;
            this.Hash = hash;

            if ( prevState != null )
                turn = prevState.turn + 1;
        }

        public bool IsFinished() { return pos1 == null && pos2 == null && pos3 == null && pos4 == null; }

        public override string ToString()
        {
            return string.Format( "move:  {0}", moveDir.ToString() );
        }

        //public bool Equals( GameState other )
        //{
        //    return pos1.Equals( other.pos1 ) && pos2.Equals( other.pos2 ) && pos3.Equals( other.pos3 ) && pos4.Equals( other.pos4 );
        //}

        public int CompareTo( GameState other )
        {
            return weight.CompareTo( other.weight );
        }

        /// <summary>
        /// Basic heuristic function for A-star search.
        /// It is a bit difficult to find out some common function for all maps.
        /// </summary>
        /// <param name="exit"></param>
        public void CalculateWeight( /*Pos exit*/ GameMap map )
        {
            int dist = 0;

            if ( pos1 != null )
            {
                //dist = Math.Max( dist, map[pos1.Value].distanceToExit );
                dist += pos1.distanceToExit;
            }
            if ( pos2 != null )
            {
                //dist = Math.Max( dist, map[pos2.Value].distanceToExit );
                dist += pos2.distanceToExit;
            }
            if ( pos3 != null )
            {
                //dist = Math.Max( dist, map[pos3.Value].distanceToExit );
                dist += pos3.distanceToExit;
            }
            if ( pos4 != null )
            { 
                //dist = Math.Max( dist, map[pos4.Value].distanceToExit );
                dist += pos4.distanceToExit;
            }

            weight += dist;

            weight += GameMap.TurnCost * turn;
        }

        public IEnumerable<GameState> History
        { 
            get
            {
                List<GameState> ordered = new List<GameState>();
                GameState state = this;
                while ( state.PrevState != null ) {
                    ordered.Add( state );
                    state = state.PrevState;
                }

                for ( var idx = ordered.Count - 1; idx >= 0; idx-- )
                {
                    yield return ordered[idx];
                }
            }
        }
    }

    public class SolutionState : IComparable<SolutionState>
    {
        public GameState State { get; private set; }

        public int Turns { get { return State.turn; } }
        public int ElapsedTime { get; private set; }
        public int IterationsCount { get; private set; }
        public int MaxFrontStatesCount { get; private set; }
        public int UniqueStatesCount { get; private set; }

        public SolutionState( GameState state, int elapsedTime, int iterationsCount, int maxFrontStatesCount, int uniquesStatesCount )
        {
            State = state;
            ElapsedTime = elapsedTime;
            IterationsCount = iterationsCount;
            MaxFrontStatesCount = maxFrontStatesCount;
            UniqueStatesCount = uniquesStatesCount;
        }

        public int CompareTo( SolutionState other )
        {
            return Turns.CompareTo( other.Turns );
        }
    }

    public struct Pos
    {
        public int x;
        public int y;

        public Pos(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public bool Equals( Pos other )
        {
            return x == other.x && y == other.y;
        }

        public int Distance( Pos other )
        {
            return Math.Abs( x - other.x ) + Math.Abs( y - other.y );
        }
    }

    public class GameMap
    {
        [Flags]
        public enum eMapSymmetry
        {
            None = 0,
            Horizontal = 1,
            Vertical = 2,
            Both = Horizontal | Vertical,
        }

        public static bool TestIsOn { get; set; }
        public static float TurnCost = 1.4f;

        public int width;
        public int height;
        public eMapSymmetry Symmetry { get; private set; }

        public readonly MapTile[] actors = { null, null, null, null };
        private readonly List<MapTile> contrActors = new List<MapTile>();

        public MapTile ExitTile { get; private set; }

        public MapTile this[int x, int y]
        {
            get { return tiles[x, y]; }
        }

        private MapTile[,] tiles; // x,y indexing

        private bool LoadMapImpl( Tuple<MapTile.TileType,MovingObject.ObjectType>[,] map )
        {
            this.width = map.GetLength(1);
            this.height = map.GetLength(0);

            tiles = new MapTile[width, height];

            ExitTile = null;

            int actorsCount = 0;

            int x = 0, y = 0;
            int index = 0;

            foreach ( var t in map )
            {
                x = index % width;
                y = index / width;
                index++;

                MapTile.TileType tileType = t.Item1;
                MovingObject.ObjectType objectType = t.Item2;

                var tile = new MapTile( tileType, x, y, index, this );

                tiles[x, y] = tile;

                if ( tileType == MapTile.TileType.Exit )
                {
                    if ( ExitTile != null )
                    {
                        //Console.WriteLine( "Map exit already defined" );
                        return false;
                    }
                    ExitTile = tile;
                }

                if ( objectType != MovingObject.ObjectType.None )
                    tile.obj = new MovingObject { type = objectType };

                if ( objectType == MovingObject.ObjectType.Actor )
                    actors[actorsCount++] = tile;
                else if ( objectType == MovingObject.ObjectType.AntiActor )
                    contrActors.Add( tile );
            }

            if ( index != width * height ) {
                //Console.WriteLine( "Invalid map data" );
                return false;
            }

            if ( TestIsOn == false && actorsCount == 0 ) {
                //Console.WriteLine( "No actors on map" );
                return false;
            }

            if ( TestIsOn == false && ExitTile == null ) {
                //Console.WriteLine( "No exit on map" );
                return false;
            }

            SetupTileNeighbourhood();

            Symmetry = eMapSymmetry.None;

            FindSymmetry();

            var res = CalculateDistanceToExit();

            return res;
        }

        public int[] GetIndexBySymmetry( int index, int x, int y )
        {
            var arr = new int[4];
            arr[0] = index;
            arr[1] = width * (y + 1) - x - 1;
            arr[2] = width * ( height - y - 1 ) + x;
            arr[3] = width * height - index;
            return arr;
        }

        public bool LoadMap( Tuple<MapTile.TileType,MovingObject.ObjectType>[,] map )
        {
            return LoadMapImpl( map );
        }

        /// <summary>
        /// False means not a valid new position for an Actor. Other game objects just ignore result value.
        /// </summary>
        public bool GetNewPos( MapTile curPos, Direction? dir, out MapTile result )
        {
            if ( dir.HasValue == false )
                switch ( curPos.type )
                {
                    case MapTile.TileType.PusherDown:
                        dir = Direction.Down;
                        break;
                    case MapTile.TileType.PusherLeft:
                        dir = Direction.Left;
                        break;
                    case MapTile.TileType.PusherRight:
                        dir = Direction.Right;
                        break;
                    case MapTile.TileType.PusherUp:
                        dir = Direction.Up;
                        break;
                    default: // Stay at the same position
                        result = curPos;
                        return true;
                }
            var newPos = curPos.neighbours[(int)dir];
            if ( newPos == null )
            {
                result = curPos;
                return true;
            }
            switch ( newPos.type )
            {
                case MapTile.TileType.Block:
                    result = curPos;
                    return true;
                case MapTile.TileType.Trap:
                    result = null;
                    return false;
                case MapTile.TileType.Exit:
                    result = null;
                    return true;
                default:
                    result = newPos;
                    return true;
            }
        }

        /// <summary>
        /// Most game logic is implemented in this method.
        /// </summary>
        public GameState GetNewState( GameState currentState, Direction dir, Func<StateHash,bool> checkHash )
        {
            MapTile pos1 = null;
            MapTile pos2 = null;
            MapTile pos3 = null;
            MapTile pos4 = null;
            MapTile[] contractorNewPos = new MapTile[currentState.ContrActors.Length];
            currentState.ContrActors.CopyTo( contractorNewPos, 0 );

            // TODO: Boxes pushed by Actors and Contractors?

            if ( actor1.Push( dir ) == false )
                return null;
            if ( actor2.Push( dir ) == false )
                return null;
            if ( actor3.Push( dir ) == false )
                return null;
            if ( actor4.Push( dir ) == false )
                return null;

            // Step 1: Just Actors are moving. All movements are simultaneous.
            if ( currentState.pos1 != null && GetNewPos( currentState.pos1, dir, out pos1 ) == false )
                return null;
            if ( currentState.pos2 != null && GetNewPos( currentState.pos2, dir, out pos2 ) == false )
                return null;
            if ( currentState.pos3 != null && GetNewPos( currentState.pos3, dir, out pos3 ) == false )
                return null;
            if ( currentState.pos4 != null && GetNewPos( currentState.pos4, dir, out pos4 ) == false )
                return null;

            // Check Actors colliding themselves.
            if ( pos1 != null && ( pos1 == pos2 || pos1 == pos3 || pos1 == pos4 ) ||
                 pos2 != null && ( pos2 == pos3 || pos2 == pos4) ||
                 pos3 != null && pos3 == pos4 )
                return null;

            // Step 2: ContrActors are moving. All movements are simultaneous. Check for collision with Actors the same time.
            // Consider removing Contractors from the list instead of nullifying them
            for ( int idx = 0; idx < contractorNewPos.Length; idx++ )
            {
                MapTile pos = contractorNewPos[idx];
                if ( contractorNewPos[idx] != null )
                {
                    // Check for collision with Actors before movement
                    if ( pos1 != null && pos1 == pos || pos2 != null && pos2 == pos || pos3 != null && pos3 == pos || pos4 != null && pos4 == pos )
                        return null;

                    GetNewPos( pos, (Direction)((int)dir ^ 2), out pos );

                    if ( pos != null )
                    {
                        // Check for collision with Actors after movement
                        if ( pos1 != null && pos1 == pos || pos2 != null && pos2 == pos || pos3 != null && pos3 == pos || pos4 != null && pos4 == pos )
                            return null;

                        // Check for collision with other Contractors, remove collided
                        for ( int idxInner = 0; idxInner < idx; idxInner++ )
                            if ( contractorNewPos[idxInner] == pos )
                            {
                                pos = null;
                                break;
                            }
                    }
                    contractorNewPos[idx] = pos;
                }
            }

            // Step 3: Actors are pushed by pushers. All movements are simultaneous.
            if ( pos1 != null && GetNewPos( pos1, null, out pos1 ) == false )
                return null;
            if ( pos2 != null && GetNewPos( pos2, null, out pos2 ) == false )
                return null;
            if ( pos3 != null && GetNewPos( pos3, null, out pos3 ) == false )
                return null;
            if ( pos4 != null && GetNewPos( pos4, null, out pos4 ) == false )
                return null;

            // Check Actors colliding themselves.
            if ( pos1 != null && ( pos1 == pos2 || pos1 == pos3 || pos1 == pos4 ) ||
                 pos2 != null && ( pos2 == pos3 || pos2 == pos4) ||
                 pos3 != null && pos3 == pos4 )
                return null;

            // Step 3: Contractors and Boxes are pushed by pushers. All movements are simultaneous.
            for ( int idx = 0; idx < contractorNewPos.Length; idx++ )
            {
                MapTile pos = contractorNewPos[idx];
                if ( pos != null )
                {
                    GetNewPos( pos, null, out pos );

                    if ( pos != null )
                    {
                        // Check for collision with Actors after movement
                        if ( pos1 != null && pos1 == pos || pos2 != null && pos2 == pos || pos3 != null && pos3 == pos || pos4 != null && pos4 == pos )
                            return null;

                        // Check for collision with other Contractors, remove collided
                        for ( int idxInner = 0; idxInner < idx; idxInner++ )
                            if ( contractorNewPos[idxInner] == pos )
                            {
                                pos = null;
                                break;
                            }
                    }
                    contractorNewPos[idx] = pos;
                }
            }

            // Check if we already have same state
            var hash = CalculateStateHash( pos1, pos2, pos3, pos4, contractorNewPos, eMapSymmetry.None );
            if ( checkHash != null && checkHash( hash ) == false )
                return null;

#if SYMMETRY_FOR_STATE_HASH_CHECK
            for ( eMapSymmetry s = eMapSymmetry.Horizontal; s <= eMapSymmetry.Both; s++ )
            {
                if ( ( Symmetry & s ) == s )
                {
                    var hashOther = CalculateStateHash( pos1, pos2, pos3, pos4, s );
                    if ( checkHash( hashOther ) == false )
                        return null;
                }
            }
#endif
            return new GameState( currentState, pos1, pos2, pos3, pos4, contractorNewPos, dir, hash );
        }

        public GameState GetStartingState()
        {
            var ca = contrActors.ToArray();
            var hash = CalculateStateHash( actors[0], actors[1], actors[2], actors[3], ca, eMapSymmetry.None );

            return new GameState( null, actors[0], actors[1], actors[2], actors[3], ca, Direction.Left, hash );
        }

        private void SetupTileNeighbourhood()
        {
            for ( int row = 0; row < height; row++ )
            {
                for ( int col = 0; col < width; col++ )
                {
                    if ( col > 0 )
                        tiles[col, row].neighbours[(int)Direction.Left] = tiles[col - 1, row];
                    if ( col < width - 1 )
                        tiles[col, row].neighbours[(int)Direction.Right] = tiles[col + 1, row];
                    if ( row > 0 )
                        tiles[col, row].neighbours[(int)Direction.Up] = tiles[col, row - 1];
                    if ( row < height - 1 )
                        tiles[col, row].neighbours[(int)Direction.Down] = tiles[col, row + 1];
                }
            }
        }

        private void FindSymmetry()
        {
            bool hasVerticalSymmetry = true;

            for ( int row = 0; row < height / 2; row++ )
            {
                for ( int col = 0; col < width; col++ )
                {
                    if ( tiles[col, row].type != tiles[col, height - row - 1].type )
                    {
                        hasVerticalSymmetry = false;
                        break;
                    }
                }
                if ( hasVerticalSymmetry == false )
                    break;
            }

            if ( hasVerticalSymmetry )
                Symmetry |= eMapSymmetry.Vertical;

            bool hasHorizontalSymmetry = true;

            for ( int row = 0; row < height; row++ )
            {
                for ( int col = 0; col < width / 2; col++ )
                {
                    if ( tiles[col, row].type != tiles[width - col - 1, row].type )
                    {
                        hasHorizontalSymmetry = false;
                        break;
                    }
                }
                if ( hasHorizontalSymmetry == false )
                    break;
            }

            if ( hasHorizontalSymmetry )
                Symmetry |= eMapSymmetry.Horizontal;
        }

        /// <summary>
        /// Calculate distance to exit for every map tile. BFS used.
        /// </summary>
        private bool CalculateDistanceToExit()
        {
            if ( ExitTile == null )
                return false;

            Queue<MapTile> frontTiles = new Queue<MapTile>(256);

            frontTiles.Enqueue( ExitTile );

            ExitTile.distanceToExit = 0;

            Action<MapTile, Direction> f = ( tile, dir ) =>
            {
                var nextTile = tile.neighbours[(int)dir];
                if ( nextTile == null ||
                     nextTile.type == MapTile.TileType.Block ||
                     nextTile.type == MapTile.TileType.Trap ||
                     nextTile.distanceToExit != int.MaxValue )
                    return;

                nextTile.distanceToExit = tile.distanceToExit + 1;
                frontTiles.Enqueue( nextTile );
            };

            while ( frontTiles.Count > 0 )
            {
                var tile = frontTiles.Dequeue();
                f( tile, Direction.Left );
                f( tile, Direction.Up );
                f( tile, Direction.Right );
                f( tile, Direction.Down );
            }

            return true;
        }
        /// <summary>
        /// BFS algorithm
        /// </summary>
        /// <param name="map"></param>
        public SolutionState Solve_BFS()
        {
            var beginState = GetStartingState();

            if ( beginState.IsFinished() )
                return new SolutionState( beginState, 0, 0, 0, 0 );

            HashSet<StateHash> allUniqueStates = new HashSet<StateHash>();
            allUniqueStates.Add( beginState.Hash );

            Queue<GameState> frontStates = new Queue<GameState>(32000);
            frontStates.Enqueue( beginState );

            int iterations = 0;
            int maxFrontStatesCount = 0;

            var sw = Stopwatch.StartNew();

            GameState currentState = null;

            Func<StateHash, bool> checkHash = (hash) =>
            {
                return allUniqueStates.Contains( hash ) == false;
            };

            Func<GameState, Direction, bool> f = (state, dir) =>
            {
                GameState newState = GetNewState( state, dir, checkHash );
                if ( newState != null )
                {
                    if ( newState.IsFinished() )
                    {
                        currentState = newState;
                        return true;
                    }

                    allUniqueStates.Add( newState.Hash );
                    //if ( ( Symmetry & eMapSymmetry.Horizontal ) == eMapSymmetry.Horizontal )
                    //    allUniqueStates.Add( GetHashForSymmetricState( newState, 1, 0 ) );
                    //if ( ( Symmetry & eMapSymmetry.Vertical ) == eMapSymmetry.Vertical )
                    //    allUniqueStates.Add( GetHashForSymmetricState( newState, 0, 1 ) );
                    //if ( ( Symmetry & eMapSymmetry.Both ) == eMapSymmetry.Both )
                    //    allUniqueStates.Add( GetHashForSymmetricState( newState, 1, 1 ) );

                    frontStates.Enqueue( newState );
                }
                return false;
            };
                

            while ( frontStates.Count > 0 )
            {
                currentState = frontStates.Dequeue();

                if ( f( currentState, Direction.Left ) )
                    break;
                if ( f( currentState, Direction.Up ) )
                    break;
                if ( f( currentState, Direction.Right ) )
                    break;
                if ( f( currentState, Direction.Down ) )
                    break;

                iterations++;

                maxFrontStatesCount = Math.Max( maxFrontStatesCount, frontStates.Count() );
            }

            SolutionState solution = new SolutionState( currentState, elapsedTime: (int)(sw.ElapsedMilliseconds),
                                                                  iterationsCount: iterations,
                                                              maxFrontStatesCount: maxFrontStatesCount,
                                                               uniquesStatesCount: allUniqueStates.Count );
            return solution;
        }

        public SolutionState Solve_AStar()
        {
            var beginState = GetStartingState();

            if ( beginState.IsFinished() )
                return new SolutionState( beginState, 0, 0, 0, 0 );

            HashSet<StateHash> allUniqueStates = new HashSet<StateHash>();
            allUniqueStates.Add( beginState.Hash );

            PriorityQueue<GameState> frontStates = new PriorityQueue<GameState>( 32000 );
            frontStates.Add( beginState );

            int iterations = 0;
            int maxFrontStatesCount = 0;

            var sw = Stopwatch.StartNew();

            GameState currentState = null;

            Func<StateHash, bool> checkHash = (hash) =>
            {
                return allUniqueStates.Contains( hash ) == false;
            };

            Func<GameState, Direction, bool> f = ( state, dir ) =>
            {
                GameState newState = GetNewState( state, dir, checkHash );
                if ( newState != null )
                {
                    if ( newState.IsFinished() )
                    {
                        currentState = newState;
                        return true;
                    }

                    allUniqueStates.Add(newState.Hash);
                    //if ((Symmetry & eMapSymmetry.Horizontal) == eMapSymmetry.Horizontal)
                    //    allUniqueStates.Add(GetHashForSymmetricState(newState, 1, 0));
                    //if ((Symmetry & eMapSymmetry.Vertical) == eMapSymmetry.Vertical)
                    //    allUniqueStates.Add(GetHashForSymmetricState(newState, 0, 1));
                    //if ((Symmetry & eMapSymmetry.Both) == eMapSymmetry.Both)
                    //    allUniqueStates.Add(GetHashForSymmetricState(newState, 1, 1));

                    newState.CalculateWeight( this );

                    frontStates.Add( newState );
                }
                return false;
            };

            sw.Stop();

            while ( frontStates.Count > 0 )
            {
                currentState = frontStates.RemoveMin();

                if ( f( currentState, Direction.Left ) )
                    break;
                if ( f( currentState, Direction.Up ) )
                    break;
                if ( f( currentState, Direction.Right ) )
                    break;
                if ( f( currentState, Direction.Down ) )
                    break;

                iterations++;

                maxFrontStatesCount = Math.Max( maxFrontStatesCount, frontStates.Count );
            }

            SolutionState solution = new SolutionState( currentState, elapsedTime: (int)(sw.ElapsedMilliseconds),
                                                                  iterationsCount: iterations,
                                                              maxFrontStatesCount: maxFrontStatesCount,
                                                               uniquesStatesCount: allUniqueStates.Count );
            return solution;

        }

        private StateHash CalculateStateHash( MapTile pos1, MapTile pos2, MapTile pos3, MapTile pos4, MapTile[] positions, eMapSymmetry symmetry )
        {
            StateHash sh;
            sh.a = 0; sh.b = 0; sh.c = 0; sh.d = 0;

#if SYMMETRY_FOR_STATE_HASH_CHECK
            if ( pos1 != null )
                sh.Add( pos1.Index[(int)symmetry], 1 );
            if ( pos2 != null )
                sh.Add( pos2.Index[(int)symmetry], 1 );
            if ( pos3 != null )
                sh.Add( pos3.Index[(int)symmetry], 1 );
            if ( pos4 != null )
                sh.Add( pos4.Index[(int)symmetry], 1 );

            for ( int idx = 0; idx < positions.Length; idx++ )
            {
                if ( positions[idx] != null )
                    sh.Add( positions[idx].Index[(int)symmetry], 2 );
            }
#else
            if ( pos1 != null )
                sh.Add( pos1.Index, 1 );
            if ( pos2 != null )
                sh.Add( pos2.Index, 1 );
            if ( pos3 != null )
                sh.Add( pos3.Index, 1 );
            if ( pos4 != null )
                sh.Add( pos4.Index, 1 );

            for ( int idx = 0; idx < positions.Length; idx++ )
            {
                if ( positions[idx] != null )
                    sh.Add( positions[idx].Index, 2 );
            }
#endif

            return sh;
        }
    }

}
