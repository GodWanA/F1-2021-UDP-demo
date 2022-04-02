﻿using F1Telemetry.Models.CarStatusPacket;
using F1Telemetry.Models.LapDataPacket;
using F1Telemetry.Models.MotionPacket;
using F1Telemetry.Models.SessionPacket;
using F1TelemetryApp.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static F1Telemetry.Helpers.Appendences;

namespace F1TelemetryApp.Windows
{
    /// <summary>
    /// Interaction logic for TrackLayoutRecorderWindow.xaml
    /// </summary>
    public partial class TrackLayoutRecorderWindow : Window, IDisposable, IConnectUDP, INotifyPropertyChanged
    {
        internal List<Point> TrackCoords { get; private set; } = new List<Point>();
        internal Dictionary<Sectors, List<Point>> SectorCoords { get; private set; } = new Dictionary<Sectors, List<Point>>();
        internal Dictionary<Tuple<int, float>, List<Point>> MarshalZoneCoords { get; private set; } = new Dictionary<Tuple<int, float>, List<Point>>();
        internal Dictionary<int, List<Point>> DRSZones { get; private set; } = new Dictionary<int, List<Point>>();

        private static Point zeroPoint = new Point();
        private static Brush drsColor = new SolidColorBrush(Color.FromArgb(100, 30, 200, 30));

        private bool canLapdata = true;
        private bool canSession = true;
        private bool canCarmotion = true;

        private bool isTimeTrial;

        private bool disposedValue;

        private double minY;
        private double maxY;
        private double maxX;
        private double minX;

        private int lapNumber = 0;
        private Sectors currentSector;

        private string coordinates;

        private int marshalIndex;
        private int drsIndex = -1;
        private bool lastDRSstatus;
        private TimeSpan laptime;

        public string Coordinates
        {
            get { return this.coordinates; }
            private set
            {
                if (this.coordinates != value)
                {
                    this.coordinates = value;
                    this.OnPropertyChanged("Coordinates");
                }
            }
        }

        private int packetFormat;

        public int PacketFormat
        {
            get { return this.packetFormat; }
            private set
            {
                if (this.packetFormat != value)
                {
                    this.packetFormat = value;
                    this.OnPropertyChanged("PacketFormat");
                }
            }
        }

        private Tracks trackID;
        private ushort year;
        private string trackName;

        public Tracks TrackID
        {
            get { return this.trackID; }
            private set
            {
                if (this.trackID != value)
                {
                    this.trackID = value;
                    this.OnPropertyChanged("TrackID");
                }
            }
        }

        internal TrackLayout Map { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TrackLayoutRecorderWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    this.UnsubscribeUDPEvents();
                    this.Coordinates = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.coordinates = null;
                disposedValue = true;
            }
        }

        //// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        //~TrackLayoutRecorderWindow()
        //{
        //    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //    Dispose(disposing: false);
        //}

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SubscribeUDPEvents()
        {
            if (u.Connention != null)
            {
                u.Connention.LapDataPacket += Connention_LapDataPacket;
                u.Connention.SessionPacket += Connention_SessionPacket;
                u.Connention.CarMotionPacket += Connention_CarMotionPacket;
                u.Connention.CarStatusPacket += Connention_CarStatusPacket;
            }
        }

        public void UnsubscribeUDPEvents()
        {
            if (u.Connention != null)
            {
                u.Connention.LapDataPacket -= Connention_LapDataPacket;
                u.Connention.SessionPacket -= Connention_SessionPacket;
                u.Connention.CarMotionPacket -= Connention_CarMotionPacket;
                u.Connention.CarStatusPacket -= Connention_CarStatusPacket;
            }
        }

