using System;
using System.Collections.Generic;
using System.Linq;

namespace GVConstraints
{
    public class Connection
    {
        public byte Id { get; private set; }
        public List<Line> Lines { get; set; }

        public Connection(byte id)
        {
            Id = id;
            Lines = new List<Line>();
        }
    }

    public enum EndHeight { Top, Bottom };

    public class End
    {
        public EndHeight Height { get; private set; }
        public byte? Coordinate { get; set; }
        public bool IsBreakPoint { get; set; }

        public End(EndHeight height, byte coord)
        {
            Height = height;
            Coordinate = coord;
            IsBreakPoint = false;
        }

        public End(End end)
        {
            Height = end.Height;
            Coordinate = end.Coordinate;
            IsBreakPoint = end.IsBreakPoint;
        }
    }

    public class Line
    {
        public byte ConnectionId;
        public byte Id;
        public End LeftEnd;
        public End RightEnd;
        public byte? Route;

        public Line(byte cid, byte id, End left, End right)
        {
            ConnectionId = cid;
            Id = id;
            LeftEnd = left;
            RightEnd = right;
            Route = null;
        }

        public Line(byte cid, byte id)
        {
            ConnectionId = cid;
            Id = id;
            LeftEnd = null;
            RightEnd = null;
            Route = null;
        }
    }

    public class ChannelRouting
    {
        List<Connection> ChannelConnections { get; set; }
        List<End> FreeContacts { get; set; }

        //Отрезки всех соединений
        public List<Line> Lines
        {
            get
            {
                List<Line> lines = new List<Line>();
                foreach (Connection connection in ChannelConnections)
                {
                    lines.AddRange(connection.Lines);
                }
                return lines;
            }
        }
        //Число отрезков всех соединений
        public byte LinesQuantity
        {
            get
            {
                byte size = 0;
                foreach (Connection connection in ChannelConnections)
                {
                    size += (byte)connection.Lines.Count();
                }
                return size;
            }
        }

        public ChannelRouting(byte[,] contactPairs)
        {
            ChannelConnections = new List<Connection>();
            FreeContacts = new List<End>();
            byte lineId = 0;
            for (byte j = 0; j < contactPairs.GetLength(1); j++)
            {
                if (contactPairs[0, j] != 0)
                {
                    //Для верхнего контакта
                    Connection connection = ChannelConnections.Where(x => x.Id == contactPairs[0, j]).FirstOrDefault();
                    if (connection == null)
                    {
                        connection = new Connection(contactPairs[0, j]);
                        connection.Lines.Add(new Line(connection.Id, lineId++, new End(EndHeight.Top, j), null));
                        ChannelConnections.Add(connection);
                    }
                    else
                    {
                        if (connection.Lines.Last().RightEnd == null)
                        {
                            connection.Lines.Last().RightEnd = new End(EndHeight.Top, j);
                        }
                        else
                        {
                            Line line = new Line(connection.Id, lineId++, new End(connection.Lines.Last().RightEnd), new End(EndHeight.Top, j));
                            connection.Lines.Add(line);
                        }
                    }
                }
                else
                {
                    FreeContacts.Add(new End(EndHeight.Top, j));
                }

                if (contactPairs[1, j] != 0)
                { 
                    //Для нижнего контакта
                    Connection connection = ChannelConnections.Where(x => x.Id == contactPairs[1, j]).FirstOrDefault();
                    if (connection == null)
                    {
                        connection = new Connection(contactPairs[1, j]);
                        connection.Lines.Add(new Line(connection.Id, lineId++, new End(EndHeight.Bottom, j), null));
                        ChannelConnections.Add(connection);
                    }
                    else
                    {
                        if (connection.Lines.Last().RightEnd == null)
                        {
                            connection.Lines.Last().RightEnd = new End(EndHeight.Bottom, j);
                        }
                        else
                        {
                            Line line = new Line(connection.Id, lineId++, new End(connection.Lines.Last().RightEnd), new End(EndHeight.Bottom, j));
                            connection.Lines.Add(line);
                        }
                    }
                }
                else
                {
                    FreeContacts.Add(new End(EndHeight.Bottom, j));
                }
            }
        }

