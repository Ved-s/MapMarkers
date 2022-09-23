using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMarkers.Structures
{
    public struct Rect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public float Top => Y;
        public float Left => X;
        public float Right => X + Width;
        public float Bottom => Y + Height;

        public Vector2 Location 
        {
            get => new(X, Y);
            set { X = value.X; Y = value.Y; }
        }

        public Vector2 Size
        {
            get => new(Width, Height);
            set { Width = value.X; Height = value.Y; }
        }

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Vector2 location, Vector2 size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.X;
            Height = size.Y;
        }

        public bool Contains(Vector2 pos) => Left <= pos.X && Top <= pos.Y && Right > pos.X && Bottom > pos.Y;
        public bool Intersects(Rect rect) 
        {
            return rect.Left < Right && Left < rect.Right && rect.Top < Bottom && Top < rect.Bottom;
        }

        public Rect Floor() => new((float)Math.Floor(X), (float)Math.Floor(Y), (float)Math.Floor(Width), (float)Math.Floor(Height));

        public static implicit operator Rect(Rectangle rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
        public static explicit operator Rectangle(Rect rect) => new((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
    }
}
