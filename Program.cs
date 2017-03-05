using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SyncomaniaSolver
{
    class Program
    {


        static void Main(string[] args)
        {
//#if RELEASE
//            if (args.Length < 1)
//                return;

//            var mapFileName = args[0];

//            string data;
//            using (StreamReader sr = new StreamReader(mapFileName))
//            {
//                data = sr.ReadToEnd();
//            }
//#else
            var data = level_8;
//#endif

            Map map = new Map();
            map.LoadMap( data );

            //var state = map.PathFind_BFS_Queue();
            var state = map.PathFind_AStar();

            DumpPath( state );

            Console.ReadKey();
        }

        static void DumpPath( State stateAtFinish )
        {
            if ( stateAtFinish.IsFinished() )
            {
                List<State> ordered = new List<State>();
                while ( stateAtFinish.prevState != null )
                {
                    ordered.Add( stateAtFinish );
                    stateAtFinish = stateAtFinish.prevState;
                }

                for ( var idx = ordered.Count - 1; idx >= 0; idx-- )
                {
                    Console.WriteLine( ordered[idx].ToString() );
                }
                Console.WriteLine( "Solution turns count: {0}", ordered.Count );
            }
            else
            {
                Console.WriteLine( "No solution found." );
            }
        }

        static string[] test_map = new string[] { "@@#> #",
                                                  "_# e< ",
                                                  "  >^  " };

        static string[] test_2 = new string[] { "       ",
                                                "@# # #@",
                                                " # # # ",
                                                " # # # ",
                                                "   e   ",
                                                " # # # ",
                                                " # # # ",
                                                "@# # #@",
                                                "       " };

           static string[] level_8 = new string[] { "###     ###",
                                                    "#     #   #",
                                                    "##@   # @##",
                                                    "##    #  ##",
                                                    "## ##### ##",
                                                    "     e     ",
                                                    "## ##### ##",
                                                    "##  #    ##",
                                                    "##@ #   @##",
                                                    "#   #     #",
                                                    "###     ###" };
        // Detect and consider diagonal symmetry
        // AntiActors
        // Block and AA pushing
    }

    [Flags]
    public enum eSymmetry
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = Horizontal | Vertical,
    }

    public class Tile
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
        //public int distanceToExit;
        //public Tile[] neighbours;
    }

    public class State : IComparable<State>
    {
        public State prevState;
        public Pos? pos1;
        public Pos? pos2;
        public Pos? pos3;
        public Pos? pos4;

        public int dx;
        public int dy;

        public int weight;
        public int turn;

        public int Hash { get; private set; }

        public State( State prevState, Pos? pos1, Pos? pos2, Pos? pos3, Pos? pos4, int dx, int dy, int _hash )
        {
            this.prevState = prevState;
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.pos3 = pos3;
            this.pos4 = pos4;
            this.dx = dx;
            this.dy = dy;
            Hash = _hash;
        }

        public bool IsFinished() { return pos1.HasValue == false && pos2.HasValue == false && pos3.HasValue == false && pos4.HasValue == false; }

        public override string ToString()
        {
            return string.Format( "dx: {0,2}  dy: {1,2}", dx, dy );
        }

        public bool Equals( State other )
        {
            return pos1.Equals( other.pos1 ) && pos2.Equals( other.pos2 ) && pos3.Equals( other.pos3 ) && pos4.Equals( other.pos4 );
        }

        public int CompareTo( State other )
        {
            return weight.CompareTo( other.weight );
        }

        /// <summary>
        /// Basic heuristic function for A-star search.
        /// It is a bit difficult to find out some common function for all maps.
        /// </summary>
        /// <param name="exit"></param>
        public void CalculateWeight( Pos exit )
        {
            if ( prevState != null )
                weight = prevState.turn;

            if ( pos1.HasValue )
            {
                weight += exit.Distance( pos1.Value );
                turn++;
            }
            if ( pos2.HasValue )
            {
                weight += exit.Distance( pos2.Value );
                turn++;
            }
            if ( pos3.HasValue )
            {
                weight += exit.Distance( pos3.Value );
                turn++;
            }
            if ( pos4.HasValue )
            {
                weight += exit.Distance( pos4.Value );
                turn++;
            }

            weight += turn;
        }
    }

    public class SolutionState : IComparable<SolutionState>
    {
        public State State { get; private set; }

        public int Turns { get { return State.turn; } }

        public SolutionState( State state )
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

    public class Map
    {
        public Tile[,] tiles; // x,y indexing
        public int width;
        public int height;
        public eSymmetry Symmetry { get; private set; }

        public readonly Pos?[] actors = { null, null, null, null };
        public readonly Pos?[] antiActors = { null, null, null, null };

        public Pos ExitPos { get; private set; }

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

            tiles = new Tile[width, height];

            int actorsCount = 0;
            int antiActorsCount = 0;

            int y = 0;

            foreach ( var row in map )
            {
                int x = 0;
                foreach ( var t in row )
                {
                    var tile = new Tile();
                    tiles[x, y] = tile;

                    tile.type = Tile.TileType.Empty;

                    if ( t == 'e' )
                    {
                        tile.type = Tile.TileType.Exit;
                        ExitPos = new Pos { x = x, y = y };
                    }
                    else if ( t == 'x' )
                        tile.type = Tile.TileType.Trap;
                    else if ( t == '#' )
                        tile.type = Tile.TileType.Block;
                    else if ( t == '<' )
                        tile.type = Tile.TileType.PusherLeft;
                    else if ( t == '>' )
                        tile.type = Tile.TileType.PusherRight;
                    else if ( t == '^' )
                        tile.type = Tile.TileType.PusherUp;
                    else if ( t == '_' )
                        tile.type = Tile.TileType.PusherDown;
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

            Symmetry = eSymmetry.None;

            FindSymmetry();
        }

        public int CalculateStateHash( ref Pos? pos1, ref Pos? pos2, ref Pos? pos3, ref Pos? pos4 )
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
                if ( tiletype == Tile.TileType.Trap )
                {
                    result = null;
                    return false;
                }
                else if ( tiletype == Tile.TileType.Exit )
                {
                    result = null;
                    return true;
                }
                else if ( tiletype == Tile.TileType.Block )
                {
                    result = pos;
                    return true;
                }
                else if ( tiletype == Tile.TileType.Empty )
                {
                    pos.x = x;
                    pos.y = y;
                    result = pos;
                    return true;
                }
                else if ( tiletype >= Tile.TileType.PusherUp && tiletype <= Tile.TileType.PusherRight )
                {
                    pos.x = x;
                    pos.y = y;

                    if ( !bCheckPusher )
                    {
                        result = pos;
                        return true;
                    }

                    if ( tiletype == Tile.TileType.PusherDown )
                        y++;
                    else if ( tiletype == Tile.TileType.PusherUp )
                        y--;
                    else if ( tiletype == Tile.TileType.PusherLeft )
                        x--;
                    else if ( tiletype == Tile.TileType.PusherRight )
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

        private bool GetNewState( State currentState, int dx, int dy, Func<int,bool> checkHash, out State newState )
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

            newState = new State( currentState, pos1, pos2, pos3, pos4, dx, dy, hash );

            return true;
        }

        public State GetStartingState()
        {
            var hash = CalculateStateHash( ref actors[0], ref actors[1], ref actors[2], ref actors[3] );

            return new State( null, actors[0], actors[1], actors[2], actors[3], 0, 0, hash );
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
                Symmetry |= eSymmetry.Vertical;

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
                Symmetry |= eSymmetry.Horizontal;
        }

        /// <summary>
        /// BFS algorithm
        /// </summary>
        /// <param name="map"></param>
        public State PathFind_BFS_Queue()
        {
            var beginState = GetStartingState();

            if ( beginState.IsFinished() )
                return beginState;

            HashSet<int> allUniqueStates = new HashSet<int>();
            allUniqueStates.Add( beginState.Hash );

            Queue<State> frontStates = new Queue<State>(32000);
            frontStates.Enqueue( beginState );

            int iterations = 0;
            int maxFrontStatesCount = 0;

            var sw = Stopwatch.StartNew();

            State currentState = null;

            Func<int, bool> checkHash = (hash) =>
            {
                return allUniqueStates.Contains( hash ) == false;
            };

            Func<State, int, int, bool> f = (state, dx, dy) =>
            {
                State newState;
                if ( GetNewState( state, dx, dy, checkHash, out newState ) )
                {
                    if ( newState.IsFinished() )
                    {
                        currentState = newState;
                        return true;
                    }

                    allUniqueStates.Add( newState.Hash );
                    if ( ( Symmetry & eSymmetry.Horizontal ) == eSymmetry.Horizontal )
                        allUniqueStates.Add( GetHashForSymmetricState( newState, 1, 0 ) );
                    if ( ( Symmetry & eSymmetry.Vertical ) == eSymmetry.Vertical )
                        allUniqueStates.Add( GetHashForSymmetricState( newState, 0, 1 ) );
                    if ( ( Symmetry & eSymmetry.Both ) == eSymmetry.Both )
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

        public State PathFind_AStar()
        {
            var beginState = GetStartingState();

            if ( beginState.IsFinished() )
                return beginState;

            HashSet<int> allUniqueStates = new HashSet<int>();
            allUniqueStates.Add( beginState.Hash );

            PriorityQueue<State> frontStates = new PriorityQueue<State>( 32000 );
            frontStates.Add( beginState );

            int iterations = 0;
            int maxFrontStatesCount = 0;

            var sw = Stopwatch.StartNew();

            State currentState = null;

            PriorityQueue<SolutionState> solutions = new PriorityQueue<SolutionState>(100);

            Func<int, bool> checkHash = (hash) =>
            {
                return allUniqueStates.Contains( hash ) == false;
            };

            Func<State, int, int, bool> f = ( state, dx, dy ) =>
            {
                State newState;
                if ( GetNewState( state, dx, dy, checkHash, out newState ) )
                {
                    if ( newState.IsFinished() )
                    {
                        solutions.Add( new SolutionState( newState ) );
                        return solutions.Count == 1;
                    }

                    allUniqueStates.Add(newState.Hash);
                    if ((Symmetry & eSymmetry.Horizontal) == eSymmetry.Horizontal)
                        allUniqueStates.Add(GetHashForSymmetricState(newState, 1, 0));
                    if ((Symmetry & eSymmetry.Vertical) == eSymmetry.Vertical)
                        allUniqueStates.Add(GetHashForSymmetricState(newState, 0, 1));
                    if ((Symmetry & eSymmetry.Both) == eSymmetry.Both)
                        allUniqueStates.Add(GetHashForSymmetricState(newState, 1, 1));

                    newState.CalculateWeight( ExitPos );

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

        private int GetHashForSymmetricState( State st, int horiz, int vert )
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