        //Получение матрицы графа вертикальных ограничений
        private byte[,] GetVerticalConstraintsMatrix()
        {
            byte[,] matrix = new byte[LinesQuantity, LinesQuantity];

            foreach (Line line in Lines)
            {
                End left = line.LeftEnd;
                if (left.Height == EndHeight.Top)
                {
                    List<Line> conflict = Lines.Where(x => (x.LeftEnd.Coordinate == left.Coordinate && x.LeftEnd.Height == EndHeight.Bottom) || (x.RightEnd.Coordinate == left.Coordinate && x.RightEnd.Height == EndHeight.Bottom)).ToList();
                    if (conflict != null)
                    {
                        for (int i = 0; i < conflict.Count; i++)
                        {
                            if (line.Id != conflict[i].Id)
                                matrix[line.Id, conflict[i].Id] = 1;
                        }
                    }
                }

                End right = line.RightEnd;
                if (right.Height == EndHeight.Top)
                {
                    List<Line> conflict = Lines.Where(x => (x.LeftEnd.Coordinate == right.Coordinate && x.LeftEnd.Height == EndHeight.Bottom) || (x.RightEnd.Coordinate == right.Coordinate && x.RightEnd.Height == EndHeight.Bottom)).ToList();
                    if (conflict != null)
                    {
                        for (int i = 0; i < conflict.Count; i++)
                        {
                            if (line.Id != conflict[i].Id)
                                matrix[line.Id, conflict[i].Id] = 1;
                        }
                    }
                }
            }

            return matrix;
        }

        //Получение матрицы графа горизонтальных ограничений
        private byte[,] GetHorizontalConstraintsMatrix()
        {
            byte[,] matrix = new byte[LinesQuantity, LinesQuantity];

            for (byte i = 0; i < Lines.Count(); i++)
            {
                Line interval1 = Lines[i];
                for (byte j = 0; j < Lines.Count(); j++)
                {
                    if (i != j)
                    {
                        Line interval2 = Lines[j];
                        if (interval1.ConnectionId != interval2.ConnectionId)
                        {
                            if ((interval1.LeftEnd.Coordinate >= interval2.LeftEnd.Coordinate && interval1.LeftEnd.Coordinate <= interval2.RightEnd.Coordinate) || (interval1.RightEnd.Coordinate >= interval2.LeftEnd.Coordinate && interval1.RightEnd.Coordinate <= interval2.LeftEnd.Coordinate))
                            {
                                matrix[interval1.Id, interval2.Id] = matrix[interval2.Id, interval1.Id] = 1;
                            }
                        }
                    }
                }
            }
            return matrix;
        }

        //Поиск свободного контакта для разрезания цикла
        public End GetFreeContact(List<byte> cycle, out byte lineToBreak)
        {
            List<Line> lines = new List<Line>();
            foreach (Connection connection in ChannelConnections)
            {
                lines.AddRange(connection.Lines);
            }

            for (byte i = 0; i < cycle.Count(); i++)
            {
                Line interval = lines.FirstOrDefault(x => x.Id == cycle[i]);

                byte leftEnd = (byte)(interval.LeftEnd.Coordinate);
                byte rightEnd = (byte)(interval.RightEnd.Coordinate);
                End freeContact = FreeContacts.Where(x => x.Coordinate > leftEnd && x.Coordinate < rightEnd).FirstOrDefault();
                if (freeContact != null)
                {
                    lineToBreak = cycle[i];
                    return freeContact;
                }
            }
            lineToBreak = 0;
            return null;
        }

        #region Поиск циклов
        private List<byte> CheckForCycles(byte[,] matrix)
        {
            byte size = (byte)matrix.GetLength(0);

            int[] cl = new int[size];
            int[] p = new int[size];

            for (int i = 0; i < size; i++)
            {
                p[i] = -1;
            }

            byte cycle_st = byte.MaxValue, cycle_end = byte.MaxValue;

            List<byte> vertices = new List<byte>();

            for (byte i = 0; i < size; i++)
            {
                if (dfs(i, matrix, ref cycle_st, ref cycle_end, ref cl, ref p))
                {
                    break;
                }
            }

            if (cycle_st == byte.MaxValue)
            {
                return null;
            }
            else
            {
                List<byte> cycle = new List<byte>();
                cycle.Add((byte)(cycle_st));
                for (byte v = cycle_end; v != cycle_st; v = (byte)p[v])
                    cycle.Add((byte)(v));
                return cycle;
            }

        }

