﻿// Copyright 2016 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ArcMapAddinVisibility.Helpers;
using System.Collections;

namespace ArcMapAddinVisibility.ViewModels
{
    public class LLOSViewModel : LOSBaseViewModel
    {
        public LLOSViewModel()
        {
            TargetPoints = new ObservableCollection<IPoint>();

            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);
        }

        #region Properties

        public ObservableCollection<IPoint> TargetPoints { get; set; }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        /// <summary>
        /// Method to handle the Submit/OK button command
        /// </summary>
        /// <param name="obj">null</param>
        private void OnSubmitCommand(object obj)
        {
            var savedCursor = System.Windows.Forms.Cursor.Current;
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            System.Windows.Forms.Application.DoEvents();
            // promote temp graphics
            MoveTempGraphicsToMapGraphics();

            CreateMapElement();

            Reset(true);

            System.Windows.Forms.Cursor.Current = savedCursor;
        }

        internal override void OnDeletePointCommand(object obj)
        {
            // take care of ObserverPoints
            base.OnDeletePointCommand(obj);

            // now lets take care of Target Points
            var items = obj as IList;
            var points = items.Cast<IPoint>().ToList();

            if (points == null)
                return;

            DeleteTargetPoints(points);
        }

        private void DeleteTargetPoints(List<IPoint> points)
        {
            // temp list of point's graphic element's guids
            var guidList = new List<string>();

            foreach (var point in points)
            {
                TargetPoints.Remove(point);

                // add to graphic element guid list for removal
                var kvp = GuidPointDictionary.FirstOrDefault(i => i.Value == point);

                guidList.Add(kvp.Key);
            }

            RemoveGraphics(guidList);

            foreach (var guid in guidList)
            {
                if (GuidPointDictionary.ContainsKey(guid))
                    GuidPointDictionary.Remove(guid);
            }
        }

        internal override void OnDeleteAllPointsCommand(object obj)
        {
            var mode = obj.ToString();

            if (string.IsNullOrWhiteSpace(mode))
                return;

            if (mode == Properties.Resources.ToolModeObserver)
                base.OnDeleteAllPointsCommand(obj);
            else if (mode == Properties.Resources.ToolModeTarget)
                DeleteTargetPoints(TargetPoints.ToList<IPoint>());
        }

        #endregion

        internal override void OnNewMapPointEvent(object obj)
        {
            base.OnNewMapPointEvent(obj);

            if (!IsActiveTab)
                return;

            var point = obj as IPoint;

            if (point == null)
                return;

            if (ToolMode == MapPointToolMode.Target)
            {
                TargetPoints.Insert(0, point);
                var color = new RgbColorClass() { Red = 255 } as IColor;
                var guid = AddGraphicToMap(point, color, true, esriSimpleMarkerStyle.esriSMSSquare);
                UpdatePointDictionary(point, guid);
            }
        }

        internal override void Reset(bool toolReset)
        {
            base.Reset(toolReset);

            if (ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return;

            // reset target points
            TargetPoints.Clear();
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName) 
                    && ObserverPoints.Any() 
                    && TargetPoints.Any()
                    && TargetOffset.HasValue);
            }
        }

        internal override void CreateMapElement()
        {
            if (!CanCreateElement || ArcMap.Document == null || ArcMap.Document.FocusMap == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                return;

 	        base.CreateMapElement();
            
            // take your observer and target points and get lines of sight

            var surface = GetSurfaceFromMapByName(ArcMap.Document.FocusMap, SelectedSurfaceName);

            if(surface == null)
                return;

            var geoBridge = new GeoDatabaseHelperClass() as IGeoDatabaseBridge2;

            if (geoBridge == null)
                return;

            IPoint pointObstruction = null;
            IPolyline polyVisible = null;
            IPolyline polyInvisible = null;
            bool targetIsVisible = false;

            double finalObserverOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, ObserverOffset.Value, surface.ZFactor, OffsetUnitType);
            double finalTargetOffset = GetOffsetInZUnits(ArcMap.Document.FocusMap, TargetOffset.Value, surface.ZFactor, OffsetUnitType);

            foreach (var observerPoint in ObserverPoints)
            {
                var z1 = surface.GetElevation(observerPoint) + finalObserverOffset;

                if (surface.IsVoidZ(z1))
                {
                    //TODO handle void z
                    continue;
                }

                foreach (var targetPoint in TargetPoints)
                {
                    var z2 = surface.GetElevation(targetPoint) + finalTargetOffset;

                    if (surface.IsVoidZ(z2))
                    {
                        //TODO handle void z
                        continue;
                    }

                    //SetZFactor(surface, observerPoint, targetPoint);
                    
                    geoBridge.GetLineOfSight(surface,
                        new PointClass() { Z = z1, X = observerPoint.X, Y = observerPoint.Y, ZAware = true },
                        new PointClass() { Z = z2, X = targetPoint.X, Y = targetPoint.Y, ZAware = true },
                        out pointObstruction, out polyVisible, out polyInvisible, out targetIsVisible, false, false);

                    if (polyVisible != null)
                    {
                        var rgbColor = new ESRI.ArcGIS.Display.RgbColorClass() as IRgbColor;
                        rgbColor.Green = 255;
                        AddGraphicToMap(polyVisible, rgbColor as IColor);
                    }

                    if (polyInvisible != null)
                    {
                        var rgbColor2 = new ESRI.ArcGIS.Display.RgbColorClass() as IRgbColor;
                        rgbColor2.Red = 255;
                        AddGraphicToMap(polyInvisible, rgbColor2);
                    }
                }
            }
        }

        private void SetZFactor(ISurface surface, IPoint observerPoint, IPoint targetPoint)
        {
            if (surface == null || observerPoint == null || targetPoint == null)
                return;

            double midLatitude = 0.0;
            if (ArcMap.Document.FocusMap.SpatialReference is IGeographicCoordinateSystem2)
            {
                var construct = new Polyline() as IConstructGeodetic;

                if (construct == null)
                    return;

                // if you don't use the activator, you will get exceptions
                Type srType = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
                var srf3 = Activator.CreateInstance(srType) as ISpatialReferenceFactory3;

                var linearUnit = srf3.CreateUnit((int)esriSRUnitType.esriSRUnit_Meter) as ILinearUnit;

                construct.ConstructGeodeticLineFromPoints(esriGeodeticType.esriGeodeticTypeGeodesic, observerPoint, targetPoint, linearUnit, esriCurveDensifyMethod.esriCurveDensifyByDeviation, -1.0);

                var polyline = construct as IPolyline;
                IPoint midPoint = new PointClass();
                polyline.QueryPoint(esriSegmentExtension.esriNoExtension, 0.5, true, midPoint);
                midLatitude = midPoint.Y;
            }

            surface.ZFactor = CalculateZFactor(ArcMap.Document.FocusMap.SpatialReference, midLatitude);
        }
    }
}
