using System;
using System.Collections.Generic;
using System.Diagnostics;
using PolyMaths.Algorithms;
using PolyMaths.Utils;

namespace PolyMaths.Tests
{
    public class PolygonTestSuite
    {
        private int testsRun = 0;
        private int testsPassed = 0;
        private int testsFailed = 0;
        private List<string> failedTests = new List<string>();
        private Stopwatch totalTimer = new Stopwatch();

        public void RunAllTests()
        {
            totalTimer.Start();

            Logger.Header("POLYGON SYSTEM - COMPREHENSIVE TEST SUITE");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(string.Format("  Started at: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            TestBasicPointOperations();
            TestPolygonGeometry();
            TestPolygonQueries();
            TestLCAFillAlgorithm();
            TestSutherlandHodgmanClipping();
            TestMatrixTransformations();
            TestComplexScenarios();

            totalTimer.Stop();
            PrintSummary();
        }

        private void TestBasicPointOperations()
        {
            Logger.Section("TEST 1: Basic Point Operations");

            var p1 = new Point2D(3, 4);
            var p2 = new Point2D(1, 2);

            Logger.Data("Point 1", p1);
            Logger.Data("Point 2", p2);
            Logger.Separator();

            Logger.SubSection("Arithmetic");
            var sum = p1 + p2;
            Logger.Data("p1 + p2", sum);
            Assert("Point addition", sum.Equals(new Point2D(4, 6)));

            var diff = p1 - p2;
            Logger.Data("p1 - p2", diff);
            Assert("Point subtraction", diff.Equals(new Point2D(2, 2)));

            var scaled = p1 * 2;
            Logger.Data("p1 * 2", scaled);
            Assert("Scalar multiplication", scaled.Equals(new Point2D(6, 8)));

            Logger.SubSection("Products & Magnitudes");
            float dot = p1.Dot(p2);
            Logger.Data("p1 . p2 (dot)", dot);
            Assert("Dot product", Math.Abs(dot - 11) < 1e-6f);

            float cross = p1.Cross(p2);
            Logger.Data("p1 x p2 (cross)", cross);
            Assert("Cross product", Math.Abs(cross - 2) < 1e-6f);

            Logger.Data("|p1|", string.Format("{0:F4}", p1.Magnitude));
            Assert("Magnitude", Math.Abs(p1.Magnitude - 5) < 1e-6f);

            float dist = p1.DistanceTo(p2);
            Logger.Data("dist(p1, p2)", string.Format("{0:F6}", dist));
            Assert("Distance", Math.Abs(dist - 2.828427f) < 0.001f);

            Logger.SectionEnd();
        }

        private void TestPolygonGeometry()
        {
            Logger.Section("TEST 2: Polygon Geometry");

            Logger.SubSection("Square (10x10)");
            var square = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(10, 0),
                new Point2D(10, 10), new Point2D(0, 10)
            }, "Square");

            Logger.Vertices("Vertices", square.Vertices);
            Logger.Data("Closed", square.IsClosed);
            Logger.Data("Convex", square.IsConvex());
            Logger.Data("Area", string.Format("{0:F2}", square.GetArea()));
            Logger.Data("Perimeter", string.Format("{0:F2}", square.GetPerimeter()));
            Logger.Data("Center", square.GetCenter());
            Logger.Data("BBox", string.Format("{0} -> {1}", square.GetBoundingBox().Item1, square.GetBoundingBox().Item2));

            Assert("Square is closed", square.IsClosed);
            Assert("Square is convex", square.IsConvex());
            Assert("Square area = 100", Math.Abs(square.GetArea() - 100) < 1e-6f);
            Assert("Square perimeter = 40", Math.Abs(square.GetPerimeter() - 40) < 1e-6f);

            var center = square.GetCenter();
            Assert("Square center = (5, 5)",
                Math.Abs(center.x - 5) < 1e-6f && Math.Abs(center.y - 5) < 1e-6f);

            Logger.SubSection("Triangle");
            var triangle = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(10, 0), new Point2D(5, 10)
            }, "Triangle");

            Logger.Vertices("Vertices", triangle.Vertices);
            Logger.Data("Area", string.Format("{0:F2}", triangle.GetArea()));
            Logger.Data("Convex", triangle.IsConvex());

