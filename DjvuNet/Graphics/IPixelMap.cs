﻿namespace DjvuNet.Graphics
{
    public interface IPixelMap : IMap2
    {
        Rectangle BoundingRectangle { get; }

        void ApplyGammaCorrection(double gamma);

        void Attenuate(IBitmap bm, int xpos, int ypos);

        void Blit(IBitmap bm, int xpos, int ypos, IPixel color);

        void Downsample(IMap2 src, int subsample, Rectangle targetRect);

        void Downsample43(IMap2 src, Rectangle pdr);

        IPixel GetPixelAt(int row, int column);

        IPixelMap Init(int height, int width, IPixel color);

        IPixelMap Init(IMap2 source);

        IPixelMap Init(IMap2 source, Rectangle rect);

        IPixelMap Init(sbyte[] data, int arows, int acolumns);

        void Stencil(IBitmap mask, IPixelMap foregroundMap, int supersample, int subsample, Rectangle bounds, double gamma);
    }
}