        //Depth-first search - алгоритм поиска в глубину
        private bool dfs(byte v, byte[,] matrix, ref byte start, ref byte finish, ref int[] cl, ref int[] p)
        {
            byte size = (byte)matrix.GetLength(0);
            cl[v] = 1;
            for (byte i = 0; i < size; ++i)
            {
                if (matrix[v, i] == 1)
                {
                    byte to = i;
                    if (cl[to] == 0)
                    {
                        p[to] = v;
                        if (dfs(to, matrix, ref start, ref finish, ref cl, ref p)) return true;
                    }
                    else if (cl[to] == 1)
                    {
                        finish = v;
                        start = to;
                        return true;
                    }
                }
            }
            cl[v] = 2;
            return false;
        }
        #endregion

        public string[,] FindSolution(byte[,] contactPairs, out Dictionary<byte, List<Line>> outRoutes)
        {
            outRoutes = new Dictionary<byte, List<Line>>();
            List<byte> cycle = null;
            byte[,] vmatrix;
            byte[,] hmatrix;

            do
            {
                hmatrix = GetHorizontalConstraintsMatrix();
                vmatrix = GetVerticalConstraintsMatrix();
                cycle = CheckForCycles(vmatrix);
                if (cycle != null && cycle.Count != 1)
                {
                    byte lineToBreak = 0;
                    End freeContact = GetFreeContact(cycle, out lineToBreak);
                    if (freeContact == null)
                    {
                        outRoutes = null;
                        return null;
                    }
                    else
                    {
                        FreeContacts.Remove(freeContact);
                        freeContact.IsBreakPoint = true;
                        Connection connection = ChannelConnections.Where(x => x.Lines.Where(y => y.Id == lineToBreak).FirstOrDefault() != null).FirstOrDefault();
                        Line line = connection.Lines.Where(y => y.Id == lineToBreak).FirstOrDefault();
                        Line newLine = new Line(connection.Id, LinesQuantity, new End(freeContact), new End(line.RightEnd));
                        connection.Lines.Where(y => y.Id == lineToBreak).FirstOrDefault().RightEnd = new End(freeContact);
                        connection.Lines.Add(newLine);
                    }
                }
            }
            while (cycle != null && cycle.Count != 1);

            Dictionary<sbyte, List<byte>> routes = GetOrder(vmatrix, hmatrix);

            int routesQuantity = routes.Count();

            Dictionary<byte, List<byte>> newRoutes = new Dictionary<byte, List<byte>>();

            foreach (var item in routes)
            {
                if (item.Key >= 0)
                {
                    newRoutes.Add((byte)item.Key, item.Value);
                    outRoutes.Add((byte)item.Key, new List<Line>());
                }
                else
                {
                    newRoutes.Add((byte)(item.Key + routesQuantity), item.Value);
                    outRoutes.Add((byte)(item.Key + routesQuantity), new List<Line>());
                }
            }

            for (byte i = 0; i < ChannelConnections.Count; i++)
            {
                Connection connection = ChannelConnections[i];
                for (byte j = 0; j < connection.Lines.Count; j++)
                {
                    connection.Lines[j].Route = newRoutes.Where(x => x.Value.Contains(connection.Lines[j].Id)).FirstOrDefault().Key;
                    outRoutes[connection.Lines[j].Route.Value].Add(connection.Lines[j]);
                }
            }

            string[,] outputMatrix = PutIntoChannel(contactPairs, newRoutes);

            outputMatrix = DrawVerticalLines(outputMatrix, contactPairs, newRoutes);
            return outputMatrix;
        }

