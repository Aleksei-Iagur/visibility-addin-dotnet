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
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using VisibilityLibrary;
using VisibilityLibrary.Helpers;
using ProAppVisibilityModule.Helpers;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;

namespace ProAppVisibilityModule.ViewModels
{
    public class ProRLOSViewModel : ProLOSBaseViewModel
    {
        #region Properties

        public double SurfaceOffset { get; set; }
        public double MinDistance { get; set; }
        public double MaxDistance { get; set; }
        public double LeftHorizontalFOV { get; set; }
        public double RightHorizontalFOV { get; set; }
        public double BottomVerticalFOV { get; set; }
        public double TopVerticalFOV { get; set; }
        public bool ShowNonVisibleData { get; set; }
        public int RunCount { get; set; }

        private bool isCancelEnabled;
        public bool IsCancelEnabled
        {
            get { return isCancelEnabled; }
            set
            {
                isCancelEnabled = value;
                RaisePropertyChanged(() => IsCancelEnabled);
            }
        }

        private bool isOkEnabled = false;
        public bool IsOkEnabled
        {
            get { return isOkEnabled; }
            set
            {
                isOkEnabled = value;
                RaisePropertyChanged(() => IsOkEnabled);
            }
        }

        private bool isClearEnabled = false;
        public bool IsClearEnabled
        {
            get { return isClearEnabled; }
            set
            {
                isClearEnabled = value;
                RaisePropertyChanged(() => IsClearEnabled);
            }
        }

        private Visibility _displayProgressBar = Visibility.Collapsed;
        public Visibility DisplayProgressBar
        {
            get
            {
                return _displayProgressBar;
            }
            set
            {
                _displayProgressBar = value;
                RaisePropertyChanged(() => DisplayProgressBar);
            }
        }

        #endregion

        #region Commands

        public RelayCommand SubmitCommand { get; set; }

        private async void OnSubmitCommand(object obj)
        {
            DisplayProgressBar = Visibility.Visible;
            await CreateMapElement();
            DisplayProgressBar = Visibility.Hidden;
        }

        private async void OnCancelCommand(object obj)
        {
            await Reset(true);
        }

        private async void OnClearCommand(object obj)
        {
            await Reset(true);
        }



        #endregion

        /// <summary>
        /// One and only constructor
        /// </summary>
        public ProRLOSViewModel()
        {
            SurfaceOffset = 0.0;
            MinDistance = 0.0;
            MaxDistance = 1000;
            LeftHorizontalFOV = 0.0;
            RightHorizontalFOV = 360.0;
            BottomVerticalFOV = -90.0;
            TopVerticalFOV = 90.0;
            ShowNonVisibleData = false;
            RunCount = 1;
            IsClearEnabled = false;
            IsOkEnabled = false;
            IsCancelEnabled = false;
            DisplayProgressBar = Visibility.Hidden;

            // commands
            SubmitCommand = new RelayCommand(OnSubmitCommand);
            ClearGraphicsCommand = new RelayCommand(OnClearCommand);
            CancelCommand = new RelayCommand(OnCancelCommand);
        }

        #region override

        internal override void OnDeletePointCommand(object obj)
        {
            base.OnDeletePointCommand(obj);

            EnableOkCancelClearBtns(ObserverAddInPoints.Any());
        }

        internal override void OnDeleteAllPointsCommand(object obj)
        {
            base.OnDeleteAllPointsCommand(obj);

            EnableOkCancelClearBtns(ObserverAddInPoints.Any());
        }

        public override bool CanCreateElement
        {
            get
            {
                return (!string.IsNullOrWhiteSpace(SelectedSurfaceName)
                    && ObserverAddInPoints.Any());
            }
        }

