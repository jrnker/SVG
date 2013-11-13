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

        static List<PointF> StringToPointsList(XmlAttribute points)
        {
            // List to hold all points in the polygon
            List<PointF> pointList = new List<PointF>();

            //  \r\n\t\tM557.792,527.031c0-2.349-1.904-4.252-4.252-4.252c-2.349,0-4.252,1.903-4.252,4.252c0,2.348,1.903,4.252,4.252,4.252\r\n\t\tC555.888,531.283,557.792,529.378,557.792,527.031L557.792,527.031z
            var pointsString = points.Value;
            //pointsString = pointsString.ToLower();
            //for (int i=97;i<122;i++)
            //{
            //    pointsString = pointsString.Replace(((char)i).ToString()," ");
            //}
            pointsString = pointsString.Trim();
            pointsString = pointsString.Replace(" ", ",");
            pointsString = pointsString.Replace("-", ",-");
            string[] splitter1 = { "," };
            string[] pointArray = pointsString.Split(splitter1, StringSplitOptions.RemoveEmptyEntries);
            float extracted, xPos = 0, yPos = 0;
            bool isX = true;
            bool upperCase = true;
            for (int i = 0; i < pointArray.Length; i++)
            {
            doAgain:
                pointArray[i] = pointArray[i].Trim();
            if (float.TryParse(pointArray[i], out extracted))
                {
                    // Get x and y
                    // xExtracted = Convert.ToDouble(pointArray[i].Trim());
                    // yExtracted = Convert.ToDouble(pointArray[i+1].Trim());

                    // Add the point to the pointlist
                    if (!upperCase)
                    {
                        if (isX)
                        {
                            extracted += xPos;
                        }
                        else
                        {
                            extracted += yPos;
                        }
                    }
                    if (isX)
                    {
                        xPos = extracted;
                    }
                    else
                    {
                        yPos = extracted;
                    }
                    if (!isX)
                    {
                        pointList.Add(new PointF()
                        {
                            X = xPos,
                            Y = yPos
                        });
                    }
                    isX = !isX;
                }
                else
                {
                    if (char.IsLetter(pointArray[i], 0))
                    {
                        upperCase = char.IsUpper(pointArray[i], 0);
                        pointArray[i] = pointArray[i].Substring(1);
                        if (float.TryParse(pointArray[i], out extracted))
                        {
                            goto doAgain;
                        }
                    }
                    for (int n = 0; n < pointArray[i].Length; n++)
                    {
                        if (char.IsLetter(pointArray[i], n))
                        {
                            upperCase = char.IsUpper(pointArray[i], n);
                            if (n == pointArray[i].Length - 1)
                            {
                                pointArray[i] = pointArray[i].Substring(0, n);
                            }
                            else
                            {
                                Array.Resize(ref pointArray, pointArray.Length + 1);
                                for (int m = pointArray.Length - 1; m > i; m--)
                                {
                                    pointArray[m] = pointArray[m - 1];
                                }
                                pointArray[i] = pointArray[i].Substring(0, n);
                                pointArray[i + 1] = pointArray[i + 1].Substring(n);
                            }
                            goto doAgain;
                        }
                    }

                }
            }
            return pointList;
        }
    }
}