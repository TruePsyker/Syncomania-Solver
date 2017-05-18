using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public interface ILevelEncoder
    {
        bool DecodeTile( char input, out MapTile.TileType tileType, out MovingObject.ObjectType objectType );
        char EncodeTile( MapTile.TileType tileType, MovingObject.ObjectType objectType );
    }

    public class DefaultLevelEncoder : ILevelEncoder
    {
        public static string encodingChars = "abcdefghijklmnopqrstuvwxyzABCDEF"; // 32bits

        public bool DecodeTile( char input, out MapTile.TileType tileType, out MovingObject.ObjectType objectType )
        {
            return DecodeTileImpl( input, out tileType, out objectType );
        }
        public char EncodeTile( MapTile.TileType tileType, MovingObject.ObjectType objectType )
        {
            return EncodeTileImpl( tileType, objectType );
        }

        public static bool DecodeTileImpl( char input, out MapTile.TileType tileType, out MovingObject.ObjectType objectType )
        {
            var idx = encodingChars.IndexOf( input );
            if ( idx == -1 )
            {
                tileType = MapTile.TileType.Empty;
                objectType = MovingObject.ObjectType.None;
                return false;
            }
            var tt = idx & 0x7;
            tileType = (MapTile.TileType)tt;
            tt = ( idx >> 3 ) & 0x3;
            objectType = (MovingObject.ObjectType)tt;
            return true;
        }

        public static char EncodeTileImpl( MapTile.TileType tileType, MovingObject.ObjectType objectType )
        {
            char result;
            result = (char)tileType;
            result |= (char)( (int)objectType << 3 );
            return encodingChars[result];
        }
    }

    public class TestLevelEncoder : ILevelEncoder
    {
        public bool DecodeTile( char input, out MapTile.TileType tileType, out MovingObject.ObjectType objectType )
        {
            return DecodeTileImpl( input, out tileType, out objectType );
        }

        public static bool DecodeTileImpl( char input, out MapTile.TileType tileType, out MovingObject.ObjectType objectType )
        {
            tileType = MapTile.TileType.Empty;
            objectType = MovingObject.ObjectType.None;

            switch ( input )
            {
                case ' ':
                    tileType = MapTile.TileType.Empty;
                    break;
                case 'e':
                    tileType = MapTile.TileType.Exit;
                    break;
                case 'x':
                    tileType = MapTile.TileType.Trap;
                    break;
                case '#':
                    tileType = MapTile.TileType.Block;
                    break;
                case '<':
                    tileType = MapTile.TileType.PusherLeft;
                    break;
                case '>':
                    tileType = MapTile.TileType.PusherRight;
                    break;
                case '^':
                    tileType = MapTile.TileType.PusherUp;
                    break;
                case '_':
                    tileType = MapTile.TileType.PusherDown;
                    break;
                case '@':
                    objectType = MovingObject.ObjectType.Actor;
                    break;
                case 'a':
                    objectType = MovingObject.ObjectType.AntiActor;
                    break;
                case 'b':
                    objectType = MovingObject.ObjectType.Box;
                    break;
                default:
                    return false;
            }
            return true;
        }

        public char EncodeTile( MapTile.TileType tileType, MovingObject.ObjectType objectType )
        {
            throw new NotImplementedException();
        }
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

        public static char[] EncodedTiles = { '.', 'b', 'e', 't', 'u', 'd', 'l', 'r' };

        public MapTile( TileType type, int x, int y, int[] index )
        {
            this.type = type;
            position = new Pos{ x = x, y = y };
            Index = index;
        }
        public TileType type;
        public int distanceToExit = int.MaxValue;
        public Pos position;
        public MapTile[] neighbours = new MapTile[4];
        public int[] Index { get; private set; }
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

    public class GameState : IComparable<GameState>
    {
        public GameState PrevState { get; private set; }
        public MapTile pos1;
        public MapTile pos2;
        public MapTile pos3;
        public MapTile pos4;

        public Direction moveDir;

        public float weight;
        public int turn;

        public int Hash { get; private set; }

        public GameState( GameState prevState, MapTile pos1, MapTile pos2, MapTile pos3, MapTile pos4, Direction moveDir, int _hash )
        {
            this.PrevState = prevState;
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.pos3 = pos3;
            this.pos4 = pos4;
            this.moveDir = moveDir;
            Hash = _hash;

            if ( prevState != null )
                turn = prevState.turn + 1;
        }

        public bool IsFinished() { return pos1 == null && pos2 == null && pos3 == null && pos4 == null; }

        public override string ToString()
        {
            return string.Format( "move:  {0}", moveDir.ToString() );
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

            weight += 1.58f * turn;
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

        public int width;
        public int height;
        public eMapSymmetry Symmetry { get; private set; }

        public readonly MapTile[] actors = { null, null, null, null };
        public readonly MapTile[] antiActors = { null, null, null, null };

        public MapTile ExitTile { get; private set; }

        public MapTile this[int x, int y]
        {
            get { return tiles[x, y]; }
        }

        private MapTile[,] tiles; // x,y indexing

        private ILevelEncoder levelEncoder;

        public GameMap()
        {
            if ( TestIsOn )
                levelEncoder = new TestLevelEncoder();
            else
                levelEncoder = new DefaultLevelEncoder();
        }

        private bool LoadMapImpl( int width, int height, IEnumerable<char> mapEnumerable )
        {
            this.width = width;
            this.height = height;

            tiles = new MapTile[width, height];

            ExitTile = null;

            int actorsCount = 0;
            int antiActorsCount = 0;

            int x = 0, y = 0;
            int index = 0;

            foreach ( var t in mapEnumerable )
            {
                x = index % width;
                y = index / width;
                index++;

                MapTile.TileType tileType;
                MovingObject.ObjectType objectType;

                if ( levelEncoder.DecodeTile( t, out tileType, out objectType ) == false )
                {
                    Console.WriteLine( "Unknown map tile" );
                    return false;
                }

                var tile = new MapTile( tileType, x, y, GetIndexBySymmetry( index, x, y ) );
                tiles[x, y] = tile;

                if ( tileType == MapTile.TileType.Exit )
                {
                    if ( ExitTile != null )
                    {
                        Console.WriteLine( "Map exit already defined" );
                        return false;
                    }
                    ExitTile = tile;
                }

                if ( objectType == MovingObject.ObjectType.Actor )
                    actors[actorsCount++] = tile;
                else if ( objectType == MovingObject.ObjectType.AntiActor )
                    antiActors[antiActorsCount++] = tile;
            }

            if ( index != width * height ) {
                Console.WriteLine( "Invalid map data" );
                return false;
            }

            if ( TestIsOn == false && actorsCount == 0 ) {
                Console.WriteLine( "No actors on map" );
                return false;
            }

            if ( TestIsOn == false && ExitTile == null ) {
                Console.WriteLine( "No exit on map" );
                return false;
            }

            SetupTileNeighbourhood();

            Symmetry = eMapSymmetry.None;

            FindSymmetry();

            var res = CalculateDistanceToExit();

            return res;
        }

        private int[] GetIndexBySymmetry( int index, int x, int y )
        {
            var arr = new int[4];
            arr[0] = index;
            arr[1] = width * (y + 1) - x - 1;
            arr[2] = width * ( height - y - 1 ) + x;
            arr[3] = width * height - index;
            return arr;
        }

        public bool LoadMap( string map )
        {
            if ( String.IsNullOrEmpty( map ) ) {
                Console.WriteLine( "Map data is null or empty" );
                return false;
            }

            return LoadMapImpl( 11, 11, map );
        }

        /// <summary>
        /// Only single exit is supported
        /// </summary>
        /// <param name="map"></param>
        public bool LoadMap( string[] map )
        {
            if ( map == null || map.Length == 0 || map[0].Length == 0 ) {
                Console.WriteLine( "Map data is null or empty" );
                return false;
            }

            var q = from row in map
                     from item in row
                     select item;

            return LoadMapImpl( map[0].Length, map.Length, q );
        }

        /// <summary>
        /// false means not a valid new pos (getting into trap)
        /// </summary>
        public bool GetNewPos( MapTile pos, Direction dir, out MapTile result )
        {
            bool bCheckPusher = true;
            while ( true )
            {
                var neightbourTile = pos.neighbours[(int)dir];
                if ( neightbourTile == null )
                {
                    result = pos;
                    return true;
                }

                var tiletype = neightbourTile.type;
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
                    result = neightbourTile;
                    return true;
                }
                else if ( tiletype >= MapTile.TileType.PusherUp && tiletype <= MapTile.TileType.PusherRight )
                {
                    pos = neightbourTile;

                    if ( !bCheckPusher )
                    {
                        result = pos;
                        return true;
                    }

                    if ( tiletype == MapTile.TileType.PusherDown )
                        dir = Direction.Down;
                    else if ( tiletype == MapTile.TileType.PusherUp )
                        dir = Direction.Up;
                    else if ( tiletype == MapTile.TileType.PusherLeft )
                        dir = Direction.Left;
                    else if ( tiletype == MapTile.TileType.PusherRight )
                        dir = Direction.Right;

                    bCheckPusher = false;

                    continue;
                }
                else
                {
                    throw new Exception( "Unknown tile type" );
                }
            }
        }

        public GameState GetNewState( GameState currentState, Direction dir, Func<int,bool> checkHash )
        {
            MapTile pos1 = null;
            MapTile pos2 = null;
            MapTile pos3 = null;
            MapTile pos4 = null;

            if ( currentState.pos1 != null )
            {
                if ( GetNewPos( currentState.pos1, dir, out pos1 ) == false )
                    return null;
            }
            if ( currentState.pos2 != null )
            {
                if ( GetNewPos( currentState.pos2, dir, out pos2 ) == false )
                    return null;
            }
            if ( currentState.pos3 != null )
            {
                if ( GetNewPos( currentState.pos3, dir, out pos3 ) == false )
                    return null;
            }
            if ( currentState.pos4 != null )
            {
                if ( GetNewPos( currentState.pos4, dir, out pos4 ) == false )
                    return null;
            }

            if ( pos1 != null && ( pos1 == pos2 || pos1 == pos3 || pos1 == pos4 ) ||
                 pos2 != null && ( pos2 == pos3 || pos2 == pos4) ||
                 pos3 != null && pos3 == pos4 )
                return null;

            var hash = CalculateStateHash( pos1, pos2, pos3, pos4, eMapSymmetry.None );
            if ( checkHash != null && checkHash( hash ) == false )
                return null;

            //for ( eMapSymmetry s = eMapSymmetry.Horizontal; s <= eMapSymmetry.Both; s++ )
            //{
            //    if ( ( Symmetry & s ) == s )
            //    {
            //        var hashOther = CalculateStateHash( pos1, pos2, pos3, pos4, s );
            //        if ( checkHash( hashOther ) == false )
            //            return null;
            //    }
            //}

            return new GameState( currentState, pos1, pos2, pos3, pos4, dir, hash );
        }

        public GameState GetStartingState()
        {
            var hash = CalculateStateHash( actors[0], actors[1], actors[2], actors[3], eMapSymmetry.None );

            return new GameState( null, actors[0], actors[1], actors[2], actors[3], Direction.Left, hash );
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

            Func<GameState, Direction, bool> f = ( state, dir ) =>
            {
                GameState newState = GetNewState( state, dir, checkHash );
                if ( newState != null )
                {
                    if ( newState.IsFinished() )
                    {
                        solutions.Add( new SolutionState( newState ) );
                        return solutions.Count == 1;
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

            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine( "Iterations: {0}", iterations );
            Console.WriteLine( "Unique states created: {0}", allUniqueStates.Count );
            Console.WriteLine( "Max front states count: {0}", maxFrontStatesCount );
            Console.WriteLine( "Elapsed time: {0} ms", elapsed );

            return solutions.RemoveMin().State;

        }

        private int CalculateStateHash( MapTile pos1, MapTile pos2, MapTile pos3, MapTile pos4, eMapSymmetry symmetry )
        {
            byte hl0 = (byte)( pos1 != null ? pos1.Index[(int)symmetry] : 0 );
            byte hl1 = (byte)( pos2 != null ? pos2.Index[(int)symmetry] : 0 );
            byte hl2 = (byte)( pos3 != null ? pos3.Index[(int)symmetry] : 0 );
            byte hl3 = (byte)( pos4 != null ? pos4.Index[(int)symmetry] : 0 );

            byte tmp;
            if ( hl1 < hl0 ) {
                tmp = hl0;
                hl0 = hl1;
                hl1 = tmp;
            }
            if ( hl3 < hl2 ) {
                tmp = hl2;
                hl2 = hl3;
                hl3 = tmp;
            }
            if ( hl3 < hl0 ) {
                tmp = hl0;
                hl0 = hl3;
                hl3 = tmp;
            }
            if ( hl2 < hl1 ) {
                tmp = hl2;
                hl2 = hl1;
                hl1 = tmp;
            }
            if ( hl1 < hl0 ) {
                tmp = hl0;
                hl0 = hl1;
                hl1 = tmp;
            }
            if ( hl3 < hl2 ) {
                tmp = hl2;
                hl2 = hl3;
                hl3 = tmp;
            }

            return hl0 | ( hl1 << 8 ) | ( hl2 << 16 ) | ( hl3 << 24 );
        }
    }

}
