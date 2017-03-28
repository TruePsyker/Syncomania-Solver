using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SyncomaniaSolver;

namespace SyncomaniaSolverTests
{
    [TestClass]
    public class GameLogicTests
    {
        [TestMethod]
        public void Map_LoadMapIsValid()
        {
            GameMap.TestIsOn = true;

            var map = new GameMap();
            var data = new string[] { "ex #<>^_@" };
            map.LoadMap( data );

            Assert.AreEqual( MapTile.TileType.Exit, map[0, 0].type );
            Assert.AreEqual( MapTile.TileType.Trap, map[1, 0].type );
            Assert.AreEqual( MapTile.TileType.Empty, map[2, 0].type );
            Assert.AreEqual( MapTile.TileType.Block, map[3, 0].type );
            Assert.AreEqual( MapTile.TileType.PusherLeft, map[4, 0].type );
            Assert.AreEqual( MapTile.TileType.PusherRight, map[5, 0].type );
            Assert.AreEqual( MapTile.TileType.PusherUp, map[6, 0].type );
            Assert.AreEqual( MapTile.TileType.PusherDown, map[7, 0].type );
            Assert.AreEqual( MapTile.TileType.Empty, map[8, 0].type );

            Assert.AreEqual( map[8, 0], map.actors[0] );
        }

        [TestMethod]
        public void Map_FindSymmetryIsValid()
        {
            GameMap.TestIsOn = true;

            var map = new GameMap();
            var data = new string[] { "# # #",
                                      "  #  " };
            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.Horizontal, map.Symmetry );

            data = new string[] { "# # ",
                                  "# # " };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.Vertical, map.Symmetry );

            // Both symmetry
            data = new string[] { "  ",
                                  "  " };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.Both, map.Symmetry );

            data = new string[] { "# # #",
                                  "# # #" };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.Both, map.Symmetry );

            data = new string[] { "# # #",
                                  "#   #",
                                  "# # #"};

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.Both, map.Symmetry );


            // None symmetry
            data = new string[] { "  # #",
                                  "# #  " };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.None, map.Symmetry );

            data = new string[] { "####",
                                  "#   ",
                                  "#   " };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.None, map.Symmetry );

            data = new string[] { "##",
                                  "# " };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.None, map.Symmetry );

            data = new string[] { "  #",
                                  "# #",
                                  "# #" };

            map.LoadMap( data );
            Assert.AreEqual( GameMap.eMapSymmetry.None, map.Symmetry );
        }

        [TestMethod]
        public void Map_GetNewPos_BoundChecking_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();
            var data = new string[] { "  ",
                                      "  " };
            map.LoadMap( data );
            MapTile pos, newpos;
            bool bValidPos;

            pos = map[0, 0];
            bValidPos = map.GetNewPos( pos, Direction.Left, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( pos, newpos );

            pos = map[1, 0];
            bValidPos = map.GetNewPos( pos, Direction.Right, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( pos, newpos );

            pos = map[1, 0];
            bValidPos = map.GetNewPos( pos, Direction.Up, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( pos, newpos );

            pos = map[1, 1];
            bValidPos = map.GetNewPos( pos, Direction.Down, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( pos, newpos );
        }

        [TestMethod]
        public void Map_GetNewPos_Empty_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();
            var data = new string[] { "  ",
                                      "  " };
            map.LoadMap( data );
            MapTile pos, newpos;

            pos = map[0, 0];
            bool bValidPos = map.GetNewPos( pos, Direction.Down, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[0, 1], newpos );
        }

        [TestMethod]
        public void Map_GetNewPos_Block_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();
            var data = new string[] { "  ",
                                      " #" };
            map.LoadMap( data );
            MapTile pos, newpos;

            pos = map[0, 1];
            bool bValidPos = map.GetNewPos( pos, Direction.Right, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( newpos, pos );
        }

        [TestMethod]
        public void Map_GetNewPos_Exit_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();
            var data = new string[] { " e",
                                      "  " };
            map.LoadMap( data );
            MapTile pos, newpos;

            pos = map[0, 0];
            bool bValidPos = map.GetNewPos( pos, Direction.Right, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.IsNull( newpos );
        }

        [TestMethod]
        public void Map_GetNewPos_Trap_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();
            var data = new string[] { "x ",
                                      "  " };
            map.LoadMap( data );
            MapTile pos, newpos;

            pos = map[0, 1];
            bool bValidPos = map.GetNewPos( pos, Direction.Up, out newpos );
            Assert.IsFalse( bValidPos );
        }

        [TestMethod]
        public void Map_GetNewPos_Pushers_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();
            var data1 = new string[] { " > ^",
                                       "^ _<",
                                       " <  " };
            map.LoadMap( data1 );
            MapTile pos, newpos;

            pos = map[0, 0];
            bool bValidPos = map.GetNewPos( pos, Direction.Right, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[2, 0], newpos );

            pos = map[2, 0];
            bValidPos = map.GetNewPos( pos, Direction.Down, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[2, 2], newpos );

            pos = map[2, 2];
            bValidPos = map.GetNewPos( pos, Direction.Left, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[0, 2], newpos );

            pos = map[0, 2];
            bValidPos = map.GetNewPos( pos, Direction.Up, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[0, 0], newpos );

            // Pusher looks into wall. Should stay at pusher position.
            pos = map[2, 0];
            bValidPos = map.GetNewPos( pos, Direction.Right, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[3, 0], newpos );

            // Pusher looks into another pusher. Should stay at latter.
            pos = map[3, 2];
            bValidPos = map.GetNewPos( pos, Direction.Up, out newpos );
            Assert.IsTrue( bValidPos );
            Assert.AreEqual( map[2, 1], newpos );
        }

        [TestMethod]
        public void Map_CalculateDistanceToExit_IsValid()
        {
            GameMap.TestIsOn = true;
            var map = new GameMap();

            var data = new string[] { "   #",
                                      "e# x",
                                      " #  ",
                                      " <  " };

            map.LoadMap( data );

            Assert.AreEqual( 0, map[0, 1].distanceToExit );
            Assert.AreEqual( 1, map[0, 0].distanceToExit );
            Assert.AreEqual( 5, map[3, 3].distanceToExit );
            Assert.AreEqual( int.MaxValue, map[1, 1].distanceToExit );
            Assert.AreEqual( int.MaxValue, map[3, 1].distanceToExit );
        }
    }
}
