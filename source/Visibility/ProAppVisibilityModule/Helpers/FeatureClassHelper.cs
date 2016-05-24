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

using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Mapping;
using ProAppVisibilityModule.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProAppVisibilityModule.Helpers
{
    public class FeatureClassHelper
    {
        /// <summary>
        /// Create a feature class in the default geodatabase of the project.
        /// </summary>
        /// <param name="featureclassName">Name of the feature class to be created.</param>
        /// <param name="featureclassType">Type of feature class to be created. Options are:
        /// <list type="bullet">
        /// <item>POINT</item>
        /// <item>MULTIPOINT</item>
        /// <item>POLYLINE</item>
        /// <item>POLYGON</item></list></param>
        /// <returns></returns>
        public static async Task CreateLayer(string featureclassName, string featureclassType)
        {
            List<object> arguments = new List<object>();
            // store the results in the default geodatabase
            arguments.Add(CoreModule.CurrentProject.DefaultGeodatabasePath);
            // name of the feature class
            arguments.Add(featureclassName);
            // type of geometry
            arguments.Add(featureclassType);
            // no template
            arguments.Add("");
            // m values
            arguments.Add("DISABLED");
            // z values
            arguments.Add("ENABLED");

            arguments.Add(MapView.Active.Map.SpatialReference);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management", Geoprocessing.MakeValueArray(arguments.ToArray()));
        }

        public static async Task AddFieldToLayer(string tableName, string fieldName, string fieldType)
        {
            List<object> arguments = new List<object>();
            // in_table
            arguments.Add(tableName);
            // field_name
            arguments.Add(fieldName);
            // field_type
            arguments.Add(fieldType);
            // field_precision
            arguments.Add("");
            // field_scale
            arguments.Add("");
            // field_length
            arguments.Add("");
            // field_alias
            arguments.Add("");
            // field_is_nullable
            arguments.Add("");
            // field_is_required
            arguments.Add("");
            // field_domain
            arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("AddField_management", Geoprocessing.MakeValueArray(arguments.ToArray()));
        }

        public static async Task CreateSightLines(string observersFeatureLayer, 
                                                    string targetsFeatureLayer, 
                                                    string outLineFeatureLayer, 
                                                    string observerOffsetFieldName, 
                                                    string targetOffsetFieldName)
        {
            List<object> arguments = new List<object>();
            // in_observer_points
            arguments.Add(observersFeatureLayer);
            // in_target_features
            arguments.Add(targetsFeatureLayer);
            // out_line_feature_class
            arguments.Add(outLineFeatureLayer);
            // observer_height_field (Optional)
            arguments.Add(observerOffsetFieldName);
            // target_height_field (Optional)
            arguments.Add(targetOffsetFieldName);
            // join_field (Optional)
            arguments.Add("");
            // sample_distance (Optional)
            arguments.Add("1");
            // output_the_direction (Optional) bool
            arguments.Add("");
            IGPResult result = await Geoprocessing.ExecuteToolAsync("ConstructSightLines_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), flags: GPExecuteToolFlags.Default);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }
        public static async Task AddSurfaceInformation(string featureClass, string surface, string outProperty)
        {
            //AddSurfaceInformation_3d
            List<object> arguments = new List<object>();
            // in_feature_class
            arguments.Add(featureClass);
            // in_surface
            arguments.Add(surface);
            // out_property
            arguments.Add(outProperty);

            IGPResult result = await Geoprocessing.ExecuteToolAsync("AddSurfaceInformation_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), flags: GPExecuteToolFlags.Default);

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        public static async Task CreateLOS(string surfaceName, 
                                            string lineFeatureClassName, 
                                            string outLOSFeatureClass)
        {
            List<object> arguments = new List<object>();
            // in_surface
            arguments.Add(surfaceName);
            // in_line_feature_class
            arguments.Add(lineFeatureClassName);
            // out_los_feature_class
            arguments.Add(outLOSFeatureClass);
            // out_obstruction_feature_class (Optional)
            //arguments.Add("");
            //// use_curvature (Optional)
            //arguments.Add("");
            //// use_refraction (Optional)
            //arguments.Add("");
            //// refraction_factor (Optional)
            //arguments.Add("");
            //// pyramid_level_resolution (Optional)
            //arguments.Add("");
            //// in_features (optional) multipatch features
            //arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("LineOfSight_3d", Geoprocessing.MakeValueArray(arguments.ToArray()), flags: GPExecuteToolFlags.Default);

            if(result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        public static async Task Delete(string name)
        {
            List<object> arguments = new List<object>();
            // in_data
            arguments.Add(name);
            // data_type
            arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("Delete_management", Geoprocessing.MakeValueArray(arguments.ToArray()));

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }

        public static async Task CreateVisibility(string surfaceName, string observerFeatureClassName, string outRLOSFeatureClass)
        {
            //Visibility (in_raster, in_observer_features, {out_agl_raster}, {analysis_type}, {nonvisible_cell_value}, {z_factor}, 
            // {curvature_correction}, {refractivity_coefficient}, {surface_offset}, {observer_elevation}, {observer_offset}, {inner_radius}, 
            // {outer_radius}, {horizontal_start_angle}, {horizontal_end_angle}, {vertical_upper_angle}, {vertical_lower_angle})
            List<object> arguments = new List<object>();
            // in_raster
            arguments.Add(surfaceName);
            // in_observer_features
            arguments.Add(observerFeatureClassName);
            // out_rlos_feature_class
            arguments.Add(outRLOSFeatureClass);
            // out_agl_raster
            arguments.Add("");
            // analysis_type
            arguments.Add("FREQUENCY");
            // nonvisible_cell_value
            arguments.Add("");
            // z_factor
            arguments.Add(1.0);
            // curvature_correction
            arguments.Add("FALSE");
            // refractivity_coefficient
            arguments.Add(0.13);
            // surface_offset
            arguments.Add("");
            // observer_elevation
            arguments.Add("");
            // observer_offset
            arguments.Add("");
            // inner_radius
            arguments.Add("");
            // outer_radius
            arguments.Add("");
            // horizontal_start_angle
            arguments.Add("");
            // horizontal_end_angle
            arguments.Add("");
            // vertical_upper_angle
            arguments.Add("");
            // vertical_lower_angle
            arguments.Add("");

            IGPResult result = await Geoprocessing.ExecuteToolAsync("Visibility_3d", Geoprocessing.MakeValueArray(arguments.ToArray()));

            if (result.IsFailed)
            {
                foreach (var msg in result.Messages)
                    Debug.Print(msg.Text);
            }
        }


        public static async Task CreatingFeatures(string featureClassName, ObservableCollection<AddInPoint> collection, double offset)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                foreach (var item in collection)
                                {
                                    using (var rowBuffer = enterpriseFeatureClass.CreateRowBuffer())
                                    {
                                        // Either the field index or the field name can be used in the indexer.
                                        rowBuffer[VisibilityLibrary.Properties.Resources.OffsetFieldName] = offset;
                                        var point = MapPointBuilder.CreateMapPoint(item.Point.X, item.Point.Y, 0.0, item.Point.SpatialReference);
                                        rowBuffer[shapeFieldName] = point;

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

        public static async Task UpdateShapeWithZ(string featureClassName, string zFieldName, double offsetInMeters)
        {
            try
            {
                string message = String.Empty;
                bool creationResult = false;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(async () =>
                {
                    using (Geodatabase geodatabase = new Geodatabase(CoreModule.CurrentProject.DefaultGeodatabasePath))
                    using (FeatureClass enterpriseFeatureClass = geodatabase.OpenDataset<FeatureClass>(featureClassName))
                    using (FeatureClassDefinition fcDefinition = enterpriseFeatureClass.GetDefinition())
                    {
                        int zFieldIndex = fcDefinition.FindField(zFieldName);

                        EditOperation editOperation = new EditOperation();
                        editOperation.Callback(context =>
                        {
                            try
                            {
                                var shapeFieldName = fcDefinition.GetShapeField();

                                using (RowCursor rowCursor = enterpriseFeatureClass.Search(null, false))
                                {
                                    while (rowCursor.MoveNext())
                                    {
                                        using (Feature feature = (Feature)rowCursor.Current)
                                        {
                                            context.Invalidate(feature);
                                            var mp = (MapPoint)feature[shapeFieldName];
                                            var z = (Double)feature[zFieldIndex] + offsetInMeters;
                                            feature[VisibilityLibrary.Properties.Resources.OffsetWithZFieldName] = z;
                                            feature.SetShape(MapPointBuilder.CreateMapPoint(mp.X, mp.Y, z, mp.SpatialReference));

                                            feature.Store();

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

    }
}
