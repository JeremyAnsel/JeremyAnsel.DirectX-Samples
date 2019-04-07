using BasicMaths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lesson5.Components
{
    class BasicCamera
    {
        private Float3 position;
        private Float3 direction;
        private Float4X4 view;
        private Float4X4 projection;

        public Float4X4 GetViewMatrix()
        {
            return this.view;
        }

        public Float4X4 GetProjectionMatrix()
        {
            return this.projection;
        }

        public void SetViewParameters(Float3 eyePosition, Float3 lookPosition, Float3 up)
        {
            this.position = eyePosition;
            this.direction = (lookPosition - eyePosition).Normalize();
            Float3 zAxis = -this.direction;
            Float3 xAxis = Float3.Cross(up, zAxis).Normalize();
            Float3 yAxis = Float3.Cross(zAxis, xAxis);
            float xOffset = -Float3.Dot(xAxis, this.position);
            float yOffset = -Float3.Dot(yAxis, this.position);
            float zOffset = -Float3.Dot(zAxis, this.position);
            this.view = new Float4X4(
                xAxis.X, xAxis.Y, xAxis.Z, xOffset,
                yAxis.X, yAxis.Y, yAxis.Z, yOffset,
                zAxis.X, zAxis.Y, zAxis.Z, zOffset,
                0.0f, 0.0f, 0.0f, 1.0f
                );
        }

        public void SetProjectionParameters(float minimumFieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            float minScale = 1.0f / (float)Math.Tan(minimumFieldOfView * BasicMath.PI / 360.0f);
            float xScale = 1.0f;
            float yScale = 1.0f;
            if (aspectRatio < 1.0f)
            {
                xScale = minScale;
                yScale = minScale * aspectRatio;
            }
            else
            {
                xScale = minScale / aspectRatio;
                yScale = minScale;
            }
            float zScale = farPlane / (farPlane - nearPlane);
            this.projection = new Float4X4(
                xScale, 0.0f, 0.0f, 0.0f,
                0.0f, yScale, 0.0f, 0.0f,
                0.0f, 0.0f, -zScale, -nearPlane * zScale,
                0.0f, 0.0f, -1.0f, 0.0f
                );
        }
    }
}
