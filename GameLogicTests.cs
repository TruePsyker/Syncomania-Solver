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
            var map = new Map();
            var data = new string[] { "ex #<>^_@" };
            map.LoadMap( data );

            Assert.AreEqual( Tile.TileType.Exit, map.tiles[0, 0].type );
            Assert.AreEqual( Tile.TileType.Trap, map.tiles[1, 0].type );
            Assert.AreEqual( Tile.TileType.Empty, map.tiles[2, 0].type );
            Assert.AreEqual( Tile.TileType.Block, map.tiles[3, 0].type );
            Assert.AreEqual( Tile.TileType.PusherLeft, map.tiles[4, 0].type );
            Assert.AreEqual( Tile.TileType.PusherRight, map.tiles[5, 0].type );
            Assert.AreEqual( Tile.TileType.PusherUp, map.tiles[6, 0].type );
            Assert.AreEqual( Tile.TileType.PusherDown, map.tiles[7, 0].type );
            Assert.AreEqual( Tile.TileType.Empty, map.tiles[8, 0].type );

            Assert.AreEqual( new Pos { x = 8, y = 0 }, map.actors[0] );
        }

        [TestMethod]
        public void Map_FindSymmetryIsValid()
        {
            var map = new Map();
            var data = new string[] { "# # #",
                                      "  #  " };
            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.Horizontal, map.Symmetry );

            data = new string[] { "# # ",
                                  "# # " };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.Vertical, map.Symmetry );

            // Both symmetry
            data = new string[] { "  ",
                                  "  " };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.Both, map.Symmetry );

            data = new string[] { "# # #",
                                  "# # #" };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.Both, map.Symmetry );

            data = new string[] { "# # #",
                                  "#   #",
                                  "# # #"};

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.Both, map.Symmetry );


            // None symmetry
            data = new string[] { "  # #",
                                  "# #  " };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.None, map.Symmetry );

            data = new string[] { "####",
                                  "#   ",
                                  "#   " };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.None, map.Symmetry );

            data = new string[] { "##",
                                  "# " };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.None, map.Symmetry );

            data = new string[] { "  #",
                                  "# #",
                                  "# #" };

            map.LoadMap( data );
            Assert.AreEqual( eSymmetry.None, map.Symmetry );
        }

        [TestMethod]
        public void Map_GetNewPos_BoundChecking_IsValid()
        {
            var map = new Map();
            var data = new string[] { "  ",
                                      "  " };
            map.LoadMap( data );
            Pos? pos;

            bool bValidPos = map.GetNewPos( new Pos { x = 0, y = 0 }, -1, 0, out pos );
            Assert.AreEqual( true, bValidPos );

            bValidPos = map.GetNewPos( new Pos { x = 1, y = 0 }, 1, 0, out pos );
            Assert.AreEqual( true, bValidPos );

            bValidPos = map.GetNewPos( new Pos { x = 0, y = 0 }, 0, -1, out pos );
            Assert.AreEqual( true, bValidPos );

            bValidPos = map.GetNewPos( new Pos { x = 0, y = 1 }, 0, 1, out pos );
            Assert.AreEqual( true, bValidPos );
        }

        [TestMethod]
        public void Map_GetNewPos_Empty_IsValid()
        {
            var map = new Map();
            var data = new string[] { "  ",
                                      "  " };
            map.LoadMap( data );
            Pos? pos;

            bool bValidPos = map.GetNewPos( new Pos { x = 0, y = 0 }, 0, 1, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 0, y = 1 }, pos );
        }

        [TestMethod]
        public void Map_GetNewPos_Block_IsValid()
        {
            var map = new Map();
            var data = new string[] { "  ",
                                      " #" };
            map.LoadMap( data );
            Pos? pos;

            var origPos = new Pos { x = 0, y = 1 };
            bool bValidPos = map.GetNewPos( origPos, 1, 0, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( origPos, pos );
        }

        [TestMethod]
        public void Map_GetNewPos_Exit_IsValid()
        {
            var map = new Map();
            var data = new string[] { " e",
                                      "  " };
            map.LoadMap( data );
            Pos? pos;

            var origPos = new Pos { x = 0, y = 0 };
            bool bValidPos = map.GetNewPos( origPos, 1, 0, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( null, pos );
        }

        [TestMethod]
        public void Map_GetNewPos_Trap_IsValid()
        {
            var map = new Map();
            var data = new string[] { "x ",
                                      "  " };
            map.LoadMap( data );
            Pos? pos;

            var origPos = new Pos { x = 0, y = 1 };
            bool bValidPos = map.GetNewPos( origPos, 0, -1, out pos );
            Assert.AreEqual( false, bValidPos );
        }

        [TestMethod]
        public void Map_GetNewPos_Pushers_IsValid()
        {
            var map = new Map();
            var data1 = new string[] { " > ^",
                                       "^ _<",
                                       " <  " };
            map.LoadMap( data1 );
            Pos? pos;

            var origPos = new Pos { x = 0, y = 0 };
            bool bValidPos = map.GetNewPos( origPos, 1, 0, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 2, y = 0 }, pos );

            origPos = new Pos { x = 2, y = 0 };
            bValidPos = map.GetNewPos( origPos, 0, 1, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 2, y = 2 }, pos );

            origPos = new Pos { x = 2, y = 2 };
            bValidPos = map.GetNewPos( origPos, -1, 0, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 0, y = 2 }, pos );

            origPos = new Pos { x = 0, y = 2 };
            bValidPos = map.GetNewPos( origPos, 0, -1, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 0, y = 0 }, pos );

            // Pusher looks into wall. Should stay at pusher position.
            origPos = new Pos { x = 2, y = 0 };
            bValidPos = map.GetNewPos( origPos, 1, 0, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 3, y = 0 }, pos );

            // Pusher looks into another pusher. Should stay at latter.
            origPos = new Pos { x = 3, y = 2 };
            bValidPos = map.GetNewPos( origPos, 0, -1, out pos );
            Assert.AreEqual( true, bValidPos );
            Assert.AreEqual( new Pos { x = 2, y = 1 }, pos );
        }
    }
}
