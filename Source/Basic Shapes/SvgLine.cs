using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Svg
{
    /// <summary>
    /// Represents and SVG line element.
    /// </summary>
    [SvgElement("line")]
    public class SvgLine : SvgVisualElement
    {
        private SvgUnit _startX;
        private SvgUnit _startY;
        private SvgUnit _endX;
        private SvgUnit _endY;
        private GraphicsPath _path;

        [SvgAttribute("x1")]
        public SvgUnit StartX
        {
            get { return this._startX; }
            set 
            { 
            	if(_startX != value)
            	{
            		this._startX = value;
            		this.IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "x1", Value = value });
            	}
            }
        }

        [SvgAttribute("y1")]
        public SvgUnit StartY
        {
            get { return this._startY; }
            set 
            { 
            	if(_startY != value)
            	{
            		this._startY = value;
            		this.IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "y1", Value = value });
            	}
            }
        }

        [SvgAttribute("x2")]
        public SvgUnit EndX
        {
            get { return this._endX; }
            set 
            { 
            	if(_endX != value)
            	{
            		this._endX = value;
            		this.IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "x2", Value = value });
            	}
            }
        }

        [SvgAttribute("y2")]
        public SvgUnit EndY
        {
            get { return this._endY; }
            set 
            { 
            	if(_endY != value)
            	{
            		this._endY = value;
            		this.IsPathDirty = true;
            		OnAttributeChanged(new AttributeEventArgs{ Attribute = "y2", Value = value });
            	}
            }
        }

        public override SvgPaintServer Fill
        {
            get { return null; /* Line can't have a fill */ }
            set
            {
                // Do nothing
            }
        }

        public SvgLine()
        {
        }

        public override System.Drawing.Drawing2D.GraphicsPath Path
        {
            get
            {
                if (this._path == null || this.IsPathDirty)
                {
                    PointF start = new PointF(this.StartX.ToDeviceValue(this), this.StartY.ToDeviceValue(this, true));
                    PointF end = new PointF(this.EndX.ToDeviceValue(this), this.EndY.ToDeviceValue(this, true));

                    this._path = new GraphicsPath();
                    this._path.AddLine(start, end);
                    this.IsPathDirty = false;
                }
                return this._path;
            }
            protected set
            {
                _path = value;
            }
        }

        public override System.Drawing.RectangleF Bounds
        {
            get { return this.Path.GetBounds(); }
        }

        
        public override SvgUnit PathOuterArea
        {
            get { return new SvgUnit(Svg.SvgExtentions.PathArea(this.Path.PathPoints)); }
        }
        
        public override SvgUnit PathOuterLength
        {
            get { return new SvgUnit(Svg.SvgExtentions.PathLength(this.Path.PathPoints)); }
        }

		public override SvgElement DeepCopy()
		{
			return DeepCopy<SvgLine>();
		}

		public override SvgElement DeepCopy<T>()
		{
			var newObj = base.DeepCopy<T>() as SvgLine;
			newObj.StartX = this.StartX;
			newObj.EndX = this.EndX;
			newObj.StartY = this.StartY;
			newObj.EndY = this.EndY;
			if (this.Fill != null)
				newObj.Fill = this.Fill.DeepCopy() as SvgPaintServer;

			return newObj;
		}

    }
}