        /// <summary>
        /// Where all of the work is done.  Override from TabBaseViewModel
        /// </summary>
        internal override async Task CreateMapElement()
        {
            try
            {
                IsRunning = true;

                if (!CanCreateElement || MapView.Active == null || MapView.Active.Map == null || string.IsNullOrWhiteSpace(SelectedSurfaceName))
                    return;

                await ExecuteVisibilityRLOS();

                //await base.CreateMapElement();
            }
            catch (Exception ex)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(VisibilityLibrary.Properties.Resources.ExceptionSomethingWentWrong,
                                                                VisibilityLibrary.Properties.Resources.CaptionError);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task ExecuteVisibilityRLOS()
        {
            try
            {
                await FeatureClassHelper.CreateLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, "POINT");

                // add fields for observer offset

                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetFieldName, "DOUBLE");
                await FeatureClassHelper.AddFieldToLayer(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.OffsetWithZFieldName, "DOUBLE");

                // add observer points to feature layer

                await FeatureClassHelper.CreatingFeatures(VisibilityLibrary.Properties.Resources.ObserversLayerName, ObserverAddInPoints, ConvertFromTo(OffsetUnitType, VisibilityLibrary.DistanceTypes.Meters, ObserverOffset.Value));

                // update with surface information

                await FeatureClassHelper.AddSurfaceInformation(VisibilityLibrary.Properties.Resources.ObserversLayerName, SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ZFieldName);

                await FeatureClassHelper.UpdateShapeWithZ(VisibilityLibrary.Properties.Resources.ObserversLayerName, VisibilityLibrary.Properties.Resources.ZFieldName, ObserverOffset.Value);

                await FeatureClassHelper.Delete(CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.RLOSOutputLayerName);


                // Visibility

                var surfaceSR = await GetSpatialReference(SelectedSurfaceName);

                var observerOffsetInMapZUnits = GetAsMapZUnits(surfaceSR, ObserverOffset.Value);
                var surfaceOffsetInMapZUnits = GetAsMapZUnits(surfaceSR, SurfaceOffset);
                var minDistanceInMapUnits = GetAsMapUnits(surfaceSR, MinDistance);
                var maxDistanceInMapUnits = GetAsMapUnits(surfaceSR, MaxDistance);
                var horizontalStartAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, LeftHorizontalFOV);
                var horizontalEndAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, RightHorizontalFOV);
                var verticalUpperAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, TopVerticalFOV);
                var verticalLowerAngleInDegrees = GetAngularDistanceFromTo(AngularUnitType, AngularTypes.DEGREES, BottomVerticalFOV);

                // TODO clamp angle values

                string maskFeatureClassName = CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.RLOSMaskLayerName;

                await CreateMask(maskFeatureClassName, maxDistanceInMapUnits);

                var environments = Geoprocessing.MakeEnvironmentArray(mask: maskFeatureClassName);

                await FeatureClassHelper.CreateVisibility(SelectedSurfaceName, VisibilityLibrary.Properties.Resources.ObserversLayerName,
                    CoreModule.CurrentProject.DefaultGeodatabasePath + "\\" + VisibilityLibrary.Properties.Resources.RLOSOutputLayerName,
                    observerOffsetInMapZUnits, surfaceOffsetInMapZUnits,
                    minDistanceInMapUnits, maxDistanceInMapUnits,
                    horizontalStartAngleInDegrees, horizontalEndAngleInDegrees,
                    verticalUpperAngleInDegrees, verticalLowerAngleInDegrees,
                    ShowNonVisibleData,
                    environments);

                //await Reset(true);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        /// <summary>
        /// Method to get spatial reference from a feature class
        /// </summary>
        /// <param name="fcName">name of feature class</param>
        /// <returns>SpatialReference</returns>
        private async Task<SpatialReference> GetSpatialReference(string fcName)
        {
            try
            {
                return await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(fcName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        return fcDefinition.GetSpatialReference();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Method used to create a mask for geoprocessing environment
        /// </summary>
        /// <param name="maskFeatureClassName"></param>
        /// <param name="bufferDistance"></param>
        /// <returns></returns>
        private async Task CreateMask(string maskFeatureClassName, double bufferDistance)
        {
            // delete old
            await FeatureClassHelper.Delete(maskFeatureClassName);
            // create new
            await FeatureClassHelper.CreateLayer(maskFeatureClassName, "POLYGON");

            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(maskFeatureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                foreach (var observer in ObserverAddInPoints)
                                {
                                    using (var rowBuffer = enterpriseFeatureClass.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        var polygon = GeometryEngine.Buffer(observer.Point, bufferDistance);
                                        rowBuffer[shapeFieldName] = polygon;

                                        using (var feature = enterpriseFeatureClass.CreateRow(rowBuffer))
                                        {
                                            //To Indicate that the attribute table has to be updated
                                            context.Invalidate(feature);
                                        }
                                    }
                                }
                            }
                            catch (GeodatabaseException exObj)
                            {
                                message = exObj.Message;
                            }
                        }, enterpriseFeatureClass);

                        creationResult = await editOperation.ExecuteAsync();
                        if (!creationResult)
                            message = editOperation.ErrorMessage;

                        await Project.Current.SaveEditsAsync();
                    }
                });
                if (!creationResult)
                    MessageBox.Show(message);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        internal override async Task Reset(bool toolReset)
        {
            await base.Reset(toolReset);

            if (MapView.Active == null)
                return;

            //// Disable buttons
            //EnableOkCancelClearBtns(false);
        }

        /// <summary>
        /// Override this event to collect observer points based on tool mode
        /// Setting the observer point to blue since the output is green / red
        /// </summary>
        /// <param name="obj"></param>
        internal override void OnNewMapPointEvent(object obj)
        {
            base.OnNewMapPointEvent(obj);

            if (!IsActiveTab)
                return;

            var point = obj as MapPoint;

            if (point == null)
                return;

            EnableOkCancelClearBtns(ObserverAddInPoints.Any());
        }

        #endregion

        #region public

        #endregion public

        #region private

        /// <summary>
        /// Enable or disable the form buttons
        /// </summary>
        /// <param name="enable">true to enable</param>
        private void EnableOkCancelClearBtns(bool enable)
        {
            IsOkEnabled = enable;
            IsCancelEnabled = enable;
            IsClearEnabled = enable;
        }

        /// <summary>
        /// Method to convert to/from different types of angular units
        /// </summary>
        /// <param name="fromType">DistanceTypes</param>
        /// <param name="toType">DistanceTypes</param>
        private double GetAngularDistanceFromTo(AngularTypes fromType, AngularTypes toType, double input)
        {
            double angularDistance = input;

            try
            {
                if (fromType == AngularTypes.DEGREES && toType == AngularTypes.GRADS)
                    angularDistance *= 1.11111;
                else if (fromType == AngularTypes.DEGREES && toType == AngularTypes.MILS)
                    angularDistance *= 17.777777777778;
                else if (fromType == AngularTypes.GRADS && toType == AngularTypes.DEGREES)
                    angularDistance /= 1.11111;
                else if (fromType == AngularTypes.GRADS && toType == AngularTypes.MILS)
                    angularDistance *= 16;
                else if (fromType == AngularTypes.MILS && toType == AngularTypes.DEGREES)
                    angularDistance /= 17.777777777778;
                else if (fromType == AngularTypes.MILS && toType == AngularTypes.GRADS)
                    angularDistance /= 16;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return angularDistance;
        }  

        #endregion
    }
}
