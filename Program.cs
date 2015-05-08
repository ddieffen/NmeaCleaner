using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CleanFile
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            DataTable data = Parse(@"E:\videos\NMEA 2015-05-02 12-05-03.log");
            data = fillHoles(data);
            data = computeSmallAWA(data);
            data = computerOptimals(data);
            toCsv(data, @"E:\videos\exctacted.csv");
        }

        static DataTable fillHoles(DataTable dt)
        {
            DataRow previousRow = null;
            if(dt.Rows.Count > 0)
                previousRow = dt.Rows[0];
            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < row.ItemArray.Length; i++)
                {
                    if (row[i] == System.DBNull.Value)
                        row[i] = previousRow[i];
                }
                previousRow = row;
            }
            return dt;
        }
        const string _AWAS = "awaS";
        static DataTable computeSmallAWA(DataTable dt)
        { 
            dt.Columns.Add(_AWAS, typeof(double));
            foreach (DataRow row in dt.Rows)
            {
                double awa = (double)row[_AWA];
                double awas;
                if (awa < 180)
                    awas = awa;
                else
                    awas = 180-Math.Abs(180 - awa);
                row[_AWAS] = awas;
            }
            return dt;
        }

        const string _OptiAWAS = "optiAwaS";
        const string _OptiSOWKn = "optiSowKn";
        static DataTable computerOptimals(DataTable dt)
        {
            dt.Columns.Add(_OptiAWAS, typeof(double));
            dt.Columns.Add(_OptiSOWKn, typeof(double));
            foreach (DataRow row in dt.Rows)
            {
                double twsPointKn = MetersSecondToKnots((double)row[_TWS]);
                double optimalAWAs = OptimalInterpolation(_optiTWS, _optiAwaUp, twsPointKn);
                double optimalSOWKn = OptimalInterpolation(_optiTWS, _optiSowUp, twsPointKn);
                row[_OptiAWAS] = optimalAWAs;
                row[_OptiSOWKn] = optimalSOWKn;
            }
            return dt;
        }
        static void toCsv(DataTable dt, string outputFileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Sorcerer NMEA CSV");
            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(outputFileName, sb.ToString());
        }

        static void Clean(string filename)
        {
            string line = "";
            List<string> lines = new List<string>();
            System.IO.StreamReader file = new System.IO.StreamReader(filename);
            while ((line = file.ReadLine()) != null)
            {
                string filtered = line.Trim('\0').Trim('\r').Trim('\n');
                if (!String.IsNullOrEmpty(filtered))
                    lines.Add(filtered);
            }

            file.Close();

            System.IO.File.WriteAllLines(filename, lines);
        }

        const string _DATETIME = "Time";
        const string _LAT = "latN";
        const string _LON = "longE";
        const string _SOG = "sogMS";
        const string _COGT = "cogT";
        const string _COGM = "cogM";
        const string _HDGT = "hdgT";
        const string _HDGM = "hdgM";
        const string _SOW = "sowMS";
        const string _AWA = "awa";
        const string _TWA = "twa";
        const string _TWDT = "twdT";
        const string _TWDM = "twdM";
        const string _AWS = "awsMS";
        const string _TWS = "twsMS";
        const string _DPTBT = "dptM";
        const string _DPTOFF = "dptoffsetM";
        const string _ELEV = "elev";
        const string _TEMP = "temp";
        const string _MAGDEV = "magdev";
        const string _MAGVAR = "magvar";

        static DataTable Parse(string filename)
        {
            DataTable table = new DataTable();
            table.Columns.Add(_DATETIME, typeof(DateTime));
            table.Columns.Add(_LAT, typeof(double));
            table.Columns.Add(_LON, typeof(double));
            table.Columns.Add(_SOG, typeof(double));
            table.Columns.Add(_COGT, typeof(double));
            table.Columns.Add(_COGM, typeof(double));
            table.Columns.Add(_HDGT, typeof(double));
            table.Columns.Add(_HDGM, typeof(double));
            table.Columns.Add(_SOW, typeof(double));
            table.Columns.Add(_AWA, typeof(double));
            table.Columns.Add(_TWA, typeof(double));
            table.Columns.Add(_TWDT, typeof(double));
            table.Columns.Add(_TWDM, typeof(double));
            table.Columns.Add(_AWS, typeof(double));
            table.Columns.Add(_TWS, typeof(double));
            table.Columns.Add(_DPTBT, typeof(double));
            table.Columns.Add(_DPTOFF, typeof(double));
            table.Columns.Add(_ELEV, typeof(double));
            table.Columns.Add(_TEMP, typeof(double));
            table.Columns.Add(_MAGDEV, typeof(double));
            table.Columns.Add(_MAGVAR, typeof(double));

            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(filename);
            int idx = 0;
            table.Rows.Add();
            while ((line = file.ReadLine()) != null)
            {
                string filtered = line.Trim('\0').Trim('\r').Trim('\n');
                if (!String.IsNullOrEmpty(filtered))
                    AppendSentence(table, filtered, ref idx);
            }

            return table;
        }

        static List<double> _optiTWS = new List<double> { 4, 6, 8, 10, 12, 14, 16, 20, 25, 30 };
        static List<double> _optiTwaUp = new List<double> { 46.8, 45, 43.1, 41.8, 40.8, 40, 39.6, 40.4, 42.5, 45.6 };
        static List<double> _optiAwaUp = new List<double> { 25.2, 25.2, 25.3, 25.5, 25.9, 26.3, 26.8, 28.8, 31.9, 35.5 };
        static List<double> _optiSowUp = new List<double> { 3.435, 4.739, 5.565, 6.050, 6.266, 6.369, 6.419, 6.484, 6.531, 6.549 };

        private static double OptimalInterpolation(List<double> tws, List<double> optimal, double twsPoint)
        {
            if (tws.Count != optimal.Count)
                return -1;

            int speedAbove = 0;
            int speedBelow = tws.Count-1;
            for (int i = 0; i < tws.Count; i++)
            {
                if (tws[i] - twsPoint >= 0)
                {
                    if (Math.Abs(tws[speedAbove] - tws[i]) > Math.Abs(twsPoint - tws[i]))
                        speedAbove = i; 
                }
                else
                    if (Math.Abs(tws[speedBelow] - tws[i]) > Math.Abs(twsPoint - tws[i]))
                        speedBelow = i;
            }

            if (speedAbove == 0)
                return 0;
            else if (speedBelow == tws.Count-1)
                return 0;
            else
                return Linear(tws[speedBelow], tws[speedAbove], optimal[speedBelow], optimal[speedAbove], twsPoint);
        }

        private static double Linear(double xMin, double xMax, double yMin, double yMax, double point)
        {
            return yMin + (yMax - yMin) * ((point - xMin) / (xMax - xMin));
        }

        private static void AppendSentence(DataTable table, string filtered, ref int idx)
        {
            if (validLine(filtered))
            {
                parseMWV(table, filtered, ref idx);
                parseMWD(table, filtered, ref idx);
                parseRMC(table, filtered, ref idx);
                parseRMB(table, filtered, ref idx);
                parseVHW(table, filtered, ref idx);
                parseGLL(table, filtered, ref idx);
                parseGGA(table, filtered, ref idx);
                parseDPT(table, filtered, ref idx);
                parseHDG(table, filtered, ref idx);
                parseHDM(table, filtered, ref idx);
                parseHDT(table, filtered, ref idx);
                parseMTW(table, filtered, ref idx);
                parseVTG(table, filtered, ref idx);
            }
        }

        /**
         * Parses VTG NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === VTG - Track made good and Ground speed ===
         * $--VTG,x.x,T,x.x,M,x.x,N,x.x,K,m,*hh
         * Example : 
         * 
         * 1. Track Degrees
         * 2. T = True
         * 3. Track Degrees
         * 4. M = Magnetic
         * 5. Speed Knots
         * 6. N = Knots
         * 7. Speed Kilometers Per Hour
         * 8. K = Kilometers Per Hour
         * 9. FAA mode indicator (NMEA 2.3 and later)
         * 10. Checksum
         * 
         * @param data
         * @param sentence
         */
        private static void parseVTG(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("vtg"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().StartsWith("t"))
                {
                    data.Rows[idx][_COGT] = tempValue;
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[4].ToLower().StartsWith("m"))
                {
                    data.Rows[idx][_COGM] = tempValue;
                }
                if (dataSplit.Length >= 7 &&
                    (tempValue = tryParse(dataSplit[5])) != -1
                    && dataSplit[6].ToLower().StartsWith("n"))
                {
                    data.Rows[idx][_SOG] = KnotsToMetersSecond(tempValue);
                }
                if (dataSplit.Length >= 9 &&
                    (tempValue = tryParse(dataSplit[7])) != -1
                    && dataSplit[8].ToLower().StartsWith("k"))
                {
                    data.Rows[idx][_SOG] = KphToMetersSecond(tempValue);
                }
            }
        }

        /**
         * Parses MTW NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === MTW - Mean Temperature of Water ===
         * $--MTW,x.x,C*hh
         * Example : 
         * 
         * 1. Degrees
         * 2. Unit of Measurement, Celcius
         * 3. Checksum
         *  
         * @param data
         * @param sentence
         */
        private static void parseMTW(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("mtw"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().StartsWith("c"))
                {
                    data.Rows[idx][_TEMP] = tempValue;
                }
            }
        }

        /**
         * Parses HDT NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === HDT - Heading - True ===
         * $--HDT,x.x,T*hh
         * Example : 
         * 
         * 1. Heading Degrees, true
         * 2. T = True
         * 3. Checksum
         * 
         * @param data
         * @param sentence
         */
        private static void parseHDT(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("hdt"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().StartsWith("t"))
                {
                    data.Rows[idx][_HDGT] = tempValue;
                }
            }
        }

        /**
         * Parses HDM NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === HDM - Heading - Magnetic ===
         * $--HDM,x.x,M*hh
         * Example : 
         * 
         * 1. Heading Degrees, magnetic
         * 2. M = magnetic
         * 3. Checksum
         * 
         * @param data
         * @param sentence
         */
        private static void parseHDM(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("hdm"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().StartsWith("m"))
                {
                    data.Rows[idx][_HDGM] = tempValue;
                }
            }
        }

        /**
         * Parses HDG NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === HDG - Heading - Deviation & Variation ===
         * $--HDG,x.x,x.x,a,x.x,a*hh
         * Example : 
         * 
         * 1. Magnetic Sensor heading in degrees
         * 2. Magnetic Deviation, degrees
         * 3. Magnetic Deviation direction, E = Easterly, W = Westerly
         * 4. Magnetic Variation degrees
         * 5. Magnetic Variation direction, E = Easterly, W = Westerly
         * 6. Checksum
         * 
         * @param data
         * @param sentence
         */
        private static void parseHDG(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("hdg"))
            {
                double tempValue;
                if (dataSplit.Length >= 2 &&
                    (tempValue = tryParse(dataSplit[1])) != -1)
                {
                    data.Rows[idx][_HDGM] = tempValue;
                }
                if (dataSplit.Length >= 4 &&
                    (tempValue = tryParse(dataSplit[2])) != -1)
                {
                    if (dataSplit[3].ToLower().StartsWith("e"))
                        data.Rows[idx][_MAGDEV] = tempValue;
                    else if (dataSplit[3].ToLower().StartsWith("w"))
                        data.Rows[idx][_MAGDEV] = -tempValue;
                }
                if (dataSplit.Length >= 6 &&
                    (tempValue = tryParse(dataSplit[4])) != -1)
                {
                    if (dataSplit[5].ToLower().StartsWith("e"))
                        data.Rows[idx][_MAGVAR] = tempValue;
                    else if (dataSplit[5].ToLower().StartsWith("w"))
                        data.Rows[idx][_MAGVAR] = -tempValue;
                }
            }
        }

        /**
         * Parses DPT NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === DPT - Depth of Water ===
         * $--DPT,x.x,x.x*hh
         * Example : 
         * 
         * 1. Depth, meters
         * 2. Offset from transducer, 
         *     positive means distance from tansducer to water line
         *     negative means distance from transducer to keel
         * 3. Checksum
         * 
         * @param data
         * @param sentence
         */
        private static void parseDPT(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("dpt"))
            {
                double tempValue;
                if (dataSplit.Length >= 2 &&
                    (tempValue = tryParse(dataSplit[1])) != -1)
                {
                    data.Rows[idx][_DPTBT] = tempValue;
                }
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[2])) != -1)
                {
                    data.Rows[idx][_DPTOFF] = tempValue;
                }
                //maximum depth range scale in use attribute omitted as useless for our application
            }
        }

        /**
         * Parses GGA NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === GGA - Global Positioning System Fix Data ===
         * $--GGA,hhmmss.ss,llll.ll,a,yyyyy.yy,a,x,xx,x.x,x.x,M,x.x,M,x.x,xxxx*hh
         * Example : 
         * 
         * 1. Universal Time Coordinated (UTC)
         * 2. Latitude
         * 3. N or S (North or South)
         * 4. Longitude
         * 5. E or W (East or West)
         * 6. GPS Quality Indicator,
         *      - 0 - fix not available,
         *      - 1 - GPS fix,
         *      - 2 - Differential GPS fix
         *            (values above 2 are 2.3 features)
         *      - 3 = PPS fix
         *      - 4 = Real Time Kinematic
         *      - 5 = Float RTK
         *      - 6 = estimated (dead reckoning)
         *      - 7 = Manual input mode
         *      - 8 = Simulation mode
         * 7. Number of satellites in view, 00 - 12
         * 8. Horizontal Dilution of precision (meters)
         * 9. Antenna Altitude above/below mean-sea-level (geoid) (in meters)
         * 10. Units of antenna altitude, meters
         * 11. Geoidal separation, the difference between the WGS-84 earth
         *      ellipsoid and mean-sea-level (geoid), "-" means mean-sea-level
         *      below ellipsoid
         * 12. Units of geoidal separation, meters
         * 13. Age of differential GPS data, time in seconds since last SC104
         *      type 1 or 9 update, null field when DGPS is not used
         * 14. Differential reference station ID, 0000-1023
         * 15. Checksum
         * 
         * @param data
         * @param sentence
         */
        private static void parseGGA(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("gga"))
            {
                double tempValue;

                if (dataSplit.Length >= 2 &&
                     (tempValue = tryParse(dataSplit[1])) != -1)
                {
                    int seconds = (int)(tempValue % 100);
                    int minutes = (int)(tempValue / 100) % 100;
                    int hours = (int)(tempValue / 10000);

                    if (data.Rows[idx][_DATETIME] != System.DBNull.Value)
                    {
                        DateTime dt = (DateTime)data.Rows[idx][_DATETIME];
                        dt = new DateTime(dt.Year, dt.Month, dt.Day, hours, minutes, seconds, DateTimeKind.Utc);
                        data.Rows[idx][_DATETIME] = dt;
                    }
                    else
                    {
                        DateTime utcNow = DateTime.Now.ToUniversalTime();
                        DateTime dt = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, hours, minutes, seconds, DateTimeKind.Utc);
                        data.Rows[idx][_DATETIME] = dt;
                    }
                }
                if (dataSplit.Length >= 4 &&
                    (tempValue = tryParse(dataSplit[2])) != -1)
                {
                    double minutes = (tempValue % 100);
                    double degrees = (double)(tempValue / 100) % 100;
                    if (dataSplit[3].ToLower().StartsWith("n"))
                    {
                        data.Rows[idx][_LAT] = DegreeMinutesToDegree(degrees, minutes);
                    }
                    else if (dataSplit[3].ToLower().StartsWith("s"))
                    {
                        data.Rows[idx][_LAT] = -DegreeMinutesToDegree(degrees, minutes);
                    }
                }
                if (dataSplit.Length >= 6 &&
                    (tempValue = tryParse(dataSplit[4])) != -1)
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;
                    if (dataSplit[5].ToLower().StartsWith("e"))
                    {
                        data.Rows[idx][_LON] = DegreeMinutesToDegree(degrees, minutes);
                    }
                    else if (dataSplit[5].ToLower().StartsWith("w"))
                    {
                        data.Rows[idx][_LON] = -DegreeMinutesToDegree(degrees, minutes);
                    }
                }
                if (dataSplit.Length >= 7 &&
                    (tempValue = tryParse(dataSplit[6])) != -1)
                {
                    //data.GpsQuality = (int)tempValue;
                }
                if (dataSplit.Length >= 8 &&
                    (tempValue = tryParse(dataSplit[7])) != -1)
                {
                    //data.GpsNumberSatellites = (int)tempValue;
                }
                //skipping other attributes
            }
        }

        /**
         * Parses MWD NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * MWD Wind Direction and Speed (TWD M / T and TWS)
         * Example : $IIMWD,357,T,000,M,8.00,N,4.11,M*49
         * 
         * 1. Wind direction, 0.0 to 359.9 degrees True, to the nearest 0.1 degree
         * 2. T = True
         * 3. Wind direction, 0.0 to 359.9 degrees Magnetic, to the nearest 0.1 
         *    degree
         * 4. M = Magnetic
         * 5. Wind speed, knots, to the nearest 0.1 knot.
         * 6. N = Knots
         * 7. Wind speed, meters/second, to the nearest 0.1 m/s.
         * 8. M = Meters/second
         * 
         * @param data Object that is being filled with the parsed information
         * @param frame NMEA0183 formatted string
         */
        private static void parseMWD(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("mwd"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().StartsWith("t"))
                {
                    data.Rows[idx][_TWDT] = tempValue;
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[4].ToLower().StartsWith("m"))
                {
                    data.Rows[idx][_TWDM] = tempValue;
                }
                if (dataSplit.Length >= 7 &&
                    (tempValue = tryParse(dataSplit[5])) != -1
                    && dataSplit[6].ToLower().StartsWith("n"))
                {
                    data.Rows[idx][_TWS] = KnotsToMetersSecond(tempValue);
                }
                if (dataSplit.Length >= 9 &&
                    (tempValue = tryParse(dataSplit[7])) != -1
                    && dataSplit[8].ToLower().StartsWith("m"))
                {
                    data.Rows[idx][_TWS] = tempValue;
                }
            }
        }

        /**
         * Parses RMB NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === RMB - Recommended Minimum Navigation Information ===
         * Example : $IIRMB,A,2.07,R,,WL-MARK2,4150.19,N,08730.43,W,2.09,105,1269.13,V,D*7D\r\n
         * 
         * 1. Status, A= Active, V = Void
         * 2. Cross Track error - nautical miles
         * 3. Direction to Steer, Left or Right
         * 4. TO Waypoint ID
         * 5. FROM Waypoint ID
         * 6. Destination Waypoint Latitude
         * 7. N or S
         * 8. Destination Waypoint Longitude
         * 9. E or W
         * 10. Range to destination in nautical miles
         * 11. Bearing to destination in degrees True
         * 12. Destination closing velocity in knots
         * 13. Arrival Status, A = Arrival Circle Entered
         * 14. FAA mode indicator (NMEA 2.3 and later)
         * 15. Checksum
         * 
         * @param data Object that is being filled with the parsed information
         * @param frame NMEA0183 formatted string
         */
        private static void parseRMB(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("rmb"))
            {
                double tempValue;
                if (dataSplit.Length >= 2)
                {
                    //data.Status &= dataSplit[1].ToLower().StartsWith("a");
                }
                if (dataSplit.Length >= 3 &&
                        (tempValue = tryParse(dataSplit[2])) != -1)
                {
                    //data.CrossTrackError = Tools.MilesToMeters(tempValue);
                }
                //skipping direction to steer
                if (dataSplit.Length >= 5)
                {
                    //data.WaypointOriginID = dataSplit[4];
                }
                if (dataSplit.Length >= 6)
                {
                    //data.WaypointDestinationID = dataSplit[5];
                }
                if (dataSplit.Length >= 8 &&
                       (tempValue = tryParse(dataSplit[6])) != -1)
                {
                    double minutes = (tempValue % 100);
                    double degrees = (double)(tempValue / 100) % 100;
                    if (dataSplit[7].ToLower().StartsWith("n"))
                    {
                        //data.WaypointDestinationLatitudeN = Tools.DegreeMinutesToDegree(degrees, minutes);
                    }
                    else if (dataSplit[7].ToLower().StartsWith("s"))
                    {
                        //data.WaypointDestinationLatitudeN = -Tools.DegreeMinutesToDegree(degrees, minutes);
                    }
                }
                if (dataSplit.Length >= 10 &&
                   (tempValue = tryParse(dataSplit[8])) != -1)
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;
                    if (dataSplit[9].ToLower().StartsWith("e"))
                    {
                        //data.WaypointDestinationLongitudeE = Tools.DegreeMinutesToDegree(degrees, minutes);
                    }
                    else if (dataSplit[9].ToLower().StartsWith("w"))
                    {
                        //data.WaypointDestinationLongitudeE = -Tools.DegreeMinutesToDegree(degrees, minutes);
                    }
                }
                if (dataSplit.Length >= 11 &&
                        (tempValue = tryParse(dataSplit[10])) != -1)
                {
                    //data.WaypointDestinationRange = Tools.MilesToMeters(tempValue);
                }
                if (dataSplit.Length >= 12 &&
                        (tempValue = tryParse(dataSplit[11])) != -1)
                {
                    //data.WaypointDestinationBearingT = tempValue;
                }
                if (dataSplit.Length >= 13 &&
                        (tempValue = tryParse(dataSplit[12])) != -1)
                {
                    //data.WaypointVMG = Tools.KnotsToMetersSecond(tempValue);
                }
            }
        }

        /**
         * Parses VHW NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === VHW - Water speed and heading ===
         * $--VHW,x.x,T,x.x,M,x.x,N,x.x,K*hh
         * Example : $IIVHW,322,T,325,M,5.92,N,10.97,K*63
         * 
         * 1. Degress True
         * 2. T = True
         * 3. Degrees Magnetic
         * 4. M = Magnetic
         * 5. Knots (speed of vessel relative to the water)
         * 6. N = Knots
         * 7. Kilometers (speed of vessel relative to the water)
         * 8. K = Kilometers
         * 9. Checksum
         * 
         * @param data Object that is being filled with the parsed information
         * @param frame NMEA0183 formatted string
         */
        private static void parseVHW(DataTable data, String frame, ref int idx)
        {

            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("vhw"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().Equals("t"))
                {
                    data.Rows[idx][_HDGT] = tempValue;
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[4].ToLower().Equals("m"))
                {
                    data.Rows[idx][_HDGM] = tempValue;
                }
                if (dataSplit.Length >= 7 &&
                    (tempValue = tryParse(dataSplit[5])) != -1
                    && dataSplit[6].ToLower().Equals("n"))
                {
                    data.Rows[idx][_SOW] = KnotsToMetersSecond(tempValue);
                }
            }
        }

        /**
         * Parses GLL NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === GLL - Geographic Position - Latitude/Longitude ===
         * $--GLL,llll.ll,a,yyyyy.yy,a,hhmmss.ss,a,m,*hh
         * Example : $IIGLL,4151.40,N,08734.88,W,140012.00,A,D*62
         * 
         * 1. Latitude
         * 2. N or S (North or South)
         * 3. Longitude
         * 4. E or W (East or West)
         * 5. Universal Time Coordinated (UTC)
         * 6. Status A - Data Valid, V - Data Invalid
         * 7. FAA mode indicator (NMEA 2.3 and later)
         * 8. Checksum
         * 
         * @param data Object that is being filled with the parsed information
         * @param frame NMEA0183 formatted string
         */
        private static void parseGLL(DataTable data, String frame, ref int idx)
        {

            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("gll"))
            {
                double tempValue;
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().Equals("n"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (double)(tempValue / 100) % 100;

                    data.Rows[idx][_LAT] = DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[4].ToLower().Equals("w"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;

                    data.Rows[idx][_LON] = -DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 3 &&
                    (tempValue = tryParse(dataSplit[1])) != -1
                    && dataSplit[2].ToLower().Equals("s"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;

                    data.Rows[idx][_LAT] = -DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[4].ToLower().Equals("e"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;

                    data.Rows[idx][_LON] = DegreeMinutesToDegree(degrees, minutes);
                }
            }
        }

        /**
         * Parses RMC NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === RMC - Recommended Minimum Navigation Information ===
         * $--RMC,hhmmss.ss,A,llll.ll,a,yyyyy.yy,a,x.x,x.x,xxxx,x.x,a,m,*hh<CR><LF>
         * Example : $IIRMC,183701.00,A,4150.76,N,08733.12,W,6.20,330,,003,W,D*11\r\n
         *  
         * 1. UTC Time
         * 2. Status, V=Navigation receiver warning A=Valid
         * 3. Latitude
         * 4. N or S
         * 5. Longitude
         * 6. E or W
         * 7. Speed over ground, knots
         * 8. Track made good, degrees true
         * 9. Date, ddmmyy
         * 10. Magnetic Variation, degrees
         * 11. E or W
         * 12. FAA mode indicator (NMEA 2.3 and later)
         * 13. Checksum
         *  
         * @param data Object that is being filled with the parsed information
         * @param frame NMEA0183 formatted string
         */
        private static void parseRMC(DataTable data, String frame, ref int idx)
        {

            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("rmc"))
            {
                double tempValue;
                if (dataSplit.Length >= 2 &&
                    (tempValue = tryParse(dataSplit[1])) != -1)
                {
                    int seconds = (int)(tempValue % 100);
                    int minutes = (int)(tempValue / 100) % 100;
                    int hours = (int)(tempValue / 10000);

                    if (data.Rows[idx][_DATETIME] != System.DBNull.Value)
                    {
                        idx++;
                        data.Rows.Add();
                    }

                    DateTime utcNow = DateTime.Now.ToUniversalTime();
                    DateTime dt = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, hours, minutes, seconds, DateTimeKind.Utc);
                    data.Rows[idx][_DATETIME] = dt;
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[2].ToLower().Equals("n"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;

                    data.Rows[idx][_LAT] = DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 5 &&
                    (tempValue = tryParse(dataSplit[3])) != -1
                    && dataSplit[2].ToLower().Equals("s"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;

                    data.Rows[idx][_LAT] = -DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 7 &&
                    (tempValue = tryParse(dataSplit[5])) != -1
                    && dataSplit[4].ToLower().Equals("w"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;

                    data.Rows[idx][_LON] = -DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 7 &&
                    (tempValue = tryParse(dataSplit[5])) != -1
                    && dataSplit[4].ToLower().Equals("e"))
                {
                    double minutes = (tempValue % 100);
                    double degrees = (int)(tempValue / 100) % 100;
                    data.Rows[idx][_LON] = DegreeMinutesToDegree(degrees, minutes);
                }
                if (dataSplit.Length >= 8 &&
                    (tempValue = tryParse(dataSplit[7])) != -1)
                {
                    data.Rows[idx][_SOG] = KnotsToMetersSecond(tempValue);
                }
                if (dataSplit.Length >= 10 &&
                        (tempValue = tryParse(dataSplit[9])) != -1)
                {
                    int year = (int)(tempValue % 100);
                    int month = (int)(tempValue / 100) % 100;
                    int day = (int)(tempValue / 10000);

                    if (year > 50)
                        year += 1900;
                    else
                        year += 2000;

                    if (data.Rows[idx][_DATETIME] != System.DBNull.Value)
                    {
                        DateTime dt = (DateTime)data.Rows[idx][_DATETIME];
                        dt = new DateTime(year, month, day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);
                        data.Rows[idx][_DATETIME] = dt;
                    }
                }
            }
        }

        /**
         * Parses MWV NMEA0183 sentence if found in the given frame and put information into the data object
         * 
         * === MWV - Wind Speed and Angle ===
         * $--MWV,x.x,a,x.x,a*hh
         * Example : $IIMWV,228,T,8.00,N,A*15\r\n
         * 
         * 1) Wind Angle, 0 to 360 degrees
         * 2) Reference, R = Relative, T = True (if R then angle and speed are relative, if T then angle and speed are Theorical)
         * 3) Wind Speed
         * 4) Wind Speed Units, K = km/hr, M = m/s, N = knots, S = statute 
         * 5) Status, A = Data Valid
         * 6) Checksum
         * 
         * @param data Object that is being filled with the parsed information
         * @param frame NMEA0183 formatted string
         */
        private static void parseMWV(DataTable data, String frame, ref int idx)
        {
            String[] dataSplit = frame.Split(',');
            if (dataSplit[0].ToLower().Contains("mwv"))
            {
                double tempValue;
                if (dataSplit[2].ToLower().Equals("t"))
                {//true wind readings are given by this row of data
                    if ((tempValue = tryParse(dataSplit[3])) != -1)
                        if (dataSplit[4].ToLower().Equals("n"))
                            data.Rows[idx][_TWS] = KnotsToMetersSecond(tempValue);
                    if (dataSplit[4].ToLower().Equals("k"))
                        data.Rows[idx][_TWS] = KphToMetersSecond(tempValue);
                    if (dataSplit[4].ToLower().Equals("m"))
                        data.Rows[idx][_TWS] = tempValue;
                    if (dataSplit[4].ToLower().Equals("s"))
                        data.Rows[idx][_SOG] = StatutMilesToMetersSecond(tempValue);
                    if ((tempValue = tryParse(dataSplit[1])) != -1)
                        data.Rows[idx][_TWA] = tempValue;

                }
                else if (dataSplit[2].ToLower().Equals("r"))
                {//apparent wind readings are given by this row of data
                    if ((tempValue = tryParse(dataSplit[3])) != -1)
                        if (dataSplit[4].ToLower().Equals("n"))
                            data.Rows[idx][_AWS] = KnotsToMetersSecond(tempValue);
                    if (dataSplit[4].ToLower().Equals("k"))
                        data.Rows[idx][_AWS] = KphToMetersSecond(tempValue);
                    if (dataSplit[4].ToLower().Equals("m"))
                        data.Rows[idx][_AWS] = tempValue;
                    if (dataSplit[4].ToLower().Equals("s"))
                        data.Rows[idx][_AWS] = StatutMilesToMetersSecond(tempValue);
                    if ((tempValue = tryParse(dataSplit[1])) != -1)
                        data.Rows[idx][_AWA] = tempValue;

                }
            }
        }

        /**
         * Calculates the checksum from an NMEA0183 sentence
         * returns the calculated checksum as a string of two characters
         * 
         * @param sentence must be formatted as follow : $data*
         * the checksum will be computer from the characters between the $ and * characters
         * @return 2-digit hex value of the computed checksum
         */
        public static String getChecksum(String sentence)
        {
            if (sentence.Length >= 5 && //two chars for $ and * three chars for the nmea code min
                    sentence.Contains("$") && sentence.Contains("*"))
            {

                //Start with first Item
                int checksum = (byte)sentence[sentence.IndexOf('$') + 1];
                // Loop through all chars to get a checksum
                for (int i = sentence.IndexOf('$') + 2; i < sentence.IndexOf('*'); i++)
                {
                    // No. XOR the checksum with this character's value
                    checksum ^= (byte)sentence[i];
                }

                // Return the checksum formatted as a two-character hexadecimal
                StringBuilder sb = new StringBuilder();
                sb.Append(checksum.ToString("X2"));
                if (sb.Length < 2)
                {
                    sb.Insert(0, '0'); // pad with leading zero if needed
                }
                return sb.ToString();
            }
            else
                return "";
        }

        /**
         * This method checks if a sentence is valid comparing the checksum at the end of the line
         * and the calculated checksum from the data at the beginning of the line
         * 
         * The method uses the data contained between $ and * to calculate the checksum
         * then uses the two characters following the * to compare with the computed checksum
         * 
         * @param must be formatted as follow : $data*checksum
         * @return
         */
        public static bool validLine(String sentence)
        {
            String clean = sentence.Replace("(\\r|\\n)", "");
            String checksum = getChecksum(clean);
            String[] split = clean.Split('*');

            return (split.Length == 2 && split[1].Length == 2 && split[1].Substring(0, 2).ToLower().Equals(checksum.ToLower()));
        }

        private static double tryParse(string str)
        {
            double tempVal;
            if (Double.TryParse(str, out tempVal))
                return tempVal;
            else
                return -1;
        }

        /**
     * Takes a speed in knots as an input and return a speed in meters per second
     * 
     * @param knots
     * @return
     */
        public static double KnotsToMetersSecond(double knots)
        {
            return knots * 0.5144444444444444;
        }

        /** 
         * Takes a speed in meters per second as an input and returns a speed in knots
         * 
         * @param metersSecond
         * @return
         */
        public static double MetersSecondToKnots(double metersSecond)
        {
            return metersSecond * 1.94384449244;
        }

        /**
         * Takes a distance in nautical miles as an input and return a distance in meters
         * @param miles
         * @return
         */
        public static double MilesToMeters(double miles)
        {
            return miles * 1852;
        }

        /**
         * Takes a distance in meters as an input and returns a distance in nautical miles
         * @param meters
         * @return
         */
        public static double MetersToMiles(double meters)
        {
            return meters / 1852;
        }

        /** 
         * Converts location formatted as XX YY.yy where XX are degrees and YY.yy are minutes
         * into XX.XXXX where XX.XXXX is the same location in decimal degrees
         * 
         * @param degrees
         * @param minutes
         * @return
         */
        public static double DegreeMinutesToDegree(double degrees, double minutes)
        {
            return degrees + minutes / 60;
        }

        /**
         * Converts a speed in Kilometers per Hour in a speed in meters per second
         * 
         * @param tempValue
         * @return
         */
        public static double KphToMetersSecond(double kph)
        {

            return 0.2777777777777778 * kph;
        }

        /**
         * Converts a speed in statue miles per hour in a speed in meters per second
         * @param mile
         * @return
         */
        public static double StatutMilesToMetersSecond(double mile)
        {

            return 0.44704 * mile;
        }
    }
}
