﻿using F1TelemetryApp.Classes;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace F1TelemetryApp.UserControls
{
    /// <summary>
    /// Interaction logic for TyreDataControl.xaml
    /// </summary>
    public partial class TyreDataControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        private double wear;
        private double demage;
        private double breakTemperature;
        private double condition;
        private double tyreInnerTemperature;
        private double tyreSurfaceTemperature;
        private double pressure;
        public TyreDataControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private Brush tyreConditionForeground;

        public Brush TyreConditionForeground
        {
            get { return tyreConditionForeground; }
            private set
            {
                if (value != tyreConditionForeground)
                {
                    this.tyreConditionForeground = value;
                    this.OnPropertyChanged("TyreConditionForeground");
                }
            }
        }


        public double Wear
        {
            get
            {
                return this.wear;
            }
            set
            {
                if (value != this.wear)
                {
                    this.wear = value;
                    this.Condition = 100.0 - this.Wear;

                    this.TyreConditionForeground = new SolidColorBrush(this.ColorMapTyreCondition.GradientStops.GetRelativeColor(this.Condition / 100.0));
                    //this.textBlock_wear.Text = this.wear.ToString("0.##");

                    this.OnPropertyChanged("Wear");
                }
            }
        }

        public double Demage
        {
            get
            {
                return this.demage;
            }
            set
            {
                if (value != this.demage)
                {
                    this.demage = value;
                    //this.textBlock_demage.Text = this.demage.ToString("0.##");

                    this.OnPropertyChanged("Demage");
                }
            }
        }

        public double Condition
        {
            get
            {
                return this.condition;
            }
            set
            {
                if (value != this.condition)
                {
                    this.condition = value;
                    this.progreassBar_Condition.Value = this.condition;
                    //this.textBlock_condition.Text = this.condition.ToString("0.##");
                    this.OnPropertyChanged("Condition");
                }
            }
        }

        public double BrakesTemperature
        {
            get
            {
                return this.breakTemperature;
            }
            set
            {
                if (value != this.breakTemperature)
                {
                    this.breakTemperature = value;
                    //this.textBlock_brake.Text = this.breakTemperature.ToString();
                    this.OnPropertyChanged("BrakesTemperature");
                }
            }
        }
        public double TyreInnerTemperature
        {
            get
            {
                return this.tyreInnerTemperature;
            }
            set
            {
                if (value != this.tyreInnerTemperature)
                {
                    this.tyreInnerTemperature = value;
                    //this.textBlock_inner.Text = this.tyreInnerTemperature.ToString();
                    this.OnPropertyChanged("TyreInnerTemperature");
                }
            }
        }
        public double TyreSurfaceTemperature
        {
            get
            {
                return this.tyreSurfaceTemperature;
            }
            set
            {
                if (value != this.tyreSurfaceTemperature)
                {
                    this.tyreSurfaceTemperature = value;
                    //this.textBlock_surface.Text = this.tyreSurfaceTemperature.ToString();
                    this.OnPropertyChanged("TyreSurfaceTemperature");
                }
            }
        }
        public double Pressure
        {
            get
            {
                return this.pressure;
            }
            set
            {
                if (value != this.pressure)
                {
                    this.pressure = value;
                    //this.textBlock_pressure.Text = this.pressure.ToString("0.##");
                    this.OnPropertyChanged("Pressure");
                }
            }
        }

        private Side progressbarSide;
        private bool disposedValue;

        public Side ProgressbarSide
        {
            get { return this.progressbarSide; }
            set
            {
                if (value != this.progressbarSide)
                {
                    this.progressbarSide = value;
                }
            }
        }


        public LinearGradientBrush ColorMapTyreCondition { get; set; } = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            if (this.ColorMapTyreCondition == null)
            {
                var colors = new GradientStopCollection();
                // red range
                colors.Add(new GradientStop(Color.FromRgb(255, 0, 0), 0));
                colors.Add(new GradientStop(Color.FromRgb(255, 0, 0), 0.33));
                // yellow range
                colors.Add(new GradientStop(Color.FromRgb(255, 187, 51), 0.6));
                colors.Add(new GradientStop(Color.FromRgb(255, 187, 51), 0.66));
                // green range
                colors.Add(new GradientStop(Colors.Green, 0.9));
                colors.Add(new GradientStop(Colors.Green, 1));

                this.ColorMapTyreCondition = new LinearGradientBrush(colors);
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            switch (this.ProgressbarSide)
            {
                case Side.Left:
                    this.grid_container.ColumnDefinitions[0].Width = new System.Windows.GridLength(55, System.Windows.GridUnitType.Pixel);
                    this.grid_container.ColumnDefinitions[1].Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                    Grid.SetColumn(this.grid_progress, 0);
                    Grid.SetColumn(this.grid_data, 1);
                    break;
                case Side.Right:
                    this.grid_container.ColumnDefinitions[0].Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                    this.grid_container.ColumnDefinitions[1].Width = new System.Windows.GridLength(55, System.Windows.GridUnitType.Pixel);
                    Grid.SetColumn(this.grid_progress, 1);
                    Grid.SetColumn(this.grid_data, 0);
                    break;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    this.TyreConditionForeground = null;
                    this.ColorMapTyreCondition = null;
                    this.PropertyChanged = null;
                }

                this.tyreConditionForeground = null;
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TyreDataControl()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public enum Side
    {
        Left,
        Right
    }
}