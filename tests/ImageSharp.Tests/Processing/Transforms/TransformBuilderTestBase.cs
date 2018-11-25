﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SixLabors.Primitives;

using Xunit;

namespace SixLabors.ImageSharp.Tests.Processing.Transforms
{
    public abstract class TransformBuilderTestBase<TBuilder>
    {
        private static readonly ApproximateFloatComparer Comparer = new ApproximateFloatComparer(1e-6f);

        public static readonly TheoryData<Vector2, Vector2, Vector2, Vector2> ScaleTranslate_Data =
            new TheoryData<Vector2, Vector2, Vector2, Vector2>
                {
                    // scale, translate, source, expectedDest

                    { Vector2.One, Vector2.Zero, Vector2.Zero, Vector2.Zero },
                    { Vector2.One, Vector2.Zero, new Vector2(10, 20), new Vector2(10, 20) },
                    { Vector2.One, new Vector2(3, 1), new Vector2(10, 20), new Vector2(13, 21) },
                    { new Vector2(2, 0.5f), new Vector2(3, 1), new Vector2(10, 20), new Vector2(23, 11) },
                };

        [Theory]
        [MemberData(nameof(ScaleTranslate_Data))]
        public void _1Scale_2Translate(Vector2 scale, Vector2 translate, Vector2 source, Vector2 expectedDest)
        {
            // These operations should be size-agnostic:
            var size = new Size(123, 321);
            TBuilder builder = this.CreateBuilder(size);

            this.AppendScale(builder, new SizeF(scale));
            this.AppendTranslation(builder, translate);

            Vector2 actualDest = this.Execute(builder, new Rectangle(Point.Empty, size), source);
            Assert.True(Comparer.Equals(expectedDest, actualDest));
        }

        public static readonly TheoryData<Vector2, Vector2, Vector2, Vector2> TranslateScale_Data =
            new TheoryData<Vector2, Vector2, Vector2, Vector2>
                {
                    // translate, scale, source, expectedDest

                    { Vector2.Zero, Vector2.One, Vector2.Zero, Vector2.Zero },
                    { Vector2.Zero, Vector2.One, new Vector2(10, 20), new Vector2(10, 20) },
                    { new Vector2(3, 1), new Vector2(2, 0.5f), new Vector2(10, 20), new Vector2(26, 10.5f) },
                };

        [Theory]
        [MemberData(nameof(TranslateScale_Data))]
        public void _1Translate_2Scale(Vector2 translate, Vector2 scale, Vector2 source, Vector2 expectedDest)
        {
            // Translate ans scale are size-agnostic:
            var size = new Size(456, 432);
            TBuilder builder = this.CreateBuilder(size);

            this.AppendTranslation(builder, translate);
            this.AppendScale(builder, new SizeF(scale));

            Vector2 actualDest = this.Execute(builder, new Rectangle(Point.Empty, size), source);
            Assert.Equal(expectedDest, actualDest, Comparer);
        }

        [Theory]
        [InlineData(10, 20)]
        [InlineData(-20, 10)]
        public void LocationOffsetIsPrepended(int locationX, int locationY)
        {
            var rectangle = new Rectangle(locationX, locationY, 10, 10);
            TBuilder builder = this.CreateBuilder(rectangle);

            this.AppendScale(builder, new SizeF(2, 2));

            Vector2 actual = this.Execute(builder, rectangle, Vector2.One);
            Vector2 expected = new Vector2(-locationX + 1, -locationY + 1) * 2;

            Assert.Equal(actual, expected, Comparer);
        }

        [Theory]
        [InlineData(200, 100, 10, 42, 84)]
        [InlineData(200, 100, 100, 42, 84)]
        [InlineData(100, 200, -10, 42, 84)]
        public void RotateDegrees_ShouldCreateCenteredMatrix(int width, int height, float deg, float x, float y)
        {
            var size = new Size(width, height);
            TBuilder builder = this.CreateBuilder(size);

            this.AppendRotationDegrees(builder, deg);

            // TODO: We should also test CreateRotationMatrixDegrees() (and all TransformUtils stuff!) for correctness
            Matrix3x2 matrix = TransformUtils.CreateRotationMatrixDegrees(deg, size);

            var position = new Vector2(x,  y);
            var expected = Vector2.Transform(position, matrix);
            Vector2 actual = this.Execute(builder, new Rectangle(Point.Empty, size), position);

            Assert.Equal(actual, expected, Comparer);
        }
        
        [Fact]
        public void AppendPrependOpposite()
        {
            var rectangle = new Rectangle(-1, -1, 3, 3);
            TBuilder b1 = this.CreateBuilder(rectangle);
            TBuilder b2 = this.CreateBuilder(rectangle);

            const float pi = (float)Math.PI;

            // Forwards
            this.AppendRotationRadians(b1, pi);
            this.AppendScale(b1, new SizeF(2, 0.5f));
            this.AppendTranslation(b1, new PointF(123, 321));

            // Backwards
            this.PrependTranslation(b2, new PointF(123, 321));
            this.PrependScale(b2, new SizeF(2, 0.5f));
            this.PrependRotationRadians(b2, pi);

            Vector2 p1 = this.Execute(b1, rectangle, new Vector2(32, 65));
            Vector2 p2 = this.Execute(b2, rectangle, new Vector2(32, 65));

            Assert.Equal(p1, p2, Comparer);
        }

        protected TBuilder CreateBuilder(Size size) => this.CreateBuilder(new Rectangle(Point.Empty, size));

        protected virtual TBuilder CreateBuilder(Rectangle rectangle) => (TBuilder)Activator.CreateInstance(typeof(TBuilder), rectangle);

        protected abstract void AppendTranslation(TBuilder builder, PointF translate);
        protected abstract void AppendScale(TBuilder builder, SizeF scale);
        protected abstract void AppendRotationRadians(TBuilder builder, float radians);

        protected abstract void PrependTranslation(TBuilder builder, PointF translate);
        protected abstract void PrependScale(TBuilder builder, SizeF scale);
        protected abstract void PrependRotationRadians(TBuilder builder, float radians);

        protected virtual void AppendRotationDegrees(TBuilder builder, float degrees) =>
            this.AppendRotationRadians(builder, ImageMaths.ToRadian(degrees));

        protected abstract Vector2 Execute(TBuilder builder, Rectangle rectangle, Vector2 sourcePoint);
        
        private static float Sqrt(float a) => (float)Math.Sqrt(a);
    }
}