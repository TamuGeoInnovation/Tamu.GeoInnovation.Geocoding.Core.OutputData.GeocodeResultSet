using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Drawing;
using USC.GISResearchLab.Geocoding.Core.Configurations;
using USC.GISResearchLab.Geocoding.Core.Metadata;
using USC.GISResearchLab.Common.Core.Geocoders.ReferenceDatasets.Sources.Interfaces;
using USC.GISResearchLab.Geocoding.Core.ReferenceDatasets.Sources.Implementations;
using USC.GISResearchLab.Common.Core.Databases;
using USC.GISResearchLab.Geocoding.Core.Metadata.ReferenceSources;
using USC.GISResearchLab.Geocoding.Core.Metadata.FeatureMatchingResults;
using USC.GISResearchLab.Geocoding.Core.Metadata.Qualities;
using USC.GISResearchLab.Core.WebServices.ResultCodes;
using System.Reflection;


namespace USC.GISResearchLab.Geocoding.Core.OutputData
{
    public class GeocodeResultSet
    {

        #region Properties
        public string Resultstring { get; set; }
        public QueryStatusCodes QueryStatusCodes { get; set; }
        public GeocodeCollection GeocodeCollection { get; set; }
        public GeocodeQualityType GeocodeQualityType { get; set; }

        public GeocodeStatistics Statistics { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TransactionId { get; set; }
        public string RecordId { get; set; }
        public string GeocoderName { get; set; }
        public string MicroMatchStatus { get; set; }

        public FeatureMatchingResultType FeatureMatchingResultType { get; set; }
        public int FeatureMatchingResultCount { get; set; }

        public string ErrorMessage { get; set; }
        public bool ExceptionOccurred { get; set; }

        [XmlIgnore]
        public Exception Exception { get; set; }

        public bool Valid
        {
            get
            {
                bool ret = false;
                if (GeocodeCollection.Geocodes.Count > 0)
                {
                    for (int i = 0; i < GeocodeCollection.Geocodes.Count; i++)
                    {
                        IGeocode geocode = GeocodeCollection.Geocodes[i];
                        if (geocode.Valid == true)
                        {
                            ret = true;
                            break;
                        }
                    }
                }

                return ret;
            }
        }

        public IGeocode BestGeocodeHierarchyFeatureType
        {
            get
            {
                IGeocode ret = null;
                if (GeocodeCollection != null)
                {
                    if (GeocodeCollection.Geocodes.Count > 0)
                    {
                        for (int i = 0; i < GeocodeCollection.Geocodes.Count; i++)
                        {
                            IGeocode geocode = GeocodeCollection.Geocodes[i];
                            if (geocode != null)
                            {
                                if (geocode.Valid == true && geocode.GeocodedError.ErrorBounds >= 0)
                                {
                                    ret = geocode;
                                    break;
                                }
                            }
                            else
                            {
                                string here = "";
                            }
                        }

                        // if the ret is null, none of the geocodes were valid - return the first one that was attempted
                        if (ret == null)
                        {
                            for (int i = 0; i < GeocodeCollection.Geocodes.Count; i++)
                            {
                                IGeocode geocode = GeocodeCollection.Geocodes[i];
                                if (geocode != null)
                                {
                                    if (geocode.Attempted)
                                    {
                                        ret = geocode;
                                        break;
                                    }
                                }
                                else
                                {
                                    string here = "";
                                }
                            }
                        }

                        // if the ret is still null, none of the geocodes were even attempted - return the first one
                        if (ret == null)
                        {
                            IGeocode geocode = GeocodeCollection.Geocodes[0];
                            if (geocode != null)
                            {
                                ret = GeocodeCollection.Geocodes[0];
                            }
                            else
                            {
                                string here = "";
                            }
                        }
                    }
                }
                else
                {
                    ret = new Geocode(2.94);
                }


                if (ret == null)
                {
                    ret = new Geocode(2.94);
                }

                if (ret != null)
                {

                    ret.TotalTimeTaken = TimeTaken;

                    if (Statistics != null)
                    {
                        ret.CompleteProcessStatistics = Statistics;
                    }
                    else
                    {
                        string here = "";
                    }
                }
                else
                {
                    string here = "";
                }

                return ret;
            }
        }

        public IGeocode BestGeocodeHierarchyUncertainty
        {
            get
            {
                IGeocode ret = null;
                IGeocode bestHierarchy = BestGeocodeHierarchyFeatureType;
                if (GeocodeCollection.Geocodes.Count > 0)
                {
                    double leastError = Double.MaxValue;
                    foreach (IGeocode geocode in GeocodeCollection.Geocodes)
                    {
                        if (geocode.Valid == true)
                        {
                            if (geocode.GeocodedError.ErrorBounds >= 0 && geocode.GeocodedError.ErrorBounds < leastError)
                            {
                                if (String.Compare(bestHierarchy.SourceType, geocode.SourceType, true) != 0) // don't compare the id's on the same geocode to itself
                                {
                                    string bestHierarchyId = bestHierarchy.MatchedFeature.PrimaryIdValue;
                                    string bestUncertaintyId = geocode.MatchedFeature.PrimaryIdValue;

                                    if (String.Compare(bestHierarchyId, bestUncertaintyId, true) == 0) // if the uncertainty and hierarchy found the same feature, go with the hierarchy (choose tiger over USPS tiger/zip)
                                    {
                                        // do nothing, will revert to hierarchy
                                    }
                                        // removed DG 2015-06-09 
                                        // TODO test to see if still needed
                                    //else if (bestHierarchy.FM_Result.ReferenceDatasetStatistics.ReferenceSourceQueryResultSet.SawCandidate(bestUncertaintyId)) // if the hierarchy found and rejected the uncertainty, go with the hierarchy (choose tiger over USPS tiger/zip)
                                    //{
                                    //    // do nothing, will revert to hierarchy
                                    //}
                                    else
                                    {
                                        leastError = geocode.GeocodedError.ErrorBounds;
                                        ret = geocode;
                                    }
                                }
                                else
                                {
                                    leastError = geocode.GeocodedError.ErrorBounds;
                                    ret = geocode;
                                }
                            }
                        }
                    }
                }

                if (ret == null)
                {
                    ret = bestHierarchy;
                }

                ret.TotalTimeTaken = TimeTaken;
                ret.CompleteProcessStatistics = Statistics;

                return ret;
            }
        }

        //PAYTON:MULTITHREADING Added this method due to HierarchyfeatureType no longer being sorted correctly due to how the threads return query results
        public IGeocode BestGeocodeHierarchyConfidence
        {
            get
            {

                try
                {

                    IGeocode ret = null;

                    List<IGeocode> tempList = new List<IGeocode>();
                    List<IGeocode> geocodes = new List<IGeocode>();
                    if (GeocodeCollection.Geocodes.Count > 0)
                    {
                        try
                        {
                            geocodes = GeocodeCollection.GetValidGeocodes();

                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error in BestGeocodeHierarchyConfidence: getValidGeocodes " + e.InnerException + " and msg: " + e.Message);
                        }
                        try
                        {
                            //PAYTON:MULTITHREADING-sort I'm concerned about the overhead of the sort here in batch processing - should be using sortbyconfidence()                            
                            tempList = geocodes.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ToList();

                        }
                        catch (Exception e)
                        {
                            throw new Exception("Error in OrderBy(d => d.NAACCRGISCoordinateQualityCode " + e.InnerException + " and msg: " + e.Message);
                        }

                        //This is nothing but a placeholder. It's an ok sort but we need to determine here how to determine <accept-reject-review> 


                    }
                    if (tempList.Count > 0)
                    {
                        foreach (IGeocode geocode in tempList)
                        {
                            if (geocode != null)
                            {
                                if (geocode.Valid == true && geocode.GeocodedError.ErrorBounds >= 0)
                                {
                                    ret = geocode;
                                    break;
                                }
                            }
                            else
                            {
                                string here = "";
                            }
                        }
                    }

                    if (ret == null)
                    {
                        ret = new Geocode(2.94);
                    }

                    ret.TotalTimeTaken = TimeTaken;
                    ret.CompleteProcessStatistics = Statistics;

                    return ret;
                }
                catch(Exception e)
                {
                    throw new Exception("Error in MAIN BestGeocodeHierarchyConfidence " + e.InnerException + " and msg: " + e.Message);
                }
            }
        }
        

        #endregion

        public GeocodeResultSet()
        {
            Statistics = new GeocodeStatistics();
            GeocodeCollection = new GeocodeCollection();
            QueryStatusCodes = QueryStatusCodes.Unknown;
            FeatureMatchingResultType = FeatureMatchingResultType.Unknown;
        }

        public void AddGeocode(IGeocode geocode)
        {
            GeocodeCollection.Geocodes.Add(geocode);
        }

        public void AddGeocodeList(List<IGeocode> geocodes)
        {
            try {
                GeocodeCollection.Geocodes.AddRange(geocodes);
            }
            catch (Exception e)
            {
                string excp = e.Message;
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool verbose)
        {
            StringBuilder ret = new StringBuilder();
            try
            {
                if (!verbose)
                {
                    ret.Append(GeocodeCollection);
                }
                else
                {
                    ret.AppendFormat("BestGeocodeHierarchyUncertainty: {0}", BestGeocodeHierarchyUncertainty);
                    ret.AppendLine();
                    ret.AppendFormat("BestGeocodeHierarchyFeatureType: {0}", BestGeocodeHierarchyFeatureType);
                    ret.AppendLine();
                    ret.AppendFormat("GeocodeCollection: {0}", GeocodeCollection);
                    ret.AppendLine();
                    ret.AppendFormat("Statistics: {0}", Statistics);
                }
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred converting GeocodeResult.ToString()", e);
            }


            return ret.ToString();
        }
       

        public List<IGeocode> SortByHierarchyUncertainty()
        {
            List<IGeocode> ret = new List<IGeocode>();
            if (GeocodeCollection.Geocodes.Count > 0)
            {
                List<IGeocode> geocodes = GeocodeCollection.GetValidGeocodes();
                ret = geocodes.OrderBy(x => x.GeocodedError.ErrorBounds).ToList();   
            }
            return ret;
        }


        public List<IGeocode> SortByHierarchyFeatureType()
        {
            return GeocodeCollection.GetValidGeocodes();            
        }

        //PAYTON:MULTITHREADING-SORT Added this as part of multithreading setup - will have every source query here to sort
        public List<IGeocode> SortByConfidence()
        {
            //return GeocodeCollection.GetValidGeocodes();
            List<IGeocode> ret = new List<IGeocode>();
            if (GeocodeCollection.Geocodes.Count > 0)
            {
                List<IGeocode> geocodes = GeocodeCollection.GetValidGeocodes();

                //Ideally we want to use the default order of preferred references here to get the best geocode                 
                
                //IFeatureSource[] referenceSources = BuildReferenceSources(geocoderConfiguration, geocoderConfiguration.OutputHierarchyConfiguration.FeatureMatchingHierarchyOrdering);

                //DefaultOrderedReferenceSourceTypes

                //This is nothing but a placeholder. It's an ok sort but we need to determine here how to determine <accept-reject-review> 
                ret = geocodes.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();
                this.GeocodeCollection.Geocodes.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore);
            }

            return ret;
        }

        public List<IGeocode> SortByConfidence(GeocoderConfiguration geocoderConfiguration)
        {
            //return GeocodeCollection.GetValidGeocodes();
            List<IGeocode> ret = new List<IGeocode>();
            if (GeocodeCollection.Geocodes.Count > 0)
            {
                List<IGeocode> geocodes = GeocodeCollection.GetValidGeocodes();

                var geoRefList = geocodes.ToList(); //this is the current geocode order
                var ReferenceDataSources = geocoderConfiguration.ReferenceDatasetConfiguration.ReferenceDataSources; //this is the preferred reference order

                foreach (var reference in ReferenceDataSources)
                {

                    PropertyInfo pi = reference.GetType().GetProperty("Name");
                    string refTxt = (String)(pi.GetValue(reference, null));                    
                    foreach (var geoRef in geoRefList)
                    {                        
                        if (refTxt.Contains(geoRef.SourceType))
                        {
                            ret.Add(geoRef);                            
                        }
                    }
                }
                //PAYTON:MULTITHREADING-sort at this point we have it sorted based on Preferred reference. We still need to select the 'best' geocode
            }
            return ret;
        }
        
        public List<IGeocode> SortByConfidence(List<IGeocode> geocodes)
        {
            List<IGeocode> ret = new List<IGeocode>();
            List<IGeocode> geocodeList = new List<IGeocode>();
            if (geocodes.Count > 0)            {
                
                int i = 0;
                try
                {
                    foreach (IGeocode g in geocodes)
                    {
                        if (!object.ReferenceEquals(null, g))
                        {
                            if (g.Valid)
                            {
                                geocodeList.Add(g);
                            }
                        }
                        i++;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("BOO in getValidGeocodes " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                }

                //This is nothing but a placeholder. It's an ok sort but we need to get a better sort here
                ret = geocodeList.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();
                
            }

            return ret;
        }

        public List<IGeocode> SortByConfidence(List<IGeocode> geocodes, GeocoderConfiguration geocoderConfiguration)
        {
            List<IGeocode> ret = new List<IGeocode>();
            List<IGeocode> geocodeList = new List<IGeocode>();
            if (geocodes.Count > 0)
            {

                int i = 0;
                try
                {
                    foreach (IGeocode g in geocodes)
                    {
                        if (!object.ReferenceEquals(null, g))
                        {
                            if (g.Valid)
                            {
                                geocodeList.Add(g);
                            }
                        }
                        i++;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("BOO in getValidGeocodes " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                }

                var geoRefList = geocodes.ToList(); //this is the current geocode order
                var ReferenceDataSources = geocoderConfiguration.ReferenceDatasetConfiguration.ReferenceDataSources; //this is the preferred reference order

                foreach (var reference in ReferenceDataSources)
                {

                    PropertyInfo pi = reference.GetType().GetProperty("Name");
                    string refTxt = (String)(pi.GetValue(reference, null));
                    foreach (var geoRef in geoRefList)
                    {
                        if (refTxt.Contains(geoRef.SourceType))
                        {
                            ret.Add(geoRef);
                            break;
                        }
                    }
                }
                //This is nothing but a placeholder. It's an ok sort but we need to get a better sort here
                //ret = geocodeList.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();

            }

            return ret;
        }

        //PAYTON:MICROMATCHSTATUS - we need to determine the actual micro match status here - this is just a placeholder
        public bool GetMicroMatchStatus()
        {
            bool ret = false;            
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            if (geocodes.Count > 0)
            {
                // Coordinate code should not be used here as a street segment should be a viable match as well as parcel, point etc
                //if (geocodes[0].NAACCRGISCoordinateQualityCode == "00" && geocodes[0].MatchScore > 90)
                if (geocodes[0].MatchScore > 88)
                {
                    if (geocodes[0].MatchedFeatureAddress.City != null && geocodes[0].MatchedFeatureAddress.ZIP != null)
                    {
                        if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() && geocodes[0].MatchedFeatureAddress.ZIP == geocodes[0].InputAddress.ZIP)
                        {
                            this.MicroMatchStatus = "Match";
                        }
                        else
                        {
                            this.MicroMatchStatus = "Review";
                            double avgDistance = getAverageDistance();
                            if (avgDistance < .05 && geocodes.Count > 5)
                            {
                                this.MicroMatchStatus = "Match";
                            }
                        }
                    }
                    else
                    {
                        this.MicroMatchStatus = "Review";
                    }
                }
                else //anything not match or review is returned as non-match
                {
                    this.MicroMatchStatus = "Non-Match";
                }
            }
            else //if no matches were found - return Non-match
            {
                this.MicroMatchStatus = "Non-Match";
            }
            return ret;
        }

        public double getAverageDistance()
        {
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            int num_points = geocodes.Count;
            PointF[] pts = new PointF[num_points + 1];
            //List<Point> normalPoints = new List<Point>();
            List<PointF> points = new List<PointF>();
            foreach (var resultPoint in geocodes)
            {
                //normalPoints.Add(new Point(Convert.ToInt32(resultPoint.Longitude), Convert.ToInt32(resultPoint.Latitude)));
                points.Add(new PointF(Convert.ToSingle(resultPoint.Longitude), Convert.ToSingle(resultPoint.Latitude)));
            }
            points.CopyTo(pts, 0);
            pts[num_points] = points[0];
            float area = 0;
            double distance = 0;
            for (int i = 0; i < num_points; i++)
            {
                //area +=
                //    (pts[i + 1].X - pts[i].X) *
                //    (pts[i + 1].Y + pts[i].Y) / 2;
                double dX = pts[0].X - pts[i + 1].X;
                double dY = pts[0].Y - pts[i + 1].Y;
                double multi = dX * dX + dY * dY;
                distance = distance + Math.Round(Math.Sqrt(multi), 3);
            }
            double distanceAvg = ((distance) / num_points) * 100;
            return distanceAvg;
        }
        public bool GetMicroMatchStatus(GeocoderConfiguration geocoderConfiguration)
        {
            bool ret = false;
            //            

            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn, geocoderConfiguration);
            if (geocodes.Count > 0)
            {
                // Coordinate code should not be used here as a street segment should be a viable match as well as parcel, point etc
                //if (geocodes[0].NAACCRGISCoordinateQualityCode == "00" && geocodes[0].MatchScore > 90)
                if (geocodes[0].MatchScore > 88)
                {
                    if (geocodes[0].MatchedFeatureAddress.City != null && geocodes[0].MatchedFeatureAddress.ZIP != null)
                    {
                        if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() && geocodes[0].MatchedFeatureAddress.ZIP == geocodes[0].InputAddress.ZIP)
                        {
                            this.MicroMatchStatus = "Match";
                        }
                        else
                        {
                            this.MicroMatchStatus = "Review";
                            double avgDistance = getAverageDistance();
                            //If the average distance is less than 1/5 of a mile - assume it's a good match
                            //Adding a count check as well to account for all navteq references to return a non-valid match but all the same coords
                            //if count is > 5 it's safe to assume that multiple references are reporting the same location for the address
                            if (avgDistance < .05 && geocodes.Count > 5)
                            {
                                this.MicroMatchStatus = "Match";
                            }
                        }
                    }
                    else
                    {
                        this.MicroMatchStatus = "Review";
                    }
                }
                else //anything not match or review is returned as non-match
                {
                    this.MicroMatchStatus = "Non-Match";
                }
            }
            else //anything not match or review is returned as non-match
            {
                this.MicroMatchStatus = "Non-Match";
            }
            return ret;
        }
    }
}
