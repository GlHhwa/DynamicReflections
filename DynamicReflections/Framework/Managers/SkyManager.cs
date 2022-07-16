﻿using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace DynamicReflections.Framework.Managers
{
    internal class SkyManager
    {
        internal const int SKY_TILES_X = 4;
        internal const int SKY_TILES_Y = 5;
        internal const int DEFAULT_SKY_INDEX = 0;

        internal List<TemporaryAnimatedSprite> skyEffectSprites = new List<TemporaryAnimatedSprite>();
        private Dictionary<GameLocation, bool[,]> _locationToSkyTiles;
        private Dictionary<GameLocation, List<Point>> _locationToSkyPoints;

        public void Reset()
        {
            _locationToSkyTiles = new Dictionary<GameLocation, bool[,]>();
            _locationToSkyPoints = new Dictionary<GameLocation, List<Point>>();
        }

        public void Generate(GameLocation location)
        {
            skyEffectSprites = new List<TemporaryAnimatedSprite>();
            if (location is null || location.Map is null)
            {
                return;
            }

            if (_locationToSkyTiles is null)
            {
                Reset();
            }
            else if (_locationToSkyTiles.ContainsKey(location) is true && _locationToSkyTiles[location] is not null)
            {
                //return;
            }

            GeneratePerTile(location);
        }

        private void GeneratePerTile(GameLocation location)
        {
            var random = new Random((int)((long)Game1.uniqueIDForThisGame + Game1.stats.DaysPlayed * 500 + Game1.ticks + DateTime.Now.Ticks));
            if (location.Map.GetLayer("Back") is var backLayer && backLayer is not null)
            {
                _locationToSkyTiles[location] = new bool[backLayer.LayerWidth, backLayer.LayerHeight];
                _locationToSkyPoints[location] = new List<Point>();

                for (int x = 0; x < backLayer.LayerWidth; x++)
                {
                    for (int y = 0; y < backLayer.LayerHeight; y++)
                    {
                        if (location.isWaterTile(x, y))
                        {
                            _locationToSkyTiles[location][x, y] = true;
                            _locationToSkyPoints[location].Add(new Point(x, y));
                            backLayer.Tiles[x, y].Properties["SkyIndex"] = random.NextDouble() < 0.55 ? DEFAULT_SKY_INDEX : random.Next(DEFAULT_SKY_INDEX, SKY_TILES_X * SKY_TILES_Y);
                            backLayer.Tiles[x, y].Properties["SkyEffect"] = random.Next(0, 4);
                        }
                    }
                }
            }
        }

        internal static Point GetTilePoint(int tileIndex)
        {
            return new Point(tileIndex % SKY_TILES_X, tileIndex / SKY_TILES_Y);
        }

        private static Point GetRandomTile(Random random, List<Point> tilePoints)
        {
            return tilePoints[random.Next(tilePoints.Count)];
        }

        internal void AttemptEffects(GameLocation location)
        {
            if (_locationToSkyPoints.ContainsKey(location) is false)
            {
                return;
            }

            if (location.Map.GetLayer("Back") is var backLayer && backLayer is not null)
            {
                var randomWaterTilePoint = GetRandomTile(Game1.random, _locationToSkyPoints[location]);

                var leftTile = backLayer.PickTile(new Location((randomWaterTilePoint.X - 1) * 64, randomWaterTilePoint.Y * 64), Game1.viewport.Size);
                if (leftTile is not null && location.isWaterTile(randomWaterTilePoint.X - 1, randomWaterTilePoint.Y) is false)
                {
                    // Trigger event with this tile as starting point
                    DynamicReflections.monitor.Log("HERE 1", StardewModdingAPI.LogLevel.Debug);
                    skyEffectSprites.Add(GenerateShootingStar(randomWaterTilePoint));
                    return;
                }

                var topTile = backLayer.PickTile(new Location(randomWaterTilePoint.X * 64, (randomWaterTilePoint.Y - 1) * 64), Game1.viewport.Size);
                if (topTile is not null && location.isWaterTile(randomWaterTilePoint.X, randomWaterTilePoint.Y - 1) is false)
                {
                    // Trigger event with this tile as starting point
                    DynamicReflections.monitor.Log("HERE 2", StardewModdingAPI.LogLevel.Debug);
                    skyEffectSprites.Add(GenerateShootingStar(randomWaterTilePoint));
                    return;
                }
            }
        }

        private TemporaryAnimatedSprite GenerateShootingStar(Point point)
        {
            var shootingStar = new TemporaryAnimatedSprite(DynamicReflections.assetManager.SkyEffectsTileSheetTexturePath, new Microsoft.Xna.Framework.Rectangle(0, 0, 32, 32), Game1.random.Next(150, 300), 3, 12, new Vector2(point.X - 1, point.Y) * 64f, flicker: false, flipped: false, 0f, 0f, Color.White, (float)(Game1.random.Next(1, 4) + Game1.random.NextDouble()), 0f, 0f, 0f);

            var speed = (float)(Game1.random.NextDouble() + 0.01f);
            shootingStar.acceleration = new Vector2(speed, speed);

            return shootingStar;
        }
    }
}