        private void Connention_CarStatusPacket(object sender, EventArgs e)
        {
            var data = sender as PacketCarStatusData;
            var player = data.CarStatusData[data.Header.Player1CarIndex];

            if (this.laptime != TimeSpan.Zero)
            {
                if (this.lastDRSstatus != player.IsDRSAllowed)
                {
                    this.lastDRSstatus = player.IsDRSAllowed;
                    if (player.IsDRSAllowed) this.drsIndex++;

                    this.Dispatcher.Invoke(() =>
                    {
                        if (drsColor.CanFreeze) drsColor.Freeze();

                        this.grid_drszones.Children.Add(new Path
                        {
                            Stroke = drsColor,
                            StrokeThickness = 3,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            StrokeDashCap = PenLineCap.Round,
                        });
                    }, DispatcherPriority.Render);
                }
            }
        }

        private void Connention_CarMotionPacket(object sender, EventArgs e)
        {
            var data = sender as PacketMotionData;
            var player = data.CarMotionData[data.Header.Player1CarIndex];
            var p = new Point(player.WorldPosition.X, player.WorldPosition.Z);

            if (this.canCarmotion)
            {
                this.canCarmotion = false;

                if (this.CanRecord())
                {
                    if (!this.TrackCoords.Contains(p)) this.TrackCoords.Add(p);

                    if (!this.SectorCoords.ContainsKey(this.currentSector)) this.SectorCoords.Add(this.currentSector, new List<Point> { p });
                    else if (!this.SectorCoords[this.currentSector].Contains(p)) this.SectorCoords[this.currentSector].Add(p);

                    if (this.marshalIndex != -1)
                    {
                        var array = this.MarshalZoneCoords.Where(x => x.Key.Item1 == this.marshalIndex).FirstOrDefault();
                        if (array.Value != null && !array.Value.Contains(p)) array.Value.Add(p);
                    }

                    if (this.drsIndex != -1 && this.lastDRSstatus != false)
                    {
                        if (!this.DRSZones.ContainsKey(this.drsIndex))
                        {
                            this.DRSZones.Add(drsIndex, new List<Point> { p });
                        }
                        else
                        {
                            var array = this.DRSZones[this.drsIndex];
                            if (!array.Contains(p)) array.Add(p);
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(() =>
                {
                    this.DrawMap(data);
                    this.Coordinates = player.WorldPosition.ToString();
                    this.canCarmotion = true;
                }, DispatcherPriority.Render);
            }
        }

        private void DrawMap(PacketMotionData data)
        {
            if (this.TrackCoords.Count > 1)
            {
                var d = this.col_map.ActualWidth;

                double ax, ay;

                var dx = u.ImageMiltiplier(this.TrackCoords.Select(x => x.X), d - 20, out ax);
                var dy = u.ImageMiltiplier(this.TrackCoords.Select(x => x.Y), d - 20, out ay);

                var m = dx;
                if (m > dy) m = dy;

                this.DrawPath(this.path_baseLine, ax, ay, m, this.TrackCoords);

                if (this.SectorCoords.ContainsKey(Sectors.Sector1)) this.DrawPath(this.path_sector1, ax, ay, m, this.SectorCoords[Sectors.Sector1]);
                if (this.SectorCoords.ContainsKey(Sectors.Sector2)) this.DrawPath(this.path_sector2, ax, ay, m, this.SectorCoords[Sectors.Sector2]);
                if (this.SectorCoords.ContainsKey(Sectors.Sector3)) this.DrawPath(this.path_sector3, ax, ay, m, this.SectorCoords[Sectors.Sector3]);

                if (this.marshalIndex > -1)
                {
                    for (int i = 0; i < this.MarshalZoneCoords.Count; i++)
                    {
                        var path = this.grid_marshalzones.Children[i] as Path;
                        var items = this.MarshalZoneCoords.Where(x => x.Key.Item1 == i).Select(x => x.Value).FirstOrDefault();
                        this.DrawPath(path, ax, ay, m, items);
                    }
                }

                if (this.drsIndex > -1)
                {
                    for (int i = 0; i < this.DRSZones.Count; i++)
                    {
                        var path = this.grid_drszones.Children[i] as Path;
                        this.DrawPath(path, ax, ay, m, this.DRSZones[i]);
                    }
                }

            }
            //throw new NotImplementedException();
        }

        private void DrawPath(Path p, double ax, double ay, double m, IEnumerable<Point> c)
        {
            p.Data = Geometry.Empty;
            var geometry = p.Data.GetFlattenedPathGeometry();
            Point last = new Point();

            foreach (var v in c)
            {
                if (last != zeroPoint)
                {
                    var tmp = new LineGeometry(
                            this.CalcPoint(last, ax, ay, m),
                            this.CalcPoint(v, ax, ay, m)
                        );
                    if (tmp.CanFreeze) tmp.Freeze();
                    geometry.AddGeometry(tmp);
                }

                last = v;
            }

            if (geometry.CanFreeze) geometry.Freeze();
            p.Data = geometry;
        }

        private Point CalcPoint(Point v, double midX, double midY, double multiply, bool canListenMinMax = true)
        {
            var x = 5 + (v.X - midX) * multiply;
            var y = 5 + (v.Y - midY) * multiply;
            var d = this.col_map.ActualWidth - 20;

            if (canListenMinMax)
            {
                if (x > this.maxX) this.maxX = x;
                if (x < this.minX) this.minX = x;
                if (y > this.maxY) this.maxY = y;
                if (y < this.minY) this.minY = y;
            }

            var ax = (this.minX + this.maxX) / 2;
            var ay = (this.minY + this.maxY) / 2;

            return new Point(x + d / 2 - ax, y + d / 2 - ay);
        }

        private bool CanRecord()
        {
            return this.isTimeTrial && this.lapNumber > 0;
        }

        private void Connention_SessionPacket(object sender, EventArgs e)
        {
            if (this.canSession)
            {
                this.canSession = false;
                var data = sender as PacketSessionData;
                this.Dispatcher.BeginInvoke(() =>
                {
                    this.year = data.Header.PacketFormat;
                    this.trackName = data.TrackID.ToString();
                    this.UpdateSession(data);
                    this.canSession = true;
                }, DispatcherPriority.Render);
            }
        }

        private void Connention_LapDataPacket(object sender, EventArgs e)
        {
            if (this.canLapdata)
            {
                this.canLapdata = false;
                var data = sender as PacketLapData;

                this.laptime = data.Lapdata[data.Header.Player1CarIndex].CurrentLapTime;
                this.Dispatcher.BeginInvoke(() =>
                {
                    this.UpdateLapdata(data);
                    this.canLapdata = true;
                }, DispatcherPriority.Render);
            }
        }

        private void UpdateSession(PacketSessionData packetSessionData)
        {
            if (packetSessionData.SessionType == SessionTypes.TimeTrial) this.isTimeTrial = true;
            else this.isTimeTrial = false;

            this.TrackID = packetSessionData.TrackID;
            this.PacketFormat = packetSessionData.Header.PacketFormat;

            if (this.MarshalZoneCoords.Count == 0)
            {
                SolidColorBrush b1 = new SolidColorBrush(Color.FromArgb(60, 200, 255, 0));
                if (b1.CanFreeze) b1.Freeze();
                SolidColorBrush b2 = new SolidColorBrush(Color.FromArgb(60, 255, 200, 0));
                if (b2.CanFreeze) b2.Freeze();

                for (int i = 0; i < packetSessionData.NumberOfMarshalZones; i++)
                {
                    this.MarshalZoneCoords.Add(new Tuple<int, float>(i, packetSessionData.MarshalZones[i].ZoneStartMeter), new List<Point>());
                    this.grid_marshalzones.Children.Add(new Path
                    {
                        Stroke = (i % 2 == 0 ? b1 : b2),
                        StrokeThickness = 3,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeDashCap = PenLineCap.Round,
                    });
                }

                this.UpdateGridColums();
            }
        }

        private void UpdateLapdata(PacketLapData packetLapData)
        {
            var player = packetLapData.Lapdata[packetLapData.Header.Player1CarIndex];

            if (this.lapNumber != 0 && this.lapNumber != player.CurrentLapNum) this.lapNumber = -1;

            if (player.CurrentLapTime > TimeSpan.Zero && player.DriverStatus == DriverSatuses.FlyingLap && this.lapNumber == 0) this.lapNumber = player.CurrentLapNum;

            this.currentSector = player.Sector;
            if (player.LapDistance >= 0 && player.DriverStatus == DriverSatuses.FlyingLap)
            {
                var items = this.MarshalZoneCoords.Where(x => x.Key.Item2 <= player.LapDistance).Select(x => x.Key);

                if (items != null)
                {
                    var index = items.Where(x => x.Item2 == items.Max(x => x.Item2)).FirstOrDefault();
                    if (index != null) this.marshalIndex = index.Item1;
                }
                else this.marshalIndex = 0;
            }
            else
            {
                this.marshalIndex = -1;
            }

            if (player.PitStatus != PitStatuses.None)
            {
                this.drsIndex = -1;
                this.lastDRSstatus = false;
            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.SubscribeUDPEvents();
            //this.DialogResult = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //this.DialogResult = false;
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            this.UnsubscribeUDPEvents();
            this.DialogResult = true;

            this.Map = new TrackLayout(this.year, this.trackName);
            this.Map.SetBaseline(this.TrackCoords);

            foreach (var item in this.DRSZones) this.Map.AppendDRS(item.Value);
            foreach (var item in this.MarshalZoneCoords) this.Map.AppendMarshal(item.Value);
            foreach (var item in this.SectorCoords) this.Map.AppendSector(item.Value);

            this.Close();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.UnsubscribeUDPEvents();
            this.Close();
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.IsLoaded) this.UpdateGridColums();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded) this.UpdateGridColums();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateGridColums();
        }

        private void UpdateGridColums()
        {
            var d = this.grid_main.ActualHeight;
            this.col_map.Width = new GridLength(d, GridUnitType.Pixel);
            d = d - 10;

            this.path_baseLine.Width = d;

            this.path_sector1.Width = d;
            this.path_sector3.Width = d;
            this.path_sector2.Width = d;

            this.grid_marshalzones.Width = d;
            this.grid_drszones.Width = d;

            this.minX = 0;
            this.minY = 0;
            this.maxX = 0;
            this.maxY = 0;
        }

        private void button_clear_Click(object sender, RoutedEventArgs e)
        {
            this.TrackCoords.Clear();
            this.SectorCoords.Clear();
            this.MarshalZoneCoords.Clear();

            this.lapNumber = 0;
            this.minX = 0;
            this.minY = 0;
            this.maxX = 0;
            this.maxY = 0;

            this.path_baseLine.Data = null;
            this.path_sector1.Data = null;
            this.path_sector2.Data = null;
            this.path_sector3.Data = null;

            this.grid_marshalzones.Children.Clear();

            this.drsIndex = -1;
            this.DRSZones.Clear();

            this.grid_drszones.Children.Clear();
        }

        private void checkbox_drs_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (this.checkbox_drs.IsChecked == true) this.grid_drszones.Visibility = Visibility.Visible;
                else this.grid_drszones.Visibility = Visibility.Hidden;
            }
        }

        private void checkbox_marhsal_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (this.checkbox_marhsal.IsChecked == true) this.grid_marshalzones.Visibility = Visibility.Visible;
                else this.grid_marshalzones.Visibility = Visibility.Hidden;
            }
        }

        private void checkbox_sectors_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (this.checkbox_sectors.IsChecked == true)
                {
                    this.path_sector1.Visibility = Visibility.Visible;
                    this.path_sector2.Visibility = Visibility.Visible;
                    this.path_sector3.Visibility = Visibility.Visible;
                }
                else
                {
                    this.path_sector1.Visibility = Visibility.Hidden;
                    this.path_sector2.Visibility = Visibility.Hidden;
                    this.path_sector3.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}
