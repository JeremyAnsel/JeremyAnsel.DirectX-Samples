using JeremyAnsel.DirectX.D2D1;
using JeremyAnsel.DirectX.DWrite;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.DirectX.GameWindow;
using JeremyAnsel.DirectX.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Direct2DAndDWrite
{
    class Game : GameWindowBase
    {
        private D2D1Bitmap bitmap;
        private D2D1BitmapBrush bitmapBrush;

        private D2D1Brush textBrush;
        private DWriteTextFormat textFormat;
        private DWriteTextLayout textLayout;

        public Game()
        {
        }

        protected override void Init()
        {
            base.Init();
        }

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();
        }

        protected override void ReleaseDeviceDependentResources()
        {
            base.ReleaseDeviceDependentResources();
        }

        protected override void CreateWindowSizeDependentResources()
        {
            base.CreateWindowSizeDependentResources();

            var context = this.DeviceResources.D2DRenderTarget;
            var dwriteFactory = this.DeviceResources.DWriteFactory;

            byte[] bitmapBytes = File.ReadAllBytes(@"..\..\texturedata.bin");
            this.bitmap = context.CreateBitmap(new D2D1SizeU(256, 256), bitmapBytes, 256 * 4, new D2D1BitmapProperties(new D2D1PixelFormat(DxgiFormat.R8G8B8A8UNorm, D2D1AlphaMode.Premultiplied), 96.0f, 96.0f));
            this.bitmapBrush = context.CreateBitmapBrush(this.bitmap, new D2D1BitmapBrushProperties(D2D1ExtendMode.Wrap, D2D1ExtendMode.Wrap, D2D1BitmapInterpolationMode.Linear));

            this.textBrush = context.CreateSolidColorBrush(new D2D1ColorF(D2D1KnownColor.Red));

            string text = "Hello, World!";

            this.textFormat = dwriteFactory.CreateTextFormat("Gabriola", null, DWriteFontWeight.Regular, DWriteFontStyle.Normal, DWriteFontStretch.Normal, 36.0f, "en-US");
            this.textFormat.TextAlignment = DWriteTextAlignment.Center;
            this.textFormat.ParagraphAlignment = DWriteParagraphAlignment.Near;

            this.textLayout = dwriteFactory.CreateTextLayout(
                text,
                this.textFormat,
                this.DeviceResources.ConvertPixelsToDipsX(this.Width),
                this.DeviceResources.ConvertPixelsToDipsY(this.Height));

            this.textLayout.SetFontSize(140.0f, new DWriteTextRange(0, (uint)text.Length));

            using (var typography = dwriteFactory.CreateTypography())
            {
                typography.AddFontFeature(new DWriteFontFeature(DWriteFontFeatureTag.StylisticSet7, 1));
                this.textLayout.SetTypography(typography, new DWriteTextRange(0, 13));
            }
        }

        protected override void ReleaseWindowSizeDependentResources()
        {
            base.ReleaseWindowSizeDependentResources();

            D2D1Utils.DisposeAndNull(ref this.bitmap);
            D2D1Utils.DisposeAndNull(ref this.bitmapBrush);

            D2D1Utils.DisposeAndNull(ref this.textBrush);
            DWriteUtils.DisposeAndNull(ref this.textFormat);
            DWriteUtils.DisposeAndNull(ref this.textLayout);
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void Render()
        {
            var context = this.DeviceResources.D2DRenderTarget;

            context.BeginDraw();
            context.Clear(new D2D1ColorF(D2D1KnownColor.CornflowerBlue));

            context.FillEllipse(new D2D1Ellipse(
                new D2D1Point2F(this.DeviceResources.ConvertPixelsToDipsX(this.Width / 2),
                this.DeviceResources.ConvertPixelsToDipsY(this.Height * 2 / 3)),
                this.DeviceResources.ConvertPixelsToDipsX(150),
                this.DeviceResources.ConvertPixelsToDipsY(150)), this.bitmapBrush);

            context.DrawTextLayout(new D2D1Point2F(), this.textLayout, this.textBrush);

            context.EndDrawIgnoringRecreateTargetError();
        }

        protected override void OnKeyboardEvent(VirtualKey key, int repeatCount, bool wasDown, bool isDown)
        {
            base.OnKeyboardEvent(key, repeatCount, wasDown, isDown);

            if (isDown && !wasDown && key == VirtualKey.F12)
            {
                this.FpsTextRenderer.IsEnabled = !this.FpsTextRenderer.IsEnabled;
            }
        }
    }
}
