using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SyncomaniaSolver
{
    public class MapTile
    {
        public enum TileType
        {
            Empty,
            Block,
            Exit,
            Trap,
            PusherUp,
            PusherDown,
            PusherLeft,
            PusherRight,
        }

        public TileType type;

        public int distanceToExit = int.MaxValue;

        //public Tile[] neighbours;
    }

    public class GameState : IComparable<GameState>
    {
        public GameState PrevState { get; private set; }
        public Pos? pos1;
        public Pos? pos2;
        public Pos? pos3;
        public Pos? pos4;

        public int dx;
        public int dy;

        public float weight;
        public int turn;

        public int Hash { get; private set; }

        public GameState( GameState prevState, Pos? pos1, Pos? pos2, Pos? pos3, Pos? pos4, int dx, int dy, int _hash )
        {
            this.PrevState = prevState;
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.pos3 = pos3;
            this.pos4 = pos4;
            this.dx = dx;
            this.dy = dy;
            Hash = _hash;

            if ( prevState != null )
                turn = prevState.turn + 1;
        }

        public bool IsFinished() { return pos1.HasValue == false && pos2.HasValue == false && pos3.HasValue == false && pos4.HasValue == false; }

        public override string ToString()
        {
            return string.Format( "dx: {0,2}  dy: {1,2}", dx, dy );
        }

        public bool Equals( GameState other )
        {
            return pos1.Equals( other.pos1 ) && pos2.Equals( other.pos2 ) && pos3.Equals( other.pos3 ) && pos4.Equals( other.pos4 );
        }

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

            if ( pos1.HasValue )
            {
                //dist = Math.Max( dist, map[pos1.Value].distanceToExit );
                dist += map[pos1.Value].distanceToExit;
            }
            if ( pos2.HasValue )
            {
                //dist = Math.Max( dist, map[pos2.Value].distanceToExit );
                dist += map[pos2.Value].distanceToExit;
            }
            if ( pos3.HasValue )
            {
                //dist = Math.Max( dist, map[pos3.Value].distanceToExit );
                dist += map[pos3.Value].distanceToExit;
            }
            if ( pos4.HasValue )
            { 
                //dist = Math.Max( dist, map[pos4.Value].distanceToExit );
                dist += map[pos4.Value].distanceToExit;
            }

            weight += dist;

            weight += 1.58f * turn;
        }

        public void DumpHistory( Action<GameState> dumper )
        {
            if ( IsFinished() == false )
                return;

            List<GameState> ordered = new List<GameState>();
            GameState state = this;
            while ( state.PrevState != null )
            {
                ordered.Add( state );
                state = state.PrevState;
            }
            
            for ( var idx = ordered.Count - 1; idx >= 0; idx-- )
            {
                dumper( ordered[idx] );
            }
        }
    }

    public class SolutionState : IComparable<SolutionState>
    {
        public GameState State { get; private set; }

        public int Turns { get { return State.turn; } }

        public SolutionState( GameState state )
        {
            State = state;
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

        public MapTile[,] tiles; // x,y indexing
        public int width;
        public int height;
        public eMapSymmetry Symmetry { get; private set; }

        public readonly Pos?[] actors = { null, null, null, null };
        public readonly Pos?[] antiActors = { null, null, null, null };

        public Pos ExitPos { get; private set; }

        public MapTile this[Pos pos]
        {
            get { return tiles[pos.x, pos.y]; }
        }

        /// <summary>
        /// Only single exit is supported
        /// </summary>
        /// <param name="map"></param>
        public void LoadMap( string[] map )
        {
            if ( map == null )
                throw new ArgumentNullException( "map" );

            if ( map.Length == 0 || map[0].Length == 0 )
                throw new ArgumentException( "Array should not be empty" );

            height = map.Length;
            width = map[0].Length;

            tiles = new MapTile[width, height];

            int actorsCount = 0;
            int antiActorsCount = 0;

            int y = 0;

            foreach ( var row in map )
            {
                int x = 0;
                foreach ( var t in row )
                {
                    var tile = new MapTile();
                    tiles[x, y] = tile;

                    tile.type = MapTile.TileType.Empty;

                    if ( t == 'e' )
                    {
                        tile.type = MapTile.TileType.Exit;
                        ExitPos = new Pos { x = x, y = y };
                    }
                    else if ( t == 'x' )
                        tile.type = MapTile.TileType.Trap;
                    else if ( t == '#' )
                        tile.type = MapTile.TileType.Block;
                    else if ( t == '<' )
                        tile.type = MapTile.TileType.PusherLeft;
                    else if ( t == '>' )
                        tile.type = MapTile.TileType.PusherRight;
                    else if ( t == '^' )
                        tile.type = MapTile.TileType.PusherUp;
                    else if ( t == '_' )
                        tile.type = MapTile.TileType.PusherDown;
                    else if ( t == '@' )
                        actors[actorsCount++] = new Pos { x = x, y = y };
                    else if ( t == 'o' )
                        antiActors[antiActorsCount++] = new Pos { x = x, y = y };
                    else if ( t != ' ' )
                        throw new Exception( "Unknown map tile" );

                    x++;
                }
                y++;
            }

            Symmetry = eMapSymmetry.None;

            FindSymmetry();

            CalculateDistanceToExit();
        }

        /// <summary>
        /// false means not a valid new pos (getting into trap)
        /// </summary>
        public bool GetNewPos( Pos pos, int dx, int dy, out Pos? result )
        {
            var x = pos.x + dx;
            var y = pos.y + dy;

            bool bCheckPusher = true;
            while ( true )
            {
                x = Math.Max( Math.Min( x, width - 1 ), 0 );
                y = Math.Max( Math.Min( y, height - 1 ), 0 );

                var tiletype = tiles[x, y].type;
                if ( tiletype == MapTile.TileType.Trap )
                {
                    result = null;
                    return false;
                }
                else if ( tiletype == MapTile.TileType.Exit )
                {
                    result = null;
                    return true;
                }
                else if ( tiletype == MapTile.TileType.Block )
                {
                    result = pos;
                    return true;
                }
                else if ( tiletype == MapTile.TileType.Empty )
                {
                    pos.x = x;
                    pos.y = y;
                    result = pos;
                    return true;
                }
                else if ( tiletype >= MapTile.TileType.PusherUp && tiletype <= MapTile.TileType.PusherRight )
                {
                    pos.x = x;
                    pos.y = y;

                    if ( !bCheckPusher )
                    {
                        result = pos;
                        return true;
                    }

                    if ( tiletype == MapTile.TileType.PusherDown )
                        y++;
                    else if ( tiletype == MapTile.TileType.PusherUp )
                        y--;
                    else if ( tiletype == MapTile.TileType.PusherLeft )
                        x--;
                    else if ( tiletype == MapTile.TileType.PusherRight )
                        x++;

                    bCheckPusher = false;

                    continue;
                }
                else
                {
                    throw new Exception( "Unknown tile type" );
                }
            }
        }

        private bool GetNewState( GameState currentState, int dx, int dy, Func<int,bool> checkHash, out GameState newState )
        {
            Pos? pos1 = null;
            Pos? pos2 = null;
            Pos? pos3 = null;
            Pos? pos4 = null;

            newState = null;

            if ( currentState.pos1.HasValue )
            {
                if ( GetNewPos( currentState.pos1.Value, dx, dy, out pos1 ) == false )
                    return false;
            }
            if ( currentState.pos2.HasValue )
            {
                if ( GetNewPos( currentState.pos2.Value, dx, dy, out pos2 ) == false )
                    return false;
            }
            if ( currentState.pos3.HasValue )
            {
                if ( GetNewPos( currentState.pos3.Value, dx, dy, out pos3 ) == false )
                    return false;
            }
            if ( currentState.pos4.HasValue )
            {
                if ( GetNewPos( currentState.pos4.Value, dx, dy, out pos4 ) == false )
                    return false;
            }

            if ( pos1.HasValue && pos2.HasValue && pos1.Value.Equals( pos2.Value ) || 
                 pos1.HasValue && pos3.HasValue && pos1.Value.Equals( pos3.Value ) || 
                 pos1.HasValue && pos4.HasValue && pos1.Value.Equals( pos4.Value ) || 
                 pos2.HasValue && pos3.HasValue && pos2.Value.Equals( pos3.Value ) || 
                 pos2.HasValue && pos4.HasValue && pos2.Value.Equals( pos4.Value ) || 
                 pos3.HasValue && pos4.HasValue && pos3.Value.Equals( pos4.Value ) )
                return false;

            var hash = CalculateStateHash( ref pos1, ref pos2, ref pos3, ref pos4 );

            if ( checkHash(hash) == false )
                return false;

            newState = new GameState( currentState, pos1, pos2, pos3, pos4, dx, dy, hash );

            return true;
        }

        public GameState GetStartingState()
        {
            var hash = CalculateStateHash( ref actors[0], ref actors[1], ref actors[2], ref actors[3] );

            return new GameState( null, actors[0], actors[1], actors[2], actors[3], 0, 0, hash );
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
        private void CalculateDistanceToExit()
        {
            Queue<Pos> frontTiles = new Queue<Pos>(256);

            frontTiles.Enqueue( ExitPos );

            this[ExitPos].distanceToExit = 0;

            Action<Pos, int, int> f = ( pos, dx, dy ) =>
            {
                var x = pos.x + dx;
                var y = pos.y + dy;

                if ( x < 0 || x >= width || y < 0 || y >= height )
                    return;

                var tile = tiles[x, y];
                var tiletype = tile.type;

                if ( tiletype == MapTile.TileType.Block || tiletype == MapTile.TileType.Trap || tile.distanceToExit != int.MaxValue )
                    return;

                tiles[x, y].distanceToExit = this[pos].distanceToExit + 1;

                frontTiles.Enqueue( new Pos { x = x, y = y } );
            };

            while ( frontTiles.Count > 0 )
            {
                var tile = frontTiles.Dequeue();

                f( tile, -1, 0 );
                f( tile, 0, -1 );
                f( tile, 1, 0 );
                f( tile, 0, 1 );
            }
        }
        /// <summary>
        /// BFS algorithm
        /// </summary>
        /// <param name="map"></param>
        public GameState Solve_BFS()
        {
            var beginState = GetStartingState();

            if ( beginState.IsFinished() )
                return beginState;

            HashSet<int> allUniqueStates = new HashSet<int>();
            allUniqueStates.Add( beginState.Hash );

            Queue<GameState> frontStates = new Queue<GameState>(32000);
            frontStates.Enqueue( beginState );

            int iterations = 0;
            int maxFrontStatesCount = 0;

            var sw = Stopwatch.StartNew();

            GameState currentState = null;

            Func<int, bool> checkHash = (hash) =>
            {
                return allUniqueStates.Contains( hash ) == false;
            };

            Func<GameState, int, int, bool> f = (state, dx, dy) =>
            {
                GameState newState;
                if ( GetNewState( state, dx, dy, checkHash, out newState ) )
                {
                    if ( newState.IsFinished() )
                    {
                        currentState = newState;
                        return true;
                    }

                    allUniqueStates.Add( newState.Hash );
                    if ( ( Symmetry & eMapSymmetry.Horizontal ) == eMapSymmetry.Horizontal )
                        allUniqueStates.Add( GetHashForSymmetricState( newState, 1, 0 ) );
                    if ( ( Symmetry & eMapSymmetry.Vertical ) == eMapSymmetry.Vertical )
                        allUniqueStates.Add( GetHashForSymmetricState( newState, 0, 1 ) );
                    if ( ( Symmetry & eMapSymmetry.Both ) == eMapSymmetry.Both )
                        allUniqueStates.Add( GetHashForSymmetricState( newState, 1, 1 ) );

                    frontStates.Enqueue( newState );
                }
                return false;
            };
                

            while ( frontStates.Count > 0 )
            {
                currentState = frontStates.Dequeue();

                if ( f( currentState, -1, 0 ) )
                    break;
                if ( f( currentState, 0, -1 ) )
                    break;
                if ( f( currentState, 1, 0 ) )
                    break;
                if ( f( currentState, 0, 1 ) )
                    break;

                iterations++;

                maxFrontStatesCount = Math.Max( maxFrontStatesCount, frontStates.Count() );
            }

            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine( "Iterations: {0}", iterations );
            Console.WriteLine( "Unique states created: {0}", allUniqueStates.Count );
            Console.WriteLine( "Max front states count: {0}", maxFrontStatesCount );
            Console.WriteLine( "Elapsed time: {0} ms", elapsed );

            return currentState;
        }

        public GameState Solve_AStar()
        {
            var beginState = GetStartingState();

            if ( beginState.IsFinished() )
                return beginState;

            HashSet<int> allUniqueStates = new HashSet<int>();
            allUniqueStates.Add( beginState.Hash );

            PriorityQueue<GameState> frontStates = new PriorityQueue<GameState>( 32000 );
            frontStates.Add( beginState );

            int iterations = 0;
            int maxFrontStatesCount = 0;

            var sw = Stopwatch.StartNew();

            GameState currentState = null;

            PriorityQueue<SolutionState> solutions = new PriorityQueue<SolutionState>(100);

            Func<int, bool> checkHash = (hash) =>
            {
                return allUniqueStates.Contains( hash ) == false;
            };

            Func<GameState, int, int, bool> f = ( state, dx, dy ) =>
            {
                GameState newState;
                if ( GetNewState( state, dx, dy, checkHash, out newState ) )
                {
                    if ( newState.IsFinished() )
                    {
                        solutions.Add( new SolutionState( newState ) );
                        return solutions.Count == 1;
                    }

                    allUniqueStates.Add(newState.Hash);
                    if ((Symmetry & eMapSymmetry.Horizontal) == eMapSymmetry.Horizontal)
                        allUniqueStates.Add(GetHashForSymmetricState(newState, 1, 0));
                    if ((Symmetry & eMapSymmetry.Vertical) == eMapSymmetry.Vertical)
                        allUniqueStates.Add(GetHashForSymmetricState(newState, 0, 1));
                    if ((Symmetry & eMapSymmetry.Both) == eMapSymmetry.Both)
                        allUniqueStates.Add(GetHashForSymmetricState(newState, 1, 1));

                    newState.CalculateWeight( this );

                    frontStates.Add( newState );
                }
                return false;
            };


            while ( frontStates.Count > 0 )
            {
                currentState = frontStates.RemoveMin();

                if ( f( currentState, -1, 0 ) )
                    break;
                if ( f( currentState, 0, -1 ) )
                    break;
                if ( f( currentState, 1, 0 ) )
                    break;
                if ( f( currentState, 0, 1 ) )
                    break;

                iterations++;

                maxFrontStatesCount = Math.Max( maxFrontStatesCount, frontStates.Count );
            }

            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine( "Iterations: {0}", iterations );
            Console.WriteLine( "Unique states created: {0}", allUniqueStates.Count );
            Console.WriteLine( "Max front states count: {0}", maxFrontStatesCount );
            Console.WriteLine( "Elapsed time: {0} ms", elapsed );

            return solutions.RemoveMin().State;

        }

        private int CalculateStateHash( ref Pos? pos1, ref Pos? pos2, ref Pos? pos3, ref Pos? pos4 )
        {
            byte hl0 = (byte)( pos1.HasValue ? pos1.Value.y * width + pos1.Value.x + 1 : 0 );
            byte hl1 = (byte)( pos2.HasValue ? pos2.Value.y * width + pos2.Value.x + 1 : 0 );
            byte hl2 = (byte)( pos3.HasValue ? pos3.Value.y * width + pos3.Value.x + 1 : 0 );
            byte hl3 = (byte)( pos4.HasValue ? pos4.Value.y * width + pos4.Value.x + 1 : 0 );

            byte tmp;
            if ( hl1 < hl0 )
            {
                tmp = hl0;
                hl0 = hl1;
                hl1 = tmp;
            }
            if ( hl3 < hl2 )
            {
                tmp = hl2;
                hl2 = hl3;
                hl3 = tmp;
            }

            if ( hl3 < hl0 )
            {
                tmp = hl0;
                hl0 = hl3;
                hl3 = tmp;
            }
            if ( hl2 < hl1 )
            {
                tmp = hl2;
                hl2 = hl1;
                hl1 = tmp;
            }

            if ( hl1 < hl0 )
            {
                tmp = hl0;
                hl0 = hl1;
                hl1 = tmp;
            }
            if ( hl3 < hl2 )
            {
                tmp = hl2;
                hl2 = hl3;
                hl3 = tmp;
            }

            return hl0 | ( hl1 << 8 ) | ( hl2 << 16 ) | ( hl3 << 24 );
        }

        private int GetHashForSymmetricState( GameState st, int horiz, int vert )
        {
            var add_h = horiz * (width - 1) + 1;
            var mul_h = 1 - 2 * horiz;

            var add_v = vert * ( height - 1 );
            var mul_v = 1 - 2 * vert;

            byte hl0 = (byte)( st.pos1.HasValue ? ( add_v + mul_v * st.pos1.Value.y ) * width + add_h + mul_h * st.pos1.Value.x : 0 );
            byte hl1 = (byte)( st.pos2.HasValue ? ( add_v + mul_v * st.pos2.Value.y ) * width + add_h + mul_h * st.pos2.Value.x : 0 );
            byte hl2 = (byte)( st.pos3.HasValue ? ( add_v + mul_v * st.pos3.Value.y ) * width + add_h + mul_h * st.pos3.Value.x : 0 );
            byte hl3 = (byte)( st.pos4.HasValue ? ( add_v + mul_v * st.pos4.Value.y ) * width + add_h + mul_h * st.pos4.Value.x : 0 );
            byte tmp;
            if ( hl1 < hl0 )
            {
                tmp = hl0;
                hl0 = hl1;
                hl1 = tmp;
            }
            if ( hl3 < hl2 )
            {
                tmp = hl2;
                hl2 = hl3;
                hl3 = tmp;
            }

            if ( hl3 < hl0 )
            {
                tmp = hl0;
                hl0 = hl3;
                hl3 = tmp;
            }
            if ( hl2 < hl1 )
            {
                tmp = hl2;
                hl2 = hl1;
                hl1 = tmp;
            }

            if ( hl1 < hl0 )
            {
                tmp = hl0;
                hl0 = hl1;
                hl1 = tmp;
            }
            if ( hl3 < hl2 )
            {
                tmp = hl2;
                hl2 = hl3;
                hl3 = tmp;
            }

            return hl0 | ( hl1 << 8 ) | ( hl2 << 16 ) | ( hl3 << 24 );
        }
    }

}