            Assert("Triangle is convex", triangle.IsConvex());
            Assert("Triangle area = 50", Math.Abs(triangle.GetArea() - 50) < 1e-6f);

            Logger.SubSection("L-Shape (concave)");
            var lShape = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 5),
                new Point2D(5, 5), new Point2D(5, 10), new Point2D(0, 10)
            }, "L-Shape");

            Logger.Vertices("Vertices", lShape.Vertices);
            Logger.Data("Convex", lShape.IsConvex());
            Logger.Data("Area", string.Format("{0:F2}", lShape.GetArea()));

            Assert("L-shape is concave", !lShape.IsConvex());

            Logger.SectionEnd();
        }

        private void TestPolygonQueries()
        {
            Logger.Section("TEST 3: Polygon Point Queries");

            var triangle = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(10, 0), new Point2D(5, 10)
            }, "Triangle");

            Logger.Vertices("Triangle", triangle.Vertices);

            var inside = new Point2D(5, 5);
            var outside = new Point2D(15, 5);
            var onEdge = new Point2D(5, 0);

            Logger.SubSection("Point containment");
            Logger.Data("(5, 5) inside?", triangle.ContainsPoint(inside));
            Assert("Point inside triangle", triangle.ContainsPoint(inside));

            Logger.Data("(15, 5) inside?", triangle.ContainsPoint(outside));
            Assert("Point outside triangle", !triangle.ContainsPoint(outside));

            Logger.Data("(5, 0) on edge?", triangle.ContainsPoint(onEdge));

            Logger.SubSection("Vertex proximity search");
            var nearVertex = new Point2D(0.5f, 0.5f);
            var vertexIdx = triangle.FindVertexAt(nearVertex, 1.0f);
            Logger.Data("Query point", nearVertex);
            Logger.Data("Threshold", "1.0");
            Logger.Data("Found vertex",
                vertexIdx.HasValue
                    ? string.Format("index {0} at {1} (dist={2:F3})",
                        vertexIdx.Value,
                        triangle.Vertices[vertexIdx.Value],
                        nearVertex.DistanceTo(triangle.Vertices[vertexIdx.Value]))
                    : "None");
            Assert("Find vertex near (0, 0)", vertexIdx.HasValue && vertexIdx.Value == 0);

            Logger.SectionEnd();
        }

        private void TestLCAFillAlgorithm()
        {
            Logger.Section("TEST 4: LCA Fill Algorithm");

            Logger.SubSection("Rectangle 10x10 fill");
            var rect = new Polygon(new List<Point2D>
            {
                new Point2D(10, 10), new Point2D(20, 10),
                new Point2D(20, 20), new Point2D(10, 20)
            }, "Rectangle");

            Logger.Vertices("Vertices", rect.Vertices);

            var filler = new LCAFill();
            var sw = Stopwatch.StartNew();
            var segments = filler.FillPolygon(rect);
            sw.Stop();

            Logger.Data("Y range", string.Format("{0} -> {1}", filler.GetYRange().Item1, filler.GetYRange().Item2));
            Logger.Data("Fill segments", segments.Count);
            Logger.Data("Time", string.Format("{0} ms ({1} ticks)", sw.ElapsedMilliseconds, sw.ElapsedTicks));

            Assert("Rectangle fill produces segments", segments.Count > 0);
            Assert("Rectangle fill has correct scanlines", segments.Count == 10);

            Logger.Info("    Sample fill segments (first 5):");
            for (int i = 0; i < Math.Min(5, segments.Count); i++)
            {
                var seg = segments[i];
                Logger.Detail(string.Format("y={0,3}: x=[{1:F0} .. {2:F0}]  width={3:F0}px",
                    (int)seg.Item1.y, seg.Item1.x, seg.Item2.x, seg.Item2.x - seg.Item1.x + 1));
            }

            Logger.SubSection("Triangle fill");
            var triangle = new Polygon(new List<Point2D>
            {
                new Point2D(50, 50), new Point2D(100, 50), new Point2D(75, 100)
            }, "Triangle");

            sw.Restart();
            segments = filler.FillPolygon(triangle);
            sw.Stop();

            Logger.Data("Fill segments", segments.Count);
            Logger.Data("Time", string.Format("{0} ms", sw.ElapsedMilliseconds));
            Assert("Triangle fill produces segments", segments.Count > 0);

            Logger.SubSection("Concave L-Shape fill");
            var concave = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(40, 0), new Point2D(40, 20),
                new Point2D(20, 20), new Point2D(20, 40), new Point2D(0, 40)
            }, "Concave L-Shape");

            sw.Restart();
            segments = filler.FillPolygon(concave);
            sw.Stop();

            Logger.Data("Fill segments", segments.Count);
            Logger.Data("Time", string.Format("{0} ms", sw.ElapsedMilliseconds));
            Assert("Concave polygon fill produces segments", segments.Count > 0);

            Logger.SectionEnd();
        }

        private void TestSutherlandHodgmanClipping()
        {
            Logger.Section("TEST 5: Sutherland-Hodgman Clipping");

            var window = new Polygon(new List<Point2D>
            {
                new Point2D(10, 10), new Point2D(50, 10),
                new Point2D(50, 50), new Point2D(10, 50)
            }, "Clipping Window");

            Logger.Vertices("Clip window", window.Vertices);

            var clipper = new SutherlandHodgman();

            Logger.SubSection("Case A: Subject entirely inside window");
            var inside = new Polygon(new List<Point2D>
            {
                new Point2D(20, 20), new Point2D(40, 20),
                new Point2D(40, 40), new Point2D(20, 40)
            }, "Subject (Inside)");

            Logger.Vertices("Subject", inside.Vertices);
            var sw = Stopwatch.StartNew();
            var result = clipper.ClipPolygon(inside, window);
            sw.Stop();

            Logger.Data("Result vertices", result.Vertices.Count);
            Logger.Data("Original area", string.Format("{0:F2}", inside.GetArea()));
            Logger.Data("Clipped area", string.Format("{0:F2}", result.GetArea()));
            Logger.Data("Area preserved", string.Format("{0:F4}%", (result.GetArea() / inside.GetArea()) * 100));
            Logger.Data("Time", string.Format("{0} ms", sw.ElapsedMilliseconds));

            Assert("Entirely inside - 4 vertices", result.Vertices.Count == 4);
            Assert("Entirely inside - area preserved",
                Math.Abs(result.GetArea() - inside.GetArea()) < 1e-3f);

            Logger.SubSection("Case B: Partially overlapping");
            var overlapping = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(30, 0),
                new Point2D(30, 30), new Point2D(0, 30)
            }, "Subject (Overlapping)");

            Logger.Vertices("Subject", overlapping.Vertices);
            result = clipper.ClipPolygon(overlapping, window);

            Logger.Data("Result vertices", result.Vertices.Count);
            Logger.Data("Original area", string.Format("{0:F2}", overlapping.GetArea()));
            Logger.Data("Clipped area", string.Format("{0:F2}", result.GetArea()));
            Logger.DataHighlight("Area ratio", string.Format("{0:F1}%", (result.GetArea() / overlapping.GetArea()) * 100));
            Logger.Vertices("Clipped result", result.Vertices);

            Assert("Overlapping produces valid polygon", result.Vertices.Count >= 3);

            Logger.SubSection("Case C: Subject entirely outside window");
            var outside = new Polygon(new List<Point2D>
            {
                new Point2D(60, 60), new Point2D(80, 60),
                new Point2D(80, 80), new Point2D(60, 80)
            }, "Subject (Outside)");

            Logger.Vertices("Subject", outside.Vertices);
            result = clipper.ClipPolygon(outside, window);

            Logger.Data("Result vertices", result.Vertices.Count);
            Logger.DataHighlight("Completely clipped", result.Vertices.Count == 0 ? "Yes" : "No");

            Assert("Entirely outside - empty result", result.Vertices.Count == 0);

            Logger.SectionEnd();
        }

        private void TestMatrixTransformations()
        {
            Logger.Section("TEST 6: Matrix Transformations");

            var point = new Point2D(10, 0);
            Logger.Data("Test point", point);

            Logger.SubSection("Translation (+5, +5)");
            var trans = Matrix3x3.Translation(5, 5);
            Logger.Matrix("Matrix", trans);
            var result = trans.TransformPoint(point);
            Logger.Data("Result", result);
            Assert("Translation correct",
                Math.Abs(result.x - 15) < 1e-6f && Math.Abs(result.y - 5) < 1e-6f);

            Logger.SubSection("Rotation 90 degrees");
            var rot90 = Matrix3x3.Rotation((float)(Math.PI / 2));
            Logger.Matrix("Matrix", rot90);
            result = rot90.TransformPoint(point);
            Logger.Data("Result", result);
            Logger.Detail(string.Format("Expected: (0.00, 10.00), Got: ({0:F2}, {1:F2})", result.x, result.y));
            Assert("90 degree rotation correct",
                Math.Abs(result.x - 0) < 1e-3f && Math.Abs(result.y - 10) < 1e-3f);

            Logger.SubSection("Scaling (2x, 3y)");
            var scale = Matrix3x3.Scaling(2, 3);
            Logger.Matrix("Matrix", scale);
            result = scale.TransformPoint(point);
            Logger.Data("Result", result);
            Assert("Scaling correct",
                Math.Abs(result.x - 20) < 1e-6f && Math.Abs(result.y - 0) < 1e-6f);

            Logger.SubSection("Shearing (shx=0.5, shy=0)");
            var shear = Matrix3x3.Shearing(0.5f, 0f);
            result = shear.TransformPoint(new Point2D(0, 10));
            Logger.Data("Shear (0,10)", result);
            Assert("Shearing X correct", Math.Abs(result.x - 5f) < 1e-4f && Math.Abs(result.y - 10f) < 1e-4f);

            Logger.SubSection("Combined: Scale 2x -> Rotate 45deg -> Translate (100, 100)");
            var combined = Matrix3x3.Translation(100, 100) *
                          Matrix3x3.Rotation((float)(Math.PI / 4)) *
                          Matrix3x3.Scaling(2, 2);

            Logger.Matrix("Combined matrix", combined);

            var square = new Polygon(new List<Point2D>
            {
                new Point2D(0, 0), new Point2D(10, 0),
                new Point2D(10, 10), new Point2D(0, 10)
            });

            Logger.Vertices("Original square", square.Vertices);
            var transformed = combined.TransformPoints(square.Vertices);

            Logger.Data("Original square area", string.Format("{0:F2}", square.GetArea()));
            Logger.Vertices("Transformed vertices", transformed);

            var transformedPoly = new Polygon(transformed, "Transformed Square");
            Logger.DataHighlight("Transformed area", string.Format("{0:F2} (expected: {1:F2})",
                transformedPoly.GetArea(), square.GetArea() * 4));

            Assert("Combined transformation produces 4 vertices", transformed.Count == 4);
            Logger.Info("    Pipeline: Translate(100,100) * Rotate(45deg) * Scale(2,2)");

            Logger.SectionEnd();
        }

        private void TestComplexScenarios()
        {
            Logger.Section("TEST 7: Complex Real-World Scenarios");

            Logger.SubSection("Scenario A: Create polygon -> Fill -> Clip -> Fill again");

            var original = new Polygon(new List<Point2D>
            {
                new Point2D(5, 5), new Point2D(45, 5),
                new Point2D(45, 45), new Point2D(5, 45)
            }, "Original");

            var window = new Polygon(new List<Point2D>
            {
                new Point2D(20, 20), new Point2D(60, 20),
                new Point2D(60, 60), new Point2D(20, 60)
            }, "Window");

            Logger.Vertices("Original polygon", original.Vertices);
            Logger.Vertices("Clip window", window.Vertices);

            var filler = new LCAFill();
            var sw = Stopwatch.StartNew();
            var fillSegments1 = filler.FillPolygon(original);
            sw.Stop();
            Logger.Data("Original fill time", string.Format("{0} ms", sw.ElapsedMilliseconds));

            var clipper = new SutherlandHodgman();
            sw.Restart();
            var clipped = clipper.ClipPolygon(original, window);
            sw.Stop();
            Logger.Data("Clip time", string.Format("{0} ms", sw.ElapsedMilliseconds));

            sw.Restart();
            var fillSegments2 = filler.FillPolygon(clipped);
            sw.Stop();
            Logger.Data("Clipped fill time", string.Format("{0} ms", sw.ElapsedMilliseconds));

            Logger.Separator();
            Logger.Data("Original area", string.Format("{0:F2}", original.GetArea()));
            Logger.Data("Original fill segments", fillSegments1.Count);
            Logger.Data("Clipped area", string.Format("{0:F2}", clipped.GetArea()));
            Logger.Data("Clipped fill segments", fillSegments2.Count);
            Logger.DataHighlight("Area reduction", string.Format("{0:F1}%",
                (1 - clipped.GetArea() / original.GetArea()) * 100));
            Logger.Vertices("Clipped vertices", clipped.Vertices);

            Assert("Clipped area < original area", clipped.GetArea() < original.GetArea());
            Assert("Clipped has fewer fill segments", fillSegments2.Count <= fillSegments1.Count);

            Logger.SubSection("Scenario B: Transform -> Clip -> Fill");

            var tri = new Polygon(new List<Point2D>
            {
                new Point2D(25, 25), new Point2D(45, 25), new Point2D(35, 45)
            }, "Source Triangle");

            Logger.Vertices("Original triangle", tri.Vertices);
            Logger.Data("Triangle area", string.Format("{0:F2}", tri.GetArea()));

            var windowCenter = new Point2D(35, 35);
            Logger.Data("Rotation center", windowCenter);
            Logger.Data("Rotation angle", "30 degrees");

            var rotMatrix = Matrix3x3.Translation(windowCenter.x, windowCenter.y) *
                           Matrix3x3.Rotation((float)(Math.PI / 6)) *
                           Matrix3x3.Translation(-windowCenter.x, -windowCenter.y);

            Logger.Matrix("Rotation matrix (translate-back * rotate * translate-to-origin)", rotMatrix);

            var rotatedVerts = rotMatrix.TransformPoints(tri.Vertices);
            var rotatedPoly = new Polygon(rotatedVerts, "Rotated Triangle");

            Logger.Vertices("Rotated vertices", rotatedVerts);
            Logger.Data("Rotated area", string.Format("{0:F2}", rotatedPoly.GetArea()));
            Logger.DataHighlight("Area preserved after rotation",
                Math.Abs(rotatedPoly.GetArea() - tri.GetArea()) < 0.1f ? "Yes" : "No");

            sw.Restart();
            var clippedRotated = clipper.ClipPolygon(rotatedPoly, window);
            sw.Stop();
            Logger.Data("Clip time", string.Format("{0} ms", sw.ElapsedMilliseconds));

            sw.Restart();
            var fillRotated = filler.FillPolygon(clippedRotated);
            sw.Stop();

            Logger.Separator();
            Logger.Data("After clipping: vertices", clippedRotated.Vertices.Count);
            Logger.Data("After clipping: area", string.Format("{0:F2}", clippedRotated.GetArea()));
            Logger.Data("Fill segments", fillRotated.Count);
            Logger.Data("Fill time", string.Format("{0} ms", sw.ElapsedMilliseconds));

            if (clippedRotated.Vertices.Count > 0)
            {
                Logger.Vertices("Clipped rotated vertices", clippedRotated.Vertices);
            }

            Assert("Complex pipeline produces valid result",
                clippedRotated.Vertices.Count >= 3 && fillRotated.Count > 0);

            Logger.SectionEnd();
        }

        private void Assert(string testName, bool condition)
        {
            testsRun++;
            if (condition)
            {
                Logger.Success(testName);
                testsPassed++;
            }
            else
            {
                Logger.Error(testName);
                testsFailed++;
                failedTests.Add(testName);
            }
        }

        private void PrintSummary()
        {
            Logger.Header("TEST SUMMARY");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("  Total Tests:    {0}", testsRun));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Format("  Passed:         {0} ({1:F1}%)",
                testsPassed, (testsPassed * 100.0 / testsRun)));

            if (testsFailed > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("  Failed:         {0} ({1:F1}%)",
                    testsFailed, (testsFailed * 100.0 / testsRun)));
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(string.Format("  Total time:     {0} ms", totalTimer.ElapsedMilliseconds));

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            if (testsFailed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("                                        ");
                Console.WriteLine("   ALL TESTS PASSED SUCCESSFULLY!       ");
                Console.WriteLine("                                        ");
                Console.BackgroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("                                        ");
                Console.WriteLine("   SOME TESTS FAILED                    ");
                Console.WriteLine("                                        ");
                Console.BackgroundColor = ConsoleColor.Black;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n  Failed tests:");
                foreach (var name in failedTests)
                {
                    Console.WriteLine("    - " + name);
                }
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }
    }
}
