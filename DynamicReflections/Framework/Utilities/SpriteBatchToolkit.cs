﻿using DynamicReflections.Framework.Patches.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xTile.Dimensions;

namespace DynamicReflections.Framework.Utilities
{
    public static class SpriteBatchToolkit
    {
        // General helpers
        private static bool _hasCache = false;
        private static SpriteSortMode _cachedSpriteSortMode;
        private static BlendState _cachedBlendState;
        private static SamplerState _cachedSamplerState;
        private static DepthStencilState _cachedDepthStencilState;
        private static RasterizerState _cachedRasterizerState;
        private static Effect _cachedSpriteEffect;
        private static Matrix? _cachedMatrix;

        public static void CacheSpriteBatchSettings(SpriteBatch spriteBatch, bool endSpriteBatch = false)
        {
            var reflection = DynamicReflections.modHelper.Reflection;

            _cachedSpriteSortMode = reflection.GetField<SpriteSortMode>(spriteBatch, "_sortMode").GetValue();
            _cachedBlendState = reflection.GetField<BlendState>(spriteBatch, "_blendState").GetValue();
            _cachedSamplerState = reflection.GetField<SamplerState>(spriteBatch, "_samplerState").GetValue();
            _cachedDepthStencilState = reflection.GetField<DepthStencilState>(spriteBatch, "_depthStencilState").GetValue();
            _cachedRasterizerState = reflection.GetField<RasterizerState>(spriteBatch, "_rasterizerState").GetValue();
            _cachedSpriteEffect = reflection.GetField<Effect>(spriteBatch, "_effect").GetValue();
            _cachedMatrix = reflection.GetField<SpriteEffect>(spriteBatch, "_spriteEffect").GetValue().TransformMatrix;

            _hasCache = true;
            if (endSpriteBatch is true)
            {
                spriteBatch.End();
            }
        }

        public static bool ResumeCachedSpriteBatch(SpriteBatch spriteBatch)
        {
            if (_hasCache is false)
            {
                return false;
            }
            _hasCache = false;

            spriteBatch.Begin(_cachedSpriteSortMode, _cachedBlendState, _cachedSamplerState, _cachedDepthStencilState, _cachedRasterizerState, _cachedSpriteEffect, _cachedMatrix);
            return true;
        }

        // LayerPatch helper methods
        // A note on the Render and Draw prefixed methods: These methods assume SpriteBatch has not been started via SpriteBatch.Begin
        internal static void DrawMirrorReflection()
        {
            DynamicReflections.mirrorReflectionEffect.Parameters["Mask"].SetValue(DynamicReflections.mirrorsRenderTarget);
            Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, effect: DynamicReflections.mirrorReflectionEffect);

            Game1.spriteBatch.Draw(DynamicReflections.playerMirrorReflectionRender, Vector2.Zero, Color.White);

            Game1.spriteBatch.End();
        }

        internal static void RenderMirrors()
        {
            // Set the render target
            Game1.graphics.GraphicsDevice.SetRenderTarget(DynamicReflections.mirrorsRenderTarget);

            // Draw the scene
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            if (Game1.currentLocation is not null && Game1.currentLocation.Map is not null)
            {
                if (Game1.currentLocation.Map.GetLayer("Mirrors") is var mirrorsLayer && mirrorsLayer is not null)
                {
                    Game1.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

                    // Draw the "Mirrors" layer
                    LayerPatch.DrawReversePatch(mirrorsLayer, Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                    Game1.spriteBatch.End();
                }
            }

            // Drop the render target
            Game1.graphics.GraphicsDevice.SetRenderTarget(null);

            Game1.graphics.GraphicsDevice.Clear(Game1.bgColor);
        }

        internal static void RenderMirrorReflectionPlayerSprite()
        {
            // Set the render target
            Game1.graphics.GraphicsDevice.SetRenderTarget(DynamicReflections.playerMirrorReflectionRender);

            // Draw the scene
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            var oldPosition = Game1.player.Position;
            var oldDirection = Game1.player.FacingDirection;
            var oldSprite = Game1.player.FarmerSprite;

            Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
            foreach (var mirrorPosition in DynamicReflections.activeMirrorPositions)
            {
                var mirror = DynamicReflections.mapMirrors[mirrorPosition];

                Game1.player.Position = mirror.PlayerReflectionPosition;
                Game1.player.FacingDirection = DynamicReflections.GetReflectedDirection(oldDirection, true);
                Game1.player.FarmerSprite = oldDirection == 0 ? DynamicReflections.mirrorReflectionSprite : oldSprite;

                Game1.player.draw(Game1.spriteBatch);
            }
            Game1.spriteBatch.End();

            Game1.player.Position = oldPosition;
            Game1.player.FacingDirection = oldDirection;
            Game1.player.FarmerSprite = oldSprite;

            // Drop the render target
            Game1.graphics.GraphicsDevice.SetRenderTarget(null);

            Game1.graphics.GraphicsDevice.Clear(Game1.bgColor);
        }

        internal static void RenderWaterReflectionPlayerSprite()
        {
            // Set the render target
            Game1.graphics.GraphicsDevice.SetRenderTarget(DynamicReflections.playerWaterReflectionRender);

            // Draw the scene
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            DrawReflectionViaMatrix();

            // Drop the render target
            Game1.graphics.GraphicsDevice.SetRenderTarget(null);

            Game1.graphics.GraphicsDevice.Clear(Game1.bgColor);
        }

        internal static void DrawReflectionViaMatrix()
        {
            var scale = Matrix.CreateScale(1, -1, 1);
            var position = Matrix.CreateTranslation(0, Game1.GlobalToLocal(Game1.viewport, DynamicReflections.waterReflectionPosition.Value).Y * 2, 0);
            //_monitor.LogOnce($"{Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight} | {Game1.GlobalToLocal(Game1.viewport, ModEntry.farmerReflection.Position).Y}", LogLevel.Debug);

            Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, rasterizerState: DynamicReflections.rasterizer, transformMatrix: scale * position);

            var oldPosition = Game1.player.Position;
            Game1.player.Position = DynamicReflections.waterReflectionPosition.Value;
            Game1.player.draw(Game1.spriteBatch);
            Game1.player.Position = oldPosition;

            Game1.spriteBatch.End();
        }

        internal static void DrawRenderedPlayer(bool isWavy = false)
        {
            Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, effect: isWavy ? DynamicReflections.waterReflectionEffect : null);
            //ModEntry.monitor.LogOnce($"[{ModEntry.renderTarget.Bounds}] {Game1.viewport.Width / 2} | {Game1.viewport.Height / 2}", LogLevel.Debug);
            Game1.spriteBatch.Draw(DynamicReflections.playerWaterReflectionRender, Vector2.Zero, Color.White);

            Game1.spriteBatch.End();
        }
    }
}