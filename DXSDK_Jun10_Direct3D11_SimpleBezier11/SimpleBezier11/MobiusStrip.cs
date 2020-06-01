using JeremyAnsel.DirectX.DXMath;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleBezier11
{
    // Simple Bezier patch for a Mobius strip
    // 4 patches with 16 control points each
    static class MobiusStrip
    {
        public static readonly BezierControlPoint[] Points = new BezierControlPoint[64]
        {
            new BezierControlPoint(new XMFloat3(1.0f, -0.5f, 0.0f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.5f, 0.5f)),
            new BezierControlPoint(new XMFloat3(0.5f, -0.3536f, 1.354f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.3536f, 1.354f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.1667f, 0.0f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.1667f, 0.5f)),
            new BezierControlPoint(new XMFloat3(0.5f, -0.1179f, 1.118f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.1179f, 1.118f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.1667f, 0.0f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.1667f, 0.5f)),
            new BezierControlPoint(new XMFloat3(0.5f, 0.1179f, 0.8821f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.1179f, 0.8821f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.5f, 0.0f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.5f, 0.5f)),
            new BezierControlPoint(new XMFloat3(0.5f, 0.3536f, 0.6464f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.3536f, 0.6464f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.3536f, 1.354f)),
            new BezierControlPoint(new XMFloat3(-0.5f, -0.3536f, 1.354f)),
            new BezierControlPoint(new XMFloat3(-1.5f, 0.0f, 0.5f)),
            new BezierControlPoint(new XMFloat3(-1.5f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.1179f, 1.118f)),
            new BezierControlPoint(new XMFloat3(-0.5f, -0.1179f, 1.118f)),
            new BezierControlPoint(new XMFloat3(-1.167f, 0.0f, 0.5f)),
            new BezierControlPoint(new XMFloat3(-1.167f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.1179f, 0.8821f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.1179f, 0.8821f)),
            new BezierControlPoint(new XMFloat3(-0.8333f, 0.0f, 0.5f)),
            new BezierControlPoint(new XMFloat3(-0.8333f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.3536f, 0.6464f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.3536f, 0.6464f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.0f, 0.5f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(-1.5f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(-1.5f, 0.0f, -0.5f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.3536f, -1.354f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.3536f, -1.354f)),
            new BezierControlPoint(new XMFloat3(-1.167f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(-1.167f, 0.0f, -0.5f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.1179f, -1.118f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.1179f, -1.118f)),
            new BezierControlPoint(new XMFloat3(-0.8333f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(-0.8333f, 0.0f, -0.5f)),
            new BezierControlPoint(new XMFloat3(-0.5f, -0.1179f, -0.8821f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.1179f, -0.8821f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.0f, 0.0f)),
            new BezierControlPoint(new XMFloat3(-0.5f, 0.0f, -0.5f)),
            new BezierControlPoint(new XMFloat3(-0.5f, -0.3536f, -0.6464f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.3536f, -0.6464f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.3536f, -1.354f)),
            new BezierControlPoint(new XMFloat3(0.5f, 0.3536f, -1.354f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.5f, -0.5f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.5f, 0.0f)),
            new BezierControlPoint(new XMFloat3(0.0f, 0.1179f, -1.118f)),
            new BezierControlPoint(new XMFloat3(0.5f, 0.1179f, -1.118f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.1667f, -0.5f)),
            new BezierControlPoint(new XMFloat3(1.0f, 0.1667f, 0.0f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.1179f, -0.8821f)),
            new BezierControlPoint(new XMFloat3(0.5f, -0.1179f, -0.8821f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.1667f, -0.5f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.1667f, 0.0f)),
            new BezierControlPoint(new XMFloat3(0.0f, -0.3536f, -0.6464f)),
            new BezierControlPoint(new XMFloat3(0.5f, -0.3536f, -0.6464f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.5f, -0.5f)),
            new BezierControlPoint(new XMFloat3(1.0f, -0.5f, 0.0f)),
        };
    }
}