        //Определить порядок укладки соединений в канале
        private Dictionary<sbyte, List<byte>> GetOrder(byte[,] matrixVC, byte[,] matrixHC)
        {
            List<byte> A, B, C;
            Dictionary<sbyte, List<byte>> routes = new Dictionary<sbyte, List<byte>>();
            int counter = 0;
            do
            {
                GetABCGroups(matrixVC, out A, out B, out C);

                int notPlaced = A.Count() + B.Count() + C.Count();
                if (notPlaced == 0 && counter < LinesQuantity)
                {
                    for (byte i = 0; i < LinesQuantity; i++)
                    {
                        var contains = false;
                        foreach (var item in routes)
                        {
                            if (item.Value.Contains((byte)(i)))
                            {
                                contains = true;
                                break;
                            }
                        }
                        if (!contains)
                        {
                            B.Add((byte)(i));
                        }
                    }
                }

                byte[] GroupA = A.ToArray();

                byte routesQuantityTop = (byte)routes.Where(x => x.Key >= 0).Count();

                for (byte k = 0; k < GroupA.Count(); k++) //счетчик отрезков
                {
                    for (sbyte m = 0; m <= routesQuantityTop; m++)
                    {
                        List<byte> intervals = routes.ContainsKey(m) ? routes[m] : new List<byte>();
                        bool intersection = false;
                        for (byte i = 0; i < intervals.Count; i++)
                        {
                            if (matrixHC[GroupA[k], intervals[i]] == 1)
                            {
                                intersection = true;
                                break;
                            }
                        }
                        if (intersection && m + 1 == routesQuantityTop)
                        {
                            routesQuantityTop++;
                        }
                        else if (!intersection)
                        {
                            if (!routes.ContainsKey(m))
                            {
                                routes.Add(m, new List<byte>());
                            }
                            routes[m].Add(GroupA[k]);
                            counter++;
                            for (byte l = 0; l < matrixVC.GetLength(0); l++)
                                matrixVC[GroupA[k], l] = 0;
                            break;
                        }
                    }
                }
                byte[] GroupB = B.ToArray();
                byte routesQuantityBottom = (byte)routes.Where(x => x.Key < 0).Count();

                for (byte k = 0; k < GroupB.Count(); k++)
                {
                    for (sbyte m = -1; Math.Abs(m) <= routesQuantityBottom + 1; m--)
                    {
                        List<byte> intervals = routes.ContainsKey(m) ? routes[m] : new List<byte>();
                        bool intersection = false;
                        for (byte i = 0; i < intervals.Count; i++)
                        {
                            if (matrixHC[GroupB[k], intervals[i]] == 1)
                            {
                                intersection = true;
                                break;
                            }
                        }
                        if (intersection && Math.Abs(m) + 1 == routesQuantityBottom)
                        {
                            routesQuantityBottom++;
                        }
                        else if (!intersection)
                        {
                            if (!routes.ContainsKey(m))
                            {
                                routes.Add(m, new List<byte>());
                            }
                            routes[m].Add(GroupB[k]);
                            counter++;
                            for (byte l = 0; l < matrixVC.GetLength(1); l++)
                                matrixVC[l, GroupB[k]] = 0;
                            break;
                        }
                    }
                }
            } while (C.Count != 0);
            return routes;
        }

        private void GetABCGroups(byte[,] matrixVC, out List<byte> A, out List<byte> B, out List<byte> C)
        {
            A = new List<byte>();
            B = new List<byte>();
            C = new List<byte>();

            for (byte i = 0; i < matrixVC.GetLength(0); i++)
            {
                for (byte j = 0; j < matrixVC.GetLength(1); j++)
                {
                    if (matrixVC[i, j] != 0)
                    {
                        A.Add((byte)(i));
                        break;
                    }
                }
            }

            for (byte i = 0; i < matrixVC.GetLength(1); i++)
            {
                for (byte j = 0; j < matrixVC.GetLength(0); j++)
                {
                    if (matrixVC[j, i] != 0)
                    {
                        B.Add((byte)(i));
                        break;
                    }
                }
            }

            for (byte i = 0; i < LinesQuantity; i++)
            {
                if (A.Contains(i) && B.Contains(i))
                {
                    C.Add(i);
                    A.Remove(i);
                    B.Remove(i);
                }
            }
        }

        //Уложить горизонтальные отрезки в канале (в таблице)
        private string[,] PutIntoChannel(byte[,] contactPairs, Dictionary<byte, List<byte>> routes) 
        {
            byte routesQuantity = (byte)routes.Count();
            string[,] outputMatrix = new string[2 * routesQuantity + 1, contactPairs.GetLength(1)];

            foreach(Connection connection in ChannelConnections)
            {
                foreach(Line line in connection.Lines)
                {
                    for (byte i = line.LeftEnd.Coordinate.Value; i <= line.RightEnd.Coordinate; i++)
                    {
                        outputMatrix[2 * line.Route.Value + 1, i] = line.ConnectionId.ToString();
                    }
                }
            }
            return outputMatrix;
        }

