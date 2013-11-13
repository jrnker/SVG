using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml;

namespace Svg
{
    /// <summary>
    /// Svg helpers
    /// </summary>
    public static class SvgExtentions
    {
        public static void SetRectangle(this SvgRectangle r, RectangleF bounds)
        {
            r.X = bounds.X;
            r.Y = bounds.Y;
            r.Width = bounds.Width;
            r.Height = bounds.Height;
        }

        public static string GetXML(this SvgDocument doc)
        {
            var ret = "";

            using (var ms = new MemoryStream())
            {
                doc.Write(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                ret = sr.ReadToEnd();
                sr.Close();
            }

            return ret;
        }

        public static string GetXML(this SvgElement elem)
        {
            using (StringWriter str = new StringWriter())
            using (XmlTextWriter xml = new XmlTextWriter(str))
            {
                elem.WriteElement(xml);
                return str.ToString();

            }
        }
        
        public static bool HasNonEmptyCustomAttribute(this SvgElement element, string name)
        {
        	return element.CustomAttributes.ContainsKey(name) && !string.IsNullOrEmpty(element.CustomAttributes[name]);
        }
        
        public static void ApplyRecursive(this SvgElement elem, Action<SvgElement> action)
        {
        	action(elem);
        	
        	foreach (var element in elem.Children)
        	{
        		if(!(elem is SvgDocument))
        			element.ApplyRecursive(action);
        	}
        }
        public static float PathArea(PointF[] polygon)
        {
            int i, j;
            float area = 0;

            for (i = 0; i < polygon.Length; i++)
            {
                j = (i + 1) % polygon.Length;

                area += polygon[i].X * polygon[j].Y;
                area -= polygon[i].Y * polygon[j].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }
        public static float PathLength(PointF[] points)
        {
            float distance = _PathLength(points);
            distance += GetDistanceBetweenPoints(points[points.Length - 1], points[0]);
            return distance;
        }
        private static float _PathLength(PointF[] points)
        {
            float distance = 0;
            PointF point = points[0];
            for (int x = 1; x < points.Length; x++)
            {
                distance += GetDistanceBetweenPoints(point, points[x]);
                point = points[x];
            }
            return distance;
        }
        public static float GetDistanceBetweenPoints(PointF p, PointF q)
        {
            float a = p.X - q.X;
            float b = p.Y - q.Y;
            float distance = (float)Math.Sqrt(a * a + b * b);
            return distance;
        }
    }
}