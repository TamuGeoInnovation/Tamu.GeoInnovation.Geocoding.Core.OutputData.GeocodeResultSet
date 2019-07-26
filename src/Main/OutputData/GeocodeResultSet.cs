using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Drawing;
using System.Device.Location;
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
using USC.GISResearchLab.AddressProcessing.Core.Standardizing.StandardizedAddresses.Lines.LastLines;
using Tamu.GeoInnovation.Geocoding.Core.Algorithms.PenaltyScoring;
using USC.GISResearchLab.Geocoding.Core.Algorithms.FeatureMatchScorers.MatchScoreResults;
using USC.GISResearchLab.Common.Core.TextEncodings.Soundex;
using USC.GISResearchLab.Common.Core.Geocoders.GeocodingQueries.Options;
using USC.GISResearchLab.Common.Core.JSON;
using TAMU.GeoInnovation.PointIntersectors.Census.OutputData.CensusRecords;

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
        public PenaltyCodeResult PenaltyCodeResult { get; set; }
        public string PenaltyCode { get; set; }
        public string PenaltyCodeSummary { get; set; }
        //X7-93 - these were added for calculation purposes in this class but should not be included in geocode results output
        private int parcelMatches = 0;
        private int streetMatches = 0;
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
                    IGeocode bestHierarchy = BestGeocodeHierarchyFeatureType;
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
                    else
                    {
                        
                    }

                    if (ret == null)
                    {
                        ret = bestHierarchy;
                    }

                    ret.TotalTimeTaken = TimeTaken;
                    ret.CompleteProcessStatistics = Statistics;

                    return ret;
                }
                catch (Exception e)
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

                //PAYTON:MULTITHREADING-sort at this point we have it sorted based on Preferred reference. We still need to select the 'best' geocode
                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP & ret[0].MatchScore>60) //BUG:X7-49 Added logic to only perform this if return is better than zipcode level, else use area weighting
                    {
                        double score = ret[0].MatchScore;
                        int i = 0;
                        try
                        {
                            foreach (IGeocode g in ret)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore>90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in checkForBetterMatch " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                    else //if first address zip is correct then there is no need to test remaining geocodes for better match
                    {
                        //BUG:X7-59 This is updated - should only do this when uncertainty hierarchy is checked. This is done in SortByHierarchyUncertainty()
                        //if Address is not better than zipcode level use area weighting to determine best geocode
                        //if (ret[0].MatchScore <= 60)
                        //{
                        //    //BUG:X7-59 Issue here is that our zipcode returns are points and not polygons so the area is 0. *update using area from ZCTA
                        //    ret = geocodes.OrderBy(x => x.GeocodedError.ErrorBounds).ToList();
                        //}
                    }

                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
            }
            GeocodeCollection.Geocodes = ret;
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
                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                    {
                        double score = ret[0].MatchScore;
                        int i = 0;
                        try
                        {
                            foreach (IGeocode g in ret)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore > 90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in checkForBetterMatch " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                    else //if first address zip is correct then there is no need to test remaining geocodes for better match
                    {
                        //do nothing
                    }
                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
                if(ret.Count<=0)
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
            }           
            return ret;
        }

        public List<IGeocode> SortByConfidence(List<IGeocode> geocodes)
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

                //This is nothing but a placeholder. It's an ok sort but we need to get a better sort here                
                ret = geocodeList.OrderBy(d => d.NAACCRGISCoordinateQualityCode).ThenByDescending(d => d.MatchScore).ToList();

                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                {
                    double score = ret[0].MatchScore;
                    i = 0;
                    try
                    {
                        foreach (IGeocode g in geocodes)
                        {
                            if (!object.ReferenceEquals(null, g))
                            {
                                //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore > 90)
                                //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                {
                                    ret.RemoveAt(i);
                                    ret.Insert(0, g);
                                    break;
                                }
                            }
                            i++;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("BOO in getValidGeocodes " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                    }
                 }
                //if zip is the same there is no need to check remaining geocodes
                else
                {
                    //do nothing
                }
            }
            //if no valid geocodes exist ret needs to add top to be unmatchable
            else
            {
                ret.Add(GeocodeCollection.Geocodes[0]);
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
                    throw new Exception("BOO in SortByConfidence " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
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
                //PAYTON:v4.03 Updating to return lower level street match if the zipcode matches input zipcode
                if (geocodes.Count > 0)
                {
                    if (ret[0].MatchedFeatureAddress.ZIP != ret[0].InputAddress.ZIP)
                    {
                        double score = ret[0].MatchScore;
                        i = 0;
                        try
                        {
                            foreach (IGeocode g in ret)
                            {
                                if (!object.ReferenceEquals(null, g))
                                {
                                    //Payton:v4.04 added logic to account for reference sources not having a zip but matchscore indicates good match
                                    if ((g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score) || g.MatchedFeatureAddress.ZIP == "" && g.MatchScore > 90)
                                    //if (g.MatchedFeatureAddress.ZIP == g.InputAddress.ZIP && g.MatchScore > score)
                                    {
                                        ret.RemoveAt(i);
                                        ret.Insert(0, g);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("BOO in checkForBetterMatch " + e.InnerException + " and msg: " + e.Message + "and record is: " + Convert.ToString(i) + "and value1 is: " + geocodes[i - 1].ToString() + "and value2 is: " + geocodes[i].ToString() + "and value2 is: " + geocodes[i + 1].ToString());
                        }
                    }
                }
                //if no valid geocodes exist ret needs to add top to be unmatchable
                else
                {
                    ret.Add(GeocodeCollection.Geocodes[0]);
                }
            }

            return ret;
        }

        //PAYTON:MICROMATCHSTATUS - we need to determine the actual micro match status here - this is just a placeholder
        public bool GetMicroMatchStatus()
        {
            bool ret = false;
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            if (geocodes != null)            {
                
                if (geocodes.Count > 0)
                {
                    //PAYTON:PenaltyCode
                    //if (geocodes[0].Version >= 4.4)
                    //{
                    //    this.PenaltyCodeResult = new PenaltyCodeResult();
                    //}
                    // Coordinate code should not be used here as a street segment should be a viable match as well as parcel, point etc
                    //if (geocodes[0].NAACCRGISCoordinateQualityCode == "00" && geocodes[0].MatchScore > 90)
                    this.PenaltyCodeResult = new PenaltyCodeResult(); //even though penalty code won't be displayed for < 4.4 we still need it here to prevent errors
                    if (geocodes[0].MatchScore < 100)
                    {
                        if (geocodes[0].MatchScore > 84)
                        {
                            if (geocodes[0].MatchedFeatureAddress.City != null && geocodes[0].MatchedFeatureAddress.ZIP != null)
                            {
                                //BUG:X7-88 We need to account for city alias here as well if using alias table                                    
                                if ((geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() || CityUtils.isValidAlias(geocodes[0].InputAddress.City.ToUpper(), geocodes[0].MatchedFeatureAddress.City.ToUpper(), geocodes[0].MatchedFeatureAddress.State)) && geocodes[0].InputAddress.ZIP == geocodes[0].MatchedFeatureAddress.ZIP && geocodes[0].MatchScore > 95)
                                {
                                    this.MicroMatchStatus = "Match";
                                }
                                //Here we need to check against other results
                                //if city is correct but zip is not, check other results
                                else if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() || CityUtils.isValidAlias(geocodes[0].InputAddress.City.ToUpper(), geocodes[0].MatchedFeatureAddress.City.ToUpper(), geocodes[0].MatchedFeatureAddress.State))                                
                                {                                    
                                    this.MicroMatchStatus = "Review";
                                    //double avgDistance = getAverageDistance();
                                    parcelMatches = 0;
                                    streetMatches = 0;
                                    double avgParcelDistance = getAverageDistance("parcel");
                                    double avgStreetDistance = getAverageDistance("street");
                                    //if (avgDistance < .05 && geocodes.Count > 5 && getCensusMatchStatus())
                                    //{
                                    //    this.MicroMatchStatus = "Match";
                                    //}
                                    if (avgParcelDistance < 10 && parcelMatches > 1 && getCensusMatchStatus())
                                    {
                                        this.MicroMatchStatus = "Match";                                        
                                    }                                   
                                    if (parcelMatches == 0 && streetMatches > 1 && avgStreetDistance < 10 && getCensusMatchStatus())
                                    {
                                        this.MicroMatchStatus = "Match";                                      
                                    }
                                    else
                                    {
                                        if (geocodes[0].MatchedFeatureAddress.ZIP != geocodes[0].InputAddress.ZIP && geocodes[0].Version >= 4.4)
                                        {
                                            this.PenaltyCodeResult.zipPenaltySummary = "R";
                                        }
                                    }
                                    if (geocodes[0].Version >= 4.4)
                                    {
                                        if (avgParcelDistance > 0 || avgStreetDistance > 0)
                                        {
                                            getDistancePenalty((avgParcelDistance + avgStreetDistance) / 2);
                                        }
                                        else
                                        {
                                            this.PenaltyCodeResult.distance = "M";
                                            this.PenaltyCodeResult.distanceSummary = "M";
                                        }
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
                    else //if we reach here then matchscore is 100 and we return a "Match"
                    {                        
                        this.MicroMatchStatus = "Match";
                    }
                    //PAYTON:PENALTYCODE City  **Done all in SingleThreadedFeature....                  
                    //if (geocodes[0].Version >= 4.4)
                    //{
                    //    assignPenaltyCode(geocodes);
                    //}
                }
                else //if no matches were found - return Non-match
                {
                    this.MicroMatchStatus = "Non-Match";
                }
                //PAYTON:PenaltyCode - only available in version 4.04 and after
                if (geocodes.Count>0 && geocodes[0].Version >= 4.4)
                {
                 //penalty already assigned   
                }
                else //need to set empty penalty to prevent null here if not already assigned
                {
                    this.PenaltyCodeResult = new PenaltyCodeResult();
                    this.PenaltyCode = this.PenaltyCodeResult.getPenaltyString();
                    this.PenaltyCodeSummary = this.PenaltyCodeResult.getPenaltyStringSummary();
                }
            }
            else
            {
                this.MicroMatchStatus = "Non-Match";
                this.PenaltyCodeResult = new PenaltyCodeResult();
                this.PenaltyCode = this.PenaltyCodeResult.getPenaltyString();
                this.PenaltyCodeSummary = this.PenaltyCodeResult.getPenaltyStringSummary();
            }
            this.GeocodeCollection.Geocodes = geocodes;
            return ret;
        }

        //public void assignPenaltyCode(List<IGeocode> geocodes)
        //{

        //    string inputCity = "";
        //    string featureCity = "";
        //    string inputState = "";
        //    if (geocodes[0].InputAddress.City != null)
        //    {
        //        inputCity = geocodes[0].InputAddress.City;
        //    }
        //    if (geocodes[0].InputAddress.State != null)
        //    {
        //        inputState = geocodes[0].InputAddress.State;
        //    }
        //    if (geocodes[0].MatchedFeatureAddress.City != null)
        //    {
        //        featureCity = geocodes[0].MatchedFeatureAddress.City;
        //    }
        //    string inputCitySoundex = SoundexEncoder.ComputeEncodingNew(inputCity);
        //    string featureCitySoundex = SoundexEncoder.ComputeEncodingNew(featureCity);
        //    if (inputCity.ToUpper() != featureCity.ToUpper())
        //    {
        //        if (CityUtils.isValidAlias(inputCity, featureCity, inputState))
        //        {
        //            this.PenaltyCodeResult.city = "1";
        //        }
        //        else if (inputCitySoundex == featureCitySoundex)

        //        {
        //            this.PenaltyCodeResult.city = "2";
        //        }
        //        else
        //        {
        //            this.PenaltyCodeResult.city = "3";
        //        }
        //    }
        //    Dictionary<string, string> scoreResult = new Dictionary<string, string>();
        //    var matchedScoreResults = new List<MatchScorePenaltyResult>();
        //    if (geocodes[0].MatchedFeature.MatchScoreResult != null)
        //    {
        //        matchedScoreResults = geocodes[0].MatchedFeature.MatchScoreResult.MatchScorePenaltyResults;
        //    }
        //    foreach (var penalty in matchedScoreResults)
        //    {
        //        scoreResult.Add(penalty.AddressComponent.ToString(), penalty.PenaltyValue.ToString());
        //    }
        //    string inputStreet = "";
        //    string featureStreet = "";
        //    try
        //    {
        //        string pre = "0";
        //        string post = "0";
        //        if (scoreResult.ContainsKey("PreDirectional"))
        //        {
        //            pre = scoreResult["PreDirectional"];
        //        }
        //        if (scoreResult.ContainsKey("PostDirectional"))
        //        {
        //            post = scoreResult["PostDirectional"];
        //        }
        //        if(geocodes[0].InputAddress != null)
        //        {

        //        }
        //        if(geocodes[0].InputAddress != null)
        //        {
        //            inputStreet = geocodes[0].InputAddress.PreDirectional + " " + geocodes[0].InputAddress.StreetName + " " + geocodes[0].InputAddress.PostDirectional;
        //        }
        //        if(geocodes[0].MatchedFeatureAddress != null)
        //        {
        //            featureStreet = geocodes[0].MatchedFeatureAddress.PreDirectional + " " + geocodes[0].MatchedFeatureAddress.StreetName + " " + geocodes[0].MatchedFeatureAddress.PostDirectional;
        //        }                
        //        if (Convert.ToDouble(pre) > 0 || Convert.ToDouble(post) > 0)
        //        {
        //            this.PenaltyCodeResult.assignDirectionalPenalty(inputStreet, featureStreet);
        //            //this.PenaltyCodeResult.assignDirectionalPenalty(geocodes[0].InputAddress.PreDirectional, geocodes[0].MatchedFeatureAddress.PreDirectional, geocodes[0].InputAddress.PostDirectional, geocodes[0].MatchedFeatureAddress.PostDirectional);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        string msg = "error getting scoreResults " + e.Message;
        //    }
        //    //getPenaltyCodeInputType(geocodes);
        //    //this.PenaltyCodeResult.inputType = this.PenaltyCodeResult.getPenaltyCodeInputType(geocodes[0].ParsedAddress.Number, geocodes[0].ParsedAddress.NumberFractional, geocodes[0].ParsedAddress.Name, geocodes[0].ParsedAddress.City, geocodes[0].ParsedAddress.ZIP);
        //    //getPenaltyCodeStreetType(geocodes);
        //    //this.PenaltyCodeResult.streetType = this.PenaltyCodeResult.getPenaltyCodeStreetType(geocodes[0].ParsedAddress.HasPostOfficeBoxNumber, geocodes[0].ParsedAddress.HasPostOfficeBox, geocodes[0].ParsedAddress.HasRuralRoute, geocodes[0].ParsedAddress.HasRuralRouteBox,
        //    //    geocodes[0].ParsedAddress.HasRuralRouteBoxNumber, geocodes[0].ParsedAddress.HasRuralRouteNumber, geocodes[0].ParsedAddress.HasHighwayContractRoute, geocodes[0].ParsedAddress.HasHighwayContractRouteBox,
        //    //    geocodes[0].ParsedAddress.HasHighwayContractRouteBoxNumber, geocodes[0].ParsedAddress.HasHighwayContractRouteNumber, geocodes[0].ParsedAddress.HasStarRoute, geocodes[0].ParsedAddress.HasStarRouteBox, geocodes[0].ParsedAddress.HasStarRouteBoxNumber, geocodes[0].ParsedAddress.HasStarRouteNumber);     
        //    //this.PenaltyCodeResult.assignStreetNamePenalty(inputStreet, featureStreet, geocodes[0].MatchType, geocodes[0].NAACCRGISCoordinateQualityCode);
        //    //this.PenaltyCodeResult.getPenalty(scoreResult);
        //    //this.PenaltyCode = this.PenaltyCodeResult.getPenaltyString();
        //}
        //public void getPenaltyCodeInputType(List<IGeocode> geocodes)
        //{
        //    bool hasNumber = (geocodes[0].ParsedAddress.Number != "" && geocodes[0].ParsedAddress.Number != null);
        //    bool hasNumberFrac = (geocodes[0].ParsedAddress.NumberFractional != "" && geocodes[0].ParsedAddress.NumberFractional != null);
        //    bool hasName = (geocodes[0].ParsedAddress.StreetName != "" && geocodes[0].ParsedAddress.StreetName != null);
        //    bool hasCity = geocodes[0].ParsedAddress.HasCity;
        //    bool hasZip = (geocodes[0].ParsedAddress.ZIP != "");


        //    if (!hasNumber && !hasNumberFrac && !hasName)
        //    {
        //        if (hasCity)
        //        {
        //            if (!hasZip)
        //            {
        //                this.PenaltyCodeResult.inputType = "3";
        //            }
        //            else
        //            {
        //                this.PenaltyCodeResult.inputType = "4";
        //            }
        //        }
        //        else
        //        {
        //            this.PenaltyCodeResult.inputType = "5";
        //        }
        //    }
        //    else if (!hasNumber && !hasNumberFrac && hasName)
        //    {
        //        this.PenaltyCodeResult.inputType = "1";
        //    }
        //    else if ((hasNumber || hasNumberFrac) && !hasName)
        //    {
        //        this.PenaltyCodeResult.inputType = "2";
        //    }
        //}

        //public void getPenaltyCodeStreetType(List<IGeocode> geocodes)
        //{
        //    if (geocodes[0].ParsedAddress.HasPostOfficeBoxNumber || geocodes[0].ParsedAddress.HasPostOfficeBox)
        //    {
        //        this.PenaltyCodeResult.streetType = "1";
        //    }
        //    else if (geocodes[0].ParsedAddress.HasRuralRoute || geocodes[0].ParsedAddress.HasRuralRouteBox || geocodes[0].ParsedAddress.HasRuralRouteBoxNumber || geocodes[0].ParsedAddress.HasRuralRouteNumber)
        //    {
        //        this.PenaltyCodeResult.streetType = "2";
        //    }
        //    else if (geocodes[0].ParsedAddress.HasHighwayContractRoute || geocodes[0].ParsedAddress.HasHighwayContractRouteBox || geocodes[0].ParsedAddress.HasHighwayContractRouteBoxNumber || geocodes[0].ParsedAddress.HasHighwayContractRouteNumber)
        //    {
        //        this.PenaltyCodeResult.streetType = "3";
        //    }
        //    else if (geocodes[0].ParsedAddress.HasStarRoute || geocodes[0].ParsedAddress.HasStarRouteBox || geocodes[0].ParsedAddress.HasStarRouteBoxNumber || geocodes[0].ParsedAddress.HasStarRouteNumber)
        //    {
        //        this.PenaltyCodeResult.streetType = "4";
        //    }
        //}

    public double getAverageDistance(string type)
        {
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            int num_points = 0;           
            //List<Point> normalPoints = new List<Point>();
            List<PointF> points = new List<PointF>();
            foreach (var resultPoint in geocodes)
            {
                //normalPoints.Add(new Point(Convert.ToInt32(resultPoint.Longitude), Convert.ToInt32(resultPoint.Latitude)));
                if (type == "parcel")
                {
                    if (resultPoint.NAACCRGISCoordinateQualityType == NAACCRGISCoordinateQualityType.AddressPoint ||
                       resultPoint.NAACCRGISCoordinateQualityType == NAACCRGISCoordinateQualityType.Parcel)
                    {
                        points.Add(new PointF(Convert.ToSingle(resultPoint.Longitude), Convert.ToSingle(resultPoint.Latitude)));
                        parcelMatches++;
                        num_points++;
                    }
                }
                else if (type == "street")
                {
                    if (resultPoint.NAACCRGISCoordinateQualityType == Metadata.Qualities.NAACCRGISCoordinateQualityType.StreetCentroid ||
                        resultPoint.NAACCRGISCoordinateQualityType == Metadata.Qualities.NAACCRGISCoordinateQualityType.StreetIntersection ||
                        resultPoint.NAACCRGISCoordinateQualityType == Metadata.Qualities.NAACCRGISCoordinateQualityType.StreetSegmentInterpolation)
                    {
                        points.Add(new PointF(Convert.ToSingle(resultPoint.Longitude), Convert.ToSingle(resultPoint.Latitude)));
                        streetMatches++;
                        num_points++;
                    }
                }
            }
            PointF[] pts = new PointF[num_points];
            points.CopyTo(pts, 0);
            //pts[num_points] = points[0];
            float area = 0;
            double distance = 0;
            double distanceAvg = 0;
            if (points.Count > 1)
            {
                for (int i = 0; i < num_points - 1; i++)
                {
                    //area +=
                    //    (pts[i + 1].X - pts[i].X) *
                    //    (pts[i + 1].Y + pts[i].Y) / 2;
                    double dX = pts[0].X - pts[i + 1].X;
                    double dY = pts[0].Y - pts[i + 1].Y;
                    double multi = dX * dX + dY * dY;
                    GeoCoordinate point1 = new GeoCoordinate(pts[0].Y, pts[0].X);
                    GeoCoordinate point2 = new GeoCoordinate(pts[i+1].Y, pts[i+1].X);
                    //distance = distance + Math.Round(Math.Sqrt(multi), 3);
                    //distance in meters
                    //distance = (Math.Round(Math.Sqrt(multi), 8)) * 10000;                    
                    distance = point1.GetDistanceTo(point2);
                }
                distanceAvg = ((distance) / (num_points - 1));
            }
            else
            {
                distanceAvg = 0;
            }
            return distanceAvg;
        }

        public bool getCensusMatchStatus()
        {
            bool censusMatches = true;
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            if (geocodes[0].CensusRecords != null)
            {
                if (geocodes[0].CensusRecords.Count > 0)
                {
                    string censusTract = geocodes[0].CensusRecords[0].CensusTract.ToString();
                    string censusBlock = geocodes[0].CensusRecords[0].CensusBlock.ToString();
                    string countyFips = geocodes[0].CensusRecords[0].CensusCountyFips.ToString();
                    foreach (var geocode in geocodes)
                    {
                        if (geocode.CensusRecords[0].CensusBlock != censusBlock)
                        {
                            censusMatches = false;
                            break;
                        }
                        else if (geocode.CensusRecords[0].CensusTract != censusTract)
                        {
                            censusMatches = false;
                            break;
                        }
                        else if (geocode.CensusRecords[0].CensusCountyFips != countyFips)
                        {
                            censusMatches = false;
                            break;
                        }
                    }
                }
                else
                {
                    censusMatches = false;
                }
            }
            else
            {
                censusMatches = false;
            }
            return censusMatches;
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
                GeoCoordinate point1 = new GeoCoordinate(pts[0].Y, pts[0].X);
                GeoCoordinate point2 = new GeoCoordinate(pts[i + 1].Y, pts[i + 1].X);
                //distance = distance + Math.Round(Math.Sqrt(multi), 3);
                //distance in meters
                //distance = (Math.Round(Math.Sqrt(multi), 8)) * 10000;                    
                distance = point1.GetDistanceTo(point2);
            }
            double distanceAvg = ((distance) / num_points);
            return distanceAvg;
        }
        public bool GetMicroMatchStatus(GeocoderConfiguration geocoderConfiguration)
        {
            bool ret = false;
            //            
           
            List<IGeocode> geocodesIn = GeocodeCollection.GetValidGeocodes();
            //List<IGeocode> geocodes = SortByConfidence(geocodesIn, geocoderConfiguration);
            List<IGeocode> geocodes = SortByConfidence(geocodesIn);
            this.PenaltyCodeResult = new PenaltyCodeResult();
            bool isValidAlias = false;
            if(geocoderConfiguration.ShouldUseAliasTable)
            {
                isValidAlias = CityUtils.isValidAlias(geocodes[0].InputAddress.City, geocodes[0].MatchedFeatureAddress.City, geocodes[0].InputAddress.State);
            }
            if (geocodes.Count > 0)
            {
                // Coordinate code should not be used here as a street segment should be a viable match as well as parcel, point etc
                //if (geocodes[0].NAACCRGISCoordinateQualityCode == "00" && geocodes[0].MatchScore > 90)                
                if (geocodes[0].MatchScore < 100)
                {
                    if (geocodes[0].MatchScore > 84)
                    {
                        if (geocodes[0].MatchedFeatureAddress.City != null && geocodes[0].MatchedFeatureAddress.ZIP != null)
                        {
                            //BUG:X7-88 We need to account for city alias here as well
                            if (geocoderConfiguration.ShouldUseAliasTable)
                            {
                                if ((geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() || isValidAlias) && geocodes[0].InputAddress.ZIP == geocodes[0].MatchedFeatureAddress.ZIP && geocodes[0].MatchScore > 95)
                                {
                                    this.MicroMatchStatus = "Match";
                                }
                                //Here we need to check against other results
                                //if city is correct but zip is not, check other results
                                else if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() || isValidAlias)
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
                                    else
                                    {
                                        if (geocodes[0].MatchedFeatureAddress.City.ToUpper() != geocodes[0].InputAddress.City.ToUpper() && !isValidAlias)
                                        {
                                            this.PenaltyCodeResult.citySummary = "R";
                                        }
                                        this.PenaltyCodeResult.distanceSummary = "R";
                                    }
                                    getDistancePenalty(avgDistance);
                                }
                                else
                                {
                                    this.MicroMatchStatus = "Review";                                   
                                    this.PenaltyCodeResult.citySummary = "R";
                                   
                                }
                            }
                            else //if not using alias table all city penalties will be applied normally
                            {
                                if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper() && geocodes[0].InputAddress.ZIP == geocodes[0].MatchedFeatureAddress.ZIP && geocodes[0].MatchScore > 95)
                                {
                                    this.MicroMatchStatus = "Match";
                                }
                                //Here we need to check against other results
                                //if city is correct but zip is not, check other results
                                else if (geocodes[0].MatchedFeatureAddress.City.ToUpper() == geocodes[0].InputAddress.City.ToUpper())
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
                                    else
                                    {
                                        if (geocodes[0].MatchedFeatureAddress.City.ToUpper() != geocodes[0].InputAddress.City.ToUpper())
                                        {
                                            this.PenaltyCodeResult.citySummary = "R";
                                        }
                                        this.PenaltyCodeResult.distanceSummary = "R";
                                    }
                                    getDistancePenalty(avgDistance);
                                }
                                else
                                {
                                    this.MicroMatchStatus = "Review";
                                    if (this.PenaltyCodeResult != null)
                                    {
                                        this.PenaltyCodeResult.citySummary = "R";
                                    }

                                }
                            }
                        }
                        else
                        {
                            this.MicroMatchStatus = "Review";
                            if(geocodes[0].MatchedFeatureAddress.City != null)
                            {
                                this.PenaltyCodeResult.zipPenaltySummary = "R";
                            }
                            else
                            {
                                this.PenaltyCodeResult.citySummary = "R";
                            }
                        }
                    }
                    else //anything not match or review is returned as non-match
                    {
                        this.MicroMatchStatus = "Non-Match";
                    }
                }
                else //if we reach here then matchscore is 100 and we return a "Match"
                {
                    //PAYTON:PENALTYCODE
                    if (geocodes[0].InputAddress.City != geocodes[0].MatchedFeatureAddress.City && isValidAlias)
                    {
                        this.PenaltyCodeResult.city = "1";
                        this.PenaltyCodeResult.citySummary = "M";
                    }
                    this.MicroMatchStatus = "Match";
                }
            }
            else //anything not match or review is returned as non-match
            {
                this.MicroMatchStatus = "Non-Match";
                if (geocodes[0].InputAddress.City != geocodes[0].MatchedFeatureAddress.City && isValidAlias)
                {
                    this.PenaltyCodeResult.city = "1";
                    this.PenaltyCodeResult.citySummary = "F";
                }
                if (geocodes[0].MatchedFeatureAddress.City == null)
                {
                    this.PenaltyCodeResult.citySummary = "F";
                }
                else if(geocodes[0].MatchedFeatureAddress.ZIP == null)
                {
                    this.PenaltyCodeResult.zipPenaltySummary = "F";
                }
            }
            return ret;
        }

        public void getDistancePenalty(double avgDistance)
        {
            if (avgDistance <= 10 && avgDistance > 0) //10m or less
            {
                this.PenaltyCodeResult.distance = "M";
                this.PenaltyCodeResult.distanceSummary = "M";
            }
            else if (avgDistance <= 100 && avgDistance > 10) //+10m-100m
            {
                this.PenaltyCodeResult.distance = "1";
                this.PenaltyCodeResult.distanceSummary = "M";
            }
            else if (avgDistance <= 500 && avgDistance > 100) //+100m-500m
            {
                this.PenaltyCodeResult.distance = "2";
                this.PenaltyCodeResult.distanceSummary = "R";
            }
            else if (avgDistance <= 1000 && avgDistance > 500) //+500m-1000m
            {
                this.PenaltyCodeResult.distance = "3";
                this.PenaltyCodeResult.distanceSummary = "R";
            }
            else if (avgDistance <= 5000 && avgDistance > 1000) //+1km-5km
            {
                this.PenaltyCodeResult.distance = "4";
                this.PenaltyCodeResult.distanceSummary = "R";
            }            
            else if (avgDistance > 5000)  //+5km
            {
                this.PenaltyCodeResult.distance = "5";
                this.PenaltyCodeResult.distanceSummary = "R";
            }
            else
            {
                this.PenaltyCodeResult.distance = "F";
                this.PenaltyCodeResult.distanceSummary = "F";
            }
        }

        public static string ToGeoJSONGeocodeCollection(List<IGeocode> o)
        {
            StringBuilder ret = new StringBuilder();
            ret.Append("{\n");
            ret.Append("\"OutputGeocodes\" :\n");
            ret.Append("[");
            try
            {
                int count = o.Count;
                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                    {
                        ret.Append("{ OutputGeocode" + i.ToString() + "\" : { ");
                        foreach (var prop in o[i].GetType().GetProperties())
                        {
                            var name = prop.Name;
                            ret.Append(name + "\" : \"");
                            var value = prop.GetValue(o[i], null);

                            if (value != null)
                            {
                                var valType = value.GetType();
                                if (valType.Name != "String" && valType.Name != "Double" && valType.Name != "Boolean" && valType.Name != "Int32" && valType.Name != "NAACCRGISCoordinateQualityType" && valType.Name != "CensusTractCertaintyType" && valType.Name != "InterpolationType" && valType.Name != "InterpolationSubType" && valType.Name != "FeatureMatchingSelectionMethod")
                                {
                                    var tempValue = value.GetType().GetProperties();
                                    var tempFields = value.GetType().GetFields();
                                    ret.Append("{ ");
                                    foreach (var val in value.GetType().GetProperties())
                                    {
                                        var tempName = val.Name;
                                        ret.Append("\"" + name + "\" : \"");
                                        var tempVal = val.GetValue(value);
                                        ret.Append(value.ToString() + ",");
                                    }
                                    ret.Append("} ");
                                }
                                else
                                {
                                    ret.Append(value.ToString() + ",");
                                }
                            }
                            else
                            {
                                ret.Append("" + "\",");
                            }
                        }
                        ret.Append("},");
                        //ret.Append(o[i]);

                    }
                }
                ret.Append("]}");
                //Type myType = o.GetType();
                //IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
                //var listparams = o.GetType().GetProperties();
                //foreach (var prop in o.GetType().GetProperties())
                //{
                //    var name = prop.Name;
                //    var value = prop.GetValue(o, null);
                //}
            }
            catch (Exception e)
            {
                ret.Append("Error: " + e.Message);
            }

            ret.Append("] }");


            return ret.ToString();
        }
        public static String WriteAsJsonStream(GeocodeResultSet webServiceGeocodeQueryResults, BatchOptions args, bool verbose)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            //Input Address Info
            sb.Append("\"InputAddress\" :");
            sb.Append("{");
            sb.Append("\"Street\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.NonParsedOriginalStreetAddress.NonParsedStreetAddress)).Append("\",");
            sb.Append("\"City\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.NonParsedOriginalStreetAddress.City)).Append("\",");
            sb.Append("\"State\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.NonParsedOriginalStreetAddress.State)).Append("\",");
            sb.Append("\"Zip\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIP)).Append("\"");
            sb.Append("},");

            //Parsed Address Info
            sb.Append("\"ParsedAddress\" :");
            sb.Append("{");
            sb.Append("\"Number\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.Number)).Append("\",");
            sb.Append("\"NumberFractional\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.NumberFractional)).Append("\",");
            sb.Append("\"PreDirectional\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PreDirectional)).Append("\",");
            sb.Append("\"PreQualifier\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PreQualifier)).Append("\",");
            sb.Append("\"PreType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PreType)).Append("\",");
            sb.Append("\"PreArticle\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PreArticle)).Append("\",");
            sb.Append("\"Name\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.StreetName)).Append("\",");

            sb.Append("\"PostArticle\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PostArticle)).Append("\",");
            sb.Append("\"PostQualifier\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PostQualifier)).Append("\",");
            sb.Append("\"Suffix\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.Suffix)).Append("\",");
            sb.Append("\"PostDirectional\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PostDirectional)).Append("\",");
            sb.Append("\"SuiteType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.SuiteType)).Append("\",");
            sb.Append("\"SuiteNumber\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.SuiteNumber)).Append("\",");
            sb.Append("\"PostOfficeBoxType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PostOfficeBoxType)).Append("\",");
            sb.Append("\"PostOfficeBoxNumber\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.PostOfficeBoxNumber)).Append("\",");
            sb.Append("\"City\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.City)).Append("\",");
            sb.Append("\"ConsolidatedCity\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ConsolidatedCity)).Append("\",");
            sb.Append("\"MinorCivilDivision\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.MinorCivilDivision)).Append("\",");
            sb.Append("\"CountySubRegion\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.CountySubregion)).Append("\",");
            sb.Append("\"County\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.County)).Append("\",");
            sb.Append("\"State\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.State)).Append("\",");
            sb.Append("\"Zip\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIP)).Append("\",");
            sb.Append("\"ZipPlus1\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIPPlus1)).Append("\",");
            sb.Append("\"ZipPlus2\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIPPlus2)).Append("\",");
            sb.Append("\"ZipPlus3\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIPPlus3)).Append("\",");
            sb.Append("\"ZipPlus4\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIPPlus4)).Append("\",");
            sb.Append("\"ZipPlus5\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResults.GeocodeCollection.Geocodes[0].InputAddress.ZIPPlus5)).Append("\"");
            sb.Append("},");

            //OutputGeocode Info
            sb.Append("\"OutputGeocodes\" :");
            sb.Append("[");

            if (webServiceGeocodeQueryResults.GeocodeCollection.Geocodes != null)
            {
                if (webServiceGeocodeQueryResults.GeocodeCollection.Geocodes.Count > 0)
                {
                    GeocodeCollection validGeocodes = webServiceGeocodeQueryResults.GeocodeCollection;
                    List<IGeocode> validGeocodes2 = validGeocodes.GetValidGeocodes();
                    //int geocodeCount = webServiceGeocodeQueryResults.GeocodeCollection.Geocodes.Count;
                    int geocodeCount = validGeocodes2.Count;
                    sb.Append("{");
                    int currentCount = 1;
                    foreach (Geocode webServiceGeocodeQueryResult in validGeocodes2)
                    {
                        //PAYTON:JSON We need to add total record limit here.. having issues when exporting the JSON when total records were greater than 11
                        if (webServiceGeocodeQueryResult.Valid && currentCount < 11)
                        {
                            sb.Append("\"OutputGeocode" + currentCount + "\" :");
                            sb.Append("{");
                            sb.Append("\"Latitude\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.Latitude)).Append("\",");
                            sb.Append("\"Longitude\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.Longitude)).Append("\",");
                            sb.Append("\"naaccrQualCode\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.NAACCRGISCoordinateQualityCode.ToString())).Append("\",");
                            sb.Append("\"naaccrQualType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.NAACCRGISCoordinateQualityType.ToString())).Append("\",");
                            sb.Append("\"MatchType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchType)).Append("\",");
                            //PAYTON:ALL RETURNS -- these values are not in the geocode collection 
                            //sb.Append("\"FeatureMatchingResultType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FeatureMatchingResultType.ToString())).Append("\",");
                            //sb.Append("\"FeatureMatchingResultCount\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FeatureMatchingResultCount.ToString())).Append("\",");
                            sb.Append("\"InterpolationType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.InterpolationType.ToString())).Append("\",");
                            sb.Append("\"InterpolationSubType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.InterpolationSubType.ToString())).Append("\",");
                            sb.Append("\"FeatureMatchingGeographyType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_GeographyType.ToString())).Append("\",");
                            sb.Append("\"MatchScore\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchScore.ToString())).Append("\",");
                            sb.Append("\"GeocodeQualityType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.GeocodeQualityType.ToString())).Append("\",");
                            sb.Append("\"RegionSize\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.RegionSize)).Append("\",");
                            sb.Append("\"RegionSizeUnits\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.RegionSizeUnits)).Append("\",");
                            sb.Append("\"MatchedLocationType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedLocationType)).Append("\",");
                            sb.Append("\"FeatureMatchingHierarchy\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_SelectionMethod.ToString())).Append("\",");
                            if (webServiceGeocodeQueryResult.FM_SelectionNotes != null)

                            {
                                sb.Append("\"FeatureMatchingHierarchyNotes\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_SelectionNotes.ToString())).Append("\",");
                            }
                            else
                            {
                                sb.Append("\"FeatureMatchingHierarchyNotes\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                            }
                            if (webServiceGeocodeQueryResult.FM_Notes != null)

                            {
                                sb.Append("\"FeatureMatchingResultTypeNotes\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_Notes.ToString())).Append("\",");
                            }
                            else
                            {
                                sb.Append("\"FeatureMatchingResultTypeNotes\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                            }
                            sb.Append("\"FeatureMatchingResultCount\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_ResultCount.ToString())).Append("\",");

                            if (webServiceGeocodeQueryResult.FM_TieNotes != null)

                            {
                                sb.Append("\"FeatureMatchingResultTypeTieBreakingNotes\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_TieNotes.ToString())).Append("\",");
                            }
                            else
                            {
                                sb.Append("\"FeatureMatchingResultTypeTieBreakingNotes\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                            }
                            sb.Append("\"TieHandlingStrategyType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.FM_TieStrategy.ToString())).Append("\",");
                            sb.Append("\"ExceptionOccured\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.ExceptionOccurred.ToString())).Append("\",");
                            if (webServiceGeocodeQueryResult.ExceptionOccurred && webServiceGeocodeQueryResult.ErrorMessage != null)
                            {
                                sb.Append("\"Exception\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.ErrorMessage.ToString())).Append("\",");
                            }
                            else
                            {
                                sb.Append("\"Exception\" : \"\",");
                            }
                            sb.Append("\"ErrorMessage\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.ErrorMessage)).Append("\"");

                            geocodeCount = geocodeCount - 1;
                            if (geocodeCount == 0)
                            {
                                if (args.ShouldOutputCensusFields || verbose)
                                {
                                    sb.Append("},");
                                }
                                else
                                {
                                    sb.Append("}");
                                }
                            }
                            else
                            {
                                sb.Append("},");
                            }

                            if (args.ShouldOutputCensusFields || verbose)
                            {
                                if (webServiceGeocodeQueryResult.CensusRecords != null)
                                {
                                    if (webServiceGeocodeQueryResult.CensusRecords.Count > 0)
                                    {
                                        sb.Append("\"CensusValues" + currentCount + "\" :");
                                        sb.Append("[");
                                        sb.Append("{");
                                        int censusCount = webServiceGeocodeQueryResult.CensusRecords.Count;
                                        foreach (CensusOutputRecord censusOutputRecord in webServiceGeocodeQueryResult.CensusRecords)
                                        {
                                            sb.Append("\"CensusValue" + censusCount + "\" :");
                                            sb.Append("{");
                                            sb.Append("\"CensusYear\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusYear.ToString())).Append("\",");
                                            sb.Append("\"CensusTimeTaken\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusTimeTaken.ToString())).Append("\",");
                                            sb.Append("\"naaccrCertCode\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.NAACCRCensusTractCertaintyCode.ToString())).Append("\",");
                                            sb.Append("\"naaccrCertType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.NAACCRCensusTractCertaintyType.ToString())).Append("\",");
                                            sb.Append("\"CensusBlock\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusBlock)).Append("\",");
                                            sb.Append("\"CensusBlockGroup\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusBlockGroup)).Append("\",");
                                            sb.Append("\"CensusTract\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusTract)).Append("\",");
                                            sb.Append("\"CensusCountyFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusCountyFips)).Append("\",");
                                            sb.Append("\"CensusStateFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusStateFips)).Append("\",");
                                            sb.Append("\"CensusCbsaFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusCbsaFips)).Append("\",");
                                            sb.Append("\"CensusCbsaMicro\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusCbsaMicro)).Append("\",");
                                            sb.Append("\"CensusMcdFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusMcdFips)).Append("\",");
                                            sb.Append("\"CensusMetDivFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusMetDivFips)).Append("\",");
                                            sb.Append("\"CensusMsaFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusMsaFips)).Append("\",");
                                            sb.Append("\"CensusPlaceFips\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.CensusPlaceFips)).Append("\",");
                                            sb.Append("\"ExceptionOccured\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.ExceptionOccurred.ToString())).Append("\",");

                                            if (censusOutputRecord.ExceptionOccurred && censusOutputRecord.ExceptionMessage != null)
                                            {
                                                sb.Append("\"Exception\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.ExceptionMessage.ToString())).Append("\",");
                                            }
                                            else
                                            {
                                                sb.Append("\"Exception\" : \"\",");
                                            }

                                            sb.Append("\"ErrorMessage\" : \"").Append(JSONUtils.CleanText(censusOutputRecord.ExceptionMessage)).Append("\"");
                                            censusCount = censusCount - 1;
                                            if (censusCount == 0)
                                            {
                                                sb.Append("}");

                                            }
                                            else
                                            {
                                                sb.Append("},");
                                            }
                                        }

                                        //}
                                        sb.Append("}");
                                        if (verbose)
                                        {
                                            sb.Append("],");
                                        }
                                        else
                                        {
                                            sb.Append("]");
                                        }
                                    }

                                }
                            }
                            if (verbose)
                            {

                                sb.Append("\"ReferenceFeature" + currentCount + "\" :");
                                sb.Append("{");
                                sb.Append("\"Name\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.StreetName)).Append("\",");
                                sb.Append("\"Number\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.Number)).Append("\",");
                                sb.Append("\"NumberFractional\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.NumberFractional)).Append("\",");
                                sb.Append("\"PreDirectional\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PreDirectional)).Append("\",");
                                sb.Append("\"PreQualifier\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PreQualifier)).Append("\",");
                                sb.Append("\"PreType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PreType)).Append("\",");
                                sb.Append("\"PreArticle\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PreArticle)).Append("\",");
                                sb.Append("\"PostArticle\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PostArticle)).Append("\",");
                                sb.Append("\"PostQualifier\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PostQualifier)).Append("\",");
                                sb.Append("\"Suffix\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.Suffix)).Append("\",");
                                sb.Append("\"PostDirectional\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PostDirectional)).Append("\",");
                                sb.Append("\"SuiteType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.SuiteType)).Append("\",");
                                sb.Append("\"SuiteNumber\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.SuiteNumber)).Append("\",");
                                sb.Append("\"PostOfficeBoxType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PostOfficeBoxType)).Append("\",");
                                sb.Append("\"PostOfficeBoxNumber\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.PostOfficeBoxNumber)).Append("\",");
                                sb.Append("\"City\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.City)).Append("\",");
                                sb.Append("\"ConsolidatedCity\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ConsolidatedCity)).Append("\",");
                                sb.Append("\"MinorCivilDivision\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.MinorCivilDivision)).Append("\",");
                                sb.Append("\"CountySubRegion\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.CountySubregion)).Append("\",");
                                sb.Append("\"County\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.County)).Append("\",");
                                sb.Append("\"State\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.State)).Append("\",");
                                sb.Append("\"Zip\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ZIP)).Append("\",");
                                sb.Append("\"ZipPlus1\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ZIPPlus1)).Append("\",");
                                sb.Append("\"ZipPlus2\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ZIPPlus2)).Append("\",");
                                sb.Append("\"ZipPlus3\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ZIPPlus3)).Append("\",");
                                sb.Append("\"ZipPlus4\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ZIPPlus4)).Append("\",");
                                sb.Append("\"ZipPlus5\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.ZIPPlus5)).Append("\",");
                                sb.Append("\"Area\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.MatchedReferenceFeature.StreetAddressableGeographicFeature.Geometry.Area.ToString())).Append("\",");
                                sb.Append("\"AreaType\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.MatchedReferenceFeature.StreetAddressableGeographicFeature.Geometry.AreaUnits.ToString())).Append("\",");
                                //PAYTON:JSON Geometry was simply too large to include as a string element within the JSON
                                //if (webServiceGeocodeQueryResult.MatchedFeature.MatchedReferenceFeature.StreetAddressableGeographicFeature.Geometry.SqlGeometry != null)
                                //{
                                //    sb.Append("\"Geometry\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.MatchedReferenceFeature.StreetAddressableGeographicFeature.Geometry.SqlGeometry.AsGml().Value.ToString())).Append("\",");
                                //}
                                //else
                                //{
                                sb.Append("\"Geometry\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                                //}          
                                if (webServiceGeocodeQueryResult.MatchedFeature.PrimaryIdField != null)
                                {
                                    sb.Append("\"PrimaryIdField\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.PrimaryIdField.ToString())).Append("\",");
                                }
                                else
                                {
                                    sb.Append("\"PrimaryIdField\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                                }

                                if (webServiceGeocodeQueryResult.MatchedFeature.PrimaryIdValue != null)
                                {
                                    sb.Append("\"PrimaryIdValue\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.PrimaryIdValue.ToString())).Append("\",");
                                }
                                else
                                {
                                    sb.Append("\"PrimaryIdValue\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                                }

                                if (webServiceGeocodeQueryResult.MatchedFeature.SecondaryIdField != null)
                                {
                                    sb.Append("\"SecondaryIdField\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.SecondaryIdField.ToString())).Append("\",");
                                }
                                else
                                {
                                    sb.Append("\"SecondaryIdField\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                                }

                                if (webServiceGeocodeQueryResult.MatchedFeature.SecondaryIdValue != null)
                                {
                                    sb.Append("\"SecondaryIdValue\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeature.SecondaryIdValue.ToString())).Append("\",");
                                }
                                else
                                {
                                    sb.Append("\"SecondaryIdValue\" : \"").Append(JSONUtils.CleanText("")).Append("\",");
                                }
                                sb.Append("\"Vintage\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.SourceVintage)).Append("\",");
                                sb.Append("\"Source\" : \"").Append(JSONUtils.CleanText(webServiceGeocodeQueryResult.MatchedFeatureAddress.Source)).Append("\"");

                            }
                            if (geocodeCount == 0 || currentCount == 10)
                            {
                                sb.Append("}");
                            }
                            else
                            {
                                sb.Append("},");
                            }
                            currentCount++;
                            //geocodeCount = geocodeCount - 1;
                        }
                        else
                        {
                            geocodeCount = geocodeCount - 1;
                        }
                        //currentCount++;
                    }
                    //}

                }

            }
            //}
            //sb.Append("}");
            //sb.Append("]");
            //sb.Length = sb.Length - 2;
            //sb.Append("}");
            sb.Append("}]}");
            return sb.ToString();
        }
    }
}