        //Построение вертикальных соединений с контактами и перемычек
        public string[,] DrawVerticalLines(string[,] outputMatrix, byte[,] contactPairs, Dictionary<byte, List<byte>> routes)
        {
            foreach (Connection connection in ChannelConnections)
            {
                foreach (Line line in connection.Lines)
                {
                    if (!line.LeftEnd.IsBreakPoint)
                    {
                        if (line.LeftEnd.Height == EndHeight.Top)
                        {
                            for (int i = (2 * line.Route.Value + 1); i >= 0; i--)
                            {
                                if (outputMatrix[i, line.LeftEnd.Coordinate.Value] != null && outputMatrix[i, line.LeftEnd.Coordinate.Value] != line.ConnectionId.ToString())
                                {
                                    outputMatrix[i, line.LeftEnd.Coordinate.Value] = "+";
                                }
                                else
                                {
                                    outputMatrix[i, line.LeftEnd.Coordinate.Value] = line.ConnectionId.ToString();
                                }
                            }
                        }
                        else
                        {
                            for (int i = (2 * line.Route.Value + 1); i <= 2 * routes.Count; i++)
                            {
                                if (outputMatrix[i, line.LeftEnd.Coordinate.Value] != null && outputMatrix[i, line.LeftEnd.Coordinate.Value] != line.ConnectionId.ToString())
                                {
                                    outputMatrix[i, line.LeftEnd.Coordinate.Value] = "+";
                                }
                                else
                                {
                                    outputMatrix[i, line.LeftEnd.Coordinate.Value] = line.ConnectionId.ToString();
                                }
                            }
                        }
                    }
                    else
                    {
                        Line part = connection.Lines.Where(x => x.RightEnd.Coordinate.Value == line.LeftEnd.Coordinate.Value && x.RightEnd.IsBreakPoint).FirstOrDefault();
                        int start = (line.Route < part.Route) ? (2 * line.Route.Value + 1) : (2 * part.Route.Value + 1);
                        int finish = (line.Route >= part.Route) ? (2 * line.Route.Value + 1) : (2 * part.Route.Value + 1);

                        for (int i = start; i <= finish; i++)
                        {
                            if (outputMatrix[i, line.LeftEnd.Coordinate.Value] != null && outputMatrix[i, line.LeftEnd.Coordinate.Value] != line.ConnectionId.ToString())
                            {
                                outputMatrix[i, line.LeftEnd.Coordinate.Value] = "+";
                            }
                            else
                            {
                                outputMatrix[i, line.LeftEnd.Coordinate.Value] = line.ConnectionId.ToString();
                            }
                        }
                    }

                    if (!line.RightEnd.IsBreakPoint)
                    {
                        if (line.RightEnd.Height == EndHeight.Top)
                        {
                            for (int i = (2 * line.Route.Value + 1); i >= 0; i--)
                            {
                                if (outputMatrix[i, line.RightEnd.Coordinate.Value] != null && outputMatrix[i, line.RightEnd.Coordinate.Value] != line.ConnectionId.ToString())
                                {
                                    outputMatrix[i, line.RightEnd.Coordinate.Value] = "+";
                                }
                                else
                                {
                                    outputMatrix[i, line.RightEnd.Coordinate.Value] = line.ConnectionId.ToString();
                                }
                            }
                        }
                        else
                        {
                            for (int i = (2 * line.Route.Value + 1); i <= 2 * routes.Count; i++)
                            {
                                if (outputMatrix[i, line.RightEnd.Coordinate.Value] != null && outputMatrix[i, line.RightEnd.Coordinate.Value] != line.ConnectionId.ToString())
                                {
                                    outputMatrix[i, line.RightEnd.Coordinate.Value] = "+";
                                }
                                else
                                {
                                    outputMatrix[i, line.RightEnd.Coordinate.Value] = line.ConnectionId.ToString();
                                }
                            }
                        }
                    }
                    else
                    {
                        Line part = connection.Lines.Where(x => x.LeftEnd.Coordinate.Value == line.RightEnd.Coordinate.Value && x.LeftEnd.IsBreakPoint).FirstOrDefault();
                        int start = (line.Route < part.Route) ? (2 * line.Route.Value + 1) : (2 * part.Route.Value + 1);
                        int finish = (line.Route >= part.Route) ? (2 * line.Route.Value + 1) : (2 * part.Route.Value + 1);

                        for (int i = start; i <= finish; i++)
                        {
                            if (outputMatrix[i, line.RightEnd.Coordinate.Value] != null && outputMatrix[i, line.RightEnd.Coordinate.Value] != line.ConnectionId.ToString())
                            {
                                outputMatrix[i, line.RightEnd.Coordinate.Value] = "+";
                            }
                            else
                            {
                                outputMatrix[i, line.RightEnd.Coordinate.Value] = line.ConnectionId.ToString();
                            }
                        }
                    }
                }
            }
            return outputMatrix;
        }

    }
}

