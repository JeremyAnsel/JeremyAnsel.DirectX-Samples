using JeremyAnsel.DirectX.D2D1;
using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.DWrite;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Direct2DAndDWrite
{
    class MainGameComponent : IGameComponent
    {
        private DeviceResources deviceResources;

        private D2D1Bitmap bitmap;
        private D2D1BitmapBrush bitmapBrush;

        private D2D1Brush textBrush;
        private DWriteTextFormat textFormat;
        private DWriteTextLayout textLayout;

        public MainGameComponent()
        {
        }

        public D3D11FeatureLevel MinimalFeatureLevel
        {
            get
            {
                return D3D11FeatureLevel.FeatureLevel91;
            }
        }

        public void CreateDeviceDependentResources(DeviceResources resources)
        {
            this.deviceResources = resources;

            var dwriteFactory = this.deviceResources.DWriteFactory;

            this.textFormat = dwriteFactory.CreateTextFormat("Gabriola", null, DWriteFontWeight.Regular, DWriteFontStyle.Normal, DWriteFontStretch.Normal, 36.0f, "en-US");
            this.textFormat.TextAlignment = DWriteTextAlignment.Center;
            this.textFormat.ParagraphAlignment = DWriteParagraphAlignment.Near;
        }

        public void ReleaseDeviceDependentResources()
        {
            DWriteUtils.DisposeAndNull(ref this.textFormat);
        }

        public void CreateWindowSizeDependentResources()
        {
            var context = this.deviceResources.D2DRenderTarget;
            var dwriteFactory = this.deviceResources.DWriteFactory;
            string text = "Hello, World!";

            byte[] bitmapBytes = File.ReadAllBytes(@"texturedata.bin");
            this.bitmap = context.CreateBitmap(new D2D1SizeU(256, 256), bitmapBytes, 256 * 4, new D2D1BitmapProperties(new D2D1PixelFormat(DxgiFormat.R8G8B8A8UNorm, D2D1AlphaMode.Premultiplied), 96.0f, 96.0f));
            this.bitmapBrush = context.CreateBitmapBrush(this.bitmap, new D2D1BitmapBrushProperties(D2D1ExtendMode.Wrap, D2D1ExtendMode.Wrap, D2D1BitmapInterpolationMode.Linear));

            this.textBrush = context.CreateSolidColorBrush(new D2D1ColorF(D2D1KnownColor.Red));

            this.textLayout = dwriteFactory.CreateTextLayout(
                text,
                this.textFormat,
                this.deviceResources.ConvertPixelsToDipsX(this.deviceResources.BackBufferWidth),
                this.deviceResources.ConvertPixelsToDipsY(this.deviceResources.BackBufferHeight));

            this.textLayout.SetFontSize(140.0f, new DWriteTextRange(0, (uint)text.Length));

            using (var typography = dwriteFactory.CreateTypography())
            {
                typography.AddFontFeature(new DWriteFontFeature(DWriteFontFeatureTag.StylisticSet7, 1));
                this.textLayout.SetTypography(typography, new DWriteTextRange(0, 13));
            }
        }

        public void ReleaseWindowSizeDependentResources()
        {
            D2D1Utils.DisposeAndNull(ref this.bitmap);
            D2D1Utils.DisposeAndNull(ref this.bitmapBrush);

            D2D1Utils.DisposeAndNull(ref this.textBrush);
            DWriteUtils.DisposeAndNull(ref this.textLayout);
        }

        public void Update(ITimer timer)
        {
        }

        public void Render()
        {
            var context = this.deviceResources.D2DRenderTarget;

            context.BeginDraw();
            context.Clear(new D2D1ColorF(D2D1KnownColor.CornflowerBlue));

            context.FillEllipse(new D2D1Ellipse(
                new D2D1Point2F(this.deviceResources.ConvertPixelsToDipsX(this.deviceResources.BackBufferWidth / 2),
                this.deviceResources.ConvertPixelsToDipsY(this.deviceResources.BackBufferHeight * 2 / 3)),
                this.deviceResources.ConvertPixelsToDipsX(150),
                this.deviceResources.ConvertPixelsToDipsY(150)), this.bitmapBrush);

            context.DrawTextLayout(new D2D1Point2F(), this.textLayout, this.textBrush);

            context.EndDrawIgnoringRecreateTargetError();
        }
    }
}
