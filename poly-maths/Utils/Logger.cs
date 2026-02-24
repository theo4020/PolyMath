using System;
using System.Collections.Generic;
using System.Diagnostics;
using PolyMaths.Algorithms;

namespace PolyMaths.Utils
{
    public static class Logger
    {
        private static readonly ConsoleColor DefaultColor = ConsoleColor.Gray;
        private static Stopwatch _sectionTimer = new Stopwatch();

        public static void Header(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine(new string('=', 40));
            Console.WriteLine("  " + text);
            Console.WriteLine(new string('=', 80));
            Console.WriteLine(new string('=', 40));
            Console.ForegroundColor = DefaultColor;
        }

        public static void Section(string text)
        {
            _sectionTimer.Restart();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n> " + text);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', 80));
            Console.WriteLine(new string('-', 40));
            Console.ForegroundColor = DefaultColor;
        }

        public static void SectionEnd()
        {
            _sectionTimer.Stop();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(string.Format("  [{0} ms elapsed]", _sectionTimer.ElapsedMilliseconds));
            Console.ForegroundColor = DefaultColor;
        }

        public static void SubSection(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\n  >> " + text);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  " + new string('.', 50));
            Console.ForegroundColor = DefaultColor;
        }

        public static void Success(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  [PASS] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(text);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Info(string text)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  " + text);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Detail(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("       " + text);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Data(string label, object value)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("    * " + label + ": ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(value);
            Console.ForegroundColor = DefaultColor;
        }

        public static void DataHighlight(string label, object value)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("    * " + label + ": ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(value);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Warning(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("  [WARN] " + text);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Error(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("  [FAIL] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(text);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Result(string text)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("  -> " + text);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Vertices(string label, List<Point2D> verts)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("    * " + label + ":");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            for (int i = 0; i < verts.Count; i++)
            {
                Console.WriteLine(string.Format("        [{0}] {1}", i, verts[i]));
            }
            Console.ForegroundColor = DefaultColor;
        }

        public static void Matrix(string label, Matrix3x3 mat)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("    * " + label + ":");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            var lines = mat.ToString().Split('\n');
            foreach (var line in lines)
                Console.WriteLine("        " + line);
            Console.ForegroundColor = DefaultColor;
        }

        public static void Separator()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("    " + new string('-', 40));
            Console.ForegroundColor = DefaultColor;
        }

        public static void Blank()
        {
            Console.WriteLine();
        }
    }
}